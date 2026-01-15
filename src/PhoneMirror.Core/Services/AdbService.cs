using PhoneMirror.Core.Execution;
using PhoneMirror.Core.Models;
using PhoneMirror.Core.Platform;

namespace PhoneMirror.Core.Services;

/// <summary>
/// Cross-platform implementation of ADB service for device discovery and management.
/// Uses ProcessRunner for all process execution to prevent deadlocks.
/// </summary>
public sealed class AdbService : IAdbService
{
    private readonly ProcessRunner _processRunner;
    private readonly IPlatformService _platformService;
    private readonly IResourceExtractor? _resourceExtractor;
    private string? _resolvedAdbPath;
    private bool _pathResolved;
    private readonly SemaphoreSlim _resolveLock = new(1, 1);

    private static readonly TimeSpan DefaultCommandTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan VersionTimeout = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Creates a new AdbService instance.
    /// </summary>
    /// <param name="processRunner">The process runner for executing ADB commands.</param>
    /// <param name="platformService">The platform service for OS-specific operations.</param>
    /// <param name="resourceExtractor">Optional resource extractor for embedded ADB. Pass null if not available yet.</param>
    public AdbService(
        ProcessRunner processRunner,
        IPlatformService platformService,
        IResourceExtractor? resourceExtractor = null)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _platformService = platformService ?? throw new ArgumentNullException(nameof(platformService));
        _resourceExtractor = resourceExtractor;
    }

    /// <inheritdoc />
    public string? AdbPath => GetAdbPathSync();

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync()
    {
        var adbPath = await ResolveAdbPathAsync().ConfigureAwait(false);
        if (string.IsNullOrEmpty(adbPath))
        {
            return false;
        }

        try
        {
            var result = await _processRunner.RunAsync(adbPath, "version", VersionTimeout);
            return result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task EnsureServerRunningAsync()
    {
        var adbPath = await ResolveAdbPathAsync().ConfigureAwait(false);
        if (string.IsNullOrEmpty(adbPath))
        {
            return;
        }

        await _processRunner.RunAsync(adbPath, "start-server", DefaultCommandTimeout);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AndroidDevice>> GetDevicesAsync()
    {
        var adbPath = await ResolveAdbPathAsync().ConfigureAwait(false);
        if (string.IsNullOrEmpty(adbPath))
        {
            return Array.Empty<AndroidDevice>();
        }

        var result = await _processRunner.RunAsync(adbPath, "devices -l", DefaultCommandTimeout);
        if (result.ExitCode != 0)
        {
            return Array.Empty<AndroidDevice>();
        }

        return ParseDevicesOutput(result.StandardOutput);
    }

    /// <inheritdoc />
    public async Task<(DeviceState State, AndroidDevice? Device)> GetHighLevelStateAsync()
    {
        if (!await IsAvailableAsync())
        {
            return (DeviceState.AdbNotAvailable, null);
        }

        await EnsureServerRunningAsync();

        var devices = await GetDevicesAsync();

        if (devices.Count == 0)
        {
            return (DeviceState.NoDevice, null);
        }

        if (devices.Count > 1)
        {
            // For v1, pick the first device but expose MultipleDevices state
            return (DeviceState.MultipleDevices, devices[0]);
        }

        var device = devices[0];
        var state = device.StateRaw switch
        {
            "unauthorized" => DeviceState.Unauthorized,
            "offline" => DeviceState.Offline,
            "device" => DeviceState.Connected,
            _ => DeviceState.Offline
        };

        return (state, device);
    }

    /// <inheritdoc />
    public async Task StartPollingAsync(
        TimeSpan interval,
        Action<DeviceState, AndroidDevice?> observer,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var (state, device) = await GetHighLevelStateAsync();
                observer(state, device);
            }
            catch
            {
                // Keep polling resilient - errors will surface in state
            }

            try
            {
                await Task.Delay(interval, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Parses the output of 'adb devices -l' into AndroidDevice records.
    /// </summary>
    /// <param name="output">The raw output from ADB devices command.</param>
    /// <returns>List of parsed devices.</returns>
    private static IReadOnlyList<AndroidDevice> ParseDevicesOutput(string output)
    {
        var devices = new List<AndroidDevice>();

        using var reader = new StringReader(output);
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();

            // Skip empty lines and header
            if (string.IsNullOrEmpty(line) ||
                line.StartsWith("List of devices", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Parse line format: "SERIAL STATE device product:X model:Y transport_id:Z"
            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                continue;
            }

            var serial = parts[0];
            var stateRaw = parts[1];

            // Extract model from "model:VALUE" token
            var model = string.Empty;
            foreach (var part in parts)
            {
                if (part.StartsWith("model:", StringComparison.OrdinalIgnoreCase))
                {
                    model = part.Substring("model:".Length);
                    break;
                }
            }

            devices.Add(new AndroidDevice(serial, model, stateRaw));
        }

        return devices;
    }

    /// <summary>
    /// Gets the ADB path synchronously for property access.
    /// Uses cached value if already resolved.
    /// </summary>
    private string? GetAdbPathSync()
    {
        if (_pathResolved)
        {
            return _resolvedAdbPath;
        }

        // For synchronous access, resolve without waiting for async extractor
        return ResolveAdbPathSync(extractedPath: null);
    }

    /// <summary>
    /// Resolves the ADB path asynchronously, waiting for resource extraction if needed.
    /// </summary>
    private async Task<string?> ResolveAdbPathAsync()
    {
        if (_pathResolved)
        {
            return _resolvedAdbPath;
        }

        await _resolveLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_pathResolved)
            {
                return _resolvedAdbPath;
            }

            string? extractedPath = null;
            if (_resourceExtractor != null)
            {
                extractedPath = await _resourceExtractor.GetAdbPathAsync().ConfigureAwait(false);
            }

            _resolvedAdbPath = ResolveAdbPathSync(extractedPath);
            _pathResolved = true;
            return _resolvedAdbPath;
        }
        finally
        {
            _resolveLock.Release();
        }
    }

    /// <summary>
    /// Resolves the ADB executable path using multiple strategies:
    /// 1. Embedded resources (via IResourceExtractor)
    /// 2. Android SDK locations (ANDROID_HOME, ANDROID_SDK_ROOT)
    /// 3. LocalAppData Android SDK
    /// 4. PATH environment variable
    /// 5. Fallback to bare "adb" command
    /// </summary>
    private string? ResolveAdbPathSync(string? extractedPath)
    {
        var candidates = new List<string>();
        var exeExtension = _platformService.ExecutableExtension;
        var adbExecutable = $"adb{exeExtension}";

        // 1. Check embedded resources first (highest priority)
        if (!string.IsNullOrEmpty(extractedPath) && File.Exists(extractedPath))
        {
            candidates.Add(extractedPath);
        }

        // 2. Bundled locations relative to the app
        var baseDir = AppContext.BaseDirectory;
        candidates.Add(Path.Combine(baseDir, "platform-tools", adbExecutable));
        candidates.Add(Path.Combine(baseDir, adbExecutable));

        // Check parent directories (for running from bin/Debug/net8.0)
        var currentDir = Directory.GetParent(baseDir);
        for (int i = 0; i < 5 && currentDir != null; i++)
        {
            candidates.Add(Path.Combine(currentDir.FullName, "platform-tools", adbExecutable));
            currentDir = currentDir.Parent;
        }

        // 3. Common Android SDK locations
        var androidHome = Environment.GetEnvironmentVariable("ANDROID_HOME")
                          ?? Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");
        if (!string.IsNullOrWhiteSpace(androidHome))
        {
            candidates.Add(Path.Combine(androidHome, "platform-tools", adbExecutable));
        }

        // 4. Platform-specific SDK locations
        if (_platformService.CurrentPlatform == System.Runtime.InteropServices.OSPlatform.Windows)
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrWhiteSpace(localAppData))
            {
                candidates.Add(Path.Combine(localAppData, "Android", "Sdk", "platform-tools", adbExecutable));
            }
        }
        else if (_platformService.CurrentPlatform == System.Runtime.InteropServices.OSPlatform.OSX)
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(home))
            {
                candidates.Add(Path.Combine(home, "Library", "Android", "sdk", "platform-tools", adbExecutable));
            }
        }
        else // Linux
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(home))
            {
                candidates.Add(Path.Combine(home, "Android", "Sdk", "platform-tools", adbExecutable));
            }
        }

        // 5. Scan PATH directories
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var pathSeparator = _platformService.CurrentPlatform == System.Runtime.InteropServices.OSPlatform.Windows
            ? ';'
            : ':';
        var pathParts = pathEnv.Split(pathSeparator, StringSplitOptions.RemoveEmptyEntries);

        foreach (var rawDir in pathParts)
        {
            var dir = rawDir.Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(dir))
            {
                continue;
            }

            candidates.Add(Path.Combine(dir, adbExecutable));
        }

        // Return first existing candidate
        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        // Fallback to bare command name - let the process runner fail clearly if not found
        return "adb";
    }
}
