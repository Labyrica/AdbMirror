using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PhoneMirror.Core.Platform;

/// <summary>
/// Cross-platform service for OS-specific operations.
/// Handles platform detection, path resolution, and Unix permission management.
/// </summary>
public sealed class PlatformService : IPlatformService
{
    private readonly Lazy<OSPlatform> _currentPlatform;
    private readonly Lazy<string> _appDataPath;

    public PlatformService()
    {
        _currentPlatform = new Lazy<OSPlatform>(DetectPlatform);
        _appDataPath = new Lazy<string>(ResolveAppDataPath);
    }

    /// <inheritdoc />
    public OSPlatform CurrentPlatform => _currentPlatform.Value;

    /// <inheritdoc />
    public string ExecutableExtension =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty;

    /// <inheritdoc />
    public string GetAppDataPath() => _appDataPath.Value;

    /// <inheritdoc />
    public string GetTempPath() => Path.GetTempPath();

    /// <inheritdoc />
    public async Task SetExecutablePermissionAsync(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // No-op on Windows - executables don't need permission changes
            return;
        }

        // Linux and macOS: Use chmod +x
        var startInfo = new ProcessStartInfo
        {
            FileName = "chmod",
            Arguments = $"+x \"{path}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        // Read streams asynchronously to prevent deadlock
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await Task.WhenAll(outputTask, errorTask);
        await process.WaitForExitAsync();

        // Don't throw on failure - some systems may not support chmod
        // The actual execution failure will be more informative
    }

    /// <inheritdoc />
    public async Task RemoveQuarantineAsync(string path)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // No-op on non-macOS systems
            return;
        }

        // macOS: Remove quarantine extended attribute
        var startInfo = new ProcessStartInfo
        {
            FileName = "xattr",
            Arguments = $"-d com.apple.quarantine \"{path}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        // Read streams asynchronously to prevent deadlock
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await Task.WhenAll(outputTask, errorTask);
        await process.WaitForExitAsync();

        // Don't throw on failure - the attribute may not exist
        // or the file may not have been quarantined
    }

    private static OSPlatform DetectPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return OSPlatform.Windows;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return OSPlatform.Linux;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return OSPlatform.OSX;

        // Fallback - shouldn't happen on supported platforms
        return OSPlatform.Windows;
    }

    private string ResolveAppDataPath()
    {
        string basePath;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows: %LOCALAPPDATA%\PhoneMirror
            basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS: ~/Library/Application Support/PhoneMirror
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            basePath = Path.Combine(home, "Library", "Application Support");
        }
        else
        {
            // Linux: ~/.config/PhoneMirror
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var xdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            basePath = !string.IsNullOrEmpty(xdgConfig)
                ? xdgConfig
                : Path.Combine(home, ".config");
        }

        return Path.Combine(basePath, "PhoneMirror");
    }
}
