using System.Diagnostics;
using System.Text;
using PhoneMirror.Core.Models;
using PhoneMirror.Core.Platform;

namespace PhoneMirror.Core.Services;

/// <summary>
/// Provides screen mirroring functionality using scrcpy with cross-platform support.
/// </summary>
public sealed class ScrcpyService : IScrcpyService
{
    private readonly IPlatformService _platformService;
    private string? _scrcpyPath;
    private Process? _currentProcess;

    /// <inheritdoc />
    public string? ScrcpyPath => _scrcpyPath;

    /// <inheritdoc />
    public bool IsMirroring => _currentProcess != null && !_currentProcess.HasExited;

    /// <inheritdoc />
    public event EventHandler<string>? MirroringStopped;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScrcpyService"/> class.
    /// </summary>
    /// <param name="platformService">The platform service for cross-platform operations.</param>
    public ScrcpyService(IPlatformService platformService)
    {
        _platformService = platformService;
        _scrcpyPath = ResolveScrcpyPath();
    }

    /// <inheritdoc />
    public Task<bool> IsAvailableAsync()
    {
        // Re-resolve path in case environment changed
        _scrcpyPath = ResolveScrcpyPath();
        return Task.FromResult(_scrcpyPath != null && File.Exists(_scrcpyPath));
    }

    /// <inheritdoc />
    public async Task<(bool Success, string? Error)> StartMirroringAsync(
        string deviceSerial,
        ScrcpyPreset preset,
        bool keepScreenAwake,
        bool fullscreen,
        CancellationToken cancellationToken = default)
    {
        // Clean up any existing session
        StopMirroring();

        // Check availability
        if (!await IsAvailableAsync())
        {
            var executableName = "scrcpy" + _platformService.ExecutableExtension;
            return (false, $"scrcpy not found. Please ensure {executableName} is available in the application directory or in PATH.");
        }

        var args = BuildArguments(deviceSerial, preset, keepScreenAwake, fullscreen);

        var psi = new ProcessStartInfo
        {
            FileName = _scrcpyPath!,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            // WorkingDirectory must be set to the scrcpy directory for DLL dependencies
            WorkingDirectory = Path.GetDirectoryName(Path.GetFullPath(_scrcpyPath!)) ?? AppContext.BaseDirectory
        };

        try
        {
            var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

            // Capture streams asynchronously to avoid deadlocks
            StringBuilder stdoutBuilder = new();
            StringBuilder stderrBuilder = new();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    lock (stdoutBuilder)
                    {
                        stdoutBuilder.AppendLine(e.Data);
                    }
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null)
                {
                    lock (stderrBuilder)
                    {
                        stderrBuilder.AppendLine(e.Data);
                    }
                }
            };

            process.Exited += (_, _) =>
            {
                string stdout;
                string stderr;
                lock (stdoutBuilder)
                {
                    stdout = stdoutBuilder.ToString().Trim();
                }
                lock (stderrBuilder)
                {
                    stderr = stderrBuilder.ToString().Trim();
                }

                var messageBuilder = new StringBuilder();
                messageBuilder.Append(process.ExitCode == 0
                    ? "scrcpy exited."
                    : $"scrcpy failed with code {process.ExitCode}.");

                if (!string.IsNullOrWhiteSpace(stderr))
                {
                    messageBuilder.Append(" stderr: ").Append(stderr);
                }
                else if (!string.IsNullOrWhiteSpace(stdout))
                {
                    messageBuilder.Append(" stdout: ").Append(stdout);
                }

                var message = messageBuilder.ToString();

                // Cleanup process reference
                try
                {
                    process.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }

                _currentProcess = null;

                // Raise event on a captured message to avoid closure issues
                MirroringStopped?.Invoke(this, message);
            };

            var started = process.Start();
            if (!started)
            {
                process.Dispose();
                return (false, "Failed to start scrcpy process.");
            }

            // Begin async reading of output streams
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            _currentProcess = process;
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Failed to start scrcpy: {ex.Message}\nPath: {_scrcpyPath}\nArgs: {args}");
        }
    }

    /// <inheritdoc />
    public void StopMirroring()
    {
        if (_currentProcess == null)
        {
            return;
        }

        try
        {
            if (!_currentProcess.HasExited)
            {
                _currentProcess.Kill();
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
                _currentProcess.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
            _currentProcess = null;
        }
    }

    /// <summary>
    /// Resolves the path to the scrcpy executable.
    /// </summary>
    /// <returns>The resolved path, or null if not found.</returns>
    private string? ResolveScrcpyPath()
    {
        var executableName = "scrcpy" + _platformService.ExecutableExtension;
        var candidates = new List<string>();

        var baseDir = AppContext.BaseDirectory;

        // Check bundled locations (relative to application)
        candidates.Add(Path.Combine(baseDir, "scrcpy", executableName));
        candidates.Add(Path.Combine(baseDir, executableName));

        // Check application data directory (extracted resources location)
        var appDataPath = _platformService.GetAppDataPath();
        candidates.Add(Path.Combine(appDataPath, "scrcpy", executableName));

        // Check parent directories (for development scenarios like bin\Debug\net8.0)
        var currentDir = Directory.GetParent(baseDir);
        for (int i = 0; i < 5 && currentDir != null; i++)
        {
            candidates.Add(Path.Combine(currentDir.FullName, "scrcpy", executableName));
            currentDir = currentDir.Parent;
        }

        // Check PATH directories
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var separator = _platformService.CurrentPlatform == System.Runtime.InteropServices.OSPlatform.Windows
            ? ';'
            : ':';
        var pathParts = pathEnv.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        foreach (var rawDir in pathParts)
        {
            var dir = rawDir.Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(dir)) continue;
            candidates.Add(Path.Combine(dir, executableName));
        }

        // Return the first existing candidate
        foreach (var candidate in candidates)
        {
            try
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch
            {
                // Ignore path access errors
            }
        }

        return null;
    }

    /// <summary>
    /// Builds the scrcpy command-line arguments for the given options.
    /// </summary>
    private static string BuildArguments(string serial, ScrcpyPreset preset, bool keepScreenAwake, bool fullscreen)
    {
        var builder = new StringBuilder();

        // Device serial (quoted for safety)
        builder.Append("-s \"").Append(serial).Append('"');

        // Quality preset parameters
        switch (preset)
        {
            case ScrcpyPreset.Low:
                builder.Append(" --video-bit-rate 4M --max-size 1024 --max-fps 30");
                break;
            case ScrcpyPreset.Balanced:
                builder.Append(" --video-bit-rate 8M --max-size 1280 --max-fps 60");
                break;
            case ScrcpyPreset.High:
                builder.Append(" --video-bit-rate 16M --max-size 1920 --max-fps 60");
                break;
        }

        // Keep screen awake option
        if (keepScreenAwake)
        {
            builder.Append(" --stay-awake");
        }

        // Fullscreen option
        if (fullscreen)
        {
            builder.Append(" --fullscreen");
        }

        // Turn off device screen during mirroring (requires control enabled, which is default)
        builder.Append(" --turn-screen-off");

        return builder.ToString();
    }
}
