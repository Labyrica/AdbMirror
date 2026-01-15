# AdbMirror Feature Research

**Research Date:** 2026-01-15
**Domain:** Cross-platform desktop utility (Android device mirroring)
**Current State:** Device detection, quality presets (Low/Balanced/High), auto-mirror on connect, dark theme, keep screen awake, log viewer with copy-to-clipboard

---

## Table Stakes (Features Users Expect)

These are baseline features that users expect from any screen mirroring tool. Missing these creates friction.

| Feature | Complexity | Notes |
|---------|------------|-------|
| USB device detection | LOW | Already implemented. ADB device polling with state machine. |
| Screen mirroring display | LOW | Already implemented via scrcpy process spawn. |
| Start/Stop control | LOW | Already implemented. Primary button toggles mirroring state. |
| Quality presets | LOW | Already implemented. Low/Balanced/High bitrate/resolution combos. |
| Device authorization guidance | LOW | Already implemented. Status text prompts user to check phone for USB debugging dialog. |
| Keep screen awake option | LOW | Already implemented (`--stay-awake` flag). |
| Error messages | LOW | Already implemented. Status text and log buffer surface scrcpy errors. |
| Settings persistence | LOW | Already implemented. JSON settings in LocalApplicationData. |
| Auto-start mirroring option | MEDIUM | Already implemented. AutoMirrorOnConnect setting with per-device guard. |
| Dark theme | LOW | Already implemented in XAML styles. |

### Missing Table Stakes

| Feature | Complexity | Notes |
|---------|------------|-------|
| Screenshot to clipboard | MEDIUM | Expected by all competitors. scrcpy supports this natively with keyboard shortcut, but GUI button preferred. |
| Full-screen mode | LOW | Vysor gates this behind Pro; we should include free. scrcpy window supports F11 or `--fullscreen`. |
| Window always-on-top option | LOW | Useful for reference while working. scrcpy supports `--always-on-top`. |
| Copy-paste between devices | LOW | scrcpy supports bidirectional clipboard (Ctrl+C, Ctrl+V, Ctrl+Shift+V). Just need to document or expose. |
| Audio forwarding toggle | MEDIUM | Expected on Android 11+. scrcpy 2.0+ supports this. Currently not exposed. |

---

## Differentiators (Competitive Advantage Features)

Features that set consumer apps apart from developer tools or create meaningful differentiation.

### High Value / Low Effort
| Feature | Complexity | Differentiation Value | Notes |
|---------|------------|----------------------|-------|
| One-click wireless setup | MEDIUM | HIGH | Most tools make wireless painful. Auto `adb tcpip` + `adb connect`. |
| Screenshot to clipboard button | MEDIUM | HIGH | GUI affordance beats keyboard shortcuts for consumers. |
| Simplified first-run experience | LOW | HIGH | Hide ADB complexity. Auto-download scrcpy (already implemented). |
| Smart quality auto-detection | MEDIUM | MEDIUM | Detect USB vs WiFi, adjust bitrate automatically. |

### High Value / Medium Effort
| Feature | Complexity | Differentiation Value | Notes |
|---------|------------|----------------------|-------|
| Connection status indicator | LOW | MEDIUM | Visual USB/WiFi icon with latency indicator. |
| Recent devices list | MEDIUM | MEDIUM | Remember previously connected devices for quick reconnect (especially WiFi). |
| Drag-drop file transfer | MEDIUM | HIGH | scrcpy supports drag-drop to /sdcard/Download. GUI could show progress. |
| Quick screen recording | MEDIUM | HIGH | `scrcpy --record file.mp4` with one-click save. |

### Medium Value / Higher Effort
| Feature | Complexity | Differentiation Value | Notes |
|---------|------------|----------------------|-------|
| Multi-device grid view | HIGH | MEDIUM | Show 2-4 devices simultaneously. QtScrcpy supports this. |
| Keyboard remapping for games | HIGH | LOW | QtScrcpy's differentiator. Complex and niche. |
| APK install via drag-drop | LOW | LOW | scrcpy already supports this. Just needs documentation. |
| Virtual display mode | HIGH | LOW | scrcpy 2.5+ feature. Niche use case. |

---

## Anti-Features (Do NOT Build)

Features commonly requested but problematic for consumer apps.

