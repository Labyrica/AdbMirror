namespace PhoneMirror.Core.Services;

/// <summary>
/// Represents a single logcat entry from the device.
/// </summary>
/// <param name="Timestamp">The timestamp when the entry was captured.</param>
/// <param name="Level">The log level (V, D, I, W, E, F).</param>
/// <param name="Tag">The tag/source of the log entry.</param>
/// <param name="Message">The log message content.</param>
public record LogcatEntry(DateTime Timestamp, string Level, string Tag, string Message);

/// <summary>
/// Service for capturing logcat output during mirroring sessions.
/// Captures only error-level entries (*:E filter) for diagnostic purposes.
/// </summary>
public interface ILogcatService
{
    /// <summary>
    /// Whether logcat capture is currently running.
    /// </summary>
    bool IsCapturing { get; }

    /// <summary>
    /// Starts capturing logcat errors for the specified device.
    /// Only captures errors (*:E filter).
    /// </summary>
    /// <param name="deviceSerial">The serial number of the device to capture logs from.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    Task StartCaptureAsync(string deviceSerial, CancellationToken ct = default);

    /// <summary>
    /// Stops the current capture session.
    /// </summary>
    void StopCapture();

    /// <summary>
    /// Gets recent error entries (last 10 seconds).
    /// Returns empty collection if not capturing.
    /// </summary>
    /// <returns>A read-only list of recent log entries.</returns>
    IReadOnlyList<LogcatEntry> GetRecentErrors();

    /// <summary>
    /// Gets all captured errors from the current session.
    /// </summary>
    /// <returns>A read-only list of all captured log entries.</returns>
    IReadOnlyList<LogcatEntry> GetAllSessionErrors();

    /// <summary>
    /// Clears all captured errors.
    /// </summary>
    void ClearErrors();
}
