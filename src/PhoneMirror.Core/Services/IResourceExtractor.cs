namespace PhoneMirror.Core.Services;

/// <summary>
/// Extracts bundled resources (ADB and scrcpy) for cross-platform use.
/// Handles platform-specific extraction requirements like executable permissions.
/// </summary>
public interface IResourceExtractor
{
    /// <summary>
    /// Gets the path to extracted ADB executable, or null if not bundled.
    /// </summary>
    Task<string?> GetAdbPathAsync();

    /// <summary>
    /// Gets the path to extracted scrcpy executable, or null if not bundled.
    /// </summary>
    Task<string?> GetScrcpyPathAsync();

    /// <summary>
    /// Extracts all bundled resources. Call on app startup.
    /// </summary>
    Task ExtractAllAsync();

    /// <summary>
    /// Cleans up extracted resources. Call on app exit.
    /// </summary>
    void Cleanup();
}
