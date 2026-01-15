using PhoneMirror.Core.Models;

namespace PhoneMirror.Core.Services;

/// <summary>
/// Service for interacting with Android Debug Bridge (ADB).
/// Provides device discovery, state management, and server control.
/// </summary>
public interface IAdbService
{
    /// <summary>
    /// Gets the resolved path to the ADB executable.
    /// Returns null if ADB is not available.
    /// </summary>
    string? AdbPath { get; }

    /// <summary>
    /// Checks if ADB is available and can be executed.
    /// </summary>
    /// <returns>True if ADB is available and responds to version command.</returns>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Ensures the ADB server is running.
    /// Starts the server if necessary.
    /// </summary>
    Task EnsureServerRunningAsync();

    /// <summary>
    /// Gets the list of connected Android devices.
    /// </summary>
    /// <returns>A read-only list of connected devices.</returns>
    Task<IReadOnlyList<AndroidDevice>> GetDevicesAsync();

    /// <summary>
    /// Computes a high-level device state appropriate for driving the UI.
    /// </summary>
    /// <returns>A tuple of the device state and the primary device (if any).</returns>
    Task<(DeviceState State, AndroidDevice? Device)> GetHighLevelStateAsync();

    /// <summary>
    /// Starts a background polling loop that reports device state changes.
    /// </summary>
    /// <param name="interval">The polling interval.</param>
    /// <param name="observer">Callback invoked on each poll with current state.</param>
    /// <param name="cancellationToken">Token to stop polling.</param>
    Task StartPollingAsync(
        TimeSpan interval,
        Action<DeviceState, AndroidDevice?> observer,
        CancellationToken cancellationToken);
}
