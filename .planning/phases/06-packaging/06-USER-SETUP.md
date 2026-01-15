# Phase 6: User Setup Required

**Generated:** 2026-01-15
**Phase:** 06-packaging
**Status:** Incomplete

## Overview

Phone Mirror requires platform-tools (ADB) and scrcpy binaries to be embedded for distribution. Due to licensing requirements, these must be downloaded manually.

## Environment Variables

None required - binaries are embedded in the application.

## Resource Downloads

### 1. Platform Tools (ADB)

| Status | Platform | Download URL | Destination |
|--------|----------|--------------|-------------|
| [ ] | Windows | https://dl.google.com/android/repository/platform-tools-latest-windows.zip | `src/PhoneMirror.Core/Resources/platform-tools/` |
| [ ] | macOS | https://dl.google.com/android/repository/platform-tools-latest-darwin.zip | `src/PhoneMirror.Core/Resources/platform-tools/` |
| [ ] | Linux | https://dl.google.com/android/repository/platform-tools-latest-linux.zip | `src/PhoneMirror.Core/Resources/platform-tools/` |

**Instructions:**
1. Download the appropriate ZIP for your build platform
2. Extract the contents
3. Copy the extracted `platform-tools` folder to `src/PhoneMirror.Core/Resources/platform-tools/`

### 2. Scrcpy

| Status | Download Location | Destination |
|--------|-------------------|-------------|
| [ ] | https://github.com/Genymobile/scrcpy/releases/latest | `src/PhoneMirror.Core/Resources/scrcpy/` |

**Instructions:**
1. Visit the releases page
2. Download the appropriate version for your build platform:
   - Windows: `scrcpy-win64-vX.X.zip`
   - macOS: `scrcpy-macos-vX.X.tar.gz`
   - Linux: `scrcpy-linux-vX.X.tar.gz`
3. Extract the contents
4. Copy the extracted `scrcpy` files to `src/PhoneMirror.Core/Resources/scrcpy/`

## Prepare Resources

After downloading and extracting both dependencies:

```powershell
# Run the preparation script
pwsh scripts/prepare-resources.ps1
```

This creates `platform-tools.zip` and `scrcpy.zip` in the Resources folder.

## Build with Embedded Resources

```powershell
# Build (resources will be embedded)
dotnet build src/PhoneMirror -c Release

# Or publish for distribution
pwsh scripts/publish-windows.ps1
```

## Verification

Run the following to verify resources are embedded:

```powershell
# Check the published executable size (should be ~90MB+ with resources)
ls publish/windows/PhoneMirror.exe
```

Without embedded resources, the app will fall back to searching PATH for adb and scrcpy.

---
**Once all items complete:** Mark status as "Complete"
