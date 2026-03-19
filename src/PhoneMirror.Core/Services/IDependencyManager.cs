namespace PhoneMirror.Core.Services;

/// <summary>
/// Progress information for dependency downloads.
/// </summary>
public record DependencyProgress(
    string Status,
    string CurrentItem,
    double PercentComplete,
    bool IsComplete,
    bool HasError,
    string? ErrorMessage = null);

/// <summary>
/// Manages automatic download and setup of required dependencies (ADB and scrcpy).
/// Downloads to persistent AppData directory so they survive app restarts.
/// </summary>
public interface IDependencyManager
{
    /// <summary>
    /// Checks whether all required dependencies are available.
    /// </summary>
    Task<bool> AreDependenciesAvailableAsync();

    /// <summary>
    /// Checks whether ADB is available (either downloaded or on PATH).
    /// </summary>
    Task<bool> IsAdbAvailableAsync();

    /// <summary>
    /// Checks whether scrcpy is available (either downloaded or on PATH).
    /// </summary>
    Task<bool> IsScrcpyAvailableAsync();

    /// <summary>
    /// Ensures all dependencies are downloaded and available.
    /// Downloads any missing dependencies automatically.
    /// </summary>
    Task EnsureDependenciesAsync(IProgress<DependencyProgress>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the path to the downloaded ADB executable, or null if not downloaded.
    /// </summary>
    string? GetAdbPath();

    /// <summary>
    /// Gets the path to the downloaded scrcpy executable, or null if not downloaded.
    /// </summary>
    string? GetScrcpyPath();
}
