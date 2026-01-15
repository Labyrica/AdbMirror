namespace PhoneMirror.Core.Models;

/// <summary>
/// Represents an Android device as reported by ADB.
/// Immutable record for thread safety.
/// </summary>
/// <param name="Serial">The device serial number (unique identifier).</param>
/// <param name="Model">The device model name (may be empty if not available).</param>
/// <param name="StateRaw">The raw state string from ADB (device, unauthorized, offline, etc.).</param>
public record AndroidDevice(string Serial, string Model, string StateRaw)
{
    /// <summary>
    /// Gets a user-friendly display name for the device.
    /// Returns the model if available, otherwise falls back to serial.
    /// </summary>
    public string DisplayName => string.IsNullOrEmpty(Model) ? Serial : Model;
}
