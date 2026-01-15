# Requirements: Phone Mirror

**Defined:** 2026-01-15
**Core Value:** One-click mirroring that just works — no installation guides, no terminal commands, no hunting for drivers.

## v1 Requirements

Requirements for initial release. Each maps to roadmap phases.

### Platform

- [x] **PLAT-01**: App runs on Windows, macOS, and Linux via Avalonia UI
- [x] **PLAT-02**: App bundles ADB and scrcpy for zero-setup experience
- [x] **PLAT-03**: App handles platform-specific binary extraction and permissions

### Device Connection

- [x] **CONN-01**: App detects USB-connected Android devices automatically
- [x] **CONN-02**: App displays device authorization guidance when device is unauthorized
- [x] **CONN-03**: App shows device state (connected/disconnected/unauthorized/error)
- [x] **CONN-04**: App displays friendly device name (model) instead of serial number

### Mirroring Control

- [x] **MIRR-01**: User can start/stop mirroring with single button click
- [x] **MIRR-02**: User can select quality preset (Low/Balanced/High)
- [x] **MIRR-03**: User can enable keep screen awake option
- [x] **MIRR-04**: User can enable auto-mirror on device connect
- [x] **MIRR-05**: User can toggle full-screen mode for mirror window
- [x] **MIRR-06**: User can toggle always-on-top for mirror window
- [x] **MIRR-07**: User can resize mirror window freely (windowed mode)

### Capture & Clipboard

- [x] **CAPT-01**: User can capture device screenshot to clipboard via button
- [x] **CAPT-02**: Floating toolbar appears near mirror window during active session
- [x] **CAPT-03**: Floating toolbar has screenshot-to-clipboard button
- [x] **CAPT-04**: Floating toolbar has copy-recent-errors button (last 10 seconds)

### Error & Logging

- [x] **LOGS-01**: App captures logcat errors during active mirroring session
- [x] **LOGS-02**: Main window has collapsible log panel (hidden by default)
- [x] **LOGS-03**: User can copy log contents to clipboard
- [x] **LOGS-04**: Error messages provide actionable guidance

### Consumer UX

- [x] **UX-01**: App uses dark theme (Lab visual identity)
- [x] **UX-02**: App persists user settings between sessions
- [x] **UX-03**: UI is simplified with reduced visible complexity

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### Device Connection

- **CONN-05**: One-click wireless setup wizard
- **CONN-06**: Recent devices list for quick reconnect
- **CONN-07**: Connection type indicator (USB/WiFi icon)

### Capture

- **CAPT-05**: Screen recording to file
- **CAPT-06**: Clipboard sync between PC and phone

### Consumer UX

- **UX-04**: First-run guidance for enabling USB debugging
- **UX-05**: Smart quality auto-detection (USB vs WiFi)

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Multi-device simultaneous mirroring | Adds complexity, revisit post-v2 |
| Audio mirroring controls | scrcpy handles internally |
| Recording functionality | Deferred to v2 |
| Mobile app version | Desktop-first tool |
| Custom scrcpy arguments | Consumer app, not power-user |
| Key mapping for games | Different market (QtScrcpy territory) |
| iOS mirroring | Completely different stack |

## Traceability

Which phases cover which requirements. Updated by create-roadmap.

| Requirement | Phase | Status |
|-------------|-------|--------|
| PLAT-01 | Phase 1 | Complete |
| PLAT-02 | Phase 2 | Complete |
| PLAT-03 | Phase 2 | Complete |
| CONN-01 | Phase 2 | Complete |
| CONN-02 | Phase 2 | Complete |
| CONN-03 | Phase 2 | Complete |
| CONN-04 | Phase 2 | Complete |
| MIRR-01 | Phase 3 | Complete |
| MIRR-02 | Phase 3 | Complete |
| MIRR-03 | Phase 3 | Complete |
| MIRR-04 | Phase 3 | Complete |
| MIRR-05 | Phase 3 | Complete |
| MIRR-06 | Phase 3 | Complete |
| MIRR-07 | Phase 3 | Complete |
| CAPT-01 | Phase 4 | Complete |
| CAPT-02 | Phase 4 | Complete |
| CAPT-03 | Phase 4 | Complete |
| CAPT-04 | Phase 4 | Complete |
| LOGS-01 | Phase 4 | Complete |
| LOGS-02 | Phase 5 | Complete |
| LOGS-03 | Phase 5 | Complete |
| LOGS-04 | Phase 5 | Complete |
| UX-01 | Phase 1 | Complete |
| UX-02 | Phase 3 | Complete |
| UX-03 | Phase 3 | Complete |

**Coverage:**
- v1 requirements: 25 total
- Mapped to phases: 25
- Unmapped: 0 ✓

---
*Requirements defined: 2026-01-15*
*Last updated: 2026-01-15 after Phase 5 completion*
