using PhoneMirror.Core.Models;

namespace PhoneMirror.Core.Services;

/// <summary>
/// Provides screen mirroring functionality using scrcpy.
/// </summary>
public interface IScrcpyService
{
    /// <summary>
    /// Gets the resolved path to the scrcpy executable, or null if not found.
    /// </summary>
    string? ScrcpyPath { get; }

    /// <summary>
    /// Gets whether a mirroring session is currently active.
    /// </summary>
    bool IsMirroring { get; }

    /// <summary>
    /// Checks if scrcpy is available for execution.
    /// </summary>
    /// <returns>True if scrcpy can be executed; otherwise, false.</returns>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Starts a mirroring session for the specified device.
    /// </summary>
    /// <param name="deviceSerial">The serial number of the device to mirror.</param>
    /// <param name="preset">The quality preset to use.</param>
    /// <param name="keepScreenAwake">Whether to keep the device screen awake during mirroring.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A tuple indicating success and an optional error message.</returns>
    Task<(bool Success, string? Error)> StartMirroringAsync(
        string deviceSerial,
        ScrcpyPreset preset,
        bool keepScreenAwake,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the current mirroring session.
    /// </summary>
    void StopMirroring();

    /// <summary>
    /// Raised when a mirroring session stops (either normally or due to an error).
    /// The event argument contains a status message with exit details.
    /// </summary>
    event EventHandler<string>? MirroringStopped;
}
