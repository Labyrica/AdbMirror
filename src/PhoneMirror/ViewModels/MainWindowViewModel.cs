using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhoneMirror.Core.Models;
using PhoneMirror.Core.Services;

namespace PhoneMirror.ViewModels;

/// <summary>
/// ViewModel for the main application window.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IAdbService _adbService;
    private readonly IScrcpyService _scrcpyService;
    private readonly ISettingsService _settings;
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
    /// Serial of the last device that was auto-mirrored (to prevent repeated triggers).
    /// </summary>
    private string? _lastAutoMirroredSerial;

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
    private ScrcpyPreset _selectedPreset;

    /// <summary>
    /// Gets or sets the currently selected quality preset.
    /// </summary>
    public ScrcpyPreset SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (SetProperty(ref _selectedPreset, value))
            {
                _settings.DefaultPreset = value;
            }
        }
    }

    // ========== Settings Checkbox Properties ==========

    private bool _autoMirrorOnConnect;

    /// <summary>
    /// Gets or sets whether to automatically start mirroring when a device connects.
    /// </summary>
    public bool AutoMirrorOnConnect
    {
        get => _autoMirrorOnConnect;
        set
        {
            if (SetProperty(ref _autoMirrorOnConnect, value))
            {
                _settings.AutoMirrorOnConnect = value;
            }
        }
    }

    private bool _startFullscreen;

    /// <summary>
    /// Gets or sets whether to start scrcpy in fullscreen mode.
    /// </summary>
    public bool StartFullscreen
    {
        get => _startFullscreen;
        set
        {
            if (SetProperty(ref _startFullscreen, value))
            {
                _settings.StartFullscreen = value;
            }
        }
    }

    private bool _keepScreenAwake;

    /// <summary>
    /// Gets or sets whether to keep the device screen awake during mirroring.
    /// </summary>
    public bool KeepScreenAwake
    {
        get => _keepScreenAwake;
        set
        {
            if (SetProperty(ref _keepScreenAwake, value))
            {
                _settings.KeepScreenAwake = value;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the MainWindowViewModel.
    /// </summary>
    /// <param name="adbService">The ADB service for device communication.</param>
    /// <param name="scrcpyService">The scrcpy service for screen mirroring.</param>
    /// <param name="settingsService">The settings service for persisting user preferences.</param>
    public MainWindowViewModel(IAdbService adbService, IScrcpyService scrcpyService, ISettingsService settingsService)
    {
        _adbService = adbService ?? throw new ArgumentNullException(nameof(adbService));
        _scrcpyService = scrcpyService ?? throw new ArgumentNullException(nameof(scrcpyService));
        _settings = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

        // Initialize all properties from persisted settings
        _selectedPreset = _settings.DefaultPreset;
        _autoMirrorOnConnect = _settings.AutoMirrorOnConnect;
        _startFullscreen = _settings.StartFullscreen;
        _keepScreenAwake = _settings.KeepScreenAwake;

        // Subscribe to mirroring stopped event
        _scrcpyService.MirroringStopped += OnMirroringStopped;

        // Start initialization (fire-and-forget)
        _ = InitializeAsync();
    }

    /// <summary>
    /// Initializes the ViewModel by checking ADB availability and starting device polling.
    /// </summary>
    private async Task InitializeAsync()
    {
        try
        {
            // Check if ADB is available
            var isAvailable = await _adbService.IsAvailableAsync();
            if (!isAvailable)
            {
                StatusText = "ADB not available";
                return;
            }

            // Set ADB path
            AdbPathText = _adbService.AdbPath ?? "Unknown";

            // Start polling for device state changes
            await _adbService.StartPollingAsync(
                TimeSpan.FromSeconds(1),
                OnDeviceStateChanged,
                _pollCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Polling was cancelled, expected during shutdown
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Callback invoked when device state changes.
    /// </summary>
    /// <param name="state">The new device state.</param>
    /// <param name="device">The current device (if any).</param>
    private void OnDeviceStateChanged(DeviceState state, AndroidDevice? device)
    {
        // Store state in private fields
        _currentState = state;
        _currentDevice = device;

        // Reset auto-mirror tracking when device disconnects
        if (state == DeviceState.NoDevice || state == DeviceState.Offline)
        {
            _lastAutoMirroredSerial = null;
        }

        // Update UI on the UI thread
        Dispatcher.UIThread.Post(() =>
        {
            // Map DeviceState to user-friendly status text
            StatusText = state switch
            {
                DeviceState.NoDevice => "No device connected",
                DeviceState.Unauthorized => "Device unauthorized: check phone prompt",
                DeviceState.Offline => "Device offline",
                DeviceState.Connected => $"Device connected: {device?.DisplayName ?? "Unknown"}",
                DeviceState.MultipleDevices => "Multiple devices connected",
                DeviceState.AdbNotAvailable => "ADB not available",
                DeviceState.ScrcpyNotAvailable => "scrcpy not available",
                DeviceState.Mirroring => "Mirroring active",
                _ => "Unknown state"
            };

            // Update button enabled state based on device availability
            // Enable when connected, multiple devices (first one used), or currently mirroring
            IsPrimaryEnabled = state == DeviceState.Connected || state == DeviceState.MultipleDevices || _isMirroring;

            // Notify that DeviceDisplayName may have changed
            OnPropertyChanged(nameof(DeviceDisplayName));

            // Notify that CanMirror may have changed
            MirrorCommand.NotifyCanExecuteChanged();

            // Auto-mirror when device connects (if enabled and not already auto-mirrored)
            if (state == DeviceState.Connected &&
                AutoMirrorOnConnect &&
                !_isMirroring &&
                device != null &&
                device.Serial != _lastAutoMirroredSerial)
            {
                _lastAutoMirroredSerial = device.Serial;
                _ = MirrorAsync();
            }
        });
    }

    // ========== Mirroring Event Handlers ==========

    /// <summary>
    /// Handles the MirroringStopped event from the scrcpy service.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="message">The exit message from scrcpy.</param>
    private void OnMirroringStopped(object? sender, string message)
    {
        Dispatcher.UIThread.Post(() =>
        {
            // Reset mirroring state
            _isMirroring = false;
            PrimaryButtonText = "Mirror";

            // Update status with exit message
            StatusText = message;

            // Re-evaluate device state to update button enabled state
            OnDeviceStateChanged(_currentState, _currentDevice);
        });
    }

    // ========== Mirroring Commands ==========

    /// <summary>
    /// Determines whether the mirror command can execute.
    /// </summary>
    /// <returns>True if mirroring can be started or stopped.</returns>
    private bool CanMirror() => IsPrimaryEnabled;

    /// <summary>
    /// Toggles mirroring state - starts if not mirroring, stops if mirroring.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanMirror))]
    private async Task MirrorAsync()
    {
        if (_isMirroring)
        {
            // Stop mirroring
            _scrcpyService.StopMirroring();
            _isMirroring = false;
            PrimaryButtonText = "Mirror";
        }
        else
        {
            // Ensure we have a device serial
            var serial = _currentDevice?.Serial;
            if (string.IsNullOrEmpty(serial))
            {
                StatusText = "No device selected";
                return;
            }

            // Start mirroring
            _isMirroring = true;
            PrimaryButtonText = "Stop";
            StatusText = "Starting mirroring...";

            var (success, error) = await _scrcpyService.StartMirroringAsync(
                serial,
                SelectedPreset,
                KeepScreenAwake,
                StartFullscreen);

            if (!success)
            {
                _isMirroring = false;
                PrimaryButtonText = "Mirror";
                StatusText = error ?? "Failed to start mirroring";
            }
            else
            {
                StatusText = "Mirroring active";
            }
        }
    }

    /// <summary>
    /// Releases resources used by the ViewModel.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases resources used by the ViewModel.
    /// </summary>
    /// <param name="disposing">True if called from Dispose(), false if from finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            // Unsubscribe from events
            _scrcpyService.MirroringStopped -= OnMirroringStopped;

            _pollCts.Cancel();
            _pollCts.Dispose();
        }

        _disposed = true;
    }
}
