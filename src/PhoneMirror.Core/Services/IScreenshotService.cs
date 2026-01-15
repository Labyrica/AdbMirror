namespace PhoneMirror.Core.Services;

/// <summary>
/// Service for capturing device screenshots via ADB.
/// </summary>
public interface IScreenshotService
{
    /// <summary>
    /// Captures a screenshot from the device and returns the PNG bytes.
    /// </summary>
    /// <param name="deviceSerial">The serial number of the target device.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A tuple containing PNG data if successful, or an error message if failed.</returns>
    Task<(byte[]? PngData, string? Error)> CaptureAsync(string deviceSerial, CancellationToken ct = default);
}
