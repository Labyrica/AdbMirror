using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using PhoneMirror.Core.Execution;
using PhoneMirror.Core.Services;

namespace PhoneMirror.Mcp;

/// <summary>
/// Implements all ADB-based MCP tools. Each tool maps to an ADB operation
/// and returns MCP-compatible content blocks.
/// </summary>
public sealed class AdbToolHandler
{
    private readonly IAdbService _adbService;
    private readonly IScreenshotService _screenshotService;
    private readonly ProcessRunner _processRunner;

    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan LongCommandTimeout = TimeSpan.FromSeconds(30);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public AdbToolHandler(
        IAdbService adbService,
        IScreenshotService screenshotService,
        ProcessRunner processRunner)
    {
        _adbService = adbService;
        _screenshotService = screenshotService;
        _processRunner = processRunner;
    }

    public List<ToolDefinition> GetToolDefinitions()
    {
        return new List<ToolDefinition>
        {
            MakeTool("screenshot",
                "Capture the Android device screen and return as a PNG image. Includes rotation and coordinate system metadata so you know how to interpret coordinates.",
                """
                {
                    "type": "object",
                    "properties": {
                        "device_serial": { "type": "string", "description": "Device serial number. Omit to auto-detect." }
                    }
                }
                """),

            MakeTool("crash_log",
                "Get crash traces from multiple sources: logcat crash buffer, dropbox crash reports, tombstones, and ANR traces. Much more comprehensive than basic logcat.",
                """
                {
                    "type": "object",
                    "properties": {
                        "package": { "type": "string", "description": "App package name. Omit to get all crashes." },
                        "last_n": { "type": "integer", "description": "Number of recent crashes to return. Default: 5", "default": 5 }
                    }
                }
                """),

            MakeTool("app_logs",
                "Get filtered logcat output for a specific app or the whole device.",
                """
                {
                    "type": "object",
                    "properties": {
                        "package": { "type": "string", "description": "App package name. Omit for all logs." },
                        "level": { "type": "string", "enum": ["verbose", "debug", "info", "warn", "error"], "description": "Minimum log level. Default: info", "default": "info" },
                        "last_seconds": { "type": "integer", "description": "Time window in seconds. Default: 30", "default": 30 },
                        "search": { "type": "string", "description": "Regex pattern to filter log messages." },
                        "max_lines": { "type": "integer", "description": "Max lines to return. Default: 200", "default": 200 }
                    }
                }
                """),

            MakeTool("tap",
                "Tap a UI element by its text, content description, resource ID, or visual coordinates. When using x/y coordinates, these are automatically translated from screenshot (visual) coordinates to device input coordinates, accounting for screen rotation. Prefer using text/content_desc/resource_id for reliability.",
                """
                {
                    "type": "object",
                    "properties": {
                        "text": { "type": "string", "description": "Find and tap element containing this text." },
                        "content_desc": { "type": "string", "description": "Find by accessibility content description." },
                        "resource_id": { "type": "string", "description": "Find by resource ID (e.g. 'com.app:id/button')." },
                        "x": { "type": "integer", "description": "Tap at visual X coordinate (as seen in screenshot). Auto-rotated to device coords." },
                        "y": { "type": "integer", "description": "Tap at visual Y coordinate (as seen in screenshot). Auto-rotated to device coords." },
                        "index": { "type": "integer", "description": "Which match to tap if multiple found (0-based). Default: 0", "default": 0 },
                        "long_press": { "type": "boolean", "description": "Long press instead of tap. Default: false", "default": false },
                        "double_tap": { "type": "boolean", "description": "Double tap. Default: false", "default": false }
                    }
                }
                """),

            MakeTool("swipe",
                "Perform a swipe gesture on the device screen. When using coordinates, they are automatically translated from screenshot (visual) coordinates to device input coordinates.",
                """
                {
                    "type": "object",
                    "properties": {
                        "start_x": { "type": "integer", "description": "Start X visual coordinate." },
                        "start_y": { "type": "integer", "description": "Start Y visual coordinate." },
                        "end_x": { "type": "integer", "description": "End X visual coordinate." },
                        "end_y": { "type": "integer", "description": "End Y visual coordinate." },
                        "duration_ms": { "type": "integer", "description": "Swipe duration in milliseconds. Default: 300", "default": 300 },
                        "direction": { "type": "string", "enum": ["up", "down", "left", "right"], "description": "Swipe direction from screen center. Use instead of coordinates." }
                    },
                    "required": []
                }
                """),

            MakeTool("pinch",
                "Perform a pinch (zoom in/out) gesture on the device screen. Uses two concurrent touch points.",
                """
                {
                    "type": "object",
                    "properties": {
                        "center_x": { "type": "integer", "description": "Center X coordinate of the pinch gesture. Default: screen center." },
                        "center_y": { "type": "integer", "description": "Center Y coordinate of the pinch gesture. Default: screen center." },
                        "action": { "type": "string", "enum": ["zoom_in", "zoom_out"], "description": "Pinch direction: zoom_in (spread fingers apart) or zoom_out (pinch fingers together). Default: zoom_in", "default": "zoom_in" },
                        "scale": { "type": "number", "description": "Scale factor: how far fingers move relative to screen size (0.1-0.5). Default: 0.25", "default": 0.25 },
                        "duration_ms": { "type": "integer", "description": "Gesture duration in milliseconds. Default: 500", "default": 500 }
                    }
                }
                """),

            MakeTool("input_text",
                "Type text on the device. Tries standard input first, falls back to clipboard paste for Flutter/React Native/WebView text fields. Use 'use_clipboard: true' to force clipboard mode.",
                """
                {
                    "type": "object",
                    "properties": {
                        "text": { "type": "string", "description": "Text to type." },
                        "use_clipboard": { "type": "boolean", "description": "Force clipboard paste mode (works with Flutter/RN/WebView). Default: false", "default": false }
                    },
                    "required": ["text"]
                }
                """),

            MakeTool("press_key",
                "Press a key on the device (back, home, enter, volume_up, volume_down, power, tab, etc.).",
                """
                {
                    "type": "object",
                    "properties": {
                        "key": { "type": "string", "description": "Key name: back, home, enter, menu, volume_up, volume_down, power, tab, delete, escape, app_switch" }
                    },
                    "required": ["key"]
                }
                """),

            MakeTool("ui_tree",
                "Get the current UI element hierarchy from the device screen. Bounds are in the device's native coordinate system. Includes rotation metadata.",
                """
                {
                    "type": "object",
                    "properties": {
                        "clickable_only": { "type": "boolean", "description": "Only return interactive/clickable elements. Default: false", "default": false },
                        "simplified": { "type": "boolean", "description": "Return compact format (text + bounds only). Default: true", "default": true }
                    }
                }
                """),

            MakeTool("app_status",
                "Check if an app is running, its state (foreground/background/stopped), memory usage, and PID.",
                """
                {
                    "type": "object",
                    "properties": {
                        "package": { "type": "string", "description": "App package name." }
                    },
                    "required": ["package"]
                }
                """),

            MakeTool("device_info",
                "Get device details: model, Android version, screen size, rotation, battery, storage.",
                """
                {
                    "type": "object",
                    "properties": {}
                }
                """),

            MakeTool("list_packages",
                "List installed packages on the device, optionally filtered.",
                """
                {
                    "type": "object",
                    "properties": {
                        "filter": { "type": "string", "description": "Filter packages containing this string." },
                        "third_party_only": { "type": "boolean", "description": "Only show third-party (non-system) apps. Default: true", "default": true }
                    }
                }
                """),

            MakeTool("install_apk",
                "Install an APK file on the device.",
                """
                {
                    "type": "object",
                    "properties": {
                        "apk_path": { "type": "string", "description": "Local path to the APK file." },
                        "grant_permissions": { "type": "boolean", "description": "Auto-grant runtime permissions. Default: true", "default": true }
                    },
                    "required": ["apk_path"]
                }
                """),

            MakeTool("launch_app",
                "Launch an app by package name.",
                """
                {
                    "type": "object",
                    "properties": {
                        "package": { "type": "string", "description": "App package name to launch." }
                    },
                    "required": ["package"]
                }
                """),

            MakeTool("force_stop",
                "Force stop an app by package name.",
                """
                {
                    "type": "object",
                    "properties": {
                        "package": { "type": "string", "description": "App package name to stop." }
                    },
                    "required": ["package"]
                }
                """),

            MakeTool("shell",
                "Run an arbitrary ADB shell command on the device. Use for advanced operations not covered by other tools.",
                """
                {
                    "type": "object",
                    "properties": {
                        "command": { "type": "string", "description": "Shell command to execute on the device." }
                    },
                    "required": ["command"]
                }
                """),

            MakeTool("pixel_color",
                "Sample the color of a pixel at given coordinates from a fresh screenshot. Coordinates are visual (as seen in the screenshot image), auto-translated for rotation.",
                """
                {
                    "type": "object",
                    "properties": {
                        "x": { "type": "integer", "description": "Visual X coordinate (as seen in screenshot)." },
                        "y": { "type": "integer", "description": "Visual Y coordinate (as seen in screenshot)." }
                    },
                    "required": ["x", "y"]
                }
                """),

            MakeTool("performance_snapshot",
                "Capture app performance metrics: frame rendering stats, memory usage, CPU usage, and GPU rendering info.",
                """
                {
                    "type": "object",
                    "properties": {
                        "package": { "type": "string", "description": "App package name." }
                    },
                    "required": ["package"]
                }
                """),

            MakeTool("screen_record",
                "Record the device screen for a duration and save to a local file.",
                """
                {
                    "type": "object",
                    "properties": {
                        "duration_seconds": { "type": "integer", "description": "Recording duration in seconds. Default: 10, Max: 180", "default": 10 },
                        "output_path": { "type": "string", "description": "Local file path to save the recording. Default: auto-generated temp path." }
                    }
                }
                """),
        };
    }

