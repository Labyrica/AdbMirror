# Avalonia Architecture Research for AdbMirror Migration

## System Overview

```
+------------------------------------------------------------------+
|                        PRESENTATION LAYER                         |
|  +---------------------------+  +-----------------------------+  |
|  |         Views             |  |       ViewModels            |  |
|  |  - MainWindow.axaml       |  |  - MainViewModel.cs         |  |
|  |  - DeviceStatusView.axaml |  |  - DeviceStatusViewModel.cs |  |
|  |  - SettingsView.axaml     |  |  - SettingsViewModel.cs     |  |
|  +---------------------------+  +-----------------------------+  |
|                                                                  |
|  +---------------------------+  +-----------------------------+  |
|  |     ViewLocator           |  |    DataTemplates            |  |
|  |  - Convention-based       |  |  - ViewModel-View mapping   |  |
|  +---------------------------+  +-----------------------------+  |
+------------------------------------------------------------------+
                                |
                    (Dependency Injection)
                                |
+------------------------------------------------------------------+
|                        APPLICATION LAYER                          |
|  +---------------------------+  +-----------------------------+  |
|  |   Platform Services       |  |    Cross-Platform Services  |  |
|  |  - IClipboardService      |  |  - ISettingsService         |  |
|  |  - IStorageService        |  |  - ILogService              |  |
|  +---------------------------+  +-----------------------------+  |
+------------------------------------------------------------------+
                                |
                    (Dependency Injection)
                                |
+------------------------------------------------------------------+
|                      INFRASTRUCTURE LAYER                         |
|  +---------------------------+  +-----------------------------+  |
|  |   External Processes      |  |    Resource Management      |  |
|  |  - IAdbService            |  |  - IResourceExtractor       |  |
|  |  - IScrcpyService         |  |  - Embedded Assets          |  |
|  +---------------------------+  +-----------------------------+  |
|                                                                  |
|  +---------------------------+  +-----------------------------+  |
|  |   Platform Abstractions   |  |    Process Wrapper          |  |
|  |  - Windows impl           |  |  - Async output streaming   |  |
|  |  - macOS impl             |  |  - Lifetime management      |  |
|  |  - Linux impl             |  |  - Error handling           |  |
|  +---------------------------+  +-----------------------------+  |
+------------------------------------------------------------------+
                                |
+------------------------------------------------------------------+
|                          DOMAIN LAYER                             |
|  +---------------------------+  +-----------------------------+  |
|  |        Models             |  |       Enums / Constants     |  |
|  |  - AndroidDevice.cs       |  |  - DeviceState.cs           |  |
|  |  - ScrcpyPreset.cs        |  |  - ScrcpyOptions.cs         |  |
|  +---------------------------+  +-----------------------------+  |
+------------------------------------------------------------------+
```

## Component Responsibilities

| Component | Responsibility | Cross-Platform Notes |
|-----------|---------------|---------------------|
| **Views** | XAML UI definitions, no code-behind logic | Shared across all platforms via Avalonia |
| **ViewModels** | UI state, commands, property change notification | Shared, uses CommunityToolkit.MVVM |
| **ViewLocator** | Maps ViewModel types to View types by convention | Single implementation, customize if needed |
| **IAdbService** | Wraps `adb` CLI for device discovery/management | Different binary names per OS (adb vs adb.exe) |
| **IScrcpyService** | Manages scrcpy process lifecycle | Different binary names, path resolution |
| **IClipboardService** | System clipboard access | Uses Avalonia TopLevel.Clipboard |
| **ISettingsService** | Persists user preferences | Uses JSON file in app data folder |
| **IResourceExtractor** | Extracts embedded binaries to temp folder | Platform-specific paths, executable permissions |
| **Models** | Pure data classes, no framework dependencies | Shared across all layers |

## Recommended Project Structure

