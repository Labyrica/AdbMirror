# WPF-to-Avalonia Migration Pitfalls & Cross-Platform Tool Bundling

Research conducted: 2026-01-15
Domain: Cross-platform desktop utility (Android device mirroring)
Context: WPF to Avalonia migration with bundled ADB/scrcpy executables

---

## Critical Pitfalls

### 1. XAML Styling System Mismatch

**What Goes Wrong:** Developers copy WPF styles directly into Avalonia expecting them to work the same way. WPF `Style` with `ControlTemplate` becomes invalid because Avalonia uses a CSS-like styling system with `ControlTheme` for lookless controls.

**Why It Happens:** Avalonia's `Style` is similar to CSS selectors, not WPF styles. The WPF equivalent in Avalonia is `ControlTheme`, which was introduced in Avalonia 11 to solve the problem of styles that couldn't be removed once applied.

**How to Avoid:**
- Convert WPF `Style` with `ControlTemplate` to Avalonia `ControlTheme`
- Place `ControlTheme` in `Resources` collection, not `Styles` collection
- Replace `VisualStateManager` triggers with pseudoclasses (`:hover`, `:focus`, `:pressed`, `:checked`)
- Replace `Storyboard` animations with `Transitions` and `Animation`

**Warning Signs:**
- Styles not applying or leaking between controls
- Visual state changes not working
- "Cannot find style" errors at runtime

**Phase to Address:** Phase 1 (UI Migration) - Convert all XAML files systematically

---

### 2. RelativeSource Binding Failures

**What Goes Wrong:** RelativeSource bindings that work in WPF completely fail in Avalonia despite documentation claiming compatibility.

**Why It Happens:** Avalonia has different binding syntax and the `RelativeSource` implementation differs from WPF. Many developers report that direct WPF RelativeSource bindings simply don't work.

**How to Avoid:**
- Replace `{Binding RelativeSource={RelativeSource AncestorType=...}}` with Avalonia's `$parent` syntax
- Use `{Binding $parent[local:MyControl].Property}` instead
- For TemplatedParent, use `{TemplateBinding Property}` or `{Binding Property, RelativeSource={RelativeSource TemplatedParent}}`

**Warning Signs:**
- Bindings returning null or not updating
- "Cannot find source" binding errors in output
- Data not flowing from parent controls

**Phase to Address:** Phase 1 (UI Migration) - Audit all bindings during conversion

---

### 3. Dispatcher Threading Deadlocks

**What Goes Wrong:** Code using `Dispatcher.UIThread.InvokeAsync(...).Wait()` causes deadlocks that didn't occur in WPF.

**Why It Happens:** Avalonia's dispatcher behaves differently from WPF's `System.Windows.Threading.Dispatcher`. Synchronous invocation patterns that worked in WPF cause deadlocks in Avalonia.

**How to Avoid:**
- Use `Dispatcher.UIThread.Post()` for fire-and-forget operations
- Use `await Dispatcher.UIThread.InvokeAsync()` with proper async/await
- Never call `.Wait()` or `.Result` on dispatcher tasks
- Avoid mixing synchronous WPF patterns with Avalonia

**Warning Signs:**
- UI freezes during background operations
- Application hangs when updating UI from threads
- Input lag or unresponsive controls

**Phase to Address:** Phase 2 (Process Management) - Refactor all threading code

---

### 4. Linux Executable Permission Missing After Extraction

**What Goes Wrong:** Bundled executables (ADB, scrcpy) extracted from embedded resources lack execute permission on Linux/macOS, causing "Permission denied" errors.

**Why It Happens:** .NET embedded resources are just binary data - file permissions are not preserved. Windows doesn't have the Unix permission model, so this works fine there.

**How to Avoid:**
- After extraction, call `chmod +x` via Process.Start on Linux/macOS
- Use `File.SetUnixFileMode()` (.NET 7+) to set permissions programmatically
- Consider `Mono.Posix.NETStandard` NuGet package for more control
- Test extraction and execution on all three platforms

**Warning Signs:**
- "Permission denied" errors only on Linux/macOS
- Process.Start throwing exceptions with extracted files
- Works on Windows but fails on Unix systems

**Phase to Address:** Phase 3 (Cross-Platform) - Implement platform-specific extraction

---

### 5. macOS Gatekeeper Quarantine Blocking