    public async Task<ToolCallResult> ExecuteAsync(
        string toolName,
        JsonElement? arguments,
        CancellationToken ct)
    {
        try
        {
            return toolName switch
            {
                "screenshot" => await ScreenshotAsync(arguments, ct),
                "crash_log" => await CrashLogAsync(arguments, ct),
                "app_logs" => await AppLogsAsync(arguments, ct),
                "tap" => await TapAsync(arguments, ct),
                "swipe" => await SwipeAsync(arguments, ct),
                "pinch" => await PinchAsync(arguments, ct),
                "input_text" => await InputTextAsync(arguments, ct),
                "press_key" => await PressKeyAsync(arguments, ct),
                "ui_tree" => await UiTreeAsync(arguments, ct),
                "app_status" => await AppStatusAsync(arguments, ct),
                "device_info" => await DeviceInfoAsync(arguments, ct),
                "list_packages" => await ListPackagesAsync(arguments, ct),
                "install_apk" => await InstallApkAsync(arguments, ct),
                "launch_app" => await LaunchAppAsync(arguments, ct),
                "force_stop" => await ForceStopAsync(arguments, ct),
                "shell" => await ShellAsync(arguments, ct),
                "pixel_color" => await PixelColorAsync(arguments, ct),
                "performance_snapshot" => await PerformanceSnapshotAsync(arguments, ct),
                "screen_record" => await ScreenRecordAsync(arguments, ct),
                _ => ErrorResult($"Unknown tool: {toolName}")
            };
        }
        catch (Exception ex)
        {
            return ErrorResult($"Tool execution failed: {ex.Message}");
        }
    }

    // ─── Helpers ───

    private string? GetString(JsonElement? args, string key)
    {
        if (args == null) return null;
        return args.Value.TryGetProperty(key, out var val) && val.ValueKind == JsonValueKind.String
            ? val.GetString()
            : null;
    }

    private int GetInt(JsonElement? args, string key, int defaultValue)
    {
        if (args == null) return defaultValue;
        return args.Value.TryGetProperty(key, out var val) && val.ValueKind == JsonValueKind.Number
            ? val.GetInt32()
            : defaultValue;
    }

    private bool GetBool(JsonElement? args, string key, bool defaultValue)
    {
        if (args == null) return defaultValue;
        if (args.Value.TryGetProperty(key, out var val))
        {
            if (val.ValueKind == JsonValueKind.True) return true;
            if (val.ValueKind == JsonValueKind.False) return false;
        }
        return defaultValue;
    }

    private async Task<string?> GetDeviceSerialAsync(JsonElement? args)
    {
        var serial = GetString(args, "device_serial");
        if (!string.IsNullOrEmpty(serial)) return serial;

        var (state, device) = await _adbService.GetHighLevelStateAsync();
        return device?.Serial;
    }

    private async Task<string?> GetAdbPathAsync()
    {
        if (!await _adbService.IsAvailableAsync())
            return null;
        return _adbService.AdbPath;
    }

    private async Task<ProcessResult> RunAdbAsync(string args, CancellationToken ct, TimeSpan? timeout = null)
    {
        var adbPath = await GetAdbPathAsync();
        if (adbPath == null)
            return new ProcessResult(-1, "", "ADB not available");
        return await _processRunner.RunAsync(adbPath, args, timeout ?? CommandTimeout, ct);
    }

    private async Task<ProcessResult> RunAdbShellAsync(
        string? serial, string shellCommand, CancellationToken ct, TimeSpan? timeout = null)
    {
        var serialArg = string.IsNullOrEmpty(serial) ? "" : $"-s {serial} ";
        return await RunAdbAsync($"{serialArg}shell {shellCommand}", ct, timeout);
    }

    private static ToolCallResult TextResult(string text)
    {
        return new ToolCallResult
        {
            Content = new List<ContentBlock> { new TextContent { Text = text } }
        };
    }

    private static ToolCallResult ErrorResult(string message)
    {
        return new ToolCallResult
        {
            Content = new List<ContentBlock> { new TextContent { Text = message } },
            IsError = true
        };
    }

    private static ToolDefinition MakeTool(string name, string description, string schemaJson)
    {
        return new ToolDefinition
        {
            Name = name,
            Description = description,
            InputSchema = JsonDocument.Parse(schemaJson).RootElement.Clone()
        };
    }

    private async Task<string?> GetForegroundPackageAsync(string? serial, CancellationToken ct)
    {
        var result = await RunAdbShellAsync(serial,
            "dumpsys activity activities | grep -E 'mResumedActivity|mCurrentFocus'",
            ct);
        if (!result.Success) return null;

        // Parse "com.package.name/.ActivityName" pattern
        var match = Regex.Match(result.StandardOutput, @"(\w+(?:\.\w+)+)/");
        return match.Success ? match.Groups[1].Value : null;
    }

    // ─── Rotation & Coordinate Helpers ───

    /// <summary>
    /// Gets the current display rotation (0=portrait, 1=landscape CCW, 2=inverted, 3=landscape CW).
    /// </summary>
    private async Task<int> GetRotationAsync(string serial, CancellationToken ct)
    {
        var result = await RunAdbShellAsync(serial, "dumpsys input | grep SurfaceOrientation", ct);
        if (result.Success)
        {
            var match = Regex.Match(result.StandardOutput, @"SurfaceOrientation:\s*(\d)");
            if (match.Success)
                return int.Parse(match.Groups[1].Value);
        }
        // Fallback
        var result2 = await RunAdbShellAsync(serial, "dumpsys display | grep mCurrentOrientation", ct);
        if (result2.Success)
        {
            var match = Regex.Match(result2.StandardOutput, @"mCurrentOrientation=(\d)");
            if (match.Success)
                return int.Parse(match.Groups[1].Value);
        }
        return 0;
    }

    /// <summary>
    /// Gets the physical (portrait) screen dimensions.
    /// </summary>
    private async Task<(int Width, int Height)> GetPhysicalSizeAsync(string serial, CancellationToken ct)
    {
        var result = await RunAdbShellAsync(serial, "wm size", ct);
        if (result.Success)
        {
            var match = Regex.Match(result.StandardOutput, @"(\d+)x(\d+)");
            if (match.Success)
            {
                var w = int.Parse(match.Groups[1].Value);
                var h = int.Parse(match.Groups[2].Value);
                // wm size always returns portrait dimensions (shorter x longer)
                return (Math.Min(w, h), Math.Max(w, h));
            }
        }
        return (1080, 1920);
    }

    /// <summary>
    /// Transforms visual (screenshot) coordinates to device input coordinates based on rotation.
    /// </summary>
    private static (int X, int Y) VisualToInput(int vx, int vy, int rotation, int physW, int physH)
    {
        return rotation switch
        {
            // Portrait: no transform
            0 => (vx, vy),
            // Landscape 90° CCW: visual is (physH x physW), input is portrait
            1 => (physW - vy, vx),
            // Inverted portrait: visual is (physW x physH)
            2 => (physW - vx, physH - vy),
            // Landscape 90° CW: visual is (physH x physW), input is portrait
            3 => (vy, physH - vx),
            _ => (vx, vy)
        };
    }

    /// <summary>
    /// Gets the visual (screenshot) screen dimensions based on rotation.
    /// </summary>
    private static (int Width, int Height) GetVisualSize(int physW, int physH, int rotation)
    {
        return rotation switch
        {
            1 or 3 => (physH, physW), // Landscape: swap dimensions
            _ => (physW, physH)       // Portrait or inverted
        };
    }

    private string RotationLabel(int rotation) => rotation switch
    {
        0 => "portrait",
        1 => "landscape (90° CCW)",
        2 => "portrait (inverted)",
        3 => "landscape (90° CW)",
        _ => $"unknown ({rotation})"
    };

    // ─── Tool Implementations ───

