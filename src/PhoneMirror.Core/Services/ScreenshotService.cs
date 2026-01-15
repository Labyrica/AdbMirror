using System.Diagnostics;

namespace PhoneMirror.Core.Services;

/// <summary>
/// Implementation of screenshot service using ADB screencap command.
/// Uses Process directly (similar to ScrcpyService) to capture binary PNG output.
/// </summary>
public sealed class ScreenshotService : IScreenshotService
{
    private readonly IAdbService _adbService;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Creates a new ScreenshotService instance.
    /// </summary>
    /// <param name="adbService">The ADB service for path resolution.</param>
    public ScreenshotService(IAdbService adbService)
    {
        _adbService = adbService ?? throw new ArgumentNullException(nameof(adbService));
    }

    /// <inheritdoc />
    public async Task<(byte[]? PngData, string? Error)> CaptureAsync(string deviceSerial, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(deviceSerial))
        {
            return (null, "No device serial provided");
        }

        var adbPath = _adbService.AdbPath;
        if (string.IsNullOrEmpty(adbPath))
        {
            return (null, "ADB not available");
        }

        try
        {
            // Use exec-out to get raw binary output from screencap
            // Command: adb -s {serial} exec-out screencap -p
            var startInfo = new ProcessStartInfo
            {
                FileName = adbPath,
                Arguments = $"-s {deviceSerial} exec-out screencap -p",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            // Read binary output as a memory stream
            using var memoryStream = new MemoryStream();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(DefaultTimeout);

            try
            {
                // Copy binary data from stdout
                await process.StandardOutput.BaseStream.CopyToAsync(memoryStream, timeoutCts.Token);

                // Wait for process to exit
                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                KillProcess(process);
                return (null, ct.IsCancellationRequested
                    ? "Screenshot capture cancelled"
                    : "Screenshot capture timed out");
            }

            if (process.ExitCode != 0)
            {
                var errorOutput = await process.StandardError.ReadToEndAsync(ct);
                return (null, $"ADB screencap failed: {errorOutput}");
            }

            var pngData = memoryStream.ToArray();

            // Validate that we got PNG data (PNG magic bytes: 89 50 4E 47)
            if (pngData.Length < 8 ||
                pngData[0] != 0x89 ||
                pngData[1] != 0x50 ||
                pngData[2] != 0x4E ||
                pngData[3] != 0x47)
            {
                return (null, "Invalid screenshot data received");
            }

            return (pngData, null);
        }
        catch (Exception ex)
        {
            return (null, $"Screenshot capture error: {ex.Message}");
        }
    }

    private static void KillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Ignore errors during cleanup
        }
    }
}
