# Requirements: Phone Mirror

**Defined:** 2026-01-15
**Core Value:** One-click mirroring that just works — no installation guides, no terminal commands, no hunting for drivers.

## v1 Requirements

Requirements for initial release. Each maps to roadmap phases.

### Platform

- [x] **PLAT-01**: App runs on Windows, macOS, and Linux via Avalonia UI
- [ ] **PLAT-02**: App bundles ADB and scrcpy for zero-setup experience
- [ ] **PLAT-03**: App handles platform-specific binary extraction and permissions

### Device Connection

- [ ] **CONN-01**: App detects USB-connected Android devices automatically
- [ ] **CONN-02**: App displays device authorization guidance when device is unauthorized
- [ ] **CONN-03**: App shows device state (connected/disconnected/unauthorized/error)
- [ ] **CONN-04**: App displays friendly device name (model) instead of serial number

### Mirroring Control

- [ ] **MIRR-01**: User can start/stop mirroring with single button click
- [ ] **MIRR-02**: User can select quality preset (Low/Balanced/High)
- [ ] **MIRR-03**: User can enable keep screen awake option
- [ ] **MIRR-04**: User can enable auto-mirror on device connect
- [ ] **MIRR-05**: User can toggle full-screen mode for mirror window
- [ ] **MIRR-06**: User can toggle always-on-top for mirror window
- [ ] **MIRR-07**: User can resize mirror window freely (windowed mode)

### Capture & Clipboard

- [ ] **CAPT-01**: User can capture device screenshot to clipboard via button
- [ ] **CAPT-02**: Floating toolbar appears near mirror window during active session
- [ ] **CAPT-03**: Floating toolbar has screenshot-to-clipboard button
- [ ] **CAPT-04**: Floating toolbar has copy-recent-errors button (last 10 seconds)

### Error & Logging

- [ ] **LOGS-01**: App captures logcat errors during active mirroring session
- [ ] **LOGS-02**: Main window has collapsible log panel (hidden by default)
- [ ] **LOGS-03**: User can copy log contents to clipboard
- [ ] **LOGS-04**: Error messages provide actionable guidance

### Consumer UX

- [x] **UX-01**: App uses dark theme (Lab visual identity)
- [ ] **UX-02**: App persists user settings between sessions
- [ ] **UX-03**: UI is simplified with reduced visible complexity

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
| PLAT-02 | Phase 2 | Pending |
| PLAT-03 | Phase 2 | Pending |
| CONN-01 | Phase 2 | Pending |
| CONN-02 | Phase 2 | Pending |
| CONN-03 | Phase 2 | Pending |
| CONN-04 | Phase 2 | Pending |
| MIRR-01 | Phase 3 | Pending |
| MIRR-02 | Phase 3 | Pending |
| MIRR-03 | Phase 3 | Pending |
| MIRR-04 | Phase 3 | Pending |
| MIRR-05 | Phase 3 | Pending |
| MIRR-06 | Phase 3 | Pending |
| MIRR-07 | Phase 3 | Pending |
| CAPT-01 | Phase 4 | Pending |
| CAPT-02 | Phase 4 | Pending |
| CAPT-03 | Phase 4 | Pending |
| CAPT-04 | Phase 4 | Pending |
| LOGS-01 | Phase 4 | Pending |
| LOGS-02 | Phase 5 | Pending |
| LOGS-03 | Phase 5 | Pending |
| LOGS-04 | Phase 5 | Pending |
| UX-01 | Phase 1 | Complete |
| UX-02 | Phase 3 | Pending |
| UX-03 | Phase 3 | Pending |

**Coverage:**
- v1 requirements: 25 total
- Mapped to phases: 25
- Unmapped: 0 ✓

---
*Requirements defined: 2026-01-15*
*Last updated: 2026-01-15 after roadmap creation*
