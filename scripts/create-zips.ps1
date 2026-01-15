$resourcesPath = 'C:\Users\rober\Documents\GitHub\AdbMirror\src\PhoneMirror.Core\Resources'

# Create platform-tools.zip
if (Test-Path "$resourcesPath\platform-tools") {
    $zipPath = "$resourcesPath\platform-tools.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Compress-Archive -Path "$resourcesPath\platform-tools\*" -DestinationPath $zipPath -CompressionLevel Optimal
    Write-Host "Created platform-tools.zip"
}

# Create scrcpy.zip
if (Test-Path "$resourcesPath\scrcpy") {
    $zipPath = "$resourcesPath\scrcpy.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Compress-Archive -Path "$resourcesPath\scrcpy\*" -DestinationPath $zipPath -CompressionLevel Optimal
    Write-Host "Created scrcpy.zip"
}

Write-Host "Done!"