```
AdbMirror/
├── AdbMirror.sln
│
├── src/
│   ├── AdbMirror/                          # Main Avalonia application
│   │   ├── App.axaml                       # Application definition
│   │   ├── App.axaml.cs                    # DI container setup
│   │   ├── Program.cs                      # Entry point
│   │   ├── ViewLocator.cs                  # ViewModel-View resolution
│   │   │
│   │   ├── Views/                          # XAML views
│   │   │   ├── MainWindow.axaml
│   │   │   ├── MainWindow.axaml.cs         # Minimal code-behind
│   │   │   └── Controls/                   # Reusable user controls
│   │   │       └── DeviceStatusControl.axaml
│   │   │
│   │   ├── ViewModels/                     # MVVM ViewModels
│   │   │   ├── ViewModelBase.cs            # Base class with INotifyPropertyChanged
│   │   │   ├── MainViewModel.cs
│   │   │   └── SettingsViewModel.cs
│   │   │
│   │   ├── Assets/                         # Avalonia resources
│   │   │   ├── logo.png
│   │   │   └── Styles/
│   │   │       └── App.axaml               # Global styles
│   │   │
│   │   └── AdbMirror.csproj
│   │
│   ├── AdbMirror.Core/                     # Shared business logic
│   │   ├── Services/
│   │   │   ├── Interfaces/
│   │   │   │   ├── IAdbService.cs
│   │   │   │   ├── IScrcpyService.cs
│   │   │   │   ├── IResourceExtractor.cs
│   │   │   │   └── IProcessRunner.cs
│   │   │   │
│   │   │   ├── AdbService.cs
│   │   │   ├── ScrcpyService.cs
│   │   │   ├── ProcessRunner.cs            # Async process management
│   │   │   └── ResourceExtractor.cs
│   │   │
│   │   ├── Models/
│   │   │   ├── AndroidDevice.cs
│   │   │   ├── DeviceState.cs
│   │   │   └── ScrcpyOptions.cs
│   │   │
│   │   └── AdbMirror.Core.csproj
│   │
│   └── AdbMirror.Desktop/                  # Desktop bootstrap (optional)
│       ├── Program.cs                      # Desktop entry point
│       └── AdbMirror.Desktop.csproj
│
├── Resources/                              # Embedded binaries
│   ├── platform-tools.zip
│   └── scrcpy.zip
│
└── tests/
    └── AdbMirror.Tests/
        ├── ViewModels/
        └── Services/
```

### Project Structure Rationale

1. **Separation of Concerns**: Core logic in `AdbMirror.Core` allows reuse and easier testing
2. **Views/ViewModels Split**: Clear folder structure follows Avalonia conventions for ViewLocator
3. **Assets Folder**: Standard location for `<AvaloniaResource>` items
4. **Desktop Bootstrap**: Optional separate project for platform-specific startup code

## Architectural Patterns

### 1. MVVM with CommunityToolkit.MVVM (Recommended)

```csharp
// ViewModels/ViewModelBase.cs
using CommunityToolkit.Mvvm.ComponentModel;

public abstract partial class ViewModelBase : ObservableObject
{
    // Source generators handle INotifyPropertyChanged
}

// ViewModels/MainViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class MainViewModel : ViewModelBase
{
    private readonly IAdbService _adbService;
    private readonly IScrcpyService _scrcpyService;

    [ObservableProperty]
    private string _statusText = "No device connected";

    [ObservableProperty]
    private bool _isPrimaryEnabled;

    [ObservableProperty]
    private ScrcpyPreset _selectedPreset = ScrcpyPreset.Balanced;

    public MainViewModel(IAdbService adbService, IScrcpyService scrcpyService)
    {
        _adbService = adbService;
        _scrcpyService = scrcpyService;
    }

    [RelayCommand(CanExecute = nameof(IsPrimaryEnabled))]
    private async Task MirrorAsync()
    {
        // Command implementation
    }

    partial void OnSelectedPresetChanged(ScrcpyPreset value)
    {
        // React to property changes
    }
}
```

### 2. ViewLocator Pattern

```csharp
// ViewLocator.cs
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using AdbMirror.ViewModels;
using System;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        if (data is null) return null;

        var name = data.GetType().FullName!
            .Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        if (type != null)
        {
            return (Control)Activator.CreateInstance(type)!;
        }

        return new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}

// App.axaml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:local="using:AdbMirror"
             x:Class="AdbMirror.App">
    <Application.DataTemplates>
        <local:ViewLocator/>
    </Application.DataTemplates>
</Application>
```

