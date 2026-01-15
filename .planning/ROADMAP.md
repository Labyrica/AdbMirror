# Roadmap: Phone Mirror

## Overview

Migrate AdbMirror from WPF to Avalonia UI for cross-platform support, simplify the consumer experience, and add capture features. The journey starts with project scaffolding, moves through service and UI migration, adds new features, and ends with cross-platform packaging.

## Domain Expertise

None

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

- [x] **Phase 1: Project Scaffold** - Avalonia solution setup with DI and theming
- [x] **Phase 2: Core Services** - ADB/scrcpy services with cross-platform process handling
- [x] **Phase 3: Mirror UI** - Main window with device list and mirroring controls
- [ ] **Phase 4: Capture Features** - Screenshot toolbar and session error capture
- [ ] **Phase 5: Logging UI** - Collapsible log panel with copy functionality
- [ ] **Phase 6: Packaging** - Platform-specific installers and distribution

## Phase Details

### Phase 1: Project Scaffold ✓
**Goal**: Create new Avalonia solution with recommended stack (DI, FluentAvaloniaUI dark theme)
**Depends on**: Nothing (first phase)
**Requirements**: PLAT-01, UX-01
**Research**: Unlikely (established Avalonia patterns, research already complete)
**Plans**: 1 plan
**Completed**: 2026-01-15

Plans:
- [x] 01-01: Solution setup with DI, ViewLocator, and dark theme

### Phase 2: Core Services ✓
**Goal**: Migrate ADB and scrcpy services with cross-platform process management
**Depends on**: Phase 1
**Requirements**: PLAT-02, PLAT-03, CONN-01, CONN-02, CONN-03, CONN-04
**Research**: Unlikely (migration patterns documented in PITFALLS.md)
**Plans**: 4 plans
**Completed**: 2026-01-15

Plans:
- [x] 02-01: Platform abstraction and ProcessRunner
- [x] 02-02: AdbService migration with device detection
- [x] 02-03: ScrcpyService migration with quality presets
- [x] 02-04: Resource extraction for bundled binaries

### Phase 3: Mirror UI ✓
**Goal**: Convert main window with device list, controls, and settings
**Depends on**: Phase 2
**Requirements**: MIRR-01, MIRR-02, MIRR-03, MIRR-04, MIRR-05, MIRR-06, MIRR-07, UX-02, UX-03
**Research**: Unlikely (WPF to Avalonia XAML conversion documented)
**Plans**: 4 plans (3 waves)
**Completed**: 2026-01-15

Plans:
- [x] 03-01: MainWindow shell and layout (Wave 1)
- [x] 03-02: Device list and connection status UI (Wave 2)
- [x] 03-03: Mirroring controls and quality selector (Wave 2)
- [x] 03-04: Settings persistence and options panel (Wave 3)

### Phase 4: Capture Features
**Goal**: Add screenshot to clipboard and session error capture
**Depends on**: Phase 3
**Requirements**: CAPT-01, CAPT-02, CAPT-03, CAPT-04, LOGS-01
**Research**: Unlikely (ADB screencap and logcat are standard APIs)
**Plans**: 3 plans (2 waves)

Plans:
- [ ] 04-01: ADB screenshot to clipboard (Wave 1)
- [ ] 04-02: Floating toolbar window (Wave 2)
- [x] 04-03: Session logcat error capture (Wave 1)

### Phase 5: Logging UI
**Goal**: Add collapsible log panel with copy functionality and error guidance
**Depends on**: Phase 4
**Requirements**: LOGS-02, LOGS-03, LOGS-04
**Research**: Unlikely (internal UI patterns)
**Plans**: TBD

Plans:
- [ ] 05-01: Collapsible log panel in main window
- [ ] 05-02: Copy to clipboard and actionable error messages

### Phase 6: Packaging
**Goal**: Create platform-specific installers for Windows, macOS, and Linux
**Depends on**: Phase 5
**Requirements**: (polish/packaging phase - no functional requirements)
**Research**: Likely (platform-specific installers)
**Research topics**: Velopack setup, macOS notarization workflow, AppImage creation
**Plans**: TBD

Plans:
- [ ] 06-01: Windows installer (Velopack or MSIX)
- [ ] 06-02: macOS app bundle with signing
- [ ] 06-03: Linux AppImage

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5 → 6

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Project Scaffold | 1/1 | Complete | 2026-01-15 |
| 2. Core Services | 4/4 | Complete | 2026-01-15 |
| 3. Mirror UI | 4/4 | Complete | 2026-01-15 |
| 4. Capture Features | 1/3 | In progress | - |
| 5. Logging UI | 0/2 | Not started | - |
| 6. Packaging | 0/3 | Not started | - |
