# Technology Stack

**Analysis Date:** 2025-01-15

## Languages

**Primary:**
- C# 12 - All application code (`AdbMirror/*.cs`)

**Secondary:**
- XAML - WPF UI definitions (`AdbMirror/*.xaml`)
- Batch scripts - Build automation (`build.cmd`, `publish.cmd`, `prepare_resources.cmd`)

## Runtime

**Environment:**
- .NET 8.0 Windows Desktop (net8.0-windows) - `AdbMirror/AdbMirror.csproj`
- WPF framework enabled via `<UseWPF>true</UseWPF>`
- Nullable reference types enabled

**Package Manager:**
- NuGet via SDK-style project
- No lockfile (implicit restore)
- No external NuGet dependencies (framework only)

## Frameworks

**Core:**
- WPF (Windows Presentation Foundation) - Desktop UI framework
- Built-in INotifyPropertyChanged pattern for data binding

**Testing:**
- Not detected (no test project present)

**Build/Dev:**
- MSBuild via SDK-style .csproj
- Self-contained single-file publishing configured
- Ready-to-run (R2R) compilation enabled

## Key Dependencies

**Critical:**
- None - Zero external NuGet packages
- Pure .NET BCL and WPF framework usage

**External Tools (bundled or PATH):**
- Android ADB (platform-tools) - Device communication
- scrcpy - Screen mirroring binary

**Infrastructure:**
- System.Text.Json - Settings serialization (`AppSettings.cs`)
- System.IO.Compression - Resource extraction (`ResourceExtractor.cs`)
- System.Net.Http - scrcpy auto-download fallback (`ScrcpyService.cs`)

## Configuration

**Environment:**
- No environment variables required
- ANDROID_HOME/ANDROID_SDK_ROOT used for ADB discovery (optional)
- Settings stored in `%LocalAppData%\AdbMirror\settings.json`

**Build:**
- `AdbMirror/AdbMirror.csproj` - Project configuration
- `AdbMirror.sln` - Solution file
- `build.cmd` - Debug build script
- `publish.cmd` - Release publish script

## Platform Requirements

**Development:**
- Windows only (WPF)
- Visual Studio 2022+ or dotnet CLI with Windows SDK
- Optional: Android device with USB debugging enabled for testing

**Production:**
- Windows x64 (win-x64 RuntimeIdentifier)
- Single-file self-contained deployment
- External: ADB and scrcpy (auto-bundled or PATH available)

---

*Stack analysis: 2025-01-15*
*Update after major dependency changes*
