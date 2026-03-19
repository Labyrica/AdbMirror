using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PhoneMirror.Services;

/// <summary>
/// Generates the MCP setup guide text for clipboard copy.
/// Auto-detects paths based on the current platform and app location.
/// </summary>
public static class McpSetupGenerator
{
    public static string Generate()
    {
        var mcpProjectPath = FindMcpProjectPath();
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var settingsPath = isWindows
            ? @"~\.claude\settings.json"
            : "~/.claude/settings.json";

        var sb = new StringBuilder();

        // ── Settings.json snippet ──
        sb.AppendLine("# ADB Bridge — Claude Code Setup");
        sb.AppendLine();
        sb.AppendLine("## 1. Add MCP server to Claude Code");
        sb.AppendLine();
        sb.AppendLine($"Add to `{settingsPath}` (or project `.claude/settings.json`):");
        sb.AppendLine();
        sb.AppendLine("```jsonc");
        sb.AppendLine("{");
        sb.AppendLine("  \"mcpServers\": {");
        sb.AppendLine("    \"adb-bridge\": {");
        sb.AppendLine($"      \"command\": \"dotnet\",");
        sb.AppendLine($"      \"args\": [\"run\", \"--project\", \"{EscapeJsonPath(mcpProjectPath)}\"]");
        sb.AppendLine("    }");
        sb.AppendLine("  }");
        sb.AppendLine("}");
        sb.AppendLine("```");
        sb.AppendLine();

        // ── Available tools reference ──
        sb.AppendLine("## 2. Available tools");
        sb.AppendLine();
        sb.AppendLine("Once configured, Claude Code sees these as native tools (no Bash wrappers needed):");
        sb.AppendLine();
        sb.AppendLine("| Tool | Description |");
        sb.AppendLine("|------|-------------|");
        sb.AppendLine("| `screenshot` | Capture the device screen as a PNG image |");
        sb.AppendLine("| `crash_log` | Get FATAL EXCEPTION / ANR / native crash traces |");
        sb.AppendLine("| `app_logs` | Filtered logcat by package, level, time window, regex |");
        sb.AppendLine("| `tap` | Tap UI element by text, content-desc, resource-id, or coordinates |");
        sb.AppendLine("| `swipe` | Swipe gesture by coordinates or direction (up/down/left/right) |");
        sb.AppendLine("| `input_text` | Type text into the focused field |");
        sb.AppendLine("| `press_key` | Press back, home, enter, volume, power, etc. |");
        sb.AppendLine("| `ui_tree` | Get the current UI element hierarchy |");
        sb.AppendLine("| `app_status` | Check if app is running, foreground/background, memory, PID |");
        sb.AppendLine("| `device_info` | Model, Android version, screen size, battery, storage |");
        sb.AppendLine("| `list_packages` | List installed apps, filter by name |");
        sb.AppendLine("| `install_apk` | Install an APK with auto-grant permissions |");
        sb.AppendLine("| `launch_app` | Launch an app by package name |");
        sb.AppendLine("| `force_stop` | Force stop an app |");
        sb.AppendLine("| `shell` | Run any ADB shell command |");
        sb.AppendLine("| `pixel_color` | Sample pixel RGB/hex/luminance at coordinates |");
        sb.AppendLine("| `performance_snapshot` | FPS, janky frames, memory breakdown |");
        sb.AppendLine("| `screen_record` | Record screen to MP4 (max 180s) |");
        sb.AppendLine();

        // ── Requirements ──
        sb.AppendLine("## 3. Requirements");
        sb.AppendLine();
        sb.AppendLine("- .NET 8 SDK (`dotnet --version` to check)");
        sb.AppendLine("- ADB in PATH or ANDROID_HOME set");
        sb.AppendLine("- Android device connected via USB with USB debugging enabled");
        sb.AppendLine("- Works on **Windows**, **macOS**, and **Linux**");

        return sb.ToString();
    }

    private static string FindMcpProjectPath()
    {
        // Walk up from the running app to find the MCP project
        var baseDir = AppContext.BaseDirectory;
        var current = new DirectoryInfo(baseDir);

        // Try to find the solution root by looking for PhoneMirror.sln
        for (int i = 0; i < 10 && current != null; i++)
        {
            var slnPath = Path.Combine(current.FullName, "PhoneMirror.sln");
            if (File.Exists(slnPath))
            {
                var mcpCsproj = Path.Combine(current.FullName, "src", "PhoneMirror.Mcp", "PhoneMirror.Mcp.csproj");
                if (File.Exists(mcpCsproj))
                {
                    return NormalizePath(mcpCsproj);
                }
            }
            current = current.Parent;
        }

        // Fallback: use a relative path from the known structure
        // Try common dev layouts
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "PhoneMirror.Mcp", "PhoneMirror.Mcp.csproj")),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "src", "PhoneMirror.Mcp", "PhoneMirror.Mcp.csproj")),
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
                return NormalizePath(candidate);
        }

        // Last resort: placeholder
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "C:/path/to/src/PhoneMirror.Mcp/PhoneMirror.Mcp.csproj"
            : "/path/to/src/PhoneMirror.Mcp/PhoneMirror.Mcp.csproj";
    }

    private static string NormalizePath(string path)
    {
        // Always use forward slashes for JSON compatibility
        return path.Replace('\\', '/');
    }

    private static string EscapeJsonPath(string path)
    {
        return path.Replace("\\", "/");
    }
}
