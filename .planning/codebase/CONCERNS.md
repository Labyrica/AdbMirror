# Codebase Concerns

**Analysis Date:** 2025-01-15

## Tech Debt

**Silent exception swallowing:**
- Issue: Multiple empty catch blocks hide failures
- Files: `AdbMirror/Core/AdbService.cs` (lines ~126, 315), `AdbMirror/Core/ScrcpyService.cs` (lines ~196, 241, 300), `AdbMirror/AppSettings.cs` (line 55)
- Why: Quick resilience for non-critical operations
- Impact: Hard to debug when things fail silently
- Fix approach: Add logging to catch blocks, at minimum Debug.WriteLine

**RelayCommand in ViewModel file:**
- Issue: Helper class defined in `MainViewModel.cs` instead of separate file
- File: `AdbMirror/MainViewModel.cs` (lines 357-380)
- Why: Quick implementation, single use
- Impact: Minor - harder to find/reuse
- Fix approach: Extract to `AdbMirror/Core/RelayCommand.cs`

**Unused ShowSettings method:**
- Issue: Dead code - method does nothing, kept "for compatibility"
- File: `AdbMirror/MainViewModel.cs` (lines 302-306)
- Why: Settings moved inline, method not removed
- Impact: Confusing code, minor clutter
- Fix approach: Remove method entirely

## Known Bugs

**StartFullscreen setting not used:**
- Symptoms: Setting exists in UI but has no effect
- File: `AdbMirror/MainViewModel.cs` (property at line 56), `AdbMirror/Core/ScrcpyService.cs` (not passed to BuildArguments)
- Trigger: Enable "Start scrcpy in fullscreen" - no change in behavior
- Workaround: None
- Root cause: Property saved but never passed to scrcpy command
- Fix: Add `--fullscreen` flag in `BuildArguments()` when enabled

## Security Considerations

**Hardcoded scrcpy download URL:**
- Risk: URL points to specific version (v3.1), could become unavailable or compromised
- File: `AdbMirror/Core/ScrcpyService.cs` (line 129)
- Current mitigation: Fallback only - primary resolution uses bundled/PATH
- Recommendations: Consider checking GitHub API for latest release, or remove auto-download

**No command injection protection:**
- Risk: Device serial is passed to process arguments - malicious serial could inject commands
- Files: `AdbMirror/Core/ScrcpyService.cs` (line 314), `AdbMirror/Core/AdbService.cs` (line 181)
- Current mitigation: Serial comes from ADB output (trusted source)
- Recommendations: Validate serial format (alphanumeric + colon/underscore only)

## Performance Bottlenecks

**UI thread blocking on settings save:**
- Problem: Settings saved synchronously on every change
- File: `AdbMirror/AppSettings.cs` → `Save()`, called from `AdbMirror/MainViewModel.cs`
- Measurement: Minimal impact (JSON is small), but blocks UI momentarily
- Cause: Synchronous File.WriteAllText
- Improvement path: Debounce saves or use async file operations

**Polling interval:**
- Problem: Device polling every 1 second spawns new adb process
- File: `AdbMirror/MainViewModel.cs` (line 169)
- Measurement: Constant CPU/process overhead when idle
- Cause: Process spawn for each poll
- Improvement path: Consider longer interval (2-3s) or event-based detection via adb track-devices

## Fragile Areas

**Device serial parsing:**
- File: `AdbMirror/Core/AdbService.cs` → `GetDevices()` (lines 179-218)
- Why fragile: String parsing of `adb devices -l` output, format could change
- Common failures: Unexpected output format from different ADB versions
- Safe modification: Add unit tests with captured output samples
- Test coverage: None

**scrcpy working directory requirement:**
- File: `AdbMirror/Core/ScrcpyService.cs` (line 226)
- Why fragile: scrcpy needs working directory set to find DLLs
- Common failures: DLL not found errors if path is wrong
- Safe modification: Verify path resolution before launch
- Test coverage: None

## Scaling Limits

**Log buffer size:**
- Current capacity: 10,000 characters max
- File: `AdbMirror/MainViewModel.cs` (line 24)
- Limit: Older logs truncated
- Symptoms at limit: Important early logs lost in long sessions
- Scaling path: Add log rotation or export to file

**Single device assumption:**
- Current capacity: Works with multiple devices but UI shows only first
- File: `AdbMirror/Core/AdbService.cs` (line 244-245)
- Limit: Can only mirror one device at a time
- Symptoms at limit: Ambiguous which device is selected
- Scaling path: Add device picker dropdown

## Dependencies at Risk

**None identified:**
- Zero external NuGet dependencies
- Only .NET framework and Windows SDK
- External tools (ADB, scrcpy) are versioned separately

## Missing Critical Features

**No device picker:**
- Problem: Multiple devices show only first, no way to choose
- File: `AdbMirror/Core/AdbService.cs` → `GetHighLevelState()` just picks devices[0]
- Current workaround: Disconnect other devices
- Blocks: Using app with multiple devices attached
- Implementation complexity: Low (add ComboBox bound to device list)

**No audio mirroring indication:**
- Problem: scrcpy supports audio but app doesn't expose it
- Current workaround: Edit scrcpy arguments manually
- Blocks: Audio mirroring feature discoverability
- Implementation complexity: Low (add checkbox, append `--audio-codec=opus`)

## Test Coverage Gaps

**Entire codebase untested:**
- What's not tested: All functionality - no test project exists
- Risk: Regressions go unnoticed, parsing bugs not caught
- Priority: Medium
- Difficulty to test: Process spawning requires abstraction layer

**Device state machine:**
- What's not tested: State transitions in `MainViewModel`
- Risk: Edge cases in state handling could cause UI bugs
- Priority: Low
- Difficulty to test: Needs mock AdbService

---

*Concerns audit: 2025-01-15*
*Update as issues are fixed or new ones discovered*