    private async Task<ToolCallResult> ScreenshotAsync(JsonElement? args, CancellationToken ct)
    {
        var serial = await GetDeviceSerialAsync(args);
        if (serial == null)
            return ErrorResult("No device connected.");

        var (pngData, error) = await _screenshotService.CaptureAsync(serial, ct);
        if (pngData == null)
            return ErrorResult($"Screenshot failed: {error}");

        // Get rotation and size metadata
        var rotation = await GetRotationAsync(serial, ct);
        var (physW, physH) = await GetPhysicalSizeAsync(serial, ct);
        var (visW, visH) = GetVisualSize(physW, physH, rotation);

        var metadata = $"Orientation: {RotationLabel(rotation)} | Visual size: {visW}x{visH} | Physical size: {physW}x{physH}";
        if (rotation != 0)
        {
            metadata += "\nNOTE: Device is rotated. When using tap/swipe/pixel_color with x/y coordinates, provide visual coordinates as seen in this screenshot — they will be auto-translated to device input coordinates.";
        }

        return new ToolCallResult
        {
            Content = new List<ContentBlock>
            {
                new TextContent { Text = metadata },
                new ImageContent
                {
                    Data = Convert.ToBase64String(pngData),
                    MimeType = "image/png"
                }
            }
        };
    }

    private async Task<ToolCallResult> CrashLogAsync(JsonElement? args, CancellationToken ct)
    {
        var serial = await GetDeviceSerialAsync(args);
        if (serial == null)
            return ErrorResult("No device connected.");

        var package = GetString(args, "package");
        var lastN = GetInt(args, "last_n", 5);
        var output = new StringBuilder();
        var foundAnything = false;

        // ── Source 1: Logcat crash buffer (dedicated crash buffer) ──
        var crashBufResult = await RunAdbShellAsync(serial, "logcat -b crash -d", ct, LongCommandTimeout);
        if (crashBufResult.Success && !string.IsNullOrWhiteSpace(crashBufResult.StandardOutput))
        {
            var crashes = ExtractCrashBlocks(crashBufResult.StandardOutput, package);
            if (crashes.Count > 0)
            {
                foundAnything = true;
                output.AppendLine($"=== Crash Buffer ({crashes.Count} crash(es)) ===");
                foreach (var crash in crashes.TakeLast(lastN))
                {
                    output.AppendLine(crash.TrimEnd());
                    output.AppendLine();
                }
            }
        }

        // ── Source 2: Logcat main buffer for FATAL EXCEPTION / Fatal signal ──
        var mainResult = await RunAdbShellAsync(serial,
            "logcat -b main -d -e 'FATAL EXCEPTION|Fatal signal|CRASH|died|Process.*has died'", ct, LongCommandTimeout);
        if (mainResult.Success && !string.IsNullOrWhiteSpace(mainResult.StandardOutput))
        {
            var lines = mainResult.StandardOutput.Split('\n')
                .Where(l => !string.IsNullOrWhiteSpace(l));
            if (!string.IsNullOrEmpty(package))
                lines = lines.Where(l => l.Contains(package, StringComparison.OrdinalIgnoreCase));
            var mainCrashes = lines.TakeLast(lastN * 10).ToList();
            if (mainCrashes.Count > 0)
            {
                foundAnything = true;
                output.AppendLine($"=== Main Log Crashes ({mainCrashes.Count} line(s)) ===");
                foreach (var line in mainCrashes.TakeLast(lastN * 5))
                    output.AppendLine(line);
                output.AppendLine();
            }
        }

        // ── Source 3: Dropbox crash reports (system-level crash storage) ──
        var dropboxResult = await RunAdbShellAsync(serial,
            "dumpsys dropbox --print -t 3600000", ct, LongCommandTimeout);
        if (dropboxResult.Success && !string.IsNullOrWhiteSpace(dropboxResult.StandardOutput))
        {
            var dropboxLines = dropboxResult.StandardOutput.Split('\n');
            var relevantBlocks = new List<string>();
            var currentBlock = new StringBuilder();
            var inRelevantBlock = false;

            foreach (var line in dropboxLines)
            {
                if (line.Contains("data_app_crash") || line.Contains("data_app_anr") ||
                    line.Contains("system_app_crash") || line.Contains("FATAL") ||
                    line.Contains("system_server_crash"))
                {
                    if (currentBlock.Length > 0 && inRelevantBlock)
                        relevantBlocks.Add(currentBlock.ToString());
                    currentBlock.Clear();
                    inRelevantBlock = true;
                }

                if (inRelevantBlock)
                    currentBlock.AppendLine(line);

                // Limit block size
                if (currentBlock.Length > 2000)
                {
                    currentBlock.AppendLine("... (truncated)");
                    inRelevantBlock = false;
                }
            }
            if (currentBlock.Length > 0 && inRelevantBlock)
                relevantBlocks.Add(currentBlock.ToString());

            if (!string.IsNullOrEmpty(package))
                relevantBlocks = relevantBlocks.Where(b => b.Contains(package, StringComparison.OrdinalIgnoreCase)).ToList();

            if (relevantBlocks.Count > 0)
            {
                foundAnything = true;
                output.AppendLine($"=== Dropbox Reports ({relevantBlocks.Count}) ===");
                foreach (var block in relevantBlocks.TakeLast(lastN))
                {
                    output.AppendLine(block.TrimEnd());
                    output.AppendLine();
                }
            }
        }

        // ── Source 4: ANR traces ──
        var anrResult = await RunAdbShellAsync(serial, "logcat -b events -d | grep -i anr", ct);
        if (anrResult.Success && !string.IsNullOrWhiteSpace(anrResult.StandardOutput))
        {
            var anrLines = anrResult.StandardOutput.Split('\n')
                .Where(l => !string.IsNullOrWhiteSpace(l));
            if (!string.IsNullOrEmpty(package))
                anrLines = anrLines.Where(l => l.Contains(package, StringComparison.OrdinalIgnoreCase));
            var anrList = anrLines.TakeLast(lastN).ToList();
            if (anrList.Count > 0)
            {
                foundAnything = true;
                output.AppendLine($"=== ANR Events ({anrList.Count}) ===");
                foreach (var line in anrList)
                    output.AppendLine(line);
                output.AppendLine();
            }
        }

        // ── Source 5: Tombstones (native crashes) ──
        var tombResult = await RunAdbShellAsync(serial,
            "ls -lt /data/tombstones/ 2>/dev/null || echo 'no access'", ct);
        if (tombResult.Success && !tombResult.StandardOutput.Contains("no access") &&
            !tombResult.StandardOutput.Contains("No such file"))
        {
            var tombFiles = tombResult.StandardOutput.Split('\n')
                .Where(l => l.Contains("tombstone_"))
                .Take(lastN)
                .ToList();
            if (tombFiles.Count > 0)
            {
                foundAnything = true;
                output.AppendLine($"=== Tombstones (native crashes: {tombFiles.Count}) ===");
                foreach (var tf in tombFiles)
                    output.AppendLine(tf.Trim());

                // Try to read the most recent tombstone header
                var latestTomb = await RunAdbShellAsync(serial,
                    "cat /data/tombstones/tombstone_00 2>/dev/null | head -30", ct);
                if (latestTomb.Success && !string.IsNullOrWhiteSpace(latestTomb.StandardOutput))
                {
                    output.AppendLine("\n--- Latest tombstone (first 30 lines) ---");
                    output.AppendLine(latestTomb.StandardOutput.TrimEnd());
                }
                output.AppendLine();
            }
        }

        // ── Source 6: App-specific last crash via ActivityManager ──
        if (!string.IsNullOrEmpty(package))
        {
            var amResult = await RunAdbShellAsync(serial,
                $"dumpsys activity processes | grep -A 20 '{package}'", ct);
            if (amResult.Success && amResult.StandardOutput.Contains("crash"))
            {
                foundAnything = true;
                output.AppendLine("=== Activity Manager ===");
                output.AppendLine(amResult.StandardOutput.TrimEnd());
                output.AppendLine();
            }
        }

        if (!foundAnything)
        {
            output.AppendLine("No crashes found across any source.");
            output.AppendLine("Sources checked: logcat crash buffer, logcat main, dropbox, events/ANR, tombstones" +
                (string.IsNullOrEmpty(package) ? "" : $", activity manager for {package}"));
            output.AppendLine("Tip: If the app just crashed, the logcat buffer may have rotated. Try 'app_logs' with level=error for recent errors.");
        }

        return TextResult(output.ToString().TrimEnd());
    }