### 3. Dependency Injection Setup

```csharp
// App.axaml.cs
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Core services
        services.AddSingleton<IAdbService, AdbService>();
        services.AddSingleton<IScrcpyService, ScrcpyService>();
        services.AddSingleton<IResourceExtractor, ResourceExtractor>();
        services.AddSingleton<ISettingsService, SettingsService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
    }
}
```

### 4. External Process Management Pattern

```csharp
// Services/Interfaces/IProcessRunner.cs
public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(
        string executable,
        string arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> RunStreamingAsync(
        string executable,
        string arguments,
        CancellationToken cancellationToken = default);
}

// Services/ProcessRunner.cs
public class ProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(
        string executable,
        string arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        var tcs = new TaskCompletionSource<int>();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null) outputBuilder.AppendLine(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) errorBuilder.AppendLine(e.Data);
        };

        process.Exited += (_, _) => tcs.TrySetResult(process.ExitCode);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);

        try
        {
            await tcs.Task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(); } catch { }
            throw;
        }

        return new ProcessResult(
            process.ExitCode,
            outputBuilder.ToString(),
            errorBuilder.ToString());
    }

    // Streaming output using C# async streams
    public async IAsyncEnumerable<string> RunStreamingAsync(
        string executable,
        string arguments,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        process.Start();

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await process.StandardOutput.ReadLineAsync(cancellationToken);
            if (line == null) break;
            yield return line;
        }

        if (!process.HasExited)
        {
            process.Kill();
        }
    }
}

public record ProcessResult(int ExitCode, string Output, string Error);
```

### 5. Platform-Specific Service Abstraction

```csharp
// Services/Interfaces/IPlatformPaths.cs
public interface IPlatformPaths
{
    string GetAdbExecutableName();
    string GetScrcpyExecutableName();
    string GetAppDataPath();
    string GetTempPath();
}

// Services/PlatformPaths.cs
public class PlatformPaths : IPlatformPaths
{
    public string GetAdbExecutableName()
    {
        return OperatingSystem.IsWindows() ? "adb.exe" : "adb";
    }

    public string GetScrcpyExecutableName()
    {
        return OperatingSystem.IsWindows() ? "scrcpy.exe" : "scrcpy";
    }

    public string GetAppDataPath()
    {
        var appData = Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "AdbMirror");
    }

    public string GetTempPath()
    {
        return Path.GetTempPath();
    }
}
```

### 6. Clipboard Service Abstraction

```csharp
// Services/Interfaces/IClipboardService.cs
public interface IClipboardService
{
    Task SetTextAsync(string text);
    Task<string?> GetTextAsync();
}

// Services/ClipboardService.cs
public class ClipboardService : IClipboardService
{
    private readonly TopLevel _topLevel;

    public ClipboardService(TopLevel topLevel)
    {
        _topLevel = topLevel;
    }

    public async Task SetTextAsync(string text)
    {
        var clipboard = _topLevel.Clipboard;
        if (clipboard != null)
        {
            await clipboard.SetTextAsync(text);
        }
    }

    public async Task<string?> GetTextAsync()
    {
        var clipboard = _topLevel.Clipboard;
        return clipboard != null ? await clipboard.GetTextAsync() : null;
    }
}

// Alternative: Inject via ViewModel
public partial class MainViewModel : ViewModelBase
{
    [RelayCommand]
    private async Task CopyLogsAsync()
    {
        // Access clipboard through the view
        // Use messaging or callback pattern
    }
}
```

## Data Flow Diagrams

### Device State Polling Flow

```
+----------------+     poll interval      +----------------+
|   AdbService   | <---------------------- |   ViewModel    |
|                |                         |                |
|  GetDevices()  |                         | StartPolling() |
+----------------+                         +----------------+
        |                                         ^
        | DeviceState                             |
        v                                         |
+----------------+                         +----------------+
|  State Change  | ----------------------> |   UI Update    |
|    Observer    |    Dispatcher.Post()    |   (Binding)    |
+----------------+                         +----------------+
```

