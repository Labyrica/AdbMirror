# External Integrations

**Analysis Date:** 2025-01-15

## External Tools

**Android Debug Bridge (ADB):**
- Purpose: Device discovery, communication, and state management
- Binary: `adb.exe` from Android platform-tools
- Integration: `AdbMirror/Core/AdbService.cs`
- Commands used:
  - `adb version` - Availability check
  - `adb start-server` - Ensure server running
  - `adb devices -l` - List connected devices with details
- Discovery order:
  1. Embedded resources (extracted from `platform-tools.zip`)
  2. Bundled `platform-tools/` folder next to app
  3. ANDROID_HOME / ANDROID_SDK_ROOT environment variables
  4. System PATH
  5. `where adb` command result

**scrcpy:**
- Purpose: Android screen mirroring and control
- Binary: `scrcpy.exe` from Genymobile/scrcpy releases
- Integration: `AdbMirror/Core/ScrcpyService.cs`
- Arguments used:
  - `-s <serial>` - Target specific device
  - `--video-bit-rate` - Quality preset (4M/8M/16M)
  - `--max-size` - Resolution limit (1024/1280/1920)
  - `--max-fps` - Frame rate limit (30/60)
  - `--turn-screen-off` - Turn off device screen during mirror
  - `--stay-awake` - Keep device awake
- Discovery order:
  1. Embedded resources (extracted from `scrcpy.zip`)
  2. Bundled `scrcpy/` folder next to app
  3. System PATH
  4. Auto-download fallback from GitHub releases

## APIs & External Services

**GitHub Releases API:**
- Purpose: Fallback scrcpy download
- URL: `https://github.com/Genymobile/scrcpy/releases/download/v3.1/scrcpy-win64-v3.1.zip`
- Integration: `AdbMirror/Core/ScrcpyService.cs` → `TryBootstrapScrcpy()`
- Auth: None required (public release)
- Trigger: Only when scrcpy not found anywhere else

## Data Storage

**Local Settings:**
- Type: JSON file
- Location: `%LocalAppData%\AdbMirror\settings.json`
- Integration: `AdbMirror/AppSettings.cs`
- Contents:
  - `DefaultPreset` - Quality preset (Low/Balanced/High)
  - `AutoMirrorOnConnect` - Auto-start mirroring
  - `StartFullscreen` - Fullscreen scrcpy window
  - `KeepScreenAwake` - Keep device screen awake

**Temporary Resources:**
- Location: `%TEMP%\AdbMirror\<guid>/`
- Purpose: Extracted platform-tools and scrcpy binaries
- Integration: `AdbMirror/Core/ResourceExtractor.cs`
- Lifecycle: Created on first access, cleaned on exit (best-effort)

## Environment Configuration

**Development:**
- Required: None (falls back to bundled/PATH tools)
- Optional: `ANDROID_HOME` or `ANDROID_SDK_ROOT` for ADB location
- Device: Android device with USB debugging enabled for testing

**Production:**
- Self-contained single-file executable
- Embedded resources: `platform-tools.zip`, `scrcpy.zip` (if bundled)
- Fallback: System PATH for ADB/scrcpy

## Process Management

**ADB Process:**
- Spawned for: Each command (version, start-server, devices)
- Lifetime: Short-lived, waits for completion with timeout
- Error handling: Exit code + stderr capture
- Location: `AdbMirror/Core/AdbService.cs` → `RunAdbCommandRaw()`

**scrcpy Process:**
- Spawned for: Active mirroring session
- Lifetime: Long-running until user stops or device disconnects
- Exit handling: `Exited` event triggers callback with status/error
- Working directory: Set to scrcpy.exe location for DLL discovery
- Location: `AdbMirror/Core/ScrcpyService.cs` → `StartMirroring()`

## Webhooks & Callbacks

**Incoming:**
- None

**Outgoing:**
- None

**Internal Callbacks:**
- Device state observer: `Action<DeviceState, AndroidDevice?>` in `AdbService.StartPollingAsync()`
- scrcpy exit callback: `Action<string>` in `ScrcpyService.StartMirroring()`

---

*Integration audit: 2025-01-15*
*Update when adding/removing external services*
