$ErrorActionPreference = 'Stop'
$resourcesPath = 'C:\Users\rober\Documents\GitHub\AdbMirror\src\PhoneMirror.Core\Resources'

New-Item -ItemType Directory -Force -Path $resourcesPath | Out-Null

# Download platform-tools
Write-Host 'Downloading platform-tools...'
$ptZip = "$env:TEMP\platform-tools.zip"
Invoke-WebRequest -Uri 'https://dl.google.com/android/repository/platform-tools-latest-windows.zip' -OutFile $ptZip
Write-Host 'Extracting platform-tools...'
Expand-Archive -Path $ptZip -DestinationPath $resourcesPath -Force
Remove-Item $ptZip

# Download scrcpy
Write-Host 'Downloading scrcpy...'
$scrcpyZip = "$env:TEMP\scrcpy.zip"
Invoke-WebRequest -Uri 'https://github.com/Genymobile/scrcpy/releases/download/v3.1/scrcpy-win64-v3.1.zip' -OutFile $scrcpyZip
Write-Host 'Extracting scrcpy...'
$scrcpyTemp = "$env:TEMP\scrcpy-extract"
if (Test-Path $scrcpyTemp) { Remove-Item $scrcpyTemp -Recurse -Force }
Expand-Archive -Path $scrcpyZip -DestinationPath $scrcpyTemp -Force

# Move scrcpy contents to Resources/scrcpy
$scrcpyDest = "$resourcesPath\scrcpy"
if (Test-Path $scrcpyDest) { Remove-Item $scrcpyDest -Recurse -Force }
$extractedFolder = Get-ChildItem $scrcpyTemp | Select-Object -First 1
Move-Item -Path $extractedFolder.FullName -Destination $scrcpyDest

Remove-Item $scrcpyZip
Remove-Item $scrcpyTemp -Recurse -Force -ErrorAction SilentlyContinue

Write-Host 'Done! Dependencies downloaded to:' $resourcesPath
