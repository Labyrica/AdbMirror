using System.Text.Json;
using PhoneMirror.Core.Models;

namespace PhoneMirror.Core.Services;

/// <summary>
/// Provides settings persistence using JSON file storage.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsPath;
    private SettingsData _data = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsService"/> class.
    /// </summary>
    public SettingsService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var settingsDir = Path.Combine(appDataPath, "PhoneMirror");
        _settingsPath = Path.Combine(settingsDir, "settings.json");
    }

    /// <inheritdoc />
    public ScrcpyPreset DefaultPreset
    {
        get => _data.DefaultPreset;
        set
        {
            if (_data.DefaultPreset != value)
            {
                _data.DefaultPreset = value;
                Save();
            }
        }
    }

    /// <inheritdoc />
    public bool AutoMirrorOnConnect
    {
        get => _data.AutoMirrorOnConnect;
        set
        {
            if (_data.AutoMirrorOnConnect != value)
            {
                _data.AutoMirrorOnConnect = value;
                Save();
            }
        }
    }

    /// <inheritdoc />
    public bool StartFullscreen
    {
        get => _data.StartFullscreen;
        set
        {
            if (_data.StartFullscreen != value)
            {
                _data.StartFullscreen = value;
                Save();
            }
        }
    }

    /// <inheritdoc />
    public bool KeepScreenAwake
    {
        get => _data.KeepScreenAwake;
        set
        {
            if (_data.KeepScreenAwake != value)
            {
                _data.KeepScreenAwake = value;
                Save();
            }
        }
    }

    /// <inheritdoc />
    public void Save()
    {
        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_data, JsonOptions);
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            // Log error but don't throw - settings persistence failure shouldn't crash the app
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task LoadAsync()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                // No settings file - use defaults
                _data = new SettingsData();
                return;
            }

            var json = await File.ReadAllTextAsync(_settingsPath);
            var loaded = JsonSerializer.Deserialize<SettingsData>(json);
            _data = loaded ?? new SettingsData();
        }
        catch (Exception ex)
        {
            // Log error and use defaults - corrupted settings file shouldn't crash the app
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
            _data = new SettingsData();
        }
    }

    /// <summary>
    /// Internal data class for JSON serialization.
    /// </summary>
    private sealed class SettingsData
    {
        public ScrcpyPreset DefaultPreset { get; set; } = ScrcpyPreset.Balanced;
        public bool AutoMirrorOnConnect { get; set; } = false;
        public bool StartFullscreen { get; set; } = false;
        public bool KeepScreenAwake { get; set; } = true;
    }
}
