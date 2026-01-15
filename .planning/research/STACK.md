# Stack Research: Avalonia UI Migration

**Research Date:** January 2025
**Purpose:** WPF to Avalonia migration for cross-platform Android device mirroring utility
**Target Platforms:** Windows, macOS, Linux

---

## Recommended Stack

| Category | Technology | Version |
|----------|------------|---------|
| UI Framework | Avalonia UI | 11.3.x |
| MVVM Framework | CommunityToolkit.Mvvm | 8.4.x |
| Theme | FluentAvaloniaUI | 2.4.x |
| Dependency Injection | Microsoft.Extensions.DependencyInjection | 8.0.x |
| Installer/Updates | Velopack | 0.0.x (latest) |
| Target Framework | .NET 8.0 | LTS |

---

## Core Technologies

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| **Avalonia** | 11.3.11 | Cross-platform UI framework | Production-ready, used by JetBrains, Unity, GitHub. WPF-like XAML with true cross-platform support. Draws UI itself rather than relying on OS controls for consistency. |
| **Avalonia.Desktop** | 11.3.11 | Desktop platform support | Required for Windows/macOS/Linux desktop apps. Includes window management and platform integrations. |
| **Avalonia.Themes.Fluent** | 11.3.11 | Base Fluent theme | Built-in modern Fluent Design styling. Supports Light/Dark variants out of the box. |
| **.NET 8.0** | 8.0 LTS | Runtime | Current LTS release. Recommended by Avalonia. Supports single-file publish, AOT, and cross-platform deployment. |

---

## Supporting Libraries

| Library | Version | Purpose | Notes |
|---------|---------|---------|-------|
| **CommunityToolkit.Mvvm** | 8.4.0 | MVVM with source generators | UI-framework agnostic. Generates boilerplate via [ObservableProperty], [RelayCommand]. AOT-friendly. Now default in Avalonia templates. |
| **FluentAvaloniaUI** | 2.4.1 | Extended WinUI controls + Fluent v2 | NavigationView, TabView, InfoBar, NumberBox, ContentDialog. Runtime theme switching. OS accent color detection. |
| **Microsoft.Extensions.DependencyInjection** | 8.0.1 | Dependency injection container | Standard .NET DI. Lightweight, well-documented. Works seamlessly with Avalonia. |
| **Velopack** | 0.0.x | Installer + auto-updates | Cross-platform (Windows/macOS/Linux). Single command builds. Signed installers. Less than 10 lines of code for updates. Replaces Squirrel.Windows. |
| **System.Diagnostics.Process** | Built-in | External tool management | Standard .NET for launching ADB/scrcpy. Use with Avalonia Dispatcher for UI updates. |

---

## Installation Commands

### Create New Avalonia Project
```bash
# Install Avalonia templates
dotnet new install Avalonia.Templates

# Create new MVVM app (uses CommunityToolkit.Mvvm by default)
dotnet new avalonia.mvvm -n AdbMirror.Avalonia

# Or create basic app and add packages manually
dotnet new avalonia.app -n AdbMirror.Avalonia
```

### Add Required NuGet Packages
```bash
# Core Avalonia (if not from template)
dotnet add package Avalonia --version 11.3.11
dotnet add package Avalonia.Desktop --version 11.3.11
dotnet add package Avalonia.Themes.Fluent --version 11.3.11

# MVVM Framework
dotnet add package CommunityToolkit.Mvvm --version 8.4.0

# Extended Fluent theme + WinUI controls
dotnet add package FluentAvaloniaUI --version 2.4.1

# Dependency Injection
dotnet add package Microsoft.Extensions.DependencyInjection --version 8.0.1

# Installer/Updates (add when ready for distribution)
dotnet add package Velopack --version 0.0.1050
```

### Project File Configuration (.csproj)
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <BuiltInComInteropSupport>true</BuiltInComInteropSupport>
    <ApplicationManifest>app.manifest</ApplicationManifest>
    <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Avalonia" Version="11.3.11" />
    <PackageReference Include="Avalonia.Desktop" Version="11.3.11" />
    <PackageReference Include="Avalonia.Themes.Fluent" Version="11.3.11" />
    <PackageReference Include="Avalonia.Fonts.Inter" Version="11.3.11" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
    <PackageReference Include="FluentAvaloniaUI" Version="2.4.1" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.1" />
  </ItemGroup>
</Project>
```

---

## Cross-Platform Publishing Commands

### Windows (Single File)
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true
```

### macOS (Intel)
```bash
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true
```

### macOS (Apple Silicon)
```bash
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true
```

### Linux
```bash
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
```

