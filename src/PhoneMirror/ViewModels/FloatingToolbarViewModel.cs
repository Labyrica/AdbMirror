using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PhoneMirror.Core.Services;

namespace PhoneMirror.ViewModels;

/// <summary>
/// ViewModel for the floating toolbar that appears during mirroring sessions.
/// Provides quick access to screenshot capture and error log copying.
/// </summary>
public partial class FloatingToolbarViewModel : ViewModelBase
{
    private readonly IScreenshotService _screenshotService;
    private readonly ILogcatService _logcatService;
    private readonly string _deviceSerial;
    private DispatcherTimer? _statusClearTimer;

    /// <summary>
    /// Brief status message displayed to the user (auto-clears after 2 seconds).
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = "";

    /// <summary>
    /// Initializes a new instance of the FloatingToolbarViewModel.
    /// </summary>
    /// <param name="screenshotService">The screenshot service for capturing device screen.</param>
    /// <param name="logcatService">The logcat service for retrieving error logs.</param>
    /// <param name="deviceSerial">The serial number of the connected device.</param>
    public FloatingToolbarViewModel(
        IScreenshotService screenshotService,
        ILogcatService logcatService,
        string deviceSerial)
    {
        _screenshotService = screenshotService ?? throw new ArgumentNullException(nameof(screenshotService));
        _logcatService = logcatService ?? throw new ArgumentNullException(nameof(logcatService));
        _deviceSerial = deviceSerial ?? throw new ArgumentNullException(nameof(deviceSerial));
    }

    /// <summary>
    /// Captures a screenshot from the device and copies it to clipboard.
    /// </summary>
    [RelayCommand]
    private async Task ScreenshotAsync()
    {
        ShowStatus("Capturing...");

        var (pngData, error) = await _screenshotService.CaptureAsync(_deviceSerial);

        if (pngData == null || error != null)
        {
            ShowStatus(error ?? "Capture failed");
            return;
        }

        try
        {
            // Save to temp file
            var tempPath = Path.Combine(
                Path.GetTempPath(),
                $"PhoneMirror_Screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            await File.WriteAllBytesAsync(tempPath, pngData);

            var clipboard = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow?.Clipboard
                : null;

            if (clipboard != null)
            {
                // Copy actual image to clipboard using DataObject
                using var memoryStream = new MemoryStream(pngData);
                var bitmap = new Bitmap(memoryStream);
                var dataObject = new DataObject();
                dataObject.Set(DataFormats.Files, new[] { new FileInfo(tempPath) });

                // Set as text fallback (file path) for apps that don't support file drops
                dataObject.Set(DataFormats.Text, tempPath);

                await clipboard.SetDataObjectAsync(dataObject);
                ShowStatus("Copied!");
            }
            else
            {
                ShowStatus("Saved to temp");
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Copies recent logcat errors to the clipboard.
    /// </summary>
    [RelayCommand]
    private async Task CopyErrorsAsync()
    {
        var recentErrors = _logcatService.GetRecentErrors();

        if (recentErrors.Count == 0)
        {
            ShowStatus("No recent errors");
            return;
        }

        // Format errors as text (timestamp, level, tag, message - one per line)
        var sb = new StringBuilder();
        sb.AppendLine($"=== {recentErrors.Count} Recent Errors (last 10 seconds) ===");
        sb.AppendLine();

        foreach (var entry in recentErrors)
        {
            sb.AppendLine($"[{entry.Timestamp:HH:mm:ss.fff}] {entry.Level}/{entry.Tag}: {entry.Message}");
        }

        var clipboard = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow?.Clipboard
            : null;

        if (clipboard != null)
        {
            await clipboard.SetTextAsync(sb.ToString());
            ShowStatus($"{recentErrors.Count} errors copied");
        }
        else
        {
            ShowStatus("Clipboard unavailable");
        }
    }

    /// <summary>
    /// Shows a status message that auto-clears after 2 seconds.
    /// </summary>
    /// <param name="message">The message to display.</param>
    private void ShowStatus(string message)
    {
        StatusMessage = message;

        // Stop any existing timer
        _statusClearTimer?.Stop();

        // Create new timer to clear status after 2 seconds
        _statusClearTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _statusClearTimer.Tick += (_, _) =>
        {
            StatusMessage = "";
            _statusClearTimer?.Stop();
        };
        _statusClearTimer.Start();
    }
}