**What Goes Wrong:** Extracted executables are blocked by macOS Gatekeeper because they inherit the quarantine extended attribute from downloaded archives.

**Why It Happens:** macOS applies `com.apple.quarantine` to downloaded files and propagates it to extracted contents. Unsigned executables extracted at runtime are blocked.

**How to Avoid:**
- Remove quarantine attribute after extraction: `xattr -d com.apple.quarantine <path>`
- Consider code signing bundled executables
- For distribution, use notarized .app bundles
- Don't use Archive Utility programmatically; use command-line tools that don't propagate quarantine

**Warning Signs:**
- "App is damaged and can't be opened" on macOS
- Executables work when manually downloaded but not when extracted
- Works on developer machine but not on user machines

**Phase to Address:** Phase 4 (Packaging) - Implement macOS-specific handling

---

### 6. ADB Version Conflicts

**What Goes Wrong:** Bundled ADB conflicts with system-installed ADB, causing "adb server version doesn't match this client" errors.

**Why It Happens:** ADB server is a singleton - only one version can run. If another tool (Android Studio, system ADB) starts a different version, conflicts occur.

**How to Avoid:**
- Document the bundled ADB version clearly
- Consider using `ADB_SERVER_SOCKET` environment variable for isolation
- Provide option to use system ADB instead of bundled
- Kill existing ADB servers before starting bundled version (with user consent)
- Set `ADB` environment variable to specify which binary scrcpy should use

**Warning Signs:**
- Intermittent device connection failures
- "adb server version (X) doesn't match this client (Y)" errors
- Works on some machines but not others

**Phase to Address:** Phase 2 (Process Management) - Design ADB lifecycle management

---

### 7. Process.Start Path Resolution Differences

**What Goes Wrong:** Process.Start behaves differently across platforms - relative paths that work on Windows fail on Linux/macOS.

**Why It Happens:** File resolution order differs between platforms. Windows includes current directory in resolution; Unix doesn't by default. `UseShellExecute` behavior varies significantly.

**How to Avoid:**
- Always use absolute paths for `FileName` in `ProcessStartInfo`
- Get path via `Assembly.GetExecutingAssembly().Location` or `AppContext.BaseDirectory`
- Set `UseShellExecute = false` explicitly for cross-platform consistency
- Don't rely on PATH resolution for bundled tools

**Warning Signs:**
- "File not found" errors only on certain platforms
- Process works in development but not in packaged app
- Different behavior between self-contained and framework-dependent deployments

**Phase to Address:** Phase 2 (Process Management) - Abstract process spawning

---

### 8. stdout/stderr Deadlock with Large Output

**What Goes Wrong:** Process hangs indefinitely when ADB or scrcpy produces large output because the pipe buffer fills up.

**Why It Happens:** OS pipe buffers have limited size (~64KB). If parent waits for process exit before reading output, and process blocks on full buffer, deadlock occurs.

**How to Avoid:**
- Use `BeginOutputReadLine()` and `BeginErrorReadLine()` for async reading
- Or use `ReadToEndAsync()` on both streams concurrently with `Task.WhenAll()`
- Never do `process.WaitForExit()` before reading redirected streams
- Read stdout and stderr on separate threads or use async patterns

**Warning Signs:**
- Process hangs after running for a while
- Works with small outputs, hangs with large ones
- Inconsistent behavior based on device/operation

**Phase to Address:** Phase 2 (Process Management) - Implement async process output handling

---

### 9. Path Separator Hardcoding

