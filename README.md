# AdbMirror

**Phone Mirror** — A tactical Windows desktop application for live Android device mirroring via ADB and scrcpy.

## Overview

AdbMirror provides a streamlined interface to mirror your Android phone's screen to your Windows PC in real-time. Built with a dark, high-contrast UI following the Labyrica design language, it offers a focused experience for developers, testers, and power users who need reliable device mirroring.

## Features

- **Live Screen Mirroring**: Real-time Android device screen mirroring using scrcpy
- **Automatic Device Detection**: Continuously monitors for connected Android devices via ADB
- **Quality Presets**: Choose from Low, Balanced, or High quality presets for optimal performance
- **Auto-Mirror**: Optionally start mirroring automatically when a device connects
- **Device Status Monitoring**: Real-time status updates showing device connection state
- **Event Logging**: Detailed event log with timestamps for troubleshooting
- **Settings Persistence**: Saves your preferences (default preset, auto-mirror, fullscreen options)

## Requirements

- **Windows** (targets .NET 8.0 Windows)
- **.NET SDK 8.0** or compatible runtime
- **ADB (Android Debug Bridge)**: Either bundled with the app or available on PATH
- **scrcpy**: Either bundled with the app or available on PATH
- **Android Device**: USB debugging enabled and authorized

## Installation

1. Clone or download this repository
2. Ensure you have the .NET SDK 8.0 installed
3. Build the application (see [BUILD.md](BUILD.md) for instructions)

## Quick Start

1. **Connect your Android device** via USB with USB debugging enabled
2. **Authorize the computer** when prompted on your phone
3. **Launch AdbMirror**
4. Select your desired **Quality Preset** (Low/Balanced/High)
5. Click **Mirror** to start screen mirroring
6. Click **Stop** to end the mirroring session

## Usage

### Main Interface

- **Left Panel**: Shows device status, ADB path information, and event logs
- **Right Panel**: Device controls, mirror button, quality preset selector, and settings

### Quality Presets

- **Low**: Lower resolution/bitrate for better performance on slower connections
- **Balanced**: Default preset with good balance of quality and performance
- **High**: Higher resolution/bitrate for best visual quality

### Settings

Access advanced settings via the gear icon next to the quality preset:

- **Auto mirror when device connects**: Automatically start mirroring when a device is detected
- **Start scrcpy in fullscreen**: Launch the mirror window in fullscreen mode
- **Default quality preset**: Set your preferred preset as the default

### Event Log

The event log shows:
- Device connection/disconnection events
- ADB and scrcpy status messages
- Error messages and troubleshooting information
- Use the copy button (document icon) to copy logs to clipboard

## Building

See [BUILD.md](BUILD.md) for detailed build instructions.

Quick build commands:

**PowerShell:**
```powershell
cd AdbMirror
dotnet build
dotnet run
```

**CMD:**
```cmd
cd AdbMirror
dotnet build
dotnet run
```

Or use the provided scripts:
- `build_and_run.cmd` (Windows CMD)
- `build_and_run.ps1` (PowerShell)

## Architecture

- **AdbService**: Handles ADB operations, device discovery, and state polling
- **ScrcpyService**: Manages scrcpy process lifecycle and quality presets
- **MainViewModel**: UI state management and business logic
- **AppSettings**: Persistent settings storage (JSON)

## Dependencies

- **scrcpy**: Screen mirroring engine (bundled or system-installed)
- **ADB**: Android Debug Bridge (bundled or system-installed)
- **.NET 8.0 Windows**: WPF framework for the desktop UI

## Troubleshooting

### "ADB not found"
- Ensure ADB is bundled in `platform-tools/` directory, or
- Install Android SDK platform-tools and add to PATH, or
- Set ANDROID_HOME/ANDROID_SDK_ROOT environment variables

### "scrcpy not found"
- Ensure scrcpy is bundled in `scrcpy/` directory, or
- Install scrcpy and add to PATH

### Device not detected
- Enable USB debugging on your Android device
- Authorize the computer when prompted
- Check USB cable connection
- Try different USB port or cable

### Mirroring fails to start
- Check event log for specific error messages
- Ensure device is in "Connected" state (not "Unauthorized" or "Offline")
- Verify scrcpy is available and working
- Try a different quality preset

## License

See LICENSE file for details.

## Credits

Built by [Labyrica](https://labyrica.com) — Data driven solutions.