| Anti-Feature | Reason to Avoid |
|--------------|-----------------|
| Watermarks on free version | User frustration #1 in reviews. Destroys trust. |
| Session time limits | Causes rage-uninstalls. ApowerMirror's 10-minute limit is universally hated. |
| Ads or pop-ups | ApowerMirror users complain about intrusive notifications taking over screen. |
| Aggressive upsells | Vysor's approach of gating basic features (fullscreen, wireless) creates negative reviews. |
| Auto-start on boot | ApowerMirror users cite this as major complaint. Unexpected background behavior. |
| Complex key mapping editor | QtScrcpy's feature is powerful but creates maintenance burden and confuses consumers. |
| Enterprise features (RBAC, MFA) | Out of scope for consumer tool. Different market entirely. |
| Custom video codec settings | Exposes too much complexity. Presets are sufficient for 95% of users. |
| ADB path configuration | Power-user feature. Auto-detection with bundled fallback preferred. |
| Manual resolution/bitrate input | Presets + auto-detection better for consumers. |
| Root-required features | Immediately disqualifies most users. |
| In-app purchases for basic features | Vysor model generates complaints. Better: free core + optional donation. |

### Features to Explicitly Defer (v2+)
| Feature | Reason to Defer |
|---------|-----------------|
| iOS mirroring | Completely different stack. Separate product. |
| Casting to TV/Chromecast | Different use case. Use dedicated apps. |
| Remote network mirroring | Security complexity. AirDroid Cast's differentiator but requires infrastructure. |
| Annotation/drawing tools | Presentation feature. Different product category. |
| Multi-user sharing (Vysor Share) | Requires server infrastructure. |

---

## Feature Dependencies Diagram

```
                    [ADB Available]
                          |
                          v
                   [Device Connected]
                          |
            +-------------+-------------+
            |             |             |
            v             v             v
    [USB Mirroring]  [Wireless]    [Multiple Devices]
            |             |             |
            v             v             v
    [Quality Presets] [Auto-reconnect] [Device Selector]
            |
    +-------+-------+-------+
    |       |       |       |
    v       v       v       v
[Screenshot] [Record] [Audio] [Clipboard]
    |
    v
[Save to File / Copy to Clipboard]
```

### Dependency Notes
- Screenshot requires active mirroring session
- Audio forwarding requires Android 11+ and scrcpy 2.0+
- Wireless requires initial USB connection for `adb tcpip` setup
- Recording requires write permissions to output directory
- Clipboard sync requires Android 7+ (copy from PC) or Android 10+ (bidirectional)

---

## MVP Definition

### v1.0 (Current + Polish)
**Goal:** Reliable basic mirroring with consumer-friendly UX

| Feature | Status | Priority |
|---------|--------|----------|
| USB device detection | DONE | - |
| Quality presets (Low/Balanced/High) | DONE | - |
| Auto-mirror on connect | DONE | - |
| Keep screen awake option | DONE | - |
| Dark theme | DONE | - |
| Log viewer with copy button | DONE | - |
| Screenshot to clipboard | TODO | P0 |
| Full-screen mode option | TODO | P0 |
| Always-on-top option | TODO | P1 |
| Simplified error messages | TODO | P1 |

### v1.x (Quick Wins)
**Goal:** Feature parity with basic Vysor/scrcpy GUIs

| Feature | Priority | Complexity |
|---------|----------|------------|
| One-click wireless setup | P0 | MEDIUM |
| Audio forwarding toggle (Android 11+) | P1 | MEDIUM |
| Screen recording to file | P1 | MEDIUM |
| Recent devices list | P2 | MEDIUM |
| Connection type indicator (USB/WiFi) | P2 | LOW |

### v2.0+ (Differentiation)
**Goal:** Features that distinguish from free alternatives

| Feature | Priority | Complexity |
|---------|----------|------------|
| Smart quality auto-detection | P1 | MEDIUM |
| Drag-drop file transfer with progress | P2 | MEDIUM |
| Multi-device support (2-device grid) | P2 | HIGH |
| Quick settings overlay | P3 | MEDIUM |

---

## Feature Prioritization Matrix

### Impact vs Effort Grid

```
HIGH IMPACT
    ^
    |  [Wireless Setup]        [Multi-device Grid]
    |  [Screenshot Button]
    |
    |  [Audio Toggle]          [Screen Recording]
    |  [Fullscreen Option]
    |  [Always-on-top]
    |
    |  [Recent Devices]        [File Transfer UI]
    |  [Connection Indicator]
    |
    +---------------------------------> HIGH EFFORT
LOW IMPACT
```

### Prioritization Factors

| Factor | Weight | Notes |
|--------|--------|-------|
| User demand (reviews) | 40% | Screenshot, wireless, audio most requested |
| Implementation effort | 30% | Prefer scrcpy flags over custom code |
| Differentiation | 20% | Wireless setup wizard differentiates from CLI |
| Technical risk | 10% | Audio has Android version constraints |

