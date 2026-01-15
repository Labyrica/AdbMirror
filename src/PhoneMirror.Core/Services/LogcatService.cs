using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace PhoneMirror.Core.Services;

/// <summary>
/// Provides session-scoped logcat error capture during mirroring.
/// Captures only error-level entries (*:E filter) and maintains a circular buffer
/// of recent entries for diagnostic purposes.
/// </summary>
public sealed class LogcatService : ILogcatService
{
    private readonly IAdbService _adbService;
    private readonly ConcurrentQueue<LogcatEntry> _entries = new();
    private Process? _logcatProcess;
    private readonly object _processLock = new();

    /// <summary>
    /// Maximum number of entries to keep in the buffer.
    /// </summary>
    private const int MaxEntries = 1000;

    /// <summary>
    /// Time window for "recent" errors in seconds.
    /// </summary>
    private const int RecentWindowSeconds = 10;

    /// <summary>
    /// Regex to parse logcat output format: "01-15 12:34:56.789 E/Tag: message"
    /// Groups: 1=timestamp, 2=level, 3=tag, 4=message
    /// </summary>
    private static readonly Regex LogcatLineRegex = new(
        @"^(\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}\.\d{3})\s+([VDIWEF])/([^:]+):\s*(.*)$",
        RegexOptions.Compiled);

    /// <inheritdoc />
    public bool IsCapturing
    {
        get
        {
            lock (_processLock)
            {
                return _logcatProcess != null && !_logcatProcess.HasExited;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogcatService"/> class.
    /// </summary>
    /// <param name="adbService">The ADB service for path resolution.</param>
    public LogcatService(IAdbService adbService)
    {
        _adbService = adbService ?? throw new ArgumentNullException(nameof(adbService));
    }

    /// <inheritdoc />
    public Task StartCaptureAsync(string deviceSerial, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(deviceSerial))
        {
            throw new ArgumentException("Device serial cannot be null or empty.", nameof(deviceSerial));
        }

        // Stop any existing capture
        StopCapture();

        var adbPath = _adbService.AdbPath;
        if (string.IsNullOrEmpty(adbPath))
        {
            // ADB not available, silently return (logcat is optional feature)
            return Task.CompletedTask;
        }

        // Build arguments: -s {serial} logcat *:E to capture only errors
        // Clear the log buffer first with -c, then start capturing
        var args = $"-s \"{deviceSerial}\" logcat *:E";

        var psi = new ProcessStartInfo
        {
            FileName = adbPath,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        lock (_processLock)
        {
            try
            {
                var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

                process.OutputDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        ParseAndAddEntry(e.Data);
                    }
                };

                process.ErrorDataReceived += (_, e) =>
                {
                    // Ignore stderr from logcat (typically just diagnostic messages)
                };

                process.Exited += (_, _) =>
                {
                    lock (_processLock)
                    {
                        try
                        {
                            process.Dispose();
                        }
                        catch
                        {
                            // Ignore disposal errors
                        }

                        if (_logcatProcess == process)
                        {
                            _logcatProcess = null;
                        }
                    }
                };

                var started = process.Start();
                if (started)
                {
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    _logcatProcess = process;
                }
                else
                {
                    process.Dispose();
                }
            }
            catch
            {
                // Failed to start logcat - this is an optional feature, so we silently fail
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void StopCapture()
    {
        lock (_processLock)
        {
            if (_logcatProcess == null)
            {
                return;
            }

            try
            {
                if (!_logcatProcess.HasExited)
                {
                    _logcatProcess.Kill();
                }
            }
            catch
            {
                // Ignore kill failures; process may already be gone
            }
            finally
            {
                try
                {
                    _logcatProcess.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }

                _logcatProcess = null;
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<LogcatEntry> GetRecentErrors()
    {
        var cutoff = DateTime.UtcNow.AddSeconds(-RecentWindowSeconds);
        return _entries.Where(e => e.Timestamp >= cutoff).ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<LogcatEntry> GetAllSessionErrors()
    {
        return _entries.ToList();
    }

    /// <inheritdoc />
    public void ClearErrors()
    {
        // ConcurrentQueue doesn't have a Clear method, so we drain it
        while (_entries.TryDequeue(out _))
        {
            // Continue until empty
        }
    }

    /// <summary>
    /// Parses a logcat line and adds it to the circular buffer.
    /// </summary>
    /// <param name="line">The raw logcat line.</param>
    private void ParseAndAddEntry(string line)
    {
        var match = LogcatLineRegex.Match(line);
        if (!match.Success)
        {
            // Line doesn't match expected format, skip it
            return;
        }

        var timestampStr = match.Groups[1].Value;
        var level = match.Groups[2].Value;
        var tag = match.Groups[3].Value.Trim();
        var message = match.Groups[4].Value;

        // Parse the timestamp - format is "MM-dd HH:mm:ss.fff"
        // We'll use current year since logcat doesn't include it
        var now = DateTime.UtcNow;
        if (DateTime.TryParseExact(
            $"{now.Year}-{timestampStr}",
            "yyyy-MM-dd HH:mm:ss.fff",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal,
            out var timestamp))
        {
            var entry = new LogcatEntry(timestamp, level, tag, message);
            _entries.Enqueue(entry);

            // Enforce circular buffer max size
            while (_entries.Count > MaxEntries)
            {
                _entries.TryDequeue(out _);
            }
        }
    }
}
