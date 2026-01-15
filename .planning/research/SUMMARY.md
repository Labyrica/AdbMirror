# Research Summary: AdbMirror Cross-Platform Migration

**Research Date:** 2026-01-15
**Project:** AdbMirror - Android device mirroring utility
**Goal:** WPF to Avalonia migration with consumer UX improvements

---

## Executive Summary

This research validates the migration from WPF to Avalonia UI for cross-platform support (Windows, macOS, Linux). The recommended stack is mature and production-ready. Key risks center on XAML style conversion, cross-platform process management, and platform-specific binary handling. The existing codebase is well-structured for migration, with most complexity in process management code that will require careful async refactoring.

**Migration Viability:** HIGH - Avalonia 11.3.x is stable, well-documented, and used by JetBrains, Unity, and GitHub.

**Effort Estimate:** ~2-3 days for core migration + platform testing

---

## Key Findings

### Stack Recommendations

| Category | Technology | Version | Confidence |
|----------|------------|---------|------------|
| UI Framework | Avalonia UI | 11.3.11 | HIGH |
| MVVM Framework | CommunityToolkit.Mvvm | 8.4.0 | HIGH |
| Theme | FluentAvaloniaUI | 2.4.1 | HIGH |
| Dependency Injection | Microsoft.Extensions.DI | 8.0.1 | HIGH |
| Installer/Updates | Velopack | 0.0.x | MEDIUM |
| Target Framework | .NET 8.0 | LTS | HIGH |

**Why CommunityToolkit.Mvvm over ReactiveUI:**
- Simpler learning curve
- AOT-friendly source generators
- Now the default in Avalonia templates
- Better fit for this app's complexity level

### Feature Landscape

**Already Implemented (Keep):**
- USB device detection with state machine
- Quality presets (Low/Balanced/High)
- Auto-mirror on connect
- Keep screen awake option
- Dark theme
- Settings persistence

**New Features (Add):**
- Screenshot to clipboard (via ADB screencap) - P0
- Full-screen mode toggle - P0
- Always-on-top option - P1
- Collapsible log panel - P1
- Session error log viewer - P1

**Differentiators vs Competitors:**
- Free fullscreen (Vysor charges for this)
- No watermarks or session limits
- Simpler than QtScrcpy
- Clean, ad-free experience

### Architecture Decisions

**Recommended Structure:**
```
AdbMirror/
├── src/
│   ├── AdbMirror/           # Avalonia app (Views, ViewModels)
│   └── AdbMirror.Core/      # Services (IAdbService, IScrcpyService)
└── tests/
    └── AdbMirror.Tests/
```

**Key Patterns:**
1. MVVM with `[ObservableProperty]` and `[RelayCommand]` attributes
2. ViewLocator for convention-based view resolution
3. Constructor injection via Microsoft.Extensions.DI
4. Platform abstraction for paths and executable names
5. Async process management with streaming output

### Critical Pitfalls (Top 5)

| Pitfall | Impact | Phase | Mitigation |
|---------|--------|-------|------------|
| XAML Styling Mismatch | HIGH | UI Migration | Convert ControlTemplate to ControlTheme, use pseudoclasses |
| Dispatcher Threading | HIGH | Process Mgmt | Use async/await, never `.Wait()` on dispatcher |
| stdout/stderr Deadlock | HIGH | Process Mgmt | Use `BeginOutputReadLine()` or async dual-stream reading |
| Linux Execute Permission | MEDIUM | Cross-Platform | Call `chmod +x` after extracting binaries |
| macOS Gatekeeper | MEDIUM | Packaging | Remove quarantine attribute, sign binaries |

### Migration Checklist (Critical Items)

