namespace PhoneMirror.Core.Models;

/// <summary>
/// High-level device state as understood by the UI.
/// Maps from raw ADB output to user-facing states.
/// </summary>
public enum DeviceState
{
    /// <summary>No device is connected.</summary>
    NoDevice,

    /// <summary>Device requires USB debugging authorization.</summary>
    Unauthorized,

    /// <summary>Device is offline or in an unresponsive state.</summary>
    Offline,

    /// <summary>Device is connected and ready for mirroring.</summary>
    Connected,

    /// <summary>Multiple devices are connected.</summary>
    MultipleDevices,

    /// <summary>ADB executable is not available.</summary>
    AdbNotAvailable,

    /// <summary>scrcpy executable is not available.</summary>
    ScrcpyNotAvailable,

    /// <summary>Screen mirroring is currently active.</summary>
    Mirroring
}
