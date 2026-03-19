# AdbMirror

**Phone Mirror** — Android screen mirroring + an MCP server that gives Claude Code direct access to your Android device.

## Overview

AdbMirror is a desktop app that mirrors your Android phone's screen to your PC in real-time, **and** exposes an MCP (Model Context Protocol) server so Claude Code can see your screen, tap UI elements, read crash logs, and run ADB commands — all as native tools.

Built with Avalonia UI following the Labyrica design language. Works on **Windows**, **macOS**, and **Linux**.

## What's New in v2.0

### ADB Bridge MCP Server

Claude Code can now interact directly with your Android device through 18 native tools:

| Tool | What it does |
|------|-------------|
| `screenshot` | Capture the device screen as a PNG image |
| `crash_log` | Get FATAL EXCEPTION / ANR / native crash traces |
| `app_logs` | Filtered logcat by package, level, time window, regex |
| `tap` | Tap UI elements by text, content-desc, resource-id, or coordinates |
| `swipe` | Swipe gestures by coordinates or direction |
| `input_text` | Type text into the focused field |
| `press_key` | Press back, home, enter, volume, power, etc. |
| `ui_tree` | Get the current UI element hierarchy |
| `app_status` | Check if app is running, foreground/background, memory, PID |
| `device_info` | Model, Android version, screen size, battery, storage |
| `list_packages` | List installed apps |
| `install_apk` | Install an APK with auto-grant permissions |
| `launch_app` | Launch an app by package name |
| `force_stop` | Force stop an app |
| `shell` | Run any ADB shell command |
| `pixel_color` | Sample pixel RGB/hex/luminance at coordinates |
| `performance_snapshot` | FPS, janky frames, memory breakdown |
| `screen_record` | Record screen to MP4 (max 180s) |

### One-Click Setup

Click the copy button in the app header to copy the Claude Code MCP configuration to your clipboard — paste it into your `~/.claude/settings.json` and you're ready to go.

### Cross-Platform

The MCP server runs on Windows, macOS (Intel + Apple Silicon), and Linux.

---

## Features

### Screen Mirroring
- **Live Screen Mirroring** via scrcpy
- **Automatic Device Detection** with continuous ADB polling
- **Quality Presets**: Low, Balanced, High
- **Auto-Mirror**: Start mirroring automatically when a device connects
- **Screenshot to Clipboard**: Capture and copy device screenshots instantly
- **Error Monitoring**: Real-time logcat error capture during mirroring

### MCP Server for Claude Code
- **18 ADB tools** exposed as native Claude Code tools
- **No Bash wrappers** — tools appear alongside Read, Write, Grep
- **Auto-detect device** — most tools work without specifying a serial
- **Smart element finding** — `tap` parses the UI hierarchy to find elements by text
- **JSON-RPC over stdio** — follows the MCP specification

## Requirements

- **.NET 8 SDK** (`dotnet --version` to check)
- **ADB** in PATH, or `ANDROID_HOME` / `ANDROID_SDK_ROOT` set
- **scrcpy** (for mirroring only — MCP server doesn't need it)
- **Android device** with USB debugging enabled

## Installation

### From Release (recommended)

Download the latest release for your platform from [Releases](../../releases):

- **Windows**: `adb-bridge-win-x64.exe` + `PhoneMirror-win-x64.exe`
- **macOS Intel**: `adb-bridge-osx-x64`
- **macOS Apple Silicon**: `adb-bridge-osx-arm64`
- **Linux**: `adb-bridge-linux-x64`

### From Source

```bash
git clone https://github.com/user/AdbMirror.git
cd AdbMirror
dotnet build
```

## Quick Start

### Screen Mirroring

1. Connect your Android device via USB with USB debugging enabled
2. Authorize the computer when prompted on your phone
3. Launch PhoneMirror
4. Select quality preset and click **Mirror**

### Claude Code Integration

**Option A**: Click the copy button in the PhoneMirror app header, paste into `~/.claude/settings.json`

**Option B**: Add manually to `~/.claude/settings.json`:

```jsonc
{
  "mcpServers": {
    "adb-bridge": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/src/PhoneMirror.Mcp/PhoneMirror.Mcp.csproj"]
    }
  }
}
```

**Option C**: Use the published binary:

```jsonc
{
  "mcpServers": {
    "adb-bridge": {
      "command": "C:/path/to/adb-bridge.exe"
    }
  }
}
```

Then in Claude Code, the tools appear natively. Claude can:
- Take screenshots and analyze them
- Tap buttons by their text label
- Read crash logs and fix bugs
- Run automated QA sequences
- Check performance metrics

## Building

### Mirror App (Windows)

```powershell
dotnet build src/PhoneMirror/PhoneMirror.csproj
dotnet run --project src/PhoneMirror/PhoneMirror.csproj
```

### MCP Server (cross-platform)

```bash
# Development
dotnet run --project src/PhoneMirror.Mcp/PhoneMirror.Mcp.csproj

# Publish for your platform
dotnet publish src/PhoneMirror.Mcp/PhoneMirror.Mcp.csproj -c Release -r win-x64 -p:PublishSingleFile=true
dotnet publish src/PhoneMirror.Mcp/PhoneMirror.Mcp.csproj -c Release -r osx-arm64 -p:PublishSingleFile=true
dotnet publish src/PhoneMirror.Mcp/PhoneMirror.Mcp.csproj -c Release -r linux-x64 -p:PublishSingleFile=true
```

## Architecture

```
PhoneMirror.sln
├── src/PhoneMirror/          # Avalonia desktop UI app
│   ├── Views/                # XAML views
│   ├── ViewModels/           # MVVM view models
│   └── Services/             # UI services (clipboard, MCP setup generator)
├── src/PhoneMirror.Core/     # Shared core library
│   ├── Services/             # ADB, scrcpy, screenshot, logcat, settings
│   ├── Models/               # Data models
│   ├── Execution/            # Process runner
│   └── Platform/             # Cross-platform abstractions
└── src/PhoneMirror.Mcp/      # MCP server (Claude Code integration)
    ├── McpServer.cs          # JSON-RPC protocol handler
    ├── AdbToolHandler.cs     # 18 tool implementations
    └── McpTypes.cs           # Protocol DTOs
```

## Troubleshooting

### MCP server: "ADB not available"
- Ensure ADB is in your PATH or set `ANDROID_HOME`
- The MCP server logs to stderr — check Claude Code's MCP output panel

### Device not detected
- Enable USB debugging on your Android device
- Authorize the computer when prompted
- Check `adb devices` in a terminal

### `tap` tool can't find elements
- Use `ui_tree` first to see what's on screen
- Element text matching is case-insensitive and uses "contains"
- Some elements may not have text — use `content_desc` or `resource_id` instead

### macOS: permission errors
- Grant Terminal / IDE access in System Preferences > Privacy > Developer Tools
- Run `xattr -d com.apple.quarantine ./adb-bridge` if macOS blocks the binary

## License

See LICENSE file for details.

## Credits

Built by [Labyrica](https://labyrica.com) — Data driven solutions.
