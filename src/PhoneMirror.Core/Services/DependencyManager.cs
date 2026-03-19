using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using PhoneMirror.Core.Platform;

namespace PhoneMirror.Core.Services;

/// <summary>
/// Automatically downloads and manages ADB and scrcpy dependencies.
/// Stores downloaded tools in AppData for persistence across sessions.
/// </summary>
public sealed class DependencyManager : IDependencyManager, IDisposable
{
    private readonly IPlatformService _platformService;
    private readonly HttpClient _httpClient;
    private readonly string _toolsDir;
    private bool _disposed;

    // Official download URLs
    private const string AdbWindowsUrl = "https://dl.google.com/android/repository/platform-tools-latest-windows.zip";
    private const string AdbLinuxUrl = "https://dl.google.com/android/repository/platform-tools-latest-linux.zip";
    private const string AdbMacUrl = "https://dl.google.com/android/repository/platform-tools-latest-darwin.zip";

    private const string ScrcpyReleasesApi = "https://api.github.com/repos/Genymobile/scrcpy/releases/latest";

    public DependencyManager(IPlatformService platformService)
    {
        _platformService = platformService ?? throw new ArgumentNullException(nameof(platformService));
        _toolsDir = Path.Combine(_platformService.GetAppDataPath(), "tools");

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("PhoneMirror/1.0");
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
    }

    /// <inheritdoc />
    public Task<bool> AreDependenciesAvailableAsync()
    {
        return Task.FromResult(FindAdbPath() != null && FindScrcpyPath() != null);
    }

    /// <inheritdoc />
    public Task<bool> IsAdbAvailableAsync()
    {
        return Task.FromResult(FindAdbPath() != null);
    }

    /// <inheritdoc />
    public Task<bool> IsScrcpyAvailableAsync()
    {
        return Task.FromResult(FindScrcpyPath() != null);
    }

    /// <inheritdoc />
    public string? GetAdbPath() => FindAdbPath();

    /// <inheritdoc />
    public string? GetScrcpyPath() => FindScrcpyPath();