1. [ ] Replace `.xaml` with `.axaml` and update namespaces
2. [ ] Convert WPF Styles to Avalonia ControlThemes
3. [ ] Replace `Application.Current.Dispatcher` with `Dispatcher.UIThread`
4. [ ] Replace `Clipboard.SetText()` with async `TopLevel.Clipboard`
5. [ ] Use `Path.Combine()` everywhere, no hardcoded backslashes
6. [ ] Implement async dual-stream reading for process output
7. [ ] Add `chmod +x` for extracted executables on Unix
8. [ ] Test on actual macOS and Linux (not just Windows)

---

## Implications for Roadmap

Based on research findings, the recommended phase structure:

### Phase 1: Project Scaffold
- Create new Avalonia solution with recommended stack
- Set up DI container and ViewLocator
- Configure FluentAvaloniaUI dark theme
- **Risk:** Low - Standard project setup

### Phase 2: Core Services Migration
- Migrate AdbService and ScrcpyService
- Implement cross-platform ProcessRunner with async output
- Add platform-specific path resolution
- **Risk:** Medium - Async refactoring required

### Phase 3: UI Migration
- Convert MainWindow.xaml to MainWindow.axaml
- Migrate styles to Avalonia selectors
- Port MainViewModel with CommunityToolkit.Mvvm
- **Risk:** Medium - XAML style conversion

### Phase 4: New Features
- Add screenshot to clipboard (ADB screencap)
- Add collapsible log panel
- Add session error log viewer
- Add fullscreen and always-on-top toggles
- **Risk:** Low - Leverages scrcpy/ADB capabilities

### Phase 5: Cross-Platform Polish
- Platform-specific binary extraction
- Linux/macOS executable permissions
- Test on all three platforms
- **Risk:** Medium - Platform-specific edge cases

### Phase 6: Packaging & Distribution
- Windows: MSIX or Velopack installer
- macOS: Signed .app bundle with notarization
- Linux: AppImage
- **Risk:** Medium - One-time setup with iteration

---

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| **Avalonia Stability** | HIGH | Production use by major companies |
| **CommunityToolkit.Mvvm** | HIGH | Microsoft-maintained, widely adopted |
| **FluentAvaloniaUI** | HIGH | Active development, good documentation |
| **Process Management** | MEDIUM | Async patterns well-documented but require care |
| **macOS Notarization** | MEDIUM | Extra steps required, tooling exists |
| **Linux Packaging** | MEDIUM | AppImage works but varies by distro |
| **Feature Parity with scrcpy** | HIGH | All features accessible via CLI flags |

### Research Gaps

1. **Real-world Avalonia performance** - Need to verify no UI lag during device polling
2. **Velopack maturity** - Newer tool, may need fallback plan
3. **scrcpy bundling on macOS** - May require Homebrew or manual build

---

## Sources

### High Confidence (Official Documentation)
- Avalonia Documentation - https://docs.avaloniaui.net/
- CommunityToolkit.Mvvm - https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/
- scrcpy GitHub - https://github.com/Genymobile/scrcpy
- FluentAvaloniaUI - https://github.com/amwx/FluentAvalonia

### Medium Confidence (Community/Third-Party)
- WPF to Avalonia migration guides
- Velopack documentation
- Cross-platform process management articles

### Research Documents
- [STACK.md](./STACK.md) - Technology stack recommendations
- [FEATURES.md](./FEATURES.md) - Feature landscape and competitor analysis
- [ARCHITECTURE.md](./ARCHITECTURE.md) - Architectural patterns and project structure
- [PITFALLS.md](./PITFALLS.md) - Migration pitfalls and cross-platform gotchas

---

## Recommendation

**Proceed with migration.** The research confirms:

1. **Technology stack is mature** - Avalonia 11.3.x is production-ready
2. **Migration path is documented** - Official guides and community resources exist
3. **Feature goals are achievable** - scrcpy provides all needed capabilities
4. **Risks are manageable** - Known pitfalls have documented solutions

The main investment is in:
- XAML style conversion (one-time effort)
- Cross-platform process management (improves code quality)
- Platform-specific packaging (one-time setup)

The resulting application will be cross-platform, consumer-friendly, and differentiated from competitors by being free, open-source, and ad-free with all features included.