### Velopack Packaging
```bash
# Install Velopack CLI
dotnet tool install -g vpk

# Package for Windows
vpk pack -u MyApp -v 1.0.0 -p publish/win-x64 -e MyApp.exe

# Package for macOS (creates signed .pkg)
vpk pack -u MyApp -v 1.0.0 -p publish/osx-x64 -e MyApp --packTitle "My App"

# Package for Linux (creates .AppImage)
vpk pack -u MyApp -v 1.0.0 -p publish/linux-x64 -e MyApp
```

---

## Key Migration Patterns

### XAML Namespace Changes
```xml
<!-- WPF -->
xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"

<!-- Avalonia -->
xmlns="https://github.com/avaloniaui"
```

### Styling Differences
```xml
<!-- WPF Style -->
<Style TargetType="Button">
    <Setter Property="Background" Value="Blue"/>
</Style>

<!-- Avalonia Style (CSS-like selectors) -->
<Style Selector="Button">
    <Setter Property="Background" Value="Blue"/>
</Style>
```

### ViewModel with CommunityToolkit.Mvvm
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _status = "Ready";

    [ObservableProperty]
    private bool _isConnected;

    [RelayCommand]
    private async Task ConnectAsync()
    {
        Status = "Connecting...";
        // ... implementation
    }
}
```

### Dark Theme Setup (FluentAvaloniaUI)
```xml
<!-- App.axaml -->
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="AdbMirror.App"
             RequestedThemeVariant="Dark">
    <Application.Styles>
        <FluentTheme />
    </Application.Styles>
</Application>
```

### Dependency Injection Setup
```csharp
// App.axaml.cs
public override void OnFrameworkInitializationCompleted()
{
    var services = new ServiceCollection();

    // Register services
    services.AddSingleton<IAdbService, AdbService>();
    services.AddSingleton<IScrcpyService, ScrcpyService>();
    services.AddTransient<MainViewModel>();

    var provider = services.BuildServiceProvider();

    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        desktop.MainWindow = new MainWindow
        {
            DataContext = provider.GetRequiredService<MainViewModel>()
        };
    }

    base.OnFrameworkInitializationCompleted();
}
```

### Process Management (ADB/scrcpy)
```csharp
public class AdbService : IAdbService
{
    public async Task<string> ExecuteCommandAsync(string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = GetAdbPath(),
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        return output;
    }