    /// <summary>
    /// Extracts individual crash blocks from logcat output.
    /// </summary>
    private static List<string> ExtractCrashBlocks(string logcatOutput, string? packageFilter)
    {
        var crashes = new List<string>();
        var currentCrash = new StringBuilder();
        var inCrash = false;

        foreach (var line in logcatOutput.Split('\n'))
        {
            if (line.Contains("FATAL EXCEPTION") || line.Contains("Fatal signal") ||
                line.Contains("beginning of crash") || line.Contains("Build fingerprint"))
            {
                if (inCrash && currentCrash.Length > 0)
                {
                    crashes.Add(currentCrash.ToString());
                    currentCrash.Clear();
                }
                inCrash = true;
            }

            if (inCrash)
            {
                currentCrash.AppendLine(line);
            }
        }

        if (currentCrash.Length > 0)
            crashes.Add(currentCrash.ToString());

        if (!string.IsNullOrEmpty(packageFilter))
            crashes = crashes.Where(c => c.Contains(packageFilter, StringComparison.OrdinalIgnoreCase)).ToList();

        return crashes;
    }

    private async Task<ToolCallResult> AppLogsAsync(JsonElement? args, CancellationToken ct)
    {
        var serial = await GetDeviceSerialAsync(args);
        if (serial == null)
            return ErrorResult("No device connected.");

        var package = GetString(args, "package");
        var level = GetString(args, "level") ?? "info";
        var lastSeconds = GetInt(args, "last_seconds", 30);
        var search = GetString(args, "search");
        var maxLines = GetInt(args, "max_lines", 200);

        var levelFilter = level.ToLowerInvariant() switch
        {
            "verbose" => "*:V",
            "debug" => "*:D",
            "info" => "*:I",
            "warn" => "*:W",
            "error" => "*:E",
            _ => "*:I"
        };

        string logcatArgs;
        if (!string.IsNullOrEmpty(package))
        {
            // Get PID for the package
            var pidResult = await RunAdbShellAsync(serial, $"pidof {package}", ct);
            var pid = pidResult.StandardOutput.Trim();

            if (!string.IsNullOrEmpty(pid) && int.TryParse(pid, out _))
            {
                logcatArgs = $"logcat -d --pid={pid} -t {lastSeconds}.0 {levelFilter}";
            }
            else
            {
                // App not running, get recent logs and grep
                logcatArgs = $"logcat -d -t {lastSeconds}.0 {levelFilter}";
            }
        }
        else
        {
            logcatArgs = $"logcat -d -t {lastSeconds}.0 {levelFilter}";
        }

        var result = await RunAdbShellAsync(serial, logcatArgs, ct, LongCommandTimeout);
        if (!result.Success)
            return ErrorResult($"Logcat failed: {result.StandardError}");

        var lines = result.StandardOutput.Split('\n')
            .Where(l => !string.IsNullOrWhiteSpace(l));

        // Filter by package name in log lines if PID approach didn't work
        if (!string.IsNullOrEmpty(package))
        {
            lines = lines.Where(l => l.Contains(package, StringComparison.OrdinalIgnoreCase));
        }

        // Filter by search pattern
        if (!string.IsNullOrEmpty(search))
        {
            try
            {
                var regex = new Regex(search, RegexOptions.IgnoreCase);
                lines = lines.Where(l => regex.IsMatch(l));
            }
            catch (RegexParseException)
            {
                // Fall back to simple contains
                lines = lines.Where(l => l.Contains(search, StringComparison.OrdinalIgnoreCase));
            }
        }

        var output = lines.TakeLast(maxLines).ToList();

        if (output.Count == 0)
            return TextResult("No matching log entries found.");

        return TextResult(string.Join('\n', output));
    }

    private async Task<ToolCallResult> TapAsync(JsonElement? args, CancellationToken ct)
    {
        var serial = await GetDeviceSerialAsync(args);
        if (serial == null)
            return ErrorResult("No device connected.");

        var text = GetString(args, "text");
        var contentDesc = GetString(args, "content_desc");
        var resourceId = GetString(args, "resource_id");
        var x = args?.TryGetProperty("x", out var xVal) == true && xVal.ValueKind == JsonValueKind.Number
            ? (int?)xVal.GetInt32() : null;
        var y = args?.TryGetProperty("y", out var yVal) == true && yVal.ValueKind == JsonValueKind.Number
            ? (int?)yVal.GetInt32() : null;
        var index = GetInt(args, "index", 0);
        var longPress = GetBool(args, "long_press", false);
        var doubleTap = GetBool(args, "double_tap", false);

        int tapX, tapY;
        string? elementInfo = null;
        var wasTransformed = false;

        if (x.HasValue && y.HasValue)
        {
            // Auto-transform visual coordinates to device input coordinates
            var rotation = await GetRotationAsync(serial, ct);
            if (rotation != 0)
            {
                var (physW, physH) = await GetPhysicalSizeAsync(serial, ct);
                var (inputX, inputY) = VisualToInput(x.Value, y.Value, rotation, physW, physH);
                tapX = inputX;
                tapY = inputY;
                wasTransformed = true;
                elementInfo = $"Visual coords ({x.Value},{y.Value}) → Device coords ({tapX},{tapY}) [rotation={rotation}]";
            }
            else
            {
                tapX = x.Value;
                tapY = y.Value;
            }
        }
        else if (!string.IsNullOrEmpty(text) || !string.IsNullOrEmpty(contentDesc) || !string.IsNullOrEmpty(resourceId))
        {
            // Dump UI hierarchy and find element (bounds are already in device coords)
            var uiXml = await DumpUiHierarchyAsync(serial, ct);
            if (uiXml == null)
                return ErrorResult("Failed to dump UI hierarchy. The screen may be in a transition state — try again.");

            var elements = ParseUiElements(uiXml);
            var matches = elements.Where(e =>
            {
                if (!string.IsNullOrEmpty(text) &&
                    (e.Text?.Contains(text, StringComparison.OrdinalIgnoreCase) == true))
                    return true;
                if (!string.IsNullOrEmpty(contentDesc) &&
                    (e.ContentDesc?.Contains(contentDesc, StringComparison.OrdinalIgnoreCase) == true))
                    return true;
                if (!string.IsNullOrEmpty(resourceId) &&
                    (e.ResourceId?.Contains(resourceId, StringComparison.OrdinalIgnoreCase) == true))
                    return true;
                return false;
            }).ToList();

            if (matches.Count == 0)
            {
                var searchTerm = text ?? contentDesc ?? resourceId;
                return ErrorResult(
                    $"No element found matching '{searchTerm}'. " +
                    "Use ui_tree to see what's on screen.");
            }

            if (index >= matches.Count)
                return ErrorResult($"Index {index} out of range. Found {matches.Count} matches.");

            var target = matches[index];
            tapX = (target.Bounds[0] + target.Bounds[2]) / 2;
            tapY = (target.Bounds[1] + target.Bounds[3]) / 2;
            elementInfo = $"Element: text=\"{target.Text}\", class={target.ClassName}, bounds=[{string.Join(",", target.Bounds)}]";
        }
        else
        {
            return ErrorResult("Provide either text/content_desc/resource_id to find an element, or x/y coordinates.");
        }

        // Execute the tap
        string tapCommand;
        if (longPress)
        {
            tapCommand = $"input swipe {tapX} {tapY} {tapX} {tapY} 1000";
        }
        else if (doubleTap)
        {
            tapCommand = $"input tap {tapX} {tapY} && sleep 0.05 && input tap {tapX} {tapY}";
        }
        else
        {
            tapCommand = $"input tap {tapX} {tapY}";
        }

        var tapResult = await RunAdbShellAsync(serial, tapCommand, ct);
        if (!tapResult.Success)
            return ErrorResult($"Tap failed: {tapResult.StandardError}");

        var tapType = longPress ? "Long pressed" : doubleTap ? "Double tapped" : "Tapped";
        var response = $"{tapType} at ({tapX}, {tapY})";
        if (elementInfo != null)
            response += $"\n{elementInfo}";

        return TextResult(response);
    }

    private async Task<ToolCallResult> SwipeAsync(JsonElement? args, CancellationToken ct)
    {
        var serial = await GetDeviceSerialAsync(args);
        if (serial == null)
            return ErrorResult("No device connected.");

        var duration = GetInt(args, "duration_ms", 300);
        var direction = GetString(args, "direction");
        var rotation = await GetRotationAsync(serial, ct);
        var (physW, physH) = await GetPhysicalSizeAsync(serial, ct);

        int startX, startY, endX, endY;

        if (!string.IsNullOrEmpty(direction))
        {
            // Use visual screen dimensions for direction-based swipe
            var (visW, visH) = GetVisualSize(physW, physH, rotation);
            var cx = visW / 2;
            var cy = visH / 2;
            var swipeLen = Math.Min(visW, visH) / 3;

            int vsx, vsy, vex, vey;
            (vsx, vsy, vex, vey) = direction.ToLowerInvariant() switch
            {
                "up" => (cx, cy + swipeLen, cx, cy - swipeLen),
                "down" => (cx, cy - swipeLen, cx, cy + swipeLen),
                "left" => (cx + swipeLen, cy, cx - swipeLen, cy),
                "right" => (cx - swipeLen, cy, cx + swipeLen, cy),
                _ => (cx, cy + swipeLen, cx, cy - swipeLen)
            };

            // Transform visual to input coords
            (startX, startY) = VisualToInput(vsx, vsy, rotation, physW, physH);
            (endX, endY) = VisualToInput(vex, vey, rotation, physW, physH);
        }
        else
        {
            // Transform visual coordinates to input coordinates
            var vsx = GetInt(args, "start_x", 0);
            var vsy = GetInt(args, "start_y", 0);
            var vex = GetInt(args, "end_x", 0);
            var vey = GetInt(args, "end_y", 0);

            (startX, startY) = VisualToInput(vsx, vsy, rotation, physW, physH);
            (endX, endY) = VisualToInput(vex, vey, rotation, physW, physH);
        }

        var result = await RunAdbShellAsync(serial,
            $"input swipe {startX} {startY} {endX} {endY} {duration}", ct);

        if (!result.Success)
            return ErrorResult($"Swipe failed: {result.StandardError}");

        return TextResult($"Swiped from ({startX},{startY}) to ({endX},{endY}) over {duration}ms");
    }