---

## Competitor Feature Analysis

### Feature Matrix

| Feature | Vysor Free | Vysor Pro | scrcpy CLI | QtScrcpy | Scrcpy-GUI | AirDroid Cast Free | **AdbMirror** |
|---------|-----------|-----------|------------|----------|------------|-------------------|---------------|
| USB Mirroring | Yes | Yes | Yes | Yes | Yes | Yes | **Yes** |
| Wireless | No | Yes | Yes | Yes | Yes | Limited | **Planned** |
| High Resolution | No | Yes | Yes | Yes | Yes | No | **Yes** |
| Screenshot | Yes | Yes | Yes | Yes | Varies | Yes | **Planned** |
| Screen Recording | No | Yes | Yes | Yes | Varies | No | **Planned** |
| Audio Forward | No | Limited | Yes | Yes | Yes | Yes | **Planned** |
| Fullscreen | No | Yes | Yes | Yes | Yes | No | **Planned** |
| File Transfer | No | Yes | Yes | Yes | Some | Yes | **Deferred** |
| Clipboard Sync | Limited | Yes | Yes | Yes | Yes | Limited | **Yes (via scrcpy)** |
| Key Mapping | No | No | No | Yes | No | No | **No** |
| Multi-device | Limited | Yes | Yes | Yes | Some | Yes | **Deferred** |
| Price | Free | $2.50/mo | Free | Free | Free | $3.49/mo | **Free** |
| Ads | Yes | No | No | No | No | Yes | **No** |
| Open Source | No | No | Yes | Yes | Yes | No | **Yes** |

### Competitor Weaknesses to Exploit

| Competitor | Weakness | Our Opportunity |
|------------|----------|-----------------|
| Vysor | Paywalls basic features (fullscreen, wireless) | Include these free |
| Vysor | Low quality in free tier | High quality by default |
| scrcpy CLI | Command-line intimidating | GUI wrapper with presets |
| QtScrcpy | Complex interface, game-focused | Simple consumer UI |
| ApowerMirror | Intrusive ads, popup spam | Clean, ad-free experience |
| AirDroid Cast | Session limits, watermarks | No artificial limits |
| Generic GUIs | Poor error handling | Clear troubleshooting guidance |

---

## Screenshot/Clipboard Integration Patterns

### Current Landscape

**scrcpy native shortcuts:**
- `Ctrl+S`: Save screenshot to scrcpy working directory
- `MOD+Shift+o`: Turn device screen on/off

**Implementation approaches for GUI:**
1. **Capture from scrcpy window** - Take screenshot of the mirroring window (lossy, includes window chrome)
2. **Use scrcpy's native screenshot** - Trigger `Ctrl+S` programmatically and monitor output directory
3. **Direct ADB screenshot** - `adb exec-out screencap -p > screenshot.png` (works without scrcpy running)
4. **scrcpy server screenshot** - Use scrcpy's internal APIs (requires process communication)

**Recommended approach:**
- Primary: ADB `screencap` command (works always, no scrcpy dependency)
- Copy to clipboard using `System.Windows.Clipboard.SetImage()`
- Optional: Save to user-selected location
- Keyboard shortcut: `Ctrl+Shift+S` or dedicated button

### WPF Clipboard Implementation

```csharp
// Simplified pattern for WPF screenshot to clipboard
using System.Windows;
using System.Windows.Media.Imaging;

// 1. Capture via ADB
// adb exec-out screencap -p > temp.png

// 2. Load and copy to clipboard
var bitmap = new BitmapImage(new Uri(tempPath));
Clipboard.SetImage(bitmap);
```

---

## Error/Log Viewing Approaches

### Current Implementation
- StringBuilder log buffer with 10K char limit
- Timestamp prefix `[HH:mm:ss]`
- Copy-to-clipboard button
- Displayed in scrollable TextBox

### Best Practices from Research

| Pattern | Implementation | Notes |
|---------|----------------|-------|
| Log levels | Color-code ERROR/WARN/INFO | Use red for errors, yellow for warnings |
| Filtering | Hide verbose logs by default | Show only errors/warnings unless expanded |
| Auto-scroll | Scroll to bottom on new entries | With option to pause when user scrolls up |
| Export | Copy all or save to file | Already implemented copy |
| Truncation | Rolling buffer, oldest removed | Already implemented |
| Actionable errors | Link to troubleshooting | "Device unauthorized" -> "Check phone prompt" |

### Recommended Enhancements
1. Add log level icons/colors (ERROR = red, INFO = gray)
2. Collapsible log panel (hidden by default for clean UI)
3. "Show verbose logs" toggle for debugging
4. Clear logs button
5. Filter by severity dropdown

