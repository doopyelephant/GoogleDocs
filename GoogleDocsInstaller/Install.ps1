param (
    [string]$Drive = "C:\",
    [string]$Mode = "External"
)
$ProgressPreference = 'SilentlyContinue'
$AppData = "$env:USERPROFILE\AppData\Local\GoogleDocs"
$Release = "$AppData\Release.zip"
Invoke-WebRequest -Uri "https://github.com/doopyelephant/GoogleDocs/releases/download/v0.1.0-alpha/GoogleDocs-windows-x64.zip" -OutFile "$Release"
Write-Host "60"
Expand-Archive -Path $Release -DestinationPath "$AppData\Release"
Write-Host "70"
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/doopyelephant/GoogleDocs/refs/heads/master/GoogleDocs/DocsLogo.ico" -OutFile "$AppData\Release\GoogleDocs\icon.ico"
Write-Host "80"
Set-Location "$AppData\Release\GoogleDocs"
$ExePath = "$AppData\Release\GoogleDocs\GoogleDocs.exe"
$IcoPath = "$AppData\Release\GoogleDocs\icon.ico"
$WScript = New-Object -ComObject WScript.Shell
$Shortcut = $WScript.CreateShortcut("$env:USERPROFILE\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\GoogleDocs.lnk")
$Shortcut.TargetPath = $ExePath
$Shortcut.WorkingDirectory = "$AppData\Release\GoogleDocs\"
$Shortcut.IconLocation = "$IcoPath, 0"
$Shortcut.Save()
Write-Host "80"
# Define application details
$AppName = "Google Docs"
$RegistryKeyName = "GoogleDocs" # Unique key name for the registry
$DisplayVersion = "0.1.0"
$Publisher = "DoopyElephant"
$InstallLocation = "$AppData\Release\GoogleDocs"
$UninstallCommand = "powershell.exe -ExecutionPolicy Bypass -File $AppData\Release\GoogleDocs\Uninstall.ps1"
$RegistryPath = "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\$RegistryKeyName"
if (-not (Test-Path $RegistryPath)) {
    New-Item -Path $RegistryPath -Force | Out-Null
}
New-ItemProperty -Path $RegistryPath -Name "DisplayName" -Value $AppName -PropertyType String -Force | Out-Null
New-ItemProperty -Path $RegistryPath -Name "DisplayVersion" -Value $DisplayVersion -PropertyType String -Force | Out-Null
New-ItemProperty -Path $RegistryPath -Name "Publisher" -Value $Publisher -PropertyType String -Force | Out-Null
New-ItemProperty -Path $RegistryPath -Name "InstallLocation" -Value $InstallLocation -PropertyType String -Force | Out-Null
New-ItemProperty -Path $RegistryPath -Name "UninstallString" -Value $UninstallCommand -PropertyType String -Force | Out-Null
Write-Host "90"
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/doopyelephant/GoogleDocs/refs/heads/master/GoogleDocs/Uninstall.ps1" -OutFile "$AppData\Release\GoogleDocs\Uninstall.ps1"
Write-Host "100"