using System.Diagnostics;
using System.Text;
using PhoneMirror.Core.Models;
using PhoneMirror.Core.Platform;

namespace PhoneMirror.Core.Services;

/// <summary>
/// Provides screen mirroring functionality using scrcpy with cross-platform support.
/// </summary>
public sealed class ScrcpyService : IScrcpyService
{
    private readonly IPlatformService _platformService;
    private readonly IResourceExtractor? _resourceExtractor;
    private readonly SemaphoreSlim _resolveLock = new(1, 1);
    private string? _scrcpyPath;
    private bool _pathResolved;
    private Process? _currentProcess;

    /// <inheritdoc />
    public string? ScrcpyPath => _scrcpyPath;

    /// <inheritdoc />
    public bool IsMirroring => _currentProcess != null && !_currentProcess.HasExited;

    /// <inheritdoc />
    public event EventHandler<string>? MirroringStopped;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScrcpyService"/> class.
    /// </summary>
    /// <param name="platformService">The platform service for cross-platform operations.</param>
    /// <param name="resourceExtractor">Optional resource extractor for embedded scrcpy.</param>
    public ScrcpyService(IPlatformService platformService, IResourceExtractor? resourceExtractor = null)
    {
        _platformService = platformService;
        _resourceExtractor = resourceExtractor;
    }

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync()
    {
        var path = await ResolveScrcpyPathAsync().ConfigureAwait(false);
        return !string.IsNullOrEmpty(path) && File.Exists(path);
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? Error)> StartMirroringAsync(
        string deviceSerial,
        ScrcpyPreset preset,
        bool keepScreenAwake,
        bool fullscreen,
        CancellationToken cancellationToken = default)
    {
        // Clean up any existing session
        StopMirroring();

        // Resolve scrcpy path (waits for extraction if needed)
        var scrcpyPath = await ResolveScrcpyPathAsync().ConfigureAwait(false);

        // Check availability
        if (string.IsNullOrEmpty(scrcpyPath) || !File.Exists(scrcpyPath))
        {
            var executableName = "scrcpy" + _platformService.ExecutableExtension;
            return (false, $"scrcpy not found. Please ensure {executableName} is available in the application directory or in PATH.");
        }

        var args = BuildArguments(deviceSerial, preset, keepScreenAwake, fullscreen);

        var psi = new ProcessStartInfo
        {
            FileName = scrcpyPath,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            // WorkingDirectory must be set to the scrcpy directory for DLL dependencies
            WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(scrcpyPath)) ?? AppContext.BaseDirectory
        };

