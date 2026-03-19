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
                "Capture the Android device screen and return as a PNG image.",
                """
                {
                    "type": "object",
                    "properties": {
                        "device_serial": { "type": "string", "description": "Device serial number. Omit to auto-detect." }
                    }
                }
                """),

            MakeTool("crash_log",
                "Get the last crash traces (FATAL EXCEPTION, ANR, native crashes) for an app.",
                """
                {
                    "type": "object",
                    "properties": {
                        "package": { "type": "string", "description": "App package name. Omit to get all crashes." },
                        "last_n": { "type": "integer", "description": "Number of recent crashes to return. Default: 3", "default": 3 }
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
                "Tap a UI element by its text, content description, resource ID, or exact coordinates. Finds the element in the UI hierarchy and taps its center.",
                """
                {
                    "type": "object",
                    "properties": {
                        "text": { "type": "string", "description": "Find and tap element containing this text." },
                        "content_desc": { "type": "string", "description": "Find by accessibility content description." },
                        "resource_id": { "type": "string", "description": "Find by resource ID (e.g. 'com.app:id/button')." },
                        "x": { "type": "integer", "description": "Tap at exact X coordinate." },
                        "y": { "type": "integer", "description": "Tap at exact Y coordinate." },
                        "index": { "type": "integer", "description": "Which match to tap if multiple found (0-based). Default: 0", "default": 0 },
                        "long_press": { "type": "boolean", "description": "Long press instead of tap. Default: false", "default": false },
                        "double_tap": { "type": "boolean", "description": "Double tap. Default: false", "default": false }
                    }
                }
                """),

            MakeTool("swipe",
                "Perform a swipe gesture on the device screen.",
                """
                {
                    "type": "object",
                    "properties": {
                        "start_x": { "type": "integer", "description": "Start X coordinate." },
                        "start_y": { "type": "integer", "description": "Start Y coordinate." },
                        "end_x": { "type": "integer", "description": "End X coordinate." },
                        "end_y": { "type": "integer", "description": "End Y coordinate." },
                        "duration_ms": { "type": "integer", "description": "Swipe duration in milliseconds. Default: 300", "default": 300 },
                        "direction": { "type": "string", "enum": ["up", "down", "left", "right"], "description": "Swipe direction from screen center. Use instead of coordinates." }
                    },
                    "required": []
                }
                """),

            MakeTool("input_text",
                "Type text on the device. The focused text field will receive the input.",
                """
                {
                    "type": "object",
                    "properties": {
                        "text": { "type": "string", "description": "Text to type." }
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
                "Get the current UI element hierarchy from the device screen.",
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
                "Get device details: model, Android version, screen size, battery, storage.",
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
                "Sample the color of a pixel at given coordinates from a fresh screenshot.",
                """
                {
                    "type": "object",
                    "properties": {
                        "x": { "type": "integer", "description": "X coordinate." },
                        "y": { "type": "integer", "description": "Y coordinate." }
                    },
                    "required": ["x", "y"]
                }
                """),

            MakeTool("performance_snapshot",
                "Capture app performance metrics: FPS, janky frames, memory usage.",
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

    // ─── Tool Implementations ───

    private async Task<ToolCallResult> ScreenshotAsync(JsonElement? args, CancellationToken ct)
    {
        var serial = await GetDeviceSerialAsync(args);
        if (serial == null)
            return ErrorResult("No device connected.");

        var (pngData, error) = await _screenshotService.CaptureAsync(serial, ct);
        if (pngData == null)
            return ErrorResult($"Screenshot failed: {error}");

        return new ToolCallResult
        {
            Content = new List<ContentBlock>
            {
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
        var lastN = GetInt(args, "last_n", 3);

        // Get FATAL EXCEPTIONs from logcat
        var result = await RunAdbShellAsync(serial,
            "logcat -d -b crash,main -s AndroidRuntime:E '*:F'", ct, LongCommandTimeout);

        var output = new StringBuilder();

        if (result.Success && !string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            var lines = result.StandardOutput.Split('\n');
            var crashes = new List<string>();
            var currentCrash = new StringBuilder();
            var inCrash = false;

            foreach (var line in lines)
            {
                if (line.Contains("FATAL EXCEPTION") || line.Contains("Fatal signal"))
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

            // Filter by package if specified
            if (!string.IsNullOrEmpty(package))
            {
                crashes = crashes.Where(c => c.Contains(package)).ToList();
            }

            // Take last N
            var recent = crashes.TakeLast(lastN).ToList();

            if (recent.Count == 0)
            {
                output.AppendLine("No crashes found.");
            }
            else
            {
                output.AppendLine($"Found {recent.Count} crash(es):");
                for (int i = 0; i < recent.Count; i++)
                {
                    output.AppendLine($"\n--- Crash {i + 1} ---");
                    output.AppendLine(recent[i].TrimEnd());
                }
            }
        }
        else
        {
            output.AppendLine("No crash logs available (logcat may be empty).");
        }

        // Also check for ANR traces
        var anrResult = await RunAdbShellAsync(serial,
            "logcat -d -b events -s am_anr", ct);
        if (anrResult.Success && !string.IsNullOrWhiteSpace(anrResult.StandardOutput))
        {
            var anrLines = anrResult.StandardOutput.Split('\n')
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            if (!string.IsNullOrEmpty(package))
                anrLines = anrLines.Where(l => l.Contains(package)).ToList();

            if (anrLines.Count > 0)
            {
                output.AppendLine($"\n--- ANR Events ({anrLines.Count}) ---");
                foreach (var line in anrLines.TakeLast(lastN))
                    output.AppendLine(line);
            }
        }

        return TextResult(output.ToString().TrimEnd());
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

        if (x.HasValue && y.HasValue)
        {
            tapX = x.Value;
            tapY = y.Value;
        }
        else if (!string.IsNullOrEmpty(text) || !string.IsNullOrEmpty(contentDesc) || !string.IsNullOrEmpty(resourceId))
        {
            // Dump UI hierarchy and find element
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
            tapCommand = $"input tap {tapX} {tapY} && input tap {tapX} {tapY}";
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

        int startX, startY, endX, endY;

        if (!string.IsNullOrEmpty(direction))
        {
            // Get screen size for center-based swipe
            var sizeResult = await RunAdbShellAsync(serial, "wm size", ct);
            var sizeMatch = Regex.Match(sizeResult.StandardOutput, @"(\d+)x(\d+)");
            int screenW = 1080, screenH = 1920;
            if (sizeMatch.Success)
            {
                screenW = int.Parse(sizeMatch.Groups[1].Value);
                screenH = int.Parse(sizeMatch.Groups[2].Value);
            }

            var cx = screenW / 2;
            var cy = screenH / 2;
            var swipeLen = Math.Min(screenW, screenH) / 3;

            (startX, startY, endX, endY) = direction.ToLowerInvariant() switch
            {
                "up" => (cx, cy + swipeLen, cx, cy - swipeLen),
                "down" => (cx, cy - swipeLen, cx, cy + swipeLen),
                "left" => (cx + swipeLen, cy, cx - swipeLen, cy),
                "right" => (cx - swipeLen, cy, cx + swipeLen, cy),
                _ => (cx, cy + swipeLen, cx, cy - swipeLen)
            };
        }
        else
        {
            startX = GetInt(args, "start_x", 0);
            startY = GetInt(args, "start_y", 0);
            endX = GetInt(args, "end_x", 0);
            endY = GetInt(args, "end_y", 0);
        }

        var result = await RunAdbShellAsync(serial,
            $"input swipe {startX} {startY} {endX} {endY} {duration}", ct);

        if (!result.Success)
            return ErrorResult($"Swipe failed: {result.StandardError}");

        return TextResult($"Swiped from ({startX},{startY}) to ({endX},{endY}) over {duration}ms");
    }

    private async Task<ToolCallResult> InputTextAsync(JsonElement? args, CancellationToken ct)
    {
        var serial = await GetDeviceSerialAsync(args);
        if (serial == null)
            return ErrorResult("No device connected.");

        var text = GetString(args, "text");
        if (string.IsNullOrEmpty(text))
            return ErrorResult("Missing required parameter: text");

        // Escape special characters for ADB input text
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
        if (!result.Success)
            return ErrorResult($"Input text failed: {result.StandardError}");

        return TextResult($"Typed: {text}");
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

        if (simplified)
        {
            var sb = new StringBuilder();
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
            // Return the raw XML
            return TextResult(uiXml);
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

        // Screen size
        var sizeResult = await RunAdbShellAsync(serial, "wm size", ct);
        if (sizeResult.Success)
        {
            var match = Regex.Match(sizeResult.StandardOutput, @"(\d+)x(\d+)");
            if (match.Success)
                props["Screen"] = $"{match.Groups[1].Value}x{match.Groups[2].Value}";
        }

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

        // Take a screenshot and read the pixel
        var (pngData, error) = await _screenshotService.CaptureAsync(serial, ct);
        if (pngData == null)
            return ErrorResult($"Screenshot for pixel sampling failed: {error}");

        // Use the raw PNG data to extract pixel color
        // We'll use a simple approach: decode the PNG header to get dimensions,
        // then use the raw bitmap. For simplicity, save to temp, use ADB approach instead.
        // Actually, let's use a shell approach with screencap raw format
        var result = await RunAdbShellAsync(serial,
            $"screencap | head -c $((12 + ({y} * $(wm size | grep -o '[0-9]*x' | tr -d x) + {x} + 1) * 4)) | tail -c 4 | od -A n -t u1",
            ct);

        if (result.Success && !string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            var values = result.StandardOutput.Trim().Split(
                (char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (values.Length >= 3)
            {
                int r = int.Parse(values[0]);
                int g = int.Parse(values[1]);
                int b = int.Parse(values[2]);
                var hex = $"#{r:X2}{g:X2}{b:X2}";
                var luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255.0;

                return TextResult(
                    $"Pixel ({x}, {y}):\n" +
                    $"  Hex: {hex}\n" +
                    $"  RGB: ({r}, {g}, {b})\n" +
                    $"  Luminance: {luminance:F3}\n" +
                    $"  Is dark: {luminance < 0.5}");
            }
        }

        // Fallback: just report we couldn't read it
        return ErrorResult("Could not read pixel color. The shell-based pixel extraction failed on this device.");
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

        // GFX info (FPS, janky frames)
        var gfxResult = await RunAdbShellAsync(serial, $"dumpsys gfxinfo {package}", ct, LongCommandTimeout);
        if (gfxResult.Success)
        {
            var totalFramesMatch = Regex.Match(gfxResult.StandardOutput, @"Total frames rendered:\s*(\d+)");
            var jankyMatch = Regex.Match(gfxResult.StandardOutput, @"Janky frames:\s*(\d+)\s*\(([^)]+)\)");

            if (totalFramesMatch.Success)
                sb.AppendLine($"  Total frames: {totalFramesMatch.Groups[1].Value}");
            if (jankyMatch.Success)
                sb.AppendLine($"  Janky frames: {jankyMatch.Groups[1].Value} ({jankyMatch.Groups[2].Value})");
        }

        // Memory info
        var memResult = await RunAdbShellAsync(serial, $"dumpsys meminfo {package}", ct, LongCommandTimeout);
        if (memResult.Success)
        {
            var totalMatch = Regex.Match(memResult.StandardOutput, @"TOTAL\s+(\d+)");
            var nativeMatch = Regex.Match(memResult.StandardOutput, @"Native Heap\s+(\d+)");
            var javaMatch = Regex.Match(memResult.StandardOutput, @"Java Heap\s+(\d+)");

            if (totalMatch.Success)
                sb.AppendLine($"  Total memory: {int.Parse(totalMatch.Groups[1].Value) / 1024}MB");
            if (nativeMatch.Success)
                sb.AppendLine($"  Native heap: {int.Parse(nativeMatch.Groups[1].Value) / 1024}MB");
            if (javaMatch.Success)
                sb.AppendLine($"  Java heap: {int.Parse(javaMatch.Groups[1].Value) / 1024}MB");
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