    private string GetAdbPath()
    {
        // Platform-specific path resolution
        if (OperatingSystem.IsWindows())
            return Path.Combine(AppContext.BaseDirectory, "tools", "adb.exe");
        else
            return Path.Combine(AppContext.BaseDirectory, "tools", "adb");
    }
}
```

---

## Alternatives Considered

| Alternative | Reason Not Chosen |
|-------------|-------------------|
| **ReactiveUI** | Steeper learning curve. CommunityToolkit.Mvvm is simpler, has better source generators, AOT-friendly, and is now the Avalonia default. |
| **Avalonia XPF** | Commercial license required. Designed for minimal-change WPF ports. Not needed for a small app willing to adapt XAML. |
| **.NET MAUI** | Less UI flexibility than WPF/Avalonia. Requires significant rewriting. No Linux desktop support. |
| **Prism** | Heavier framework. CommunityToolkit.Mvvm + manual DI is lighter and sufficient for this app size. |
| **Material.Avalonia** | FluentAvaloniaUI has better WinUI control coverage. Fluent design aligns with Windows 11 aesthetics. |
| **Squirrel.Windows** | Deprecated. Velopack is the modern replacement with cross-platform support. |
| **MSIX** | Windows-only. Velopack provides unified cross-platform packaging. |

---

## What NOT to Use

| Technology | Reason to Avoid |
|------------|-----------------|
| **Avalonia 0.10.x** | Deprecated. Avalonia 11.x has breaking changes but better performance and features. |
| **ReactiveUI (unless needed)** | Unnecessary complexity if not using reactive patterns extensively. |
| **Splat service locator** | Anti-pattern. Use constructor injection with Microsoft.Extensions.DependencyInjection. |
| **.NET Framework** | Not cross-platform. Requires migration to modern .NET first. |
| **PublishTrimmed=true** | Can break Avalonia file dialogs and reflection-based features. Test thoroughly before enabling. |
| **Windows-specific APIs** | P/Invoke, [DllImport], WebBrowser control - need cross-platform alternatives. |
| **WPF-specific controls** | Ribbon, WebView2 - use Avalonia equivalents or FluentAvaloniaUI controls. |

---

## Version Compatibility Notes

### Avalonia + .NET Compatibility
| Avalonia Version | Minimum .NET | Recommended .NET |
|------------------|--------------|------------------|
| 11.3.x | .NET 6.0 | .NET 8.0 (LTS) |
| 11.2.x | .NET 6.0 | .NET 8.0 (LTS) |
| 11.1.x | .NET 6.0 | .NET 8.0 (LTS) |

### Package Version Matrix (Tested Compatible)
| Avalonia | FluentAvaloniaUI | CommunityToolkit.Mvvm | MS.Extensions.DI |
|----------|------------------|----------------------|------------------|
| 11.3.11 | 2.4.1 | 8.4.0 | 8.0.1 |
| 11.2.x | 2.3.x | 8.3.x | 8.0.x |

### Platform Runtime Requirements
| Platform | Runtime | Notes |
|----------|---------|-------|
| Windows 10/11 | .NET 8.0 | Self-contained recommended |
| macOS 10.15+ | .NET 8.0 | Universal binary for Intel + Apple Silicon |
| Linux (glibc) | .NET 8.0 | Ubuntu 20.04+, Debian 11+, Fedora 36+ |

### Known Issues
- **Windows 7/8.1**: Not supported by FluentAvaloniaUI
- **PublishTrimmed**: May break file dialogs; test thoroughly
- **WASM**: Experimental; some dependencies may not work
- **Validation attributes**: CommunityToolkit.Mvvm and Avalonia may generate duplicate validation errors; handle in ViewModel

---

## Migration Effort Estimate

Based on Avalonia documentation guidelines:
- **Per View**: ~9 hours average
- **Per Line of Code** (XAML + code-behind): ~4 minutes

For AdbMirror (small utility app):
- Estimated effort: 1-2 days for core migration
- Additional time for cross-platform testing and packaging

---

## Sources

### HIGH Confidence (Official Documentation)
- [Avalonia Documentation - Welcome](https://docs.avaloniaui.net/docs/welcome)
- [Migrating from WPF | Avalonia Docs](https://docs.avaloniaui.net/docs/get-started/wpf/)
- [Fluent Theme | Avalonia Docs](https://docs.avaloniaui.net/docs/basics/user-interface/styling/themes/fluent)
- [How To Implement Dependency Injection | Avalonia Docs](https://docs.avaloniaui.net/docs/guides/implementation-guides/how-to-implement-dependency-injection)
- [Native AOT Deployment | Avalonia Docs](https://docs.avaloniaui.net/docs/deployment/native-aot)
- [CommunityToolkit.Mvvm | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [NuGet Gallery | Avalonia 11.3.11](https://www.nuget.org/packages/avalonia)
- [NuGet Gallery | FluentAvaloniaUI 2.4.1](https://www.nuget.org/packages/FluentAvaloniaUI)
- [Velopack Documentation](https://docs.velopack.io/packaging/overview)

### MEDIUM Confidence (Official Blogs / GitHub)
- [The Expert Guide to Porting WPF Applications to Avalonia](https://avaloniaui.net/blog/the-expert-guide-to-porting-wpf-applications-to-avalonia)
- [From WPF to Avalonia: A Guide for .NET Developers](https://avaloniaui.net/blog/from-wpf-to-avalonia-a-guide-for-net-developers-exploring-cross-platform-ui-frameworks)
- [FluentAvalonia GitHub](https://github.com/amwx/FluentAvalonia)
- [Velopack GitHub](https://github.com/velopack/velopack)
- [ReactiveUI vs Community MVVM Toolkit Discussion](https://github.com/AvaloniaUI/Avalonia/discussions/12540)
- [.NET Community Toolkit 8.4 Announcement](https://www.infoq.com/news/2024/12/dotnet-community-toolkit-84/)

### LOW Confidence (Community / Third-Party)
- [How to use Community Toolkit MVVM in Avalonia - DEV](https://dev.to/ghostkeeper10/how-to-use-community-toolkit-mvvm-in-avalonia-39af)
- [WPF Modernization in 2025](https://wojciechowski.app/en/articles/wpf-modernization-2025)
- [Avalonia ReactiveUI vs Community Toolkit - Toxigon](https://toxigon.com/avalonia-reactiveui-vs-community-toolkit)

---

## Summary

For migrating AdbMirror from WPF to cross-platform:

1. **Use Avalonia 11.3.x** - Latest stable, production-ready
2. **Use CommunityToolkit.Mvvm 8.4.x** - Simple, AOT-friendly, source generators
3. **Use FluentAvaloniaUI 2.4.x** - Dark theme + WinUI controls
4. **Use Velopack** - Cross-platform installers + auto-updates
5. **Target .NET 8.0** - LTS, best cross-platform support
6. **Keep process management simple** - Standard System.Diagnostics.Process for ADB/scrcpy