### Mirroring Start Flow

```
User Click                     ViewModel                      ScrcpyService
    |                              |                               |
    |-- Click Mirror Button ------>|                               |
    |                              |-- StartMirroringAsync() ----->|
    |                              |                               |
    |                              |    Process.Start()            |
    |                              |    Setup exit handler         |
    |                              |<-- Success/Failure -----------|
    |                              |                               |
    |<-- Update UI ----------------|                               |
    |                              |                               |
    |                         [Process Running]                    |
    |                              |                               |
    |                              |<-- OnExited callback ---------|
    |<-- Update UI ----------------|                               |
```

### Settings Persistence Flow

```
+----------------+     Save          +----------------+
|   ViewModel    | ----------------> | SettingsService|
|                |                   |                |
| SelectedPreset |                   | Save to JSON   |
| AutoMirror     |                   | AppData folder |
+----------------+                   +----------------+
        ^                                    |
        |         Load on startup            |
        +------------------------------------+
```

## Cross-Platform Considerations

### 1. Platform-Specific Binary Names

```csharp
// Use OperatingSystem checks (preferred in .NET 5+)
public static string GetExecutableName(string baseName)
{
    if (OperatingSystem.IsWindows())
        return $"{baseName}.exe";
    return baseName;
}
```

### 2. Path Resolution Strategy

| Platform | adb Location | scrcpy Location | AppData |
|----------|--------------|-----------------|---------|
| Windows | `%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe` | PATH or bundled | `%APPDATA%\AdbMirror` |
| macOS | `~/Library/Android/sdk/platform-tools/adb` | Homebrew: `/opt/homebrew/bin/scrcpy` | `~/Library/Application Support/AdbMirror` |
| Linux | `/usr/bin/adb` or `~/.android/Sdk/platform-tools/adb` | Package manager or bundled | `~/.config/AdbMirror` |

### 3. Resource Extraction Permissions

```csharp
// After extracting on Unix-like systems, set executable permission
if (!OperatingSystem.IsWindows())
{
    var chmod = Process.Start("chmod", $"+x \"{extractedPath}\"");
    chmod?.WaitForExit();
}
```

### 4. Conditional Compilation (When Needed)

```xml
<!-- In .csproj for platform-specific resources -->
<ItemGroup Condition="$([MSBuild]::IsOSPlatform('Windows'))">
    <EmbeddedResource Include="Resources\scrcpy-win64.zip" />
</ItemGroup>
<ItemGroup Condition="$([MSBuild]::IsOSPlatform('OSX'))">
    <EmbeddedResource Include="Resources\scrcpy-macos.zip" />
</ItemGroup>
<ItemGroup Condition="$([MSBuild]::IsOSPlatform('Linux'))">
    <EmbeddedResource Include="Resources\scrcpy-linux.zip" />
</ItemGroup>
```

### 5. UI Thread Dispatching

```csharp
// Avalonia uses Dispatcher.UIThread instead of Application.Current.Dispatcher
await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
{
    StatusText = "Device connected";
});

// Or use the extension method pattern
public static class DispatcherExtensions
{
    public static void Post(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
            action();
        else
            Dispatcher.UIThread.Post(action);
    }
}
```

### 6. Publishing for Multiple Platforms

```bash
# Windows
dotnet publish -c Release -r win-x64 --self-contained

# macOS (Intel)
dotnet publish -c Release -r osx-x64 --self-contained

# macOS (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained

# Linux
dotnet publish -c Release -r linux-x64 --self-contained
```

## Anti-Patterns to Avoid

### 1. Direct UI Access in ViewModels

```csharp
// BAD: Direct clipboard access via WPF
Clipboard.SetText(logs); // WPF-specific, won't work in Avalonia

// GOOD: Inject abstraction
public class MainViewModel
{
    private readonly IClipboardService _clipboard;

    public MainViewModel(IClipboardService clipboard)
    {
        _clipboard = clipboard;
    }

    [RelayCommand]
    private async Task CopyLogsAsync()
    {
        await _clipboard.SetTextAsync(Logs);
    }
}
```

