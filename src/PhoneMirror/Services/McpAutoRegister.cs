using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PhoneMirror.Services;

/// <summary>
/// Auto-registers the adb-bridge MCP server in Claude Code's global settings
/// so it's available as soon as Phone Mirror opens.
/// </summary>
public static class McpAutoRegister
{
    /// <summary>
    /// Ensures the adb-bridge MCP server is registered in Claude Code settings.
    /// Uses the published adb-bridge executable next to PhoneMirror if available,
    /// otherwise falls back to dotnet run.
    /// </summary>
    public static void EnsureRegistered()
    {
        try
        {
            var settingsPath = GetClaudeSettingsPath();
            if (settingsPath == null) return;

            var adbBridgePath = FindAdbBridgeExecutable();
            if (adbBridgePath == null) return;

            // Read existing settings or create new
            JsonObject settings;
            if (File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                settings = JsonNode.Parse(json)?.AsObject() ?? new JsonObject();
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
                settings = new JsonObject();
            }

            // Get or create mcpServers
            if (!settings.ContainsKey("mcpServers"))
                settings["mcpServers"] = new JsonObject();

            var mcpServers = settings["mcpServers"]!.AsObject();

            // Build the server config
            var serverConfig = BuildServerConfig(adbBridgePath);

            // Check if already registered with the same command
            if (mcpServers.ContainsKey("adb-bridge"))
            {
                var existing = mcpServers["adb-bridge"];
                var existingCmd = existing?["command"]?.GetValue<string>();
                var newCmd = serverConfig["command"]?.GetValue<string>();
                if (existingCmd == newCmd)
                    return; // Already configured correctly
            }

            // Register/update
            mcpServers["adb-bridge"] = serverConfig;

            // Write back
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(settingsPath, settings.ToJsonString(options));

            System.Diagnostics.Debug.WriteLine($"MCP server registered in {settingsPath}");
        }
        catch (Exception ex)
        {
            // Non-fatal — MCP registration is a convenience feature
            System.Diagnostics.Debug.WriteLine($"MCP auto-register failed: {ex.Message}");
        }
    }

    private static JsonObject BuildServerConfig(string adbBridgePath)
    {
        var config = new JsonObject();

        if (adbBridgePath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            // Development mode: use dotnet run
            config["command"] = "dotnet";
            config["args"] = new JsonArray("run", "--project", adbBridgePath.Replace('\\', '/'));
        }
        else
        {
            // Published mode: direct executable
            config["command"] = adbBridgePath.Replace('\\', '/');
            config["args"] = new JsonArray();
        }

        return config;
    }

    private static string? FindAdbBridgeExecutable()
    {
        var exeName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "adb-bridge.exe" : "adb-bridge";

        var baseDir = AppContext.BaseDirectory;

        // 1. Same directory as PhoneMirror (published side-by-side)
        var sameDir = Path.Combine(baseDir, exeName);
        if (File.Exists(sameDir)) return sameDir;

        // 2. Sibling directory (separate publish folders)
        var parent = Directory.GetParent(baseDir)?.FullName;
        if (parent != null)
        {
            var siblings = new[]
            {
                Path.Combine(parent, "mcp-win-x64", exeName),
                Path.Combine(parent, "mcp-linux-x64", exeName),
                Path.Combine(parent, "mcp-osx-arm64", exeName),
            };
            foreach (var s in siblings)
                if (File.Exists(s)) return s;
        }

        // 3. Release directory
        var current = new DirectoryInfo(baseDir);
        for (int i = 0; i < 8 && current != null; i++)
        {
            var releaseDir = Path.Combine(current.FullName, "release", exeName.Replace(".exe", "-win-x64.exe"));
            if (File.Exists(releaseDir)) return releaseDir;

            // Also check for the plain executable in release/
            var releasePlain = Path.Combine(current.FullName, "release", exeName);
            if (File.Exists(releasePlain)) return releasePlain;

            current = current.Parent;
        }

        // 4. Development: find the .csproj and use dotnet run
        current = new DirectoryInfo(baseDir);
        for (int i = 0; i < 10 && current != null; i++)
        {
            var csproj = Path.Combine(current.FullName, "src", "PhoneMirror.Mcp", "PhoneMirror.Mcp.csproj");
            if (File.Exists(csproj)) return csproj;
            current = current.Parent;
        }

        return null;
    }

    private static string? GetClaudeSettingsPath()
    {
        // Claude Code stores settings in ~/.claude/settings.json
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(home)) return null;

        return Path.Combine(home, ".claude", "settings.json");
    }
}