    /// <inheritdoc />
    public async Task EnsureDependenciesAsync(IProgress<DependencyProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(_toolsDir);

            var needAdb = FindAdbPath() == null;
            var needScrcpy = FindScrcpyPath() == null;

            if (!needAdb && !needScrcpy)
            {
                progress?.Report(new DependencyProgress("All dependencies ready", "", 100, true, false));
                return;
            }

            var totalSteps = (needAdb ? 1 : 0) + (needScrcpy ? 1 : 0);
            var currentStep = 0;

            if (needAdb)
            {
                progress?.Report(new DependencyProgress(
                    "Downloading ADB...", "platform-tools",
                    (double)currentStep / totalSteps * 100, false, false));

                await DownloadAdbAsync(progress, totalSteps, currentStep, cancellationToken).ConfigureAwait(false);
                currentStep++;
            }

            if (needScrcpy)
            {
                progress?.Report(new DependencyProgress(
                    "Downloading scrcpy...", "scrcpy",
                    (double)currentStep / totalSteps * 100, false, false));

                await DownloadScrcpyAsync(progress, totalSteps, currentStep, cancellationToken).ConfigureAwait(false);
                currentStep++;
            }

            progress?.Report(new DependencyProgress("Setup complete", "", 100, true, false));
        }
        catch (OperationCanceledException)
        {
            progress?.Report(new DependencyProgress("Setup cancelled", "", 0, true, true, "Download was cancelled"));
            throw;
        }
        catch (Exception ex)
        {
            progress?.Report(new DependencyProgress(
                "Setup failed", "", 0, true, true,
                $"Failed to download dependencies: {ex.Message}"));
            throw;
        }
    }

    private async Task DownloadAdbAsync(IProgress<DependencyProgress>? progress, int totalSteps, int currentStep, CancellationToken ct)
    {
        var url = GetAdbDownloadUrl();
        var targetDir = Path.Combine(_toolsDir, "platform-tools");

        await DownloadAndExtractAsync(url, targetDir, "ADB", progress, totalSteps, currentStep, ct).ConfigureAwait(false);

        // Set permissions on Unix
        var adbPath = FindAdbInDirectory(targetDir);
        if (adbPath != null)
        {
            await _platformService.SetExecutablePermissionAsync(adbPath).ConfigureAwait(false);
        }
    }

    private async Task DownloadScrcpyAsync(IProgress<DependencyProgress>? progress, int totalSteps, int currentStep, CancellationToken ct)
    {
        var url = await GetScrcpyDownloadUrlAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrEmpty(url))
        {
            throw new InvalidOperationException("Could not determine scrcpy download URL for this platform");
        }

        var targetDir = Path.Combine(_toolsDir, "scrcpy");

        await DownloadAndExtractAsync(url, targetDir, "scrcpy", progress, totalSteps, currentStep, ct).ConfigureAwait(false);

        // Set permissions on Unix
        var scrcpyPath = FindScrcpyInDirectory(targetDir);
        if (scrcpyPath != null)
        {
            await _platformService.SetExecutablePermissionAsync(scrcpyPath).ConfigureAwait(false);
        }
    }

    private async Task DownloadAndExtractAsync(
        string url, string targetDir, string name,
        IProgress<DependencyProgress>? progress, int totalSteps, int currentStep,
        CancellationToken ct)
    {
        var tempZip = Path.Combine(_toolsDir, $"{name.ToLowerInvariant()}_download.zip");

        try
        {
            // Download with progress
            using (var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                long downloadedBytes = 0;

                await using var contentStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                await using var fileStream = new FileStream(tempZip, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct).ConfigureAwait(false);
                    downloadedBytes += bytesRead;

                    if (totalBytes > 0)
                    {
                        var downloadPercent = (double)downloadedBytes / totalBytes * 100;
                        var stepProgress = (currentStep + downloadPercent / 100.0) / totalSteps * 100;
                        progress?.Report(new DependencyProgress(
                            $"Downloading {name}... {downloadedBytes / 1024 / 1024}MB / {totalBytes / 1024 / 1024}MB",
                            name, stepProgress, false, false));
                    }
                }
            }

            // Extract
            progress?.Report(new DependencyProgress(
                $"Extracting {name}...", name,
                (currentStep + 0.9) / totalSteps * 100, false, false));

            // Clean target directory if exists
            if (Directory.Exists(targetDir))
            {
                Directory.Delete(targetDir, true);
            }

            // Extract zip - handle nested directory structure
            var tempExtractDir = Path.Combine(_toolsDir, $"{name.ToLowerInvariant()}_extract_temp");
            if (Directory.Exists(tempExtractDir))
            {
                Directory.Delete(tempExtractDir, true);
            }

            ZipFile.ExtractToDirectory(tempZip, tempExtractDir);

            // Check if the zip has a single root directory (common pattern)
            var extractedDirs = Directory.GetDirectories(tempExtractDir);
            var extractedFiles = Directory.GetFiles(tempExtractDir);

            if (extractedDirs.Length == 1 && extractedFiles.Length == 0)
            {
                // Single root directory - move it as the target
                Directory.Move(extractedDirs[0], targetDir);
            }
            else
            {
                // Multiple items at root - move entire temp dir as target
                Directory.Move(tempExtractDir, targetDir);
            }

            // Clean up temp extract dir if it still exists
            if (Directory.Exists(tempExtractDir))
            {
                try { Directory.Delete(tempExtractDir, true); } catch { }
            }
        }
        finally
        {
            // Clean up temp zip
            if (File.Exists(tempZip))
            {
                try { File.Delete(tempZip); } catch { }
            }
        }
    }

    private string GetAdbDownloadUrl()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return AdbWindowsUrl;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return AdbLinuxUrl;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return AdbMacUrl;
        return AdbWindowsUrl;
    }

    private async Task<string?> GetScrcpyDownloadUrlAsync(CancellationToken ct)
    {
        try
        {
            var json = await _httpClient.GetStringAsync(ScrcpyReleasesApi, ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);

            var assets = doc.RootElement.GetProperty("assets");
            var pattern = GetScrcpyAssetPattern();

            foreach (var asset in assets.EnumerateArray())
            {
                var assetName = asset.GetProperty("name").GetString() ?? "";
                if (MatchesScrcpyPattern(assetName, pattern))
                {
                    return asset.GetProperty("browser_download_url").GetString();
                }
            }

            return null;
        }
        catch
        {
            // Fallback: try a known recent version
            return GetScrcpyFallbackUrl();
        }
    }

    private string GetScrcpyAssetPattern()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return RuntimeInformation.OSArchitecture == Architecture.X64 ? "win64" : "win32";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return "linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "macos";
        return "win64";
    }

    private static bool MatchesScrcpyPattern(string assetName, string pattern)
    {
        var name = assetName.ToLowerInvariant();
        return name.Contains(pattern) && name.EndsWith(".zip") && !name.Contains("sha256");
    }

    private string? GetScrcpyFallbackUrl()
    {
        // Fallback to known version if GitHub API fails
        const string version = "3.1";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return $"https://github.com/Genymobile/scrcpy/releases/download/v{version}/scrcpy-win64-v{version}.zip";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return $"https://github.com/Genymobile/scrcpy/releases/download/v{version}/scrcpy-linux-x86_64-v{version}.tar.gz";
        return null;
    }

    private string? FindAdbPath()
    {
        var exe = "adb" + _platformService.ExecutableExtension;

        // Check downloaded location
        var downloadedDir = Path.Combine(_toolsDir, "platform-tools");
        var path = FindAdbInDirectory(downloadedDir);
        if (path != null) return path;

        // Check AppData root
        path = Path.Combine(_platformService.GetAppDataPath(), "platform-tools", exe);
        if (File.Exists(path)) return path;

        // Check PATH and Android SDK
        return FindOnPath(exe) ?? FindInAndroidSdk(exe);
    }

    private string? FindAdbInDirectory(string dir)
    {
        if (!Directory.Exists(dir)) return null;

        var exe = "adb" + _platformService.ExecutableExtension;

        // Direct
        var path = Path.Combine(dir, exe);
        if (File.Exists(path)) return path;

        // May be nested in platform-tools/ subfolder from zip
        path = Path.Combine(dir, "platform-tools", exe);
        if (File.Exists(path)) return path;

        return null;
    }

    private string? FindScrcpyPath()
    {
        var exe = "scrcpy" + _platformService.ExecutableExtension;

        // Check downloaded location
        var downloadedDir = Path.Combine(_toolsDir, "scrcpy");
        var path = FindScrcpyInDirectory(downloadedDir);
        if (path != null) return path;

        // Check AppData root
        path = Path.Combine(_platformService.GetAppDataPath(), "scrcpy", exe);
        if (File.Exists(path)) return path;

        // Check PATH
        return FindOnPath(exe);
    }

    private string? FindScrcpyInDirectory(string dir)
    {
        if (!Directory.Exists(dir)) return null;

        var exe = "scrcpy" + _platformService.ExecutableExtension;

        // Direct
        var path = Path.Combine(dir, exe);
        if (File.Exists(path)) return path;

        // Search one level deep (zip may have a version-named subfolder)
        try
        {
            foreach (var subDir in Directory.GetDirectories(dir))
            {
                path = Path.Combine(subDir, exe);
                if (File.Exists(path)) return path;
            }
        }
        catch { }

        return null;
    }

    private string? FindOnPath(string executableName)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        var separator = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':';

        foreach (var rawDir in pathEnv.Split(separator, StringSplitOptions.RemoveEmptyEntries))
        {
            var dir = rawDir.Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(dir)) continue;

            try
            {
                var path = Path.Combine(dir, executableName);
                if (File.Exists(path)) return path;
            }
            catch { }
        }

        return null;
    }

    private string? FindInAndroidSdk(string adbExecutable)
    {
        var sdkHome = Environment.GetEnvironmentVariable("ANDROID_HOME")
                      ?? Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");

        if (!string.IsNullOrWhiteSpace(sdkHome))
        {
            var path = Path.Combine(sdkHome, "platform-tools", adbExecutable);
            if (File.Exists(path)) return path;
        }

        string? userPath = null;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrWhiteSpace(localAppData))
                userPath = Path.Combine(localAppData, "Android", "Sdk", "platform-tools", adbExecutable);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(home))
                userPath = Path.Combine(home, "Library", "Android", "sdk", "platform-tools", adbExecutable);
        }
        else
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(home))
                userPath = Path.Combine(home, "Android", "Sdk", "platform-tools", adbExecutable);
        }

        return userPath != null && File.Exists(userPath) ? userPath : null;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient.Dispose();
            _disposed = true;
        }
    }
}
