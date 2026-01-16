@echo off
REM Run PhoneMirror application
setlocal

REM Find dotnet
set "DOTNET_CMD=dotnet"
where dotnet >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    if exist "%ProgramFiles%\dotnet\dotnet.exe" (
        set "DOTNET_CMD=%ProgramFiles%\dotnet\dotnet.exe"
    ) else if exist "%ProgramFiles(x86)%\dotnet\dotnet.exe" (
        set "DOTNET_CMD=%ProgramFiles(x86)%\dotnet\dotnet.exe"
    ) else if exist "%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe" (
        set "DOTNET_CMD=%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe"
    ) else (
        echo.
        echo ERROR: dotnet.exe not found!
        echo.
        echo Please install .NET SDK 8.0 or later from:
        echo https://dotnet.microsoft.com/download
        echo.
        echo After installation, restart your terminal and try again.
        echo.
        pause
        exit /b 1
    )
)

echo Restoring packages...
"%DOTNET_CMD%" restore PhoneMirror.sln
if %ERRORLEVEL% NEQ 0 (
    echo Restore failed!
    pause
    exit /b 1
)

echo.
echo Running PhoneMirror...
start "" "%DOTNET_CMD%" run --project src/PhoneMirror/PhoneMirror.csproj

echo.
echo PhoneMirror is launching in a new window...
echo You can close this window.
timeout /t 2 /nobreak >nul
