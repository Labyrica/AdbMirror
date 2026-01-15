using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using PhoneMirror.Core.Models;
using PhoneMirror.Core.Services;

namespace PhoneMirror.ViewModels;

/// <summary>
/// ViewModel for the main application window.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IAdbService _adbService;
    private readonly CancellationTokenSource _pollCts = new();
    private bool _disposed;
    /// <summary>
    /// Current status text displayed to the user.
    /// </summary>
    [ObservableProperty]
    private string _statusText = "Initializing...";

    /// <summary>
    /// Path to the ADB executable being used.
    /// </summary>
    [ObservableProperty]
    private string _adbPathText = "";

    /// <summary>
    /// The currently connected device (if any).
    /// </summary>
    private AndroidDevice? _currentDevice;

    /// <summary>
    /// The current device connection state.
    /// </summary>
    private DeviceState _currentState;

    /// <summary>
    /// Gets a user-friendly display name for the current device.
    /// </summary>
    public string DeviceDisplayName => _currentDevice?.DisplayName ?? "No device";

    // ========== Mirroring State Properties ==========

    /// <summary>
    /// Text displayed on the primary mirror button ("Mirror" or "Stop").
    /// </summary>
    [ObservableProperty]
    private string _primaryButtonText = "Mirror";

    /// <summary>
    /// Whether the primary mirror button is enabled.
    /// </summary>
    [ObservableProperty]
    private bool _isPrimaryEnabled;

    /// <summary>
    /// Whether mirroring is currently active.
    /// </summary>
    private bool _isMirroring;

    /// <summary>
    /// Available quality presets for mirroring.
    /// </summary>
    public ObservableCollection<ScrcpyPreset> Presets { get; } = new(
    [
        ScrcpyPreset.Low,
        ScrcpyPreset.Balanced,
        ScrcpyPreset.High
    ]);

    /// <summary>
    /// The currently selected quality preset.
    /// </summary>
    [ObservableProperty]
    private ScrcpyPreset _selectedPreset = ScrcpyPreset.Balanced;
}