    private async Task<ToolCallResult> PinchAsync(JsonElement? args, CancellationToken ct)
    {
        var serial = await GetDeviceSerialAsync(args);
        if (serial == null)
            return ErrorResult("No device connected.");

        var action = GetString(args, "action") ?? "zoom_in";
        var scale = 0.25;
        if (args?.TryGetProperty("scale", out var scaleVal) == true && scaleVal.ValueKind == JsonValueKind.Number)
            scale = Math.Clamp(scaleVal.GetDouble(), 0.1, 0.5);
        var durationMs = GetInt(args, "duration_ms", 500);

        var rotation = await GetRotationAsync(serial, ct);
        var (physW, physH) = await GetPhysicalSizeAsync(serial, ct);
        var (visW, visH) = GetVisualSize(physW, physH, rotation);

        // Default center
        var cx = GetInt(args, "center_x", visW / 2);
        var cy = GetInt(args, "center_y", visH / 2);

        var offset = (int)(Math.Min(visW, visH) * scale);

        int f1sx, f1sy, f1ex, f1ey; // finger 1
        int f2sx, f2sy, f2ex, f2ey; // finger 2

        if (action == "zoom_in")
        {
            // Fingers start close, move apart
            f1sx = cx - 20; f1sy = cy - 20; f1ex = cx - offset; f1ey = cy - offset;
            f2sx = cx + 20; f2sy = cy + 20; f2ex = cx + offset; f2ey = cy + offset;
        }
        else
        {
            // Fingers start apart, move together
            f1sx = cx - offset; f1sy = cy - offset; f1ex = cx - 20; f1ey = cy - 20;
            f2sx = cx + offset; f2sy = cy + offset; f2ex = cx + 20; f2ey = cy + 20;
        }

        // Transform to input coordinates
        var (i1sx, i1sy) = VisualToInput(f1sx, f1sy, rotation, physW, physH);
        var (i1ex, i1ey) = VisualToInput(f1ex, f1ey, rotation, physW, physH);
        var (i2sx, i2sy) = VisualToInput(f2sx, f2sy, rotation, physW, physH);
        var (i2ex, i2ey) = VisualToInput(f2ex, f2ey, rotation, physW, physH);

        // Run two concurrent swipes to simulate pinch
        // This works on many devices but is not a true multi-touch event
        var pinchCmd = $"(input swipe {i1sx} {i1sy} {i1ex} {i1ey} {durationMs} &) ; " +
                       $"input swipe {i2sx} {i2sy} {i2ex} {i2ey} {durationMs}";

        var result = await RunAdbShellAsync(serial, pinchCmd, ct,
            TimeSpan.FromSeconds(durationMs / 1000.0 + 5));

        if (!result.Success)
            return ErrorResult($"Pinch failed: {result.StandardError}");

        return TextResult(
            $"Pinch {action.Replace("_", " ")} at ({cx},{cy}) with scale {scale:F2}\n" +
            $"Finger 1: ({f1sx},{f1sy}) → ({f1ex},{f1ey})\n" +
            $"Finger 2: ({f2sx},{f2sy}) → ({f2ex},{f2ey})\n" +
            "Note: Uses concurrent swipes. If pinch isn't recognized, try a larger scale value or slower duration.");
    }

    private async Task<ToolCallResult> InputTextAsync(JsonElement? args, CancellationToken ct)
    {
        var serial = await GetDeviceSerialAsync(args);
        if (serial == null)
            return ErrorResult("No device connected.");

        var text = GetString(args, "text");
        if (string.IsNullOrEmpty(text))
            return ErrorResult("Missing required parameter: text");

        var useClipboard = GetBool(args, "use_clipboard", false);

        if (!useClipboard)
        {
            // Try standard input first
            var escaped = text.Replace("\\", "\\\\")
                             .Replace(" ", "%s")
                             .Replace("\"", "\\\"")
                             .Replace("'", "\\'")
                             .Replace("&", "\\&")
                             .Replace("<", "\\<")
                             .Replace(">", "\\>")
                             .Replace(";", "\\;")
                             .Replace("(", "\\(")
                             .Replace(")", "\\)");

            var result = await RunAdbShellAsync(serial, $"input text \"{escaped}\"", ct);
            if (result.Success)
                return TextResult($"Typed: {text}");

            // Fall through to clipboard method if standard input failed
        }

        // Clipboard paste method: works with Flutter, React Native, WebView text fields
        // 1. Set clipboard content via am broadcast
        var clipEscaped = text.Replace("'", "'\\''");
        var clipResult = await RunAdbShellAsync(serial,
            $"am broadcast -a clipper.set -e text '{clipEscaped}' 2>/dev/null; " +
            $"service call clipboard 2 i32 1 i64 0 s16 '{clipEscaped}' 2>/dev/null; " +
            $"input keyevent 279 2>/dev/null",  // KEYCODE_PASTE (API 24+)
            ct);

        // Also try the content provider approach as fallback
        if (!clipResult.Success || clipResult.StandardOutput.Contains("Error"))
        {
            // Alternative: use input text with broadcast receiver approach
            await RunAdbShellAsync(serial,
                $"am broadcast -a ADB_INPUT_TEXT --es msg '{clipEscaped}' 2>/dev/null", ct);
        }

        // Try paste via Ctrl+V keyevent combo
        await RunAdbShellAsync(serial, "input keyevent 279", ct); // KEYCODE_PASTE

        return TextResult($"Typed via clipboard paste: {text}\n" +
            "Note: If text didn't appear, the app may not support clipboard paste. " +
            "Try tapping the text field first, then calling input_text again.");
    }

    private async Task<ToolCallResult> PressKeyAsync(JsonElement? args, CancellationToken ct)
    {
        var serial = await GetDeviceSerialAsync(args);
        if (serial == null)
            return ErrorResult("No device connected.");

        var key = GetString(args, "key");
        if (string.IsNullOrEmpty(key))
            return ErrorResult("Missing required parameter: key");

        var keycode = key.ToLowerInvariant() switch
        {
            "back" => "KEYCODE_BACK",
            "home" => "KEYCODE_HOME",
            "enter" => "KEYCODE_ENTER",
            "menu" => "KEYCODE_MENU",
            "volume_up" => "KEYCODE_VOLUME_UP",
            "volume_down" => "KEYCODE_VOLUME_DOWN",
            "power" => "KEYCODE_POWER",
            "tab" => "KEYCODE_TAB",
            "delete" => "KEYCODE_DEL",
            "escape" => "KEYCODE_ESCAPE",
            "app_switch" => "KEYCODE_APP_SWITCH",
            "dpad_up" => "KEYCODE_DPAD_UP",
            "dpad_down" => "KEYCODE_DPAD_DOWN",
            "dpad_left" => "KEYCODE_DPAD_LEFT",
            "dpad_right" => "KEYCODE_DPAD_RIGHT",
            _ => $"KEYCODE_{key.ToUpperInvariant()}"
        };

        var result = await RunAdbShellAsync(serial, $"input keyevent {keycode}", ct);
        if (!result.Success)
            return ErrorResult($"Key press failed: {result.StandardError}");

        return TextResult($"Pressed: {key}");
    }