        try
        {
            var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

            // Capture streams asynchronously to avoid deadlocks
            StringBuilder stdoutBuilder = new();
            StringBuilder stderrBuilder = new();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    lock (stdoutBuilder)
                    {
                        stdoutBuilder.AppendLine(e.Data);
                    }
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    lock (stderrBuilder)
                    {
                        stderrBuilder.AppendLine(e.Data);
                    }
                }
            };

            process.Exited += (_, _) =>
            {
                string stdout;
                string stderr;
                lock (stdoutBuilder)
                {
                    stdout = stdoutBuilder.ToString().Trim();
                }
                lock (stderrBuilder)
                {
                    stderr = stderrBuilder.ToString().Trim();
                }

                var messageBuilder = new StringBuilder();
                messageBuilder.Append(process.ExitCode == 0
                    ? "scrcpy exited."
                    : $"scrcpy failed with code {process.ExitCode}.");

                if (!string.IsNullOrWhiteSpace(stderr))
                {
                    messageBuilder.Append(" stderr: ").Append(stderr);
                }
                else if (!string.IsNullOrWhiteSpace(stdout))
                {
                    messageBuilder.Append(" stdout: ").Append(stdout);
                }

                var message = messageBuilder.ToString();

                // Cleanup process reference
                try
                {
                    process.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }

                _currentProcess = null;

                // Raise event on a captured message to avoid closure issues
                MirroringStopped?.Invoke(this, message);
            };

            var started = process.Start();
            if (!started)
            {
                process.Dispose();
                return (false, "Failed to start scrcpy process.");
            }

            // Begin async reading of output streams
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            _currentProcess = process;
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Failed to start scrcpy: {ex.Message}\nPath: {scrcpyPath}\nArgs: {args}");
        }
    }

    /// <inheritdoc />
    public void StopMirroring()
    {
        if (_currentProcess == null)
        {
            return;
        }

        try
        {
            if (!_currentProcess.HasExited)
            {
                _currentProcess.Kill();
            }
        }
        catch
        {
            // Ignore kill failures; process may already be gone
        }
        finally
        {
            try
            {
                _currentProcess.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
            _currentProcess = null;
        }
    }

    /// <summary>
    /// Resolves the scrcpy path asynchronously, waiting for resource extraction if needed.
    /// </summary>
    private async Task<string?> ResolveScrcpyPathAsync()
    {
        if (_pathResolved)
        {
            return _scrcpyPath;
        }

        await _resolveLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_pathResolved)
            {
                return _scrcpyPath;
            }

            string? extractedPath = null;
            if (_resourceExtractor != null)
            {
                extractedPath = await _resourceExtractor.GetScrcpyPathAsync().ConfigureAwait(false);
            }

            _scrcpyPath = ResolveScrcpyPathSync(extractedPath);
            _pathResolved = true;
            return _scrcpyPath;
        }
        finally
        {
            _resolveLock.Release();
        }
    }

    /// <summary>
    /// Resolves the path to the scrcpy executable.
    /// </summary>
    /// <param name="extractedPath">Path from resource extractor, if available.</param>
    /// <returns>The resolved path, or null if not found.</returns>
    private string? ResolveScrcpyPathSync(string? extractedPath)
    {
        var executableName = "scrcpy" + _platformService.ExecutableExtension;
        var candidates = new List<string>();

        // 1. Check embedded resources first (highest priority)
        if (!string.IsNullOrEmpty(extractedPath) && File.Exists(extractedPath))
        {
            return extractedPath;
        }

        var baseDir = AppContext.BaseDirectory;

        // 2. Check bundled locations (relative to application)
        candidates.Add(Path.Combine(baseDir, "scrcpy", executableName));
        candidates.Add(Path.Combine(baseDir, executableName));

        // 3. Check application data directory (extracted resources location)
        var appDataPath = _platformService.GetAppDataPath();
        candidates.Add(Path.Combine(appDataPath, "scrcpy", executableName));

        // 4. Check parent directories (for development scenarios like bin\Debug\net8.0)
        var currentDir = Directory.GetParent(baseDir);
        for (int i = 0; i < 8 && currentDir != null; i++)
        {
            candidates.Add(Path.Combine(currentDir.FullName, "src", "PhoneMirror.Core", "Resources", "scrcpy", executableName));
            candidates.Add(Path.Combine(currentDir.FullName, "scrcpy", executableName));
            currentDir = currentDir.Parent;
        }

        // 5. Check PATH directories
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var separator = _platformService.CurrentPlatform == System.Runtime.InteropServices.OSPlatform.Windows
            ? ';'
            : ':';
        var pathParts = pathEnv.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var rawDir in pathParts)
        {
            var dir = rawDir.Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(dir)) continue;
            candidates.Add(Path.Combine(dir, executableName));
        }

        // Return the first existing candidate
        foreach (var candidate in candidates)
        {
            try
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch
            {
                // Ignore path access errors
            }
        }

        return null;
    }

    /// <summary>
    /// Builds the scrcpy command-line arguments for the given options.
    /// </summary>
    private static string BuildArguments(string serial, ScrcpyPreset preset, bool keepScreenAwake, bool fullscreen)
    {
        var builder = new StringBuilder();

        // Device serial (quoted for safety)
        builder.Append("-s \"").Append(serial).Append('"');

        // Quality preset parameters
        switch (preset)
        {
            case ScrcpyPreset.Low:
                builder.Append(" --video-bit-rate 4M --max-size 1024 --max-fps 30");
                break;
            case ScrcpyPreset.Balanced:
                builder.Append(" --video-bit-rate 8M --max-size 1280 --max-fps 60");
                break;
            case ScrcpyPreset.High:
                builder.Append(" --video-bit-rate 16M --max-size 1920 --max-fps 60");
                break;
        }

        // Keep screen awake option
        if (keepScreenAwake)
        {
            builder.Append(" --stay-awake");
        }

        // Fullscreen option
        if (fullscreen)
        {
            builder.Append(" --fullscreen");
        }

        // Turn off device screen during mirroring (requires control enabled, which is default)
        builder.Append(" --turn-screen-off");

        return builder.ToString();
    }
}