**What Goes Wrong:** Hardcoded backslashes (`\`) in paths break on Linux/macOS.

**Why It Happens:** Developers on Windows naturally use backslashes. `Path.Combine()` doesn't normalize existing separators - it preserves them.

**How to Avoid:**
- Use `Path.Combine()` for all path construction
- Use `Path.DirectorySeparatorChar` when building paths manually
- Forward slash (`/`) works on all platforms if hardcoding is unavoidable
- Be aware that `Path.Combine("a", "b\\c")` produces `/a/b\c` on Linux

**Warning Signs:**
- "File not found" with paths containing backslashes on Unix
- Paths look correct in logs but don't resolve
- Works on Windows, fails on Linux/macOS

**Phase to Address:** Phase 3 (Cross-Platform) - Audit all path handling code

---

### 10. avares:// URI Scheme Confusion

**What Goes Wrong:** Assets load in WPF with `pack://` URIs but fail in Avalonia, or assets from other assemblies don't load.

**Why It Happens:** Avalonia uses `avares://` scheme instead of WPF's `pack://`. Also, relative paths only work within the same assembly.

**How to Avoid:**
- Use relative paths for same-assembly assets: `<Image Source="Assets/icon.png"/>`
- Use `avares://AssemblyName/Path/file.ext` for cross-assembly resources
- Note: It's `avares://` (two slashes), not `avares:/`
- No support for `file://`, `http://`, or `https://` schemes - implement yourself if needed

**Warning Signs:**
- Images/resources showing blank or throwing exceptions
- Assets work in designer but not at runtime
- "Could not find resource" errors

**Phase to Address:** Phase 1 (UI Migration) - Convert all resource references

---

## Technical Debt Patterns

| Pattern | Symptom | Root Cause | Remediation | Priority |
|---------|---------|------------|-------------|----------|
| WPF Style Copypasta | Styles don't apply, visual glitches | Direct XAML copy without conversion | Convert to ControlTheme, use pseudoclasses | High |
| Synchronous Dispatcher | UI freezes, deadlocks | `.Wait()` on dispatcher calls | Use async/await properly | High |
| Hardcoded Windows Paths | Unix failures | Backslash literals, no Path.Combine | Audit and fix all path code | High |
| Single-stream Process Reading | Hangs with large output | Synchronous stdout/stderr reading | Implement async dual-stream reading | High |
| Missing Chmod | Linux "permission denied" | Embedded resources lose permissions | Add platform-specific chmod | Medium |
| Blocking Process.Start | UI lag during tool execution | Synchronous process management | Move to async Task-based spawning | Medium |
| Magic Path Strings | Deployment failures | Relative paths, assumed locations | Use AppContext.BaseDirectory | Medium |
| VisualStateManager Usage | States don't change | VSM not supported in Avalonia | Convert to pseudoclasses | Medium |
| DataTemplateSelector | Runtime errors | Different implementation in Avalonia | Implement IDataTemplate instead | Medium |
| Implicit DataType Templates | Templates not applying | WPF implicit typing doesn't work same | Use explicit x:DataType | Low |

---

## Integration Gotchas (External Process Management)

### ADB/scrcpy Lifecycle Management

| Issue | Description | Solution |
|-------|-------------|----------|
| Server Port Conflicts | ADB server binds to 5037, may conflict | Allow configurable port, detect conflicts |
| Orphaned Processes | ADB server persists after app exit | Track PIDs, cleanup on shutdown |
| Device State Changes | Device connect/disconnect during operation | Implement device monitoring, graceful recovery |
| Multiple App Instances | Two instances fighting over ADB | Implement single-instance check or shared server |
| scrcpy Process Cleanup | scrcpy window stays open after disconnect | Kill process tree, not just parent |

### Process Output Handling Best Practices

```csharp
// WRONG - Causes deadlock with large output
process.WaitForExit();
var output = process.StandardOutput.ReadToEnd();
var error = process.StandardError.ReadToEnd();

// RIGHT - Async dual-stream reading
var outputTask = process.StandardOutput.ReadToEndAsync();
var errorTask = process.StandardError.ReadToEndAsync();
await Task.WhenAll(outputTask, errorTask);
await process.WaitForExitAsync();
```

### Platform-Specific Process.Start Patterns

```csharp
// Cross-platform executable execution
var startInfo = new ProcessStartInfo
{
    FileName = GetAbsoluteToolPath("adb"), // Always absolute
    Arguments = args,
    UseShellExecute = false,  // Required for redirection
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    CreateNoWindow = true
};

// Linux/macOS: Set execute permission first
if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    Process.Start("chmod", $"+x \"{startInfo.FileName}\"")?.WaitForExit();
}
```

---

## Performance Traps

### Avalonia vs WPF Performance Differences

| Scenario | WPF Behavior | Avalonia Behavior | Mitigation |
|----------|--------------|-------------------|------------|
| Many visual elements (100+) | Handles well | Can drop to 15fps | Use virtualization, custom OnRender |
| Hidden elements | No perf impact | Still affects rendering | Remove from visual tree instead |
| Drag operations | Smooth at 60fps | Notable input lag | Retained mode limitation, minimize redraws |
| Canvas with many items | Good with CacheMode | No CacheMode equivalent | Implement virtualized canvas |
| Complex bindings | Reflection-based | Reflection-based | Use CompiledBindings |

### Performance Optimization Checklist

- [ ] Enable `CompiledBindings` globally (`<x:CompileBindings>true</x:CompileBindings>`)
- [ ] Use virtualization for lists (ItemsRepeater, TreeDataGrid)
- [ ] Minimize visual tree depth
- [ ] Use `StreamGeometry` instead of `PathGeometry`
- [ ] Avoid layout-triggering changes during animations
- [ ] For custom drawing, override `OnRender` in custom control
- [ ] Consider GPU memory settings for Skia

### scrcpy/ADB Performance Considerations

- scrcpy video decoding is CPU/GPU intensive
- Multiple device connections multiply resource usage
- ADB commands can be batched to reduce process spawn overhead
- Consider connection pooling for frequent ADB operations

---

## Security Mistakes

### Command Injection Vulnerabilities

**HIGH RISK:** Constructing ADB commands with user input without sanitization.

```csharp
// VULNERABLE - Command injection possible
var filename = userInput; // Could be "file; rm -rf /"
Process.Start("adb", $"push {filename} /sdcard/");

// SAFER - Use argument arrays, validate input
var args = new[] { "push", SanitizeFilename(filename), "/sdcard/" };
```

**Prevention Strategies:**
1. Never concatenate user input into command strings
2. Use argument arrays/lists instead of string concatenation
3. Validate all inputs against allowlists where possible
4. Escape special characters (`;`, `&`, `|`, `$`, backticks)
5. Run processes with least privilege
6. Don't use shell execution (`cmd /c`, `/bin/bash -c`) with user data

### Sensitive Data Exposure

| Risk | Scenario | Mitigation |
|------|----------|------------|
| Device ID Logging | ADB serial numbers in logs | Mask device IDs in user-visible logs |
| Path Disclosure | Full paths in error messages | Use relative paths in UI messages |
| Temp File Exposure | Extracted tools in world-readable temp | Use user-specific temp directory |
| Process Arguments | Sensitive data in command line | Avoid passing secrets via args |

### macOS Code Signing Requirements

For notarization (required for distribution outside App Store):
1. All Mach-O binaries must be signed with Developer ID
2. Hardened runtime must be enabled
3. Entitlements file required for .NET apps (`com.apple.security.cs.allow-jit`)
4. Sign "bottom-up" - deepest binaries first, then parent bundles
5. Never use `--deep` flag with codesign
6. DLLs go in `/Resources`, not `/MacOS`

---

## UX Pitfalls

### Platform Convention Violations

| Convention | Windows | macOS | Linux |
|------------|---------|-------|-------|
| Window controls position | Right | Left | Varies (usually right) |
| Menu bar | In window | System menu bar | In window |
| Default font | Segoe UI | San Francisco | System default |
| Button style | Square/rounded | Rounded | Varies by DE |
| Tray icon behavior | Click for menu | Click for menu | Varies |
| File dialogs | Native | Native | May need GTK/Qt |

### Common UX Mistakes

1. **Ignoring Dark Mode:** Not responding to system theme changes
2. **Fixed Layouts:** Not adapting to different DPI/scaling
3. **Windows-centric Paths:** Showing backslash paths on macOS/Linux
4. **Notification Spam:** Not respecting system notification settings
5. **Native Dialog Gaps:** Using non-native dialogs that feel foreign
6. **Missing Keyboard Shortcuts:** Platform-specific shortcuts (Cmd vs Ctrl)
7. **Icon Size Issues:** Not providing proper sizes for retina/HiDPI

### Device Mirroring Specific UX Issues

- Don't block UI during device connection (use async with progress)
- Show clear device state (connected/disconnected/error)
- Graceful handling of USB permission prompts
- Clear error messages when ADB not detected/configured
- Handle device disconnection mid-session gracefully

---

## "Looks Done But Isn't" Checklist

### UI Migration

- [ ] All VisualStateManager triggers converted to pseudoclasses
- [ ] All Storyboards converted to Transitions/Animation
- [ ] RelativeSource bindings tested and working
- [ ] ControlTemplates converted to ControlThemes
- [ ] Styles moved from Resources to Styles collection (or vice versa for themes)
- [ ] DataTemplateSelector replaced with IDataTemplate
- [ ] CompiledBindings enabled and working
- [ ] Resource URIs converted from pack:// to avares://
- [ ] Visibility converted from Visibility enum to bool IsVisible
- [ ] Label controls converted to TextBlock
- [ ] Grid row/column definitions use Avalonia shorthand syntax
- [ ] Clipboard calls updated to async API
- [ ] ShowDialog calls pass owner parameter

### Process Management

- [ ] All Process.Start uses absolute paths
- [ ] stdout/stderr read asynchronously
- [ ] Linux/macOS chmod executed after extraction
- [ ] macOS quarantine attribute handled
- [ ] Process cleanup on app exit implemented
- [ ] ADB server lifecycle managed
- [ ] Large output handling tested (device with many apps/files)
- [ ] Error streams captured and displayed
- [ ] Timeout handling for hung processes

### Cross-Platform

- [ ] No hardcoded path separators
- [ ] Path.Combine used everywhere
- [ ] Case sensitivity tested (Unix filesystems)
- [ ] Tested on actual Linux distribution (not just WSL)
- [ ] Tested on actual macOS (not just virtualized)
- [ ] Temp directory uses platform-appropriate location
- [ ] File permissions work on Unix systems
- [ ] Environment variables resolved correctly

### Packaging

- [ ] Self-contained deployment tested
- [ ] Single-file deployment tested
- [ ] macOS code signing configured
- [ ] macOS notarization configured
- [ ] Linux AppImage tested
- [ ] Windows installer tested
- [ ] Bundled tools have correct versions
- [ ] All platforms can locate extracted tools

---

## Recovery Strategies

### When Styles Break After Migration

1. Start with Avalonia's Fluent or Simple theme as base
2. Migrate styles incrementally, one control type at a time
3. Use Avalonia DevTools (F12) to inspect applied styles
4. Check pseudoclass names match Avalonia's conventions
5. Verify Style Selector syntax (different from WPF)

### When Processes Hang

1. Check for stdout/stderr buffer deadlock first
2. Add timeout to WaitForExit with fallback kill
3. Log process state before and after each operation
4. Test with large outputs (many devices, large file lists)
5. Implement cancellation token support

### When Cross-Platform Tests Fail

1. Run in Docker containers for reproducible Linux testing
2. Use macOS VM or CI service for macOS testing
3. Check file case sensitivity (Linux is case-sensitive)
4. Verify environment variables are set correctly
5. Check file permissions (ls -la)

### When Notarization Fails

1. Verify all binaries are signed (recursively check bundle)
2. Check entitlements file is correct
3. Ensure hardened runtime is enabled
4. Run `codesign --verify --verbose` on bundle
5. Check Apple's notarization logs for specific failures

---

## Pitfall-to-Phase Mapping

| Phase | Critical Pitfalls | Effort Impact |
|-------|-------------------|---------------|
| **Phase 1: UI Migration** | XAML Styling (#1), RelativeSource (#2), avares:// URIs (#10), Performance Traps | High - Foundation for all UI |
| **Phase 2: Process Management** | Dispatcher Threading (#3), stdout/stderr Deadlock (#8), ADB Conflicts (#6) | High - Core functionality |
| **Phase 3: Cross-Platform** | Linux Permissions (#4), macOS Gatekeeper (#5), Path Resolution (#7, #9) | Medium - Platform-specific fixes |
| **Phase 4: Packaging** | Code Signing, Notarization, AppImage, Distribution | Medium - One-time setup with iteration |
| **Phase 5: Polish** | UX Conventions, Performance Optimization, Security Hardening | Low-Medium - Incremental improvements |

### Recommended Order of Attack

1. **First:** Get basic UI rendering on all platforms (catch XAML issues early)
2. **Second:** Implement cross-platform process spawning abstraction
3. **Third:** Add platform-specific extraction and permission handling
4. **Fourth:** Set up packaging pipeline for each platform
5. **Last:** Polish UX, optimize performance, security audit

---

## Sources

### WPF to Avalonia Migration (High Confidence)
- [Migrating from WPF - Avalonia Docs](https://docs.avaloniaui.net/docs/get-started/wpf/) - Official migration guide
- [The Expert Guide to Porting WPF Applications to Avalonia](https://avaloniaui.net/blog/the-expert-guide-to-porting-wpf-applications-to-avalonia) - Comprehensive porting guide
- [WPF and UWP Comparison - Avalonia Docs](https://docs.avaloniaui.net/docs/get-started/wpf/comparison-of-avalonia-with-wpf-and-uwp) - Official comparison
- [Avalonia UI: Noteworthy Differences from WPF - DMC](https://www.dmcinfo.com/blog/15571/avalonia-ui-noteworthy-differences-from-wpf/) - Practical differences
- [Porting WPF to AvaloniaUI - jammer.biz](https://www.jammer.biz/porting-wpf-to-avaloniaui/) - Real-world porting experience
- [Differences in WPF and Avalonia - mcraiha](https://mcraiha.github.io/xaml/wpf/avalonia/2020/03/03/Differences-in-wpf-and-avalonia.html) - Detailed syntax differences

### Avalonia Styling & Control Themes (High Confidence)
- [Control Themes - Avalonia Docs](https://docs.avaloniaui.net/docs/basics/user-interface/styling/control-themes) - Official ControlTheme docs
- [Porting to Control Themes - Avalonia Wiki](https://github.com/AvaloniaUI/Avalonia/wiki/Porting-to-Control-Themes) - Migration guide

### Process Management & Cross-Platform (High Confidence)
- [The .NET Process class on Linux - Red Hat](https://developers.redhat.com/blog/2019/10/29/the-net-process-class-on-linux) - Authoritative Linux guidance
- [Process.StandardOutput - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.standardoutput) - Official deadlock documentation
- [Process.Start for URLs on .NET Core - brockallen](https://brockallen.com/2016/09/24/process-start-for-urls-on-net-core/) - Cross-platform workarounds

### Path Handling (High Confidence)
- [Path.Combine() isn't as cross-platform as you think - David Boike](https://www.davidboike.dev/2020/06/path-combine-isnt-as-cross-platform-as-you-think-it-is/) - Critical path gotchas
- [Classic Path.DirectorySeparatorChar gotchas - Scott Hanselman](https://www.hanselman.com/blog/classic-pathdirectoryseparatorchar-gotchas-when-moving-from-net-core-on-windows-to-linux) - Migration warnings

### macOS Code Signing (High Confidence)
- [Notarizing .NET Console Apps for macOS - Ken Muse](https://www.kenmuse.com/blog/notarizing-dotnet-console-apps-for-macos/) - .NET-specific guide
- [Publish .NET apps for macOS - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/deploying/macos) - Official documentation
- [macOS Deployment - Avalonia Docs](https://docs.avaloniaui.net/docs/deployment/macOS) - Avalonia-specific guidance

### Security (High Confidence)
- [OS Command Injection in .NET - SecureFlag](https://knowledge-base.secureflag.com/vulnerabilities/code_injection/os_command_injection__net.html) - Security knowledge base
- [CA3006: Process command injection - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca3006) - Official security guidance

### ADB/scrcpy (Medium Confidence)
- [scrcpy FAQ - GitHub](https://github.com/Genymobile/scrcpy/blob/master/FAQ.md) - Official FAQ
- [ADB version mismatch issues - GitHub](https://github.com/Genymobile/scrcpy/issues/527) - Known issues

### Linux Packaging (Medium Confidence)
- [Flatpak vs Snaps vs AppImage - Medium](https://medium.com/@krishna-alagiri/flatpak-vs-snaps-vs-appimage-vs-packages-linux-packaging-formats-compared-e0540e25a4a8) - Format comparison
- [AppImage Documentation](https://docs.appimage.org/) - Official docs

### Performance (Medium Confidence)
- [Improving Performance - Avalonia Docs](https://docs.avaloniaui.net/docs/guides/development-guides/improving-performance) - Official optimization guide
- [Low performance compared to WPF - GitHub Discussion](https://github.com/AvaloniaUI/Avalonia/discussions/15622) - Known issues

### Threading (Medium Confidence)
- [How To Access the UI Thread - Avalonia Docs](https://docs.avaloniaui.net/docs/guides/development-guides/accessing-the-ui-thread) - Official dispatcher guidance
- [Avalonia's Dispatcher - code4ward.net](https://code4ward.net/blog/2024/02/28/dispatcher/) - Detailed analysis

### UX (Low-Medium Confidence)
- [Designing desktop apps for cross-platform UX - ToDesktop](https://www.todesktop.com/blog/posts/designing-desktop-apps-cross-platform-ux) - UX best practices
