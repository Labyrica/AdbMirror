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

    /// <summary>
    /// Initializes a new instance of the MainWindowViewModel.
    /// </summary>
    /// <param name="adbService">The ADB service for device communication.</param>
    public MainWindowViewModel(IAdbService adbService)
    {
        _adbService = adbService ?? throw new ArgumentNullException(nameof(adbService));

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
        // Placeholder - will be fully implemented in Task 3
        _currentState = state;
        _currentDevice = device;
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
            _pollCts.Cancel();
            _pollCts.Dispose();
        }

        _disposed = true;
    }
}