    private async Task<ToolCallResult> UiTreeAsync(JsonElement? args, CancellationToken ct)
    {
        var serial = await GetDeviceSerialAsync(args);
        if (serial == null)
            return ErrorResult("No device connected.");

        var clickableOnly = GetBool(args, "clickable_only", false);
        var simplified = GetBool(args, "simplified", true);

        var uiXml = await DumpUiHierarchyAsync(serial, ct);
        if (uiXml == null)
            return ErrorResult("Failed to dump UI hierarchy.");

        var elements = ParseUiElements(uiXml);

        if (clickableOnly)
            elements = elements.Where(e => e.Clickable).ToList();

        // Add rotation metadata
        var rotation = await GetRotationAsync(serial, ct);
        var (physW, physH) = await GetPhysicalSizeAsync(serial, ct);

        var header = $"Orientation: {RotationLabel(rotation)} | Physical: {physW}x{physH}";
        if (rotation != 0)
        {
            header += "\nNOTE: UI tree bounds are in the device's native coordinate system. " +
                      "Use tap with text/content_desc/resource_id to tap elements reliably.";
        }

        if (simplified)
        {
            var sb = new StringBuilder();
            sb.AppendLine(header);
            sb.AppendLine();
            foreach (var e in elements)
            {
                if (string.IsNullOrEmpty(e.Text) && string.IsNullOrEmpty(e.ContentDesc) &&
                    string.IsNullOrEmpty(e.ResourceId) && !e.Clickable)
                    continue;

                var parts = new List<string>();
                if (!string.IsNullOrEmpty(e.Text))
                    parts.Add($"text=\"{e.Text}\"");
                if (!string.IsNullOrEmpty(e.ContentDesc))
                    parts.Add($"desc=\"{e.ContentDesc}\"");
                if (!string.IsNullOrEmpty(e.ResourceId))
                    parts.Add($"id=\"{e.ResourceId}\"");
                parts.Add($"bounds=[{string.Join(",", e.Bounds)}]");
                if (e.Clickable)
                    parts.Add("clickable");

                var className = e.ClassName?.Split('.').LastOrDefault() ?? "";
                sb.AppendLine($"[{className}] {string.Join(" ", parts)}");
            }
            return TextResult(sb.ToString().TrimEnd());
        }
        else
        {
            return TextResult($"{header}\n\n{uiXml}");
        }
    }

    private async Task<ToolCallResult> AppStatusAsync(JsonElement? args, CancellationToken ct)
    {
        var serial = await GetDeviceSerialAsync(args);
        if (serial == null)
            return ErrorResult("No device connected.");

        var package = GetString(args, "package");
        if (string.IsNullOrEmpty(package))
            return ErrorResult("Missing required parameter: package");

        var sb = new StringBuilder();

        // Check if running
        var pidResult = await RunAdbShellAsync(serial, $"pidof {package}", ct);
        var pidStr = pidResult.StandardOutput.Trim();
        var isRunning = !string.IsNullOrEmpty(pidStr) && int.TryParse(pidStr, out var pid);

        sb.AppendLine($"Package: {package}");
        sb.AppendLine($"Running: {isRunning}");

        if (isRunning)
        {
            sb.AppendLine($"PID: {pidStr}");

            // Check foreground/background
            var fg = await GetForegroundPackageAsync(serial, ct);
            var state = package.Equals(fg, StringComparison.OrdinalIgnoreCase) ? "foreground" : "background";
            sb.AppendLine($"State: {state}");

            // Memory info
            var memResult = await RunAdbShellAsync(serial,
                $"dumpsys meminfo {package} | head -5", ct);
            if (memResult.Success)
            {
                var totalMatch = Regex.Match(memResult.StandardOutput, @"TOTAL\s+(\d+)");
                if (totalMatch.Success)
                    sb.AppendLine($"Memory: {int.Parse(totalMatch.Groups[1].Value) / 1024}MB");
            }
        }
        else
        {
            sb.AppendLine("State: stopped");
        }

        return TextResult(sb.ToString().TrimEnd());
    }

    private async Task<ToolCallResult> DeviceInfoAsync(JsonElement? args, CancellationToken ct)
    {
        var serial = await GetDeviceSerialAsync(args);
        if (serial == null)
            return ErrorResult("No device connected.");

        var props = new Dictionary<string, string>();

        // Batch property queries
        var commands = new[]
        {
            ("Model", "getprop ro.product.model"),
            ("Manufacturer", "getprop ro.product.manufacturer"),
            ("Android Version", "getprop ro.build.version.release"),
            ("SDK", "getprop ro.build.version.sdk"),
            ("Build", "getprop ro.build.display.id"),
        };

        foreach (var (label, cmd) in commands)
        {
            var r = await RunAdbShellAsync(serial, cmd, ct);
            if (r.Success)
                props[label] = r.StandardOutput.Trim();
        }

        // Screen size and rotation
        var sizeResult = await RunAdbShellAsync(serial, "wm size", ct);
        if (sizeResult.Success)
        {
            var match = Regex.Match(sizeResult.StandardOutput, @"(\d+)x(\d+)");
            if (match.Success)
                props["Physical Screen"] = $"{match.Groups[1].Value}x{match.Groups[2].Value}";
        }

        var rotation = await GetRotationAsync(serial, ct);
        var (pw, ph) = await GetPhysicalSizeAsync(serial, ct);
        var (vw, vh) = GetVisualSize(pw, ph, rotation);
        props["Rotation"] = $"{rotation} ({RotationLabel(rotation)})";
        props["Visual Screen"] = $"{vw}x{vh}";

        // Density
        var densityResult = await RunAdbShellAsync(serial, "wm density", ct);
        if (densityResult.Success)
        {
            var match = Regex.Match(densityResult.StandardOutput, @"(\d+)");
            if (match.Success)
                props["Density"] = match.Groups[1].Value + "dpi";
        }

        // Battery
        var battResult = await RunAdbShellAsync(serial, "dumpsys battery | grep -E 'level|status|plugged'", ct);
        if (battResult.Success)
        {
            var levelMatch = Regex.Match(battResult.StandardOutput, @"level:\s*(\d+)");
            var statusMatch = Regex.Match(battResult.StandardOutput, @"status:\s*(\d+)");
            if (levelMatch.Success)
            {
                var level = levelMatch.Groups[1].Value;
                var charging = statusMatch.Success && statusMatch.Groups[1].Value == "2" ? " (charging)" : "";
                props["Battery"] = $"{level}%{charging}";
            }
        }

        // Storage
        var storageResult = await RunAdbShellAsync(serial, "df /data | tail -1", ct);
        if (storageResult.Success)
        {
            var parts = storageResult.StandardOutput.Trim().Split(
                (char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 4)
            {
                if (long.TryParse(parts[3], out var availKb))
                    props["Available Storage"] = $"{availKb / 1024}MB";
            }
        }

        var sb = new StringBuilder();
        foreach (var (key, value) in props)
            sb.AppendLine($"{key}: {value}");

        return TextResult(sb.ToString().TrimEnd());
    }

    private async Task<ToolCallResult> ListPackagesAsync(JsonElement? args, CancellationToken ct)
    {
        var serial = await GetDeviceSerialAsync(args);
        if (serial == null)
            return ErrorResult("No device connected.");

        var filter = GetString(args, "filter");
        var thirdPartyOnly = GetBool(args, "third_party_only", true);

        var listArg = thirdPartyOnly ? "-3" : "";
        var result = await RunAdbShellAsync(serial, $"pm list packages {listArg}", ct, LongCommandTimeout);

        if (!result.Success)
            return ErrorResult($"Failed to list packages: {result.StandardError}");

        var packages = result.StandardOutput.Split('\n')
            .Select(l => l.Replace("package:", "").Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l));

        if (!string.IsNullOrEmpty(filter))
            packages = packages.Where(p => p.Contains(filter, StringComparison.OrdinalIgnoreCase));

        var list = packages.OrderBy(p => p).ToList();
        return TextResult(list.Count == 0
            ? "No packages found."
            : string.Join('\n', list));
    }

    private async Task<ToolCallResult> InstallApkAsync(JsonElement? args, CancellationToken ct)
    {
        var apkPath = GetString(args, "apk_path");
        if (string.IsNullOrEmpty(apkPath))
            return ErrorResult("Missing required parameter: apk_path");

        if (!File.Exists(apkPath))
            return ErrorResult($"APK file not found: {apkPath}");

        var grantPerms = GetBool(args, "grant_permissions", true);
        var installArgs = grantPerms ? $"install -r -g \"{apkPath}\"" : $"install -r \"{apkPath}\"";

        var result = await RunAdbAsync(installArgs, ct, TimeSpan.FromMinutes(2));
        if (!result.Success)
            return ErrorResult($"Install failed: {result.StandardError}\n{result.StandardOutput}");

        return TextResult($"Installed: {apkPath}\n{result.StandardOutput.Trim()}");
    }

    private async Task<ToolCallResult> LaunchAppAsync(JsonElement? args, CancellationToken ct)
    {
        var serial = await GetDeviceSerialAsync(args);
        if (serial == null)
            return ErrorResult("No device connected.");

        var package = GetString(args, "package");
        if (string.IsNullOrEmpty(package))
            return ErrorResult("Missing required parameter: package");

        // Use monkey to launch - works without knowing the activity name
        var result = await RunAdbShellAsync(serial,
            $"monkey -p {package} -c android.intent.category.LAUNCHER 1", ct);

        if (!result.Success || result.StandardOutput.Contains("No activities found"))
            return ErrorResult($"Failed to launch {package}: {result.StandardOutput} {result.StandardError}");

        return TextResult($"Launched: {package}");
    }

