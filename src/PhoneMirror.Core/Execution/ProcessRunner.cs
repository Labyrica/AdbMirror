using System.Diagnostics;
using System.Text;
using PhoneMirror.Core.Platform;

namespace PhoneMirror.Core.Execution;

/// <summary>
/// Executes external processes with proper async handling to prevent deadlocks.
/// Uses the dual-stream reading pattern as documented in PITFALLS.md.
/// </summary>
public sealed class ProcessRunner
{
    private readonly IPlatformService _platformService;
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    public ProcessRunner(IPlatformService platformService)
    {
        _platformService = platformService ?? throw new ArgumentNullException(nameof(platformService));
    }

    /// <summary>
    /// Runs an external process asynchronously with proper stream handling.
    /// </summary>
    /// <param name="executable">The absolute path to the executable.</param>
    /// <param name="arguments">The command-line arguments.</param>
    /// <param name="timeout">Optional timeout. Defaults to 30 seconds.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A ProcessResult containing exit code and captured output.</returns>
    /// <remarks>
    /// CRITICAL: This implementation follows the async dual-stream pattern to prevent deadlocks:
    /// 1. Reads stdout and stderr concurrently using ReadToEndAsync
    /// 2. Awaits both streams with Task.WhenAll before WaitForExitAsync
    /// 3. Never blocks on stream reading before process exit
    ///
    /// See PITFALLS.md section 8 for details on why this is necessary.
    /// </remarks>
    public async Task<ProcessResult> RunAsync(
        string executable,
        string arguments,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(executable))
        {
            throw new ArgumentException("Executable path cannot be null or empty.", nameof(executable));
        }

        var effectiveTimeout = timeout ?? DefaultTimeout;

        var startInfo = new ProcessStartInfo
        {
            // CRITICAL: Always use absolute paths (PITFALLS.md section 7)
            FileName = executable,
            Arguments = arguments,
            UseShellExecute = false,  // Required for stream redirection
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new System.Diagnostics.Process { StartInfo = startInfo };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            return new ProcessResult(-1, string.Empty, $"Failed to start process: {ex.Message}");
        }

        // CRITICAL: Async dual-stream reading to prevent deadlock (PITFALLS.md section 8)
        // The OS pipe buffers have limited size (~64KB). If we wait for process exit
        // before reading, and the process blocks on a full buffer, we deadlock.
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        // Create a combined cancellation source for timeout
        using var timeoutCts = new CancellationTokenSource(effectiveTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        try
        {
            // Wait for both streams to be fully read
            await Task.WhenAll(outputTask, errorTask);

            // Now wait for the process to exit
            await process.WaitForExitAsync(linkedCts.Token);

            return new ProcessResult(
                process.ExitCode,
                outputTask.Result,
                errorTask.Result);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            // Timeout occurred - kill the process
            KillProcess(process);
            return new ProcessResult(
                -1,
                TryGetResult(outputTask),
                $"Process timed out after {effectiveTimeout.TotalSeconds} seconds");
        }
        catch (OperationCanceledException)
        {
            // User cancellation - kill the process
            KillProcess(process);
            return new ProcessResult(
                -1,
                TryGetResult(outputTask),
                "Process was cancelled");
        }
    }

    /// <summary>
    /// Runs an external process and streams output line-by-line to callbacks.
    /// Useful for long-running processes where you need real-time output.
    /// </summary>
    /// <param name="executable">The absolute path to the executable.</param>
    /// <param name="arguments">The command-line arguments.</param>
    /// <param name="onOutputLine">Callback for each standard output line.</param>
    /// <param name="onErrorLine">Callback for each standard error line.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The exit code of the process.</returns>
    public async Task<int> RunWithCallbacksAsync(
        string executable,
        string arguments,
        Action<string>? onOutputLine,
        Action<string>? onErrorLine,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(executable))
        {
            throw new ArgumentException("Executable path cannot be null or empty.", nameof(executable));
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new System.Diagnostics.Process { StartInfo = startInfo };

        if (onOutputLine != null)
        {
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    onOutputLine(e.Data);
            };
        }

        if (onErrorLine != null)
        {
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                    onErrorLine(e.Data);
            };
        }

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode;
        }
        catch (OperationCanceledException)
        {
            KillProcess(process);
            return -1;
        }
        catch
        {
            KillProcess(process);
            throw;
        }
    }

    private static void KillProcess(System.Diagnostics.Process process)
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
            // Ignore errors during cleanup - process may have already exited
        }
    }

    private static string TryGetResult(Task<string> task)
    {
        return task.IsCompletedSuccessfully ? task.Result : string.Empty;
    }
}