### 2. Service Locator Anti-Pattern

```csharp
// BAD: Service locator (hidden dependencies)
var adb = App.Services.GetService<IAdbService>();

// GOOD: Constructor injection (explicit dependencies)
public MainViewModel(IAdbService adbService)
{
    _adbService = adbService;
}
```

### 3. Blocking UI Thread

```csharp
// BAD: Blocking call on UI thread
var devices = _adbService.GetDevices(); // Synchronous

// GOOD: Async with proper await
var devices = await _adbService.GetDevicesAsync();
```

### 4. Hardcoded Platform Assumptions

```csharp
// BAD: Windows-only path
var adbPath = @"C:\Users\...\platform-tools\adb.exe";

// GOOD: Platform-agnostic resolution
var adbPath = _platformPaths.ResolveAdbPath();
```

### 5. Mixed Sync/Async Process Reading

```csharp
// BAD: Can cause deadlocks
process.Start();
process.WaitForExit();
var output = process.StandardOutput.ReadToEnd(); // May deadlock

// GOOD: Async reading before wait
process.Start();
var outputTask = process.StandardOutput.ReadToEndAsync();
await process.WaitForExitAsync();
var output = await outputTask;
```

### 6. Code-Behind Business Logic

```csharp
// BAD: Logic in code-behind
public partial class MainWindow : Window
{
    private void Button_Click(object sender, EventArgs e)
    {
        var devices = new AdbService().GetDevices(); // Business logic in view
    }
}

// GOOD: All logic in ViewModel, view only binds
<Button Command="{Binding MirrorCommand}" />
```

### 7. Implementing INotifyPropertyChanged Manually

```csharp
// BAD: Boilerplate everywhere
private string _status;
public string Status
{
    get => _status;
    set
    {
        if (_status != value)
        {
            _status = value;
            OnPropertyChanged();
        }
    }
}

// GOOD: Use CommunityToolkit.MVVM source generators
[ObservableProperty]
private string _status;
```

## Integration Points

### 1. External Tool Integration (adb, scrcpy)

| Tool | Purpose | Integration Method |
|------|---------|-------------------|
| **adb** | Device detection, USB debugging | Process.Start with output capture |
| **scrcpy** | Screen mirroring | Long-running process with exit callback |

**Key Considerations:**
- Both tools must be bundled or available on PATH
- Output streams need async reading to prevent blocking
- Process lifecycle must be managed (stop on app close)
- Working directory matters for scrcpy (needs access to scrcpy-server)

### 2. System Clipboard

```csharp
// Access via TopLevel.Clipboard
var topLevel = TopLevel.GetTopLevel(this);
await topLevel?.Clipboard?.SetTextAsync("text");
```

### 3. File System / Storage

```csharp
// Settings storage
var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
var settingsPath = Path.Combine(appData, "AdbMirror", "settings.json");

// Temp extraction
var tempPath = Path.Combine(Path.GetTempPath(), "AdbMirror");
```

### 4. Embedded Resources

```xml
<!-- Include in .csproj -->
<ItemGroup>
    <AvaloniaResource Include="Assets\**"/>
</ItemGroup>

<!-- Embedded binaries -->
<ItemGroup>
    <EmbeddedResource Include="Resources\platform-tools.zip">
        <LogicalName>AdbMirror.Resources.platform-tools.zip</LogicalName>
    </EmbeddedResource>
</ItemGroup>
```

```csharp
// Access embedded resources
var assembly = Assembly.GetExecutingAssembly();
using var stream = assembly.GetManifestResourceStream("AdbMirror.Resources.platform-tools.zip");
```

### 5. Application Lifecycle

```csharp
// Handle application shutdown
if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
{
    desktop.ShutdownRequested += (_, e) =>
    {
        // Cleanup: stop scrcpy, cancel polling
        var vm = Services.GetService<MainViewModel>();
        vm?.Dispose();
    };
}
```

## Sources and Confidence Levels

### Official Documentation (High Confidence)

