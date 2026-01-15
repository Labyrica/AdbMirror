# Architecture

**Analysis Date:** 2025-01-15

## Pattern Overview

**Overall:** WPF Desktop Application with MVVM Pattern

**Key Characteristics:**
- Single-window desktop application
- MVVM (Model-View-ViewModel) architecture
- Service layer for external tool integration
- Event-driven device polling
- Single executable with embedded resources

## Layers

**View Layer:**
- Purpose: UI presentation and user interaction
- Contains: XAML layouts, code-behind for navigation
- Location: `AdbMirror/MainWindow.xaml`, `AdbMirror/MainWindow.xaml.cs`
- Depends on: ViewModel layer via data binding
- Used by: WPF runtime

**ViewModel Layer:**
- Purpose: UI state management and command handling
- Contains: MainViewModel with INotifyPropertyChanged, RelayCommand
- Location: `AdbMirror/MainViewModel.cs`
- Depends on: Service layer (AdbService, ScrcpyService), Models
- Used by: View layer via DataContext binding

**Service Layer:**
- Purpose: External process management and business logic
- Contains: AdbService, ScrcpyService, ResourceExtractor
- Location: `AdbMirror/Core/*.cs`
- Depends on: System.Diagnostics.Process, Models
- Used by: ViewModel layer

**Model Layer:**
- Purpose: Domain entities and enumerations
- Contains: AndroidDevice, DeviceState, ScrcpyPreset
- Location: `AdbMirror/Models/AndroidDevice.cs`, `AdbMirror/Core/DeviceState.cs`
- Depends on: Nothing (POCOs)
- Used by: Service and ViewModel layers

**Configuration Layer:**
- Purpose: Persistent user settings
- Contains: AppSettings with JSON serialization
- Location: `AdbMirror/AppSettings.cs`
- Depends on: System.Text.Json
- Used by: ViewModel layer

## Data Flow

**Application Startup:**

1. `App.xaml` → `App.xaml.cs.OnStartup()` registers global exception handlers
2. `MainWindow` constructor creates `MainViewModel` as DataContext
3. `MainViewModel.InitializeAsync()` checks ADB/scrcpy availability
4. Device polling loop starts via `AdbService.StartPollingAsync()`
5. Polling callback updates UI state via `OnDeviceStateChanged()`

**Mirror Command Execution:**

1. User clicks Mirror button → `PrimaryCommand` executes
2. `MainViewModel.OnPrimaryClicked()` validates device state
3. `ScrcpyService.StartMirroring()` launches scrcpy process
4. Process exit callback updates UI via dispatcher
5. Status and logs updated throughout

**State Management:**
- Application state: Held in `MainViewModel` private fields
- Persistent state: `AppSettings` saved to `%LocalAppData%\AdbMirror\settings.json`
- Device state: Continuously polled by background task in `AdbService`

## Key Abstractions

**Services:**
- Purpose: Encapsulate external tool interaction
- Examples: `AdbService` (device discovery), `ScrcpyService` (mirroring), `ResourceExtractor` (embedded resources)
- Pattern: Instantiated directly, stateful (holds process references)

**ViewModel:**
- Purpose: Bridge between UI and services
- Examples: `MainViewModel`
- Pattern: INotifyPropertyChanged with RelayCommand for WPF binding

**Device State Machine:**
- Purpose: High-level device connection status
- Examples: `DeviceState` enum (NoDevice, Unauthorized, Connected, etc.)
- Pattern: Polling-based state detection, observer callback

**Settings:**
- Purpose: User preferences persistence
- Examples: `AppSettings`
- Pattern: JSON file in LocalAppData, load/save methods

## Entry Points

**Application Entry:**
- Location: `AdbMirror/App.xaml`, `AdbMirror/App.xaml.cs`
- Triggers: Windows executable launch
- Responsibilities: Global exception handling, WPF startup

**Main Window:**
- Location: `AdbMirror/MainWindow.xaml`, `AdbMirror/MainWindow.xaml.cs`
- Triggers: Application startup
- Responsibilities: Create ViewModel, host UI

## Error Handling

**Strategy:** Global exception handlers + local try/catch in services

**Patterns:**
- `App.xaml.cs` catches unhandled exceptions, shows MessageBox, prevents crash
- Service methods return bool success with `out error` pattern
- ViewModel catches exceptions, updates StatusText for user feedback
- External process failures captured via exit codes and stderr

## Cross-Cutting Concerns

**Logging:**
- In-memory log buffer in MainViewModel (`_logBuffer`)
- Displayed in UI via Logs binding
- Timestamped entries, 10KB max buffer size
- Copy to clipboard via CopyLogsCommand

**Settings Persistence:**
- `AppSettings.Load()` / `AppSettings.Save()` methods
- JSON serialization via System.Text.Json
- Silent failure on save errors (best-effort)

**Resource Extraction:**
- `ResourceExtractor` extracts embedded zip files on first access
- Thread-safe via lock
- Falls back gracefully if resources not embedded

**UI Thread Marshalling:**
- `Application.Current.Dispatcher.Invoke()` for cross-thread UI updates
- Used in polling callback and process exit handlers

---

*Architecture analysis: 2025-01-15*
*Update when major patterns change*
