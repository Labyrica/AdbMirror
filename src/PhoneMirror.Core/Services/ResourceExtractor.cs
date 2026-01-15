using System.IO.Compression;
using System.Reflection;
using PhoneMirror.Core.Platform;

namespace PhoneMirror.Core.Services;

/// <summary>
/// Extracts bundled platform-tools and scrcpy resources to a temporary directory.
/// Handles cross-platform requirements including executable permissions on Unix.
/// </summary>
public sealed class ResourceExtractor : IResourceExtractor
{
    private readonly IPlatformService _platformService;
    private readonly SemaphoreSlim _extractionLock = new(1, 1);

    private string? _extractedBasePath;
    private bool _extracted;

    public ResourceExtractor(IPlatformService platformService)
    {
        _platformService = platformService ?? throw new ArgumentNullException(nameof(platformService));
    }

    /// <inheritdoc />
    public async Task ExtractAllAsync()
    {
        if (_extracted && _extractedBasePath != null && Directory.Exists(_extractedBasePath))
        {
            return;
        }

        await _extractionLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // Double-check after acquiring lock
            if (_extracted && _extractedBasePath != null && Directory.Exists(_extractedBasePath))
            {
                return;
            }

            // Create unique extraction directory
            var randomSuffix = Guid.NewGuid().ToString("N")[..8];
            var basePath = Path.Combine(_platformService.GetTempPath(), "PhoneMirror", randomSuffix);
            Directory.CreateDirectory(basePath);

            // Extract platform-tools if embedded
            await ExtractResourceAsync("platform-tools.zip", Path.Combine(basePath, "platform-tools")).ConfigureAwait(false);

            // Extract scrcpy if embedded
            await ExtractResourceAsync("scrcpy.zip", Path.Combine(basePath, "scrcpy")).ConfigureAwait(false);

            _extractedBasePath = basePath;
            _extracted = true;
        }
        finally
        {
            _extractionLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetAdbPathAsync()
    {
        await EnsureExtractedAsync().ConfigureAwait(false);

        if (_extractedBasePath == null)
        {
            return null;
        }

        var adbFileName = "adb" + _platformService.ExecutableExtension;
        var path = Path.Combine(_extractedBasePath, "platform-tools", adbFileName);

        return File.Exists(path) ? path : null;
    }

    /// <inheritdoc />
    public async Task<string?> GetScrcpyPathAsync()
    {
        await EnsureExtractedAsync().ConfigureAwait(false);

        if (_extractedBasePath == null)
        {
            return null;
        }

        var scrcpyFileName = "scrcpy" + _platformService.ExecutableExtension;
        var path = Path.Combine(_extractedBasePath, "scrcpy", scrcpyFileName);

        return File.Exists(path) ? path : null;
    }

    /// <inheritdoc />
    public void Cleanup()
    {
        if (_extractedBasePath != null && Directory.Exists(_extractedBasePath))
        {
            try
            {
                Directory.Delete(_extractedBasePath, recursive: true);
            }
            catch
            {
                // Best effort cleanup - may fail if processes still running
            }
        }
    }

    /// <summary>
    /// Ensures extraction has occurred, triggering it if not.
    /// </summary>
    private async Task EnsureExtractedAsync()
    {
        if (!_extracted)
        {
            await ExtractAllAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Extracts an embedded resource ZIP to the target directory.
    /// </summary>
    private async Task ExtractResourceAsync(string resourceName, string targetDirectory)
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fullResourceName = $"PhoneMirror.Core.Resources.{resourceName}";

            await using var stream = assembly.GetManifestResourceStream(fullResourceName);
            if (stream == null)
            {
                // Resource not embedded - graceful fallback, app will use PATH
                return;
            }

            Directory.CreateDirectory(targetDirectory);

            using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
            zip.ExtractToDirectory(targetDirectory, overwriteFiles: true);

            // Set executable permissions on Unix platforms
            await SetExecutablePermissionsAsync(targetDirectory).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Log but don't throw - app will fall back to PATH
            System.Diagnostics.Debug.WriteLine($"Failed to extract {resourceName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Sets executable permissions on extracted binaries (Linux/macOS only).
    /// Also removes quarantine attribute on macOS.
    /// </summary>
    private async Task SetExecutablePermissionsAsync(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        // Find all executable files (no extension or common Unix executable patterns)
        var executablePatterns = new[]
        {
            "adb",
            "adb" + _platformService.ExecutableExtension,
            "scrcpy",
            "scrcpy" + _platformService.ExecutableExtension,
            "scrcpy-server"
        };

        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
        {
            var fileName = Path.GetFileName(file);

            // Check if this is a known executable
            if (executablePatterns.Any(p => fileName.Equals(p, StringComparison.OrdinalIgnoreCase)))
            {
                // Set executable permission (no-op on Windows)
                await _platformService.SetExecutablePermissionAsync(file).ConfigureAwait(false);

                // Remove quarantine attribute (macOS only, no-op elsewhere)
                await _platformService.RemoveQuarantineAsync(file).ConfigureAwait(false);
            }
        }
    }
}
