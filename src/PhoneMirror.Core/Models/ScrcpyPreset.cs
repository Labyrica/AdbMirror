namespace PhoneMirror.Core.Models;

/// <summary>
/// Quality presets for scrcpy mirroring sessions.
/// </summary>
public enum ScrcpyPreset
{
    /// <summary>
    /// Low quality: 4M bitrate, 1024 max-size, 30fps.
    /// Best for slower network connections or older devices.
    /// </summary>
    Low,

    /// <summary>
    /// Balanced quality: 8M bitrate, 1280 max-size, 60fps.
    /// Good balance between quality and performance.
    /// </summary>
    Balanced,

    /// <summary>
    /// High quality: 16M bitrate, 1920 max-size, 60fps.
    /// Best quality for powerful devices with good connections.
    /// </summary>
    High
}