    private async Task<ToolCallResult> ForceStopAsync(JsonElement? args, CancellationToken ct)
    {
        var serial = await GetDeviceSerialAsync(args);
        if (serial == null)
            return ErrorResult("No device connected.");

        var package = GetString(args, "package");
        if (string.IsNullOrEmpty(package))
            return ErrorResult("Missing required parameter: package");

        var result = await RunAdbShellAsync(serial, $"am force-stop {package}", ct);
        if (!result.Success)
            return ErrorResult($"Force stop failed: {result.StandardError}");

        return TextResult($"Force stopped: {package}");
    }

    private async Task<ToolCallResult> ShellAsync(JsonElement? args, CancellationToken ct)
    {
        var serial = await GetDeviceSerialAsync(args);
        if (serial == null)
            return ErrorResult("No device connected.");

        var command = GetString(args, "command");
        if (string.IsNullOrEmpty(command))
            return ErrorResult("Missing required parameter: command");

        var result = await RunAdbShellAsync(serial, command, ct, LongCommandTimeout);

        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(result.StandardOutput))
            sb.Append(result.StandardOutput);
        if (!string.IsNullOrWhiteSpace(result.StandardError))
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.Append($"[stderr] {result.StandardError}");
        }
        sb.AppendLine($"\n[exit code: {result.ExitCode}]");

        return TextResult(sb.ToString().TrimEnd());
    }

    private async Task<ToolCallResult> PixelColorAsync(JsonElement? args, CancellationToken ct)
    {
        var serial = await GetDeviceSerialAsync(args);
        if (serial == null)
            return ErrorResult("No device connected.");

        var x = GetInt(args, "x", -1);
        var y = GetInt(args, "y", -1);
        if (x < 0 || y < 0)
            return ErrorResult("Missing required parameters: x, y");

        // Use screencap in raw format and read the specific pixel
        // Raw format: 4-byte width + 4-byte height + 4-byte pixel_format + RGBA pixel data
        // Each pixel is 4 bytes (RGBA)
        // This uses visual coordinates (same as screenshot) - no rotation transform needed
        // because screencap captures in the current display orientation
        var result = await RunAdbShellAsync(serial,
            "screencap -p /sdcard/mcp_pixel_tmp.png && " +
            $"dd if=/dev/urandom bs=1 count=0 2>/dev/null; " +  // no-op separator
            "screencap | dd bs=4 count=1 2>/dev/null | od -A n -t u4 | tr -d ' '",
            ct);

        // Better approach: use the raw screencap and compute the pixel offset
        var widthResult = await RunAdbShellAsync(serial,
            "screencap | head -c 4 | od -A n -t u4 | tr -d ' '", ct);
        var rawWidth = 0;
        if (widthResult.Success)
            int.TryParse(widthResult.StandardOutput.Trim(), out rawWidth);

        if (rawWidth <= 0)
        {
            // Fallback: get width from wm size + rotation
            var rotation = await GetRotationAsync(serial, ct);
            var (physW, physH) = await GetPhysicalSizeAsync(serial, ct);
            var (visW, _) = GetVisualSize(physW, physH, rotation);
            rawWidth = visW;
        }

        if (rawWidth <= 0)
            return ErrorResult("Could not determine screen width for pixel sampling.");

        // Read the specific pixel: skip header (12 bytes) + pixel offset
        // Pixel offset = (y * width + x) * 4 bytes per pixel
        var pixelOffset = 12 + ((long)y * rawWidth + x) * 4;
        var pixelResult = await RunAdbShellAsync(serial,
            $"screencap | dd bs=1 skip={pixelOffset} count=4 2>/dev/null | od -A n -t u1",
            ct);

        if (pixelResult.Success && !string.IsNullOrWhiteSpace(pixelResult.StandardOutput))
        {
            var values = pixelResult.StandardOutput.Trim().Split(
                (char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (values.Length >= 3)
            {
                // RGBA format
                int r = int.Parse(values[0]);
                int g = int.Parse(values[1]);
                int b = int.Parse(values[2]);
                var hex = $"#{r:X2}{g:X2}{b:X2}";
                var luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255.0;

                return TextResult(
                    $"Pixel ({x}, {y}) — visual/screenshot coordinates:\n" +
                    $"  Hex: {hex}\n" +
                    $"  RGB: ({r}, {g}, {b})\n" +
                    $"  Luminance: {luminance:F3}\n" +
                    $"  Is dark: {luminance < 0.5}");
            }
        }

        return ErrorResult(
            $"Could not read pixel at ({x}, {y}). " +
            $"Screen width detected: {rawWidth}. " +
            "Make sure coordinates are within screen bounds.");
    }

    private async Task<ToolCallResult> PerformanceSnapshotAsync(JsonElement? args, CancellationToken ct)
    {
        var serial = await GetDeviceSerialAsync(args);
        if (serial == null)
            return ErrorResult("No device connected.");

        var package = GetString(args, "package");
        if (string.IsNullOrEmpty(package))
            return ErrorResult("Missing required parameter: package");

        var sb = new StringBuilder();
        sb.AppendLine($"Performance snapshot for {package}:");
        var hasData = false;

        // ── 1. GFX info (frame rendering stats) ──
        var gfxResult = await RunAdbShellAsync(serial,
            $"dumpsys gfxinfo {package} framestats", ct, LongCommandTimeout);
        if (gfxResult.Success && !string.IsNullOrWhiteSpace(gfxResult.StandardOutput))
        {
            var gfx = gfxResult.StandardOutput;
            sb.AppendLine("\n--- Frame Rendering ---");

            // Try multiple regex patterns for different Android versions
            var totalFrames = Regex.Match(gfx, @"Total frames rendered:\s*(\d+)");
            var jankyFrames = Regex.Match(gfx, @"Janky frames:\s*(\d+)\s*\(([^)]+)\)");
            var p50 = Regex.Match(gfx, @"50th percentile:\s*(\d+)ms");
            var p90 = Regex.Match(gfx, @"90th percentile:\s*(\d+)ms");
            var p95 = Regex.Match(gfx, @"95th percentile:\s*(\d+)ms");
            var p99 = Regex.Match(gfx, @"99th percentile:\s*(\d+)ms");
            var missedVsync = Regex.Match(gfx, @"Number Missed Vsync:\s*(\d+)");
            var highInputLat = Regex.Match(gfx, @"Number High input latency:\s*(\d+)");
            var slowUiThread = Regex.Match(gfx, @"Number Slow UI thread:\s*(\d+)");
            var slowBitmapUploads = Regex.Match(gfx, @"Number Slow bitmap uploads:\s*(\d+)");
            var slowIssue = Regex.Match(gfx, @"Number Slow issue draw commands:\s*(\d+)");

            if (totalFrames.Success) { sb.AppendLine($"  Total frames: {totalFrames.Groups[1].Value}"); hasData = true; }
            if (jankyFrames.Success) { sb.AppendLine($"  Janky frames: {jankyFrames.Groups[1].Value} ({jankyFrames.Groups[2].Value})"); hasData = true; }
            if (p50.Success) sb.AppendLine($"  50th percentile: {p50.Groups[1].Value}ms");
            if (p90.Success) sb.AppendLine($"  90th percentile: {p90.Groups[1].Value}ms");
            if (p95.Success) sb.AppendLine($"  95th percentile: {p95.Groups[1].Value}ms");
            if (p99.Success) sb.AppendLine($"  99th percentile: {p99.Groups[1].Value}ms");
            if (missedVsync.Success) sb.AppendLine($"  Missed Vsync: {missedVsync.Groups[1].Value}");
            if (highInputLat.Success) sb.AppendLine($"  High input latency: {highInputLat.Groups[1].Value}");
            if (slowUiThread.Success) sb.AppendLine($"  Slow UI thread: {slowUiThread.Groups[1].Value}");
            if (slowBitmapUploads.Success) sb.AppendLine($"  Slow bitmap uploads: {slowBitmapUploads.Groups[1].Value}");
            if (slowIssue.Success) sb.AppendLine($"  Slow draw commands: {slowIssue.Groups[1].Value}");

            // If none of the above matched, dump raw stats section
            if (!totalFrames.Success && !jankyFrames.Success)
            {
                // Look for the stats section
                var statsSection = Regex.Match(gfx, @"(Stats since.*?)(?:\n\n|\z)", RegexOptions.Singleline);
                if (statsSection.Success)
                {
                    sb.AppendLine(statsSection.Groups[1].Value.Trim());
                    hasData = true;
                }
            }
        }

        // ── 2. Memory info ──
        var memResult = await RunAdbShellAsync(serial,
            $"dumpsys meminfo {package}", ct, LongCommandTimeout);
        if (memResult.Success && !string.IsNullOrWhiteSpace(memResult.StandardOutput))
        {
            sb.AppendLine("\n--- Memory ---");
            var mem = memResult.StandardOutput;

            // Try multiple patterns for different Android versions
            var totalPss = Regex.Match(mem, @"TOTAL\s+PSS:\s+(\d+)", RegexOptions.IgnoreCase);
            if (!totalPss.Success) totalPss = Regex.Match(mem, @"TOTAL\s+(\d+)");

            var nativeHeap = Regex.Match(mem, @"Native Heap\s+(\d+)");
            var javaHeap = Regex.Match(mem, @"(?:Dalvik|Java) Heap\s+(\d+)");
            var totalSwapPss = Regex.Match(mem, @"TOTAL SWAP PSS:\s+(\d+)");

            // Also try the "App Summary" section (newer Android)
            var appSummary = Regex.Match(mem, @"App Summary\s*\n(.*?)(?:\n\s*\n|\z)", RegexOptions.Singleline);

            if (totalPss.Success)
            {
                var totalKb = int.Parse(totalPss.Groups[1].Value);
                sb.AppendLine($"  Total PSS: {totalKb / 1024}MB ({totalKb}KB)");
                hasData = true;
            }
            if (nativeHeap.Success)
                sb.AppendLine($"  Native heap: {int.Parse(nativeHeap.Groups[1].Value) / 1024}MB");
            if (javaHeap.Success)
                sb.AppendLine($"  Java/Dalvik heap: {int.Parse(javaHeap.Groups[1].Value) / 1024}MB");
            if (totalSwapPss.Success)
                sb.AppendLine($"  Swap PSS: {int.Parse(totalSwapPss.Groups[1].Value) / 1024}MB");

            if (appSummary.Success && !totalPss.Success)
            {
                sb.AppendLine(appSummary.Groups[1].Value.Trim());
                hasData = true;
            }

            // Fallback: extract the TOTAL line directly
            if (!totalPss.Success && !appSummary.Success)
            {
                var totalLine = mem.Split('\n')
                    .FirstOrDefault(l => l.TrimStart().StartsWith("TOTAL"));
                if (totalLine != null)
                {
                    sb.AppendLine($"  {totalLine.Trim()}");
                    hasData = true;
                }
            }
        }

        // ── 3. CPU usage ──
        var cpuResult = await RunAdbShellAsync(serial,
            $"top -b -n 1 -p $(pidof {package} 2>/dev/null || echo 0) 2>/dev/null | tail -2",
            ct);
        if (cpuResult.Success && !string.IsNullOrWhiteSpace(cpuResult.StandardOutput))
        {
            var cpuLines = cpuResult.StandardOutput.Split('\n')
                .Where(l => l.Contains(package, StringComparison.OrdinalIgnoreCase) ||
                            Regex.IsMatch(l, @"^\s*\d+"))
                .ToList();
            if (cpuLines.Count > 0)
            {
                sb.AppendLine("\n--- CPU ---");
                foreach (var line in cpuLines)
                    sb.AppendLine($"  {line.Trim()}");
                hasData = true;
            }
        }

        // Fallback CPU: use dumpsys cpuinfo
        if (!hasData || !sb.ToString().Contains("CPU"))
        {
            var cpuInfo = await RunAdbShellAsync(serial,
                $"dumpsys cpuinfo | grep -i '{package}'", ct);
            if (cpuInfo.Success && !string.IsNullOrWhiteSpace(cpuInfo.StandardOutput))
            {
                sb.AppendLine("\n--- CPU (dumpsys) ---");
                sb.AppendLine($"  {cpuInfo.StandardOutput.Trim()}");
                hasData = true;
            }
        }

        // ── 4. GPU rendering mode (if available) ──
        var gpuResult = await RunAdbShellAsync(serial,
            "dumpsys gpu | head -20 2>/dev/null", ct);
        if (gpuResult.Success && !string.IsNullOrWhiteSpace(gpuResult.StandardOutput)
            && !gpuResult.StandardOutput.Contains("not found"))
        {
            sb.AppendLine("\n--- GPU ---");
            sb.AppendLine($"  {gpuResult.StandardOutput.Trim()}");
            hasData = true;
        }

        if (!hasData)
        {
            sb.AppendLine("\nNo performance data could be collected.");
            sb.AppendLine("Possible reasons:");
            sb.AppendLine("  - App uses Flutter/React Native (bypasses Android rendering pipeline for gfxinfo)");
            sb.AppendLine("  - App is not currently running (use launch_app first)");
            sb.AppendLine("  - Try: shell command='dumpsys gfxinfo " + package + "' to see raw output");
            sb.AppendLine("  - Try: shell command='dumpsys meminfo " + package + "' for memory details");
        }

        return TextResult(sb.ToString().TrimEnd());
    }

    private async Task<ToolCallResult> ScreenRecordAsync(JsonElement? args, CancellationToken ct)
    {
        var serial = await GetDeviceSerialAsync(args);
        if (serial == null)
            return ErrorResult("No device connected.");

        var duration = Math.Min(GetInt(args, "duration_seconds", 10), 180);
        var outputPath = GetString(args, "output_path")
            ?? Path.Combine(Path.GetTempPath(), $"screenrecord_{DateTime.Now:yyyyMMdd_HHmmss}.mp4");

        var remotePath = "/sdcard/mcp_screenrecord.mp4";

        // Start recording (this blocks for the duration)
        var recordResult = await RunAdbShellAsync(serial,
            $"screenrecord --time-limit {duration} {remotePath}",
            ct, TimeSpan.FromSeconds(duration + 10));

        if (!recordResult.Success)
            return ErrorResult($"Screen recording failed: {recordResult.StandardError}");

        // Pull the file
        var serialArg = string.IsNullOrEmpty(serial) ? "" : $"-s {serial} ";
        var pullResult = await RunAdbAsync(
            $"{serialArg}pull {remotePath} \"{outputPath}\"", ct, LongCommandTimeout);

        // Clean up remote file
        await RunAdbShellAsync(serial, $"rm {remotePath}", ct);

        if (!pullResult.Success)
            return ErrorResult($"Failed to pull recording: {pullResult.StandardError}");

        return TextResult($"Screen recording saved to: {outputPath}\nDuration: {duration}s");
    }

    // ─── UI Hierarchy Helpers ───

    private async Task<string?> DumpUiHierarchyAsync(string serial, CancellationToken ct)
    {
        // Dump to a temp file on device, then cat it (more reliable than /dev/tty)
        var dumpPath = "/sdcard/mcp_window_dump.xml";

        var dumpResult = await RunAdbShellAsync(serial,
            $"uiautomator dump {dumpPath}", ct, CommandTimeout);

        if (!dumpResult.Success && !dumpResult.StandardOutput.Contains("dumped"))
            return null;

        var catResult = await RunAdbShellAsync(serial, $"cat {dumpPath}", ct);

        // Clean up
        _ = RunAdbShellAsync(serial, $"rm {dumpPath}", ct);

        return catResult.Success ? catResult.StandardOutput : null;
    }

    private static List<UiElement> ParseUiElements(string xml)
    {
        var elements = new List<UiElement>();
        try
        {
            var doc = XDocument.Parse(xml);
            foreach (var node in doc.Descendants("node"))
            {
                var boundsStr = node.Attribute("bounds")?.Value ?? "";
                var bounds = ParseBounds(boundsStr);
                if (bounds == null) continue;

                elements.Add(new UiElement
                {
                    Text = node.Attribute("text")?.Value ?? "",
                    ContentDesc = node.Attribute("content-desc")?.Value ?? "",
                    ResourceId = node.Attribute("resource-id")?.Value ?? "",
                    ClassName = node.Attribute("class")?.Value ?? "",
                    Clickable = node.Attribute("clickable")?.Value == "true",
                    Enabled = node.Attribute("enabled")?.Value != "false",
                    Bounds = bounds
                });
            }
        }
        catch
        {
            // XML parse failure - return empty list
        }
        return elements;
    }

    private static int[]? ParseBounds(string bounds)
    {
        // Format: [left,top][right,bottom]
        var match = Regex.Match(bounds, @"\[(\d+),(\d+)\]\[(\d+),(\d+)\]");
        if (!match.Success) return null;

        return new[]
        {
            int.Parse(match.Groups[1].Value),
            int.Parse(match.Groups[2].Value),
            int.Parse(match.Groups[3].Value),
            int.Parse(match.Groups[4].Value)
        };
    }

    private class UiElement
    {
        public string Text { get; set; } = "";
        public string ContentDesc { get; set; } = "";
        public string ResourceId { get; set; } = "";
        public string ClassName { get; set; } = "";
        public bool Clickable { get; set; }
        public bool Enabled { get; set; }
        public int[] Bounds { get; set; } = Array.Empty<int>();
    }
}
