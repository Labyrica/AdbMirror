using System.Runtime.InteropServices;

namespace PhoneMirror.Core.Platform;

/// <summary>
/// Provides cross-platform abstractions for OS-specific operations.
/// </summary>
public interface IPlatformService
{
    /// <summary>
    /// Gets the current operating system platform.
    /// </summary>
    OSPlatform CurrentPlatform { get; }

    /// <summary>
    /// Gets the executable file extension for the current platform.
    /// Returns ".exe" on Windows, empty string on Unix-like systems.
    /// </summary>
    string ExecutableExtension { get; }

    /// <summary>
    /// Gets the platform-appropriate application data directory.
    /// </summary>
    /// <returns>
    /// Windows: %LOCALAPPDATA%\PhoneMirror
    /// Linux: ~/.config/PhoneMirror
    /// macOS: ~/Library/Application Support/PhoneMirror
    /// </returns>
    string GetAppDataPath();

    /// <summary>
    /// Gets the platform-appropriate temporary directory.
    /// </summary>
    /// <returns>The platform-specific temp directory path.</returns>
    string GetTempPath();

    /// <summary>
    /// Sets executable permission on a file (chmod +x on Unix, no-op on Windows).
    /// </summary>
    /// <param name="path">The absolute path to the file.</param>
    Task SetExecutablePermissionAsync(string path);

    /// <summary>
    /// Removes the quarantine attribute from a file (macOS only, no-op elsewhere).
    /// This is necessary for executables extracted from downloaded archives on macOS.
    /// </summary>
    /// <param name="path">The absolute path to the file.</param>
    Task RemoveQuarantineAsync(string path);
}
