# Phone Mirror

## What This Is

A cross-platform desktop app that mirrors Android device screens to PC/Mac/Linux for debugging and everyday use. Built for developers who need quick device access, but simple enough for anyone to use without technical setup.

## Core Value

One-click mirroring that just works — no installation guides, no terminal commands, no hunting for drivers.

## Requirements

### Validated

<!-- Shipped and confirmed valuable — existing functionality -->

- ✓ Device mirroring via scrcpy — existing
- ✓ Automatic device detection and connection — existing
- ✓ Quality presets (Low/Balanced/High) — existing
- ✓ Auto-mirror on device connect option — existing
- ✓ Keep screen awake option — existing
- ✓ Dark theme UI (Lab visual identity) — existing

### Active

<!-- Current scope. Building toward these. -->

- [ ] Cross-platform support (Windows, macOS, Linux) via Avalonia UI
- [ ] Simplified UI — reduce visible complexity, consumer-friendly layout
- [ ] Collapsible event log — hidden by default, toggle to expand
- [ ] Screenshot to clipboard — capture device screen via ADB, copy to clipboard
- [ ] Session error log — capture logcat errors while mirroring, copyable panel
- [ ] Bundled tools — ship with platform-tools and scrcpy embedded for zero-setup
- [ ] Friendly device names — show model names instead of serial numbers where possible

### Out of Scope

- Multi-device simultaneous mirroring — adds complexity, revisit post-v2
- Audio mirroring controls — scrcpy handles this, no need to expose
- Recording functionality — separate use case, keep app focused
- Mobile app version — desktop-first tool
- Custom scrcpy arguments — consumer app, not power-user tool

## Context

**Existing codebase:**
- WPF app with MVVM pattern (see `.planning/codebase/ARCHITECTURE.md`)
- Core services (AdbService, ScrcpyService) are reusable
- ResourceExtractor handles embedded tool extraction
- Settings persistence via JSON in LocalAppData

**Technical migration:**
- WPF → Avalonia UI (similar XAML syntax, some differences)
- Keep service layer mostly intact
- Rewrite views and adjust ViewModels for Avalonia patterns

**Target users:**
- Mobile developers debugging apps
- QA testers needing device screenshots
- Anyone who wants their phone screen on their computer

## Constraints

- **Framework**: Avalonia UI — cross-platform requirement, C#/XAML familiarity
- **Visual identity**: Maintain current dark "Lab" theme aesthetic
- **Binary size**: Accept larger download for bundled tools (better UX tradeoff)
- **Platform tools**: Bundle ADB and scrcpy — no user installation required

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Avalonia over MAUI | More mature desktop support, closer to WPF patterns | — Pending |
| Bundle tools vs auto-download | Zero-setup consumer experience worth the download size | — Pending |
| Session-only error logs | Simpler than full logcat stream, focused on active debugging | — Pending |
| ADB screencap for screenshots | Consistent quality, works regardless of window state | — Pending |

---
*Last updated: 2025-01-15 after initialization*
