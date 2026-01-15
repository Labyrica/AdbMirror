# Testing Patterns

**Analysis Date:** 2025-01-15

## Test Framework

**Runner:**
- Not detected - no test project present

**Assertion Library:**
- Not applicable

**Run Commands:**
```bash
# No test commands available
# Manual testing against physical Android device required
```

## Test File Organization

**Location:**
- No test files present
- No test project in solution

**Naming:**
- Not applicable

**Structure:**
```
# Current state - no tests
AdbMirror/           # Main project only
AdbMirror.sln        # Single project solution
```

## Test Structure

**Suite Organization:**
- Not applicable - no tests exist

**Patterns:**
- Not established

## Mocking

**Framework:**
- Not established

**Patterns:**
- Not applicable

**What Would Need Mocking:**
- ADB process execution (`System.Diagnostics.Process`)
- File system operations for resource extraction
- scrcpy process execution
- HTTP client for scrcpy auto-download

## Fixtures and Factories

**Test Data:**
- Not established

**Location:**
- Not applicable

## Coverage

**Requirements:**
- None defined

**Configuration:**
- Not applicable

**View Coverage:**
```bash
# No coverage tooling configured
```

## Test Types

**Unit Tests:**
- Not present
- Would benefit: `AdbService.GetDevices()` parsing, `AppSettings` serialization

**Integration Tests:**
- Not present
- Would require: Mock ADB responses, file system access

**E2E Tests:**
- Manual testing only
- Requires physical Android device with USB debugging enabled

## Common Patterns

**Current Testing Approach:**
- Manual testing with physical devices
- Visual verification of UI functionality
- Console/debug output for diagnostics

**Recommended Test Structure (if adding tests):**
```csharp
// Suggested pattern for future test project
[TestClass]
public class AdbServiceTests
{
    [TestMethod]
    public void GetDevices_ValidOutput_ParsesCorrectly()
    {
        // Arrange
        var mockOutput = "List of devices attached\nABC123\tdevice model:Pixel_6";

        // Act - would need refactoring to allow injection

        // Assert
    }
}
```

## Testing Gaps Analysis

**Critical Untested Areas:**
1. Device parsing logic in `AdbService.GetDevices()` - `AdbMirror/Core/AdbService.cs`
2. Settings serialization/deserialization - `AdbMirror/AppSettings.cs`
3. Resource extraction - `AdbMirror/Core/ResourceExtractor.cs`
4. State machine transitions in `MainViewModel`

**Testing Challenges:**
- Heavy dependency on external processes (ADB, scrcpy)
- WPF UI testing requires specialized frameworks
- File system and process operations tightly coupled

**Recommended Approach:**
1. Add unit test project: `AdbMirror.Tests`
2. Refactor services to accept interfaces for process execution
3. Start with parsing logic and settings tests
4. Consider MSTest, xUnit, or NUnit

---

*Testing analysis: 2025-01-15*
*Update when test patterns change*