---

## Consumer UX vs Developer Tools

### Key Differences

| Aspect | Developer Tool | Consumer App |
|--------|---------------|--------------|
| Error messages | Technical (exit codes, stderr) | Friendly ("Check USB cable") |
| Configuration | Expose all flags | Smart defaults + presets |
| First-run | Assume ADB knowledge | Guided setup wizard |
| Documentation | README, man pages | In-app tooltips |
| Failure mode | Show raw errors | Suggest solutions |
| Features | Exhaustive | Curated essentials |
| UI density | Information-dense | Spacious, focused |

### Consumer UX Patterns to Apply

1. **Progressive disclosure** - Start simple, reveal advanced options on demand
2. **Sensible defaults** - Balanced preset, auto-mirror off, keep awake on
3. **Status clarity** - Always show what's happening (Connecting..., Mirroring..., Error)
4. **Single primary action** - One big button (Mirror/Stop)
5. **Inline help** - Tooltips on hover, not separate docs
6. **Error recovery** - Suggest next steps, not just report failures

### What to Hide from Consumers

- ADB path configuration
- scrcpy command-line flags
- Technical error codes
- Video codec selection
- Bitrate/resolution numbers (use quality presets instead)
- Process management details

---

## Sources

### High Confidence (Official documentation, direct testing)
- [scrcpy GitHub Repository](https://github.com/Genymobile/scrcpy) - Primary source for scrcpy features and flags
- [scrcpy Video Documentation](https://github.com/Genymobile/scrcpy/blob/master/doc/video.md) - Bitrate and quality settings
- [scrcpy Recording Documentation](https://github.com/Genymobile/scrcpy/blob/master/doc/recording.md) - Recording formats and options
- [scrcpy Control Documentation](https://raw.githubusercontent.com/Genymobile/scrcpy/master/doc/control.md) - Drag-drop, clipboard, shortcuts

### Medium Confidence (Third-party reviews, user feedback)
- [Vysor Official Site](https://www.vysor.io/) - Pricing and feature tiers
- [AirDroid Cast Pricing](https://www.airdroid.com/pricing/airdroid-cast/) - Competitor pricing model
- [QtScrcpy GitHub](https://github.com/barry-ran/QtScrcpy) - Key mapping and multi-device features
- [Scrcpy-GUI by GeorgeEnglezos](https://github.com/GeorgeEnglezos/Scrcpy-GUI) - Flutter-based GUI approach
- [XDA Developers - scrcpy Android 13 clipboard](https://www.xda-developers.com/scrcpy-update-android-13-clipboard-access/) - Clipboard compatibility
- [ApowerMirror Reviews](https://www.airdroid.com/screen-mirror/apowermirror-review/) - User complaints and anti-patterns

### Lower Confidence (Aggregated reviews, older sources)
- [G2 Vysor Reviews](https://www.g2.com/products/vysor/reviews) - User satisfaction data
- [SourceForge Scrcpy GUI Reviews](https://sourceforge.net/projects/scrcpy-gui.mirror/reviews/) - Community feedback
- [Mobikin Vysor Alternatives](https://www.mobikin.com/screen-mirror/vysor-alternative.html) - Competitor landscape
- [UXMatters Log Monitoring](https://www.uxmatters.com/mt/archives/2024/05/crafting-seamless-user-experiences-a-ux-driven-approach-to-log-monitoring-and-observability.php) - Log viewer UX patterns
- [TubikStudio Error Screens](https://blog.tubikstudio.com/error-screens-and-messages/) - Error message design patterns

---

## Summary Recommendations

### Immediate Actions (v1.0 Polish)
1. Add screenshot-to-clipboard button (ADB screencap approach)
2. Add fullscreen mode toggle (scrcpy `--fullscreen` flag)
3. Add always-on-top option (scrcpy `--always-on-top` flag)
4. Simplify error messages with actionable guidance

### Near-term Actions (v1.x)
1. Implement one-click wireless setup wizard
2. Add audio forwarding toggle for Android 11+
3. Add screen recording with file picker
4. Remember recently connected devices

### Avoid
1. Feature-gating behind paywalls (stay free)
2. Ads or upsell popups
3. Exposing technical complexity (keep consumer-focused)
4. Game key mapping (different market)
5. Enterprise features (out of scope)

### Positioning
**"The free, no-nonsense Android mirroring app."**
- No ads, no watermarks, no time limits
- Quality features Vysor charges for (fullscreen, wireless)
- Simpler than QtScrcpy, more polished than generic GUIs
- Open source for trust and transparency
