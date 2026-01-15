using PhoneMirror.Core.Models;

namespace PhoneMirror.Core.Services;

/// <summary>
/// Provides persistence for user settings across sessions.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets or sets the default quality preset for mirroring.
    /// </summary>
    ScrcpyPreset DefaultPreset { get; set; }

    /// <summary>
    /// Gets or sets whether to automatically start mirroring when a device connects.
    /// </summary>
    bool AutoMirrorOnConnect { get; set; }

    /// <summary>
    /// Gets or sets whether to start scrcpy in fullscreen mode.
    /// </summary>
    bool StartFullscreen { get; set; }

    /// <summary>
    /// Gets or sets whether to keep the device screen awake during mirroring.
    /// </summary>
    bool KeepScreenAwake { get; set; }

    /// <summary>
    /// Saves the current settings to persistent storage.
    /// </summary>
    void Save();

    /// <summary>
    /// Loads settings from persistent storage asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LoadAsync();
}
