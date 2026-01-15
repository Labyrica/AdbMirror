# Coding Conventions

**Analysis Date:** 2025-01-15

## Naming Patterns

**Files:**
- PascalCase for all C# files (e.g., `MainViewModel.cs`, `AdbService.cs`)
- PascalCase for XAML files (e.g., `MainWindow.xaml`)
- *.xaml.cs for code-behind files

**Functions:**
- PascalCase for public methods (e.g., `StartMirroring`, `GetDevices`)
- PascalCase for private methods (e.g., `OnPrimaryClicked`, `OnDeviceStateChanged`)
- Event handlers prefixed with `On` (e.g., `OnScrcpyExited`, `OnCreditsClick`)

**Variables:**
- camelCase for local variables (e.g., `device`, `candidates`)
- _camelCase for private fields with underscore prefix (e.g., `_adbPath`, `_currentProcess`)
- PascalCase for properties (e.g., `StatusText`, `IsPrimaryEnabled`)

**Types:**
- PascalCase for classes, no prefix (e.g., `AdbService`, `AndroidDevice`)
- PascalCase for enums (e.g., `DeviceState`, `ScrcpyPreset`)
- Suffix `Service` for service classes
- Suffix `ViewModel` for view model classes

## Code Style

**Formatting:**
- No explicit config file (.editorconfig not present)
- 4 space indentation (inferred from code)
- Allman-style braces (opening brace on new line)
- File-scoped namespaces (e.g., `namespace AdbMirror.Core;`)

**Nullable Reference Types:**
- Enabled via `<Nullable>enable</Nullable>` in csproj
- `?` suffix for nullable types (e.g., `string?`, `AndroidDevice?`)
- `out` parameters with nullable return (e.g., `out string? error`)

**Expression Body:**
- Used for simple property getters: `public string GetAdbPath() => _adbPath;`
- Used for simple predicate methods: `public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;`

## Import Organization

**Order:**
1. System namespaces (System, System.*)
2. Third-party namespaces (none currently)
3. Project namespaces (AdbMirror.*)

**Grouping:**
- No blank lines between using groups
- No sorting preference observed

**Aliases:**
- None used

## Error Handling

**Patterns:**
- Boolean return with `out string? error` parameter for operations that can fail
- Try/catch with empty catch for non-critical operations (e.g., process.Kill())
- Global exception handler in `App.xaml.cs` for unhandled exceptions

**Error Types:**
- Return false with error message for expected failures
- Silent failures for cleanup operations (catch blocks swallow exceptions)
- MessageBox for user-facing errors

**Examples:**
```csharp
// out error pattern - AdbService.cs, ScrcpyService.cs
public bool IsAdbAvailable(out string? error)

// Silent catch - multiple files
try { process.Kill(); } catch { /* ignore */ }
```

## Logging

**Framework:**
- Custom in-memory log buffer in `MainViewModel`
- StringBuilder with max size limit (10KB)

**Patterns:**
- Timestamped log entries: `[HH:mm:ss] message`
- Log via `Log(string message)` private method
- Displayed in UI, copyable to clipboard

**When:**
- Application startup and initialization
- Device state changes
- Mirror start/stop operations
- Errors and failures

## Comments

**When to Comment:**
- XML doc comments (`/// <summary>`) for all public types and methods
- Inline comments for non-obvious logic or workarounds
- TODO comments for planned improvements

**JSDoc/TSDoc:**
- Not applicable (C# uses XML docs)
- XML doc format: `<summary>`, `<param>`, `<returns>`

**Style:**
```csharp
/// <summary>
/// Thin wrapper around the adb executable: device discovery, state and server management.
/// </summary>
public sealed class AdbService

// NOTE: comment style for inline explanations
// If `where` is not available, silently fall back to other strategies.
```

## Function Design

**Size:**
- Methods generally under 50 lines
- Longer methods like `ResolveAdbPath` are acceptable for configuration logic

**Parameters:**
- Simple types preferred
- Optional parameters with null default
- `out` parameters for multiple return values

**Return Values:**
- Boolean for success/failure operations
- Tuples for internal helper methods: `(int ExitCode, string Output, string Error)`
- void for side-effect methods

## Module Design

**Exports:**
- Public classes for external usage
- Internal/private for implementation details
- sealed classes when not designed for inheritance

**Organization:**
- One primary class per file
- Helper classes can be in same file (e.g., `RelayCommand` in `MainViewModel.cs`)
- Enums in separate files or with related class

---

*Convention analysis: 2025-01-15*
*Update when patterns change*