- [Avalonia Architecture Guide](https://docs.avaloniaui.net/docs/guides/building-cross-platform-applications/architecture) - Layered architecture recommendations
- [Avalonia MVVM Pattern](https://docs.avaloniaui.net/docs/concepts/the-mvvm-pattern) - Official MVVM guidance
- [Avalonia ReactiveUI](https://docs.avaloniaui.net/docs/concepts/reactiveui/) - ReactiveUI integration
- [Avalonia DI Guide](https://docs.avaloniaui.net/docs/guides/implementation-guides/how-to-implement-dependency-injection) - Dependency injection patterns
- [Avalonia View Locator](https://docs.avaloniaui.net/docs/concepts/view-locator) - ViewModel-View resolution
- [Avalonia Assets](https://docs.avaloniaui.net/docs/basics/user-interface/assets) - Resource embedding
- [Avalonia Clipboard](https://docs.avaloniaui.net/docs/concepts/services/clipboard) - Clipboard service
- [Avalonia Platform-Specific Code](https://docs.avaloniaui.net/docs/guides/platforms/platform-specific-code/dotnet) - Conditional compilation
- [macOS Deployment](https://docs.avaloniaui.net/docs/deployment/macOS) - Cross-platform publishing

### Community Discussions (Medium-High Confidence)

- [ReactiveUI vs CommunityToolkit.MVVM](https://github.com/AvaloniaUI/Avalonia/discussions/12540) - Framework comparison
- [CommunityToolkit MVVM with Avalonia](https://github.com/AvaloniaUI/Avalonia/discussions/9020) - Integration guidance
- [DI on ViewModels](https://github.com/AvaloniaUI/Avalonia/discussions/15630) - Injection patterns
- [Large Modularized Projects](https://github.com/AvaloniaUI/Avalonia/discussions/6791) - Project structure advice
- [Clipboard MVVM Access](https://github.com/AvaloniaUI/Avalonia/discussions/11170) - Service abstraction patterns

### Technical Articles (Medium Confidence)

- [Avalonia Clean Architecture](https://medium.com/c-sharp-programming/avalonia-and-reactiveui-mvvm-di-clean-architecture-67fe4777d463) - Architecture patterns
- [CommunityToolkit.MVVM in Avalonia](https://dev.to/ghostkeeper10/how-to-use-community-toolkit-mvvm-in-avalonia-39af) - Step-by-step guide
- [DI with Avalonia](https://khalidabuhakmeh.com/dependency-injection-with-avalonia-ui-apps) - Real-world examples
- [Avalonia DI Tutorial](https://dev.to/ingvarx/avaloniaui-dependency-injection-4aka) - Implementation details

### Process Management (High Confidence)

- [ProcessX Library](https://github.com/Cysharp/ProcessX) - Async process streaming
- [Process.BeginOutputReadLine](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.beginoutputreadline) - .NET async patterns
- [Process.OutputDataReceived](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.outputdatareceived) - Event-based output

### Confidence Level Key

| Level | Meaning |
|-------|---------|
| **High** | Official documentation, Microsoft docs, well-established patterns |
| **Medium-High** | Active GitHub discussions with team member responses |
| **Medium** | Community articles, third-party tutorials with recent updates |
| **Low** | Older posts, experimental approaches, single-source information |

## Migration Checklist from WPF

1. [ ] Replace `net8.0-windows` TFM with `net8.0`
2. [ ] Replace WPF NuGet packages with Avalonia equivalents
3. [ ] Rename `.xaml` files to `.axaml`
4. [ ] Update XAML namespaces (`xmlns` declarations)
5. [ ] Replace `System.Windows.Input.ICommand` usage with commands from MVVM toolkit
6. [ ] Replace `Application.Current.Dispatcher` with `Dispatcher.UIThread`
7. [ ] Replace `System.Windows.Clipboard` with `TopLevel.Clipboard`
8. [ ] Add `ViewLocator` and register in `App.axaml`
9. [ ] Set up dependency injection in `App.axaml.cs`
10. [ ] Convert `RelayCommand` to `[RelayCommand]` attribute
11. [ ] Abstract platform-specific paths
12. [ ] Test on Windows, macOS, and Linux
