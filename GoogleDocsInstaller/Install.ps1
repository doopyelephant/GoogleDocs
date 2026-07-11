param (
    [string]$Drive = "C:\",
    [string]$Mode = "External"
)
$ProgressPreference = 'SilentlyContinue'

if($IsWindows)
{
    if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]"Administrator"))
    {
        $arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`""
        Start-Process powershell.exe -ArgumentList $arguments -Verb RunAs
        Exit
    }
}
$serverName = "GoogleDocsInstallerPipe"


# Create client stream
$client = New-Object System.IO.Pipes.NamedPipeClientStream(".", $serverName, [System.IO.Pipes.PipeDirection]::Out)

Write-Host "Connecting to pipe '$serverName'..."

# Connect with a 5-second timeout (in milliseconds)
$client.Connect(5000)

Write-Host "Connected!"
$writer = New-Object System.IO.StreamWriter($client)
$AppData = ""
if($IsWindows)
{
    $AppData = "$env:USERPROFILE\AppData\Local\GoogleDocs"
}
if($IsLinux)
{
    $AppData = "/opt/GoogleDocs"
}
mkdir $AppData
$Release = "$AppData\Release.zip"
$OSarch = ""
if($IsWindows)
{
    $OSarch = $env:PROCESSOR_ARCHITECTURE
}
if($IsLinux)
{
    $OSarch = (uname -m).Trim()
}
$OSarch = $OSarch.ToLower()
switch($OSarch)
{
    'amd64' { $OSarch = "x64" }
    'x86_64'{ $OSarch = "x64" }
    'arm64' { $OSarch = "arm64" }
    'aarch64' { $OSarch = "arm64" }
    'x86'   { Write-Host "x86" }
}
$OS = ""
if($IsWindows)
{
    $OS = "windows"
}
if($IsLinux)
{
    $OS = "linux"
}
Invoke-WebRequest -Uri "https://github.com/doopyelephant/GoogleDocs/releases/download/v0.4.0-alpha/GoogleDocs-${OS}-${OSarch}.zip" -OutFile "$Release"
$writer.WriteLine("Progress: 60")
Expand-Archive -Path $Release -DestinationPath "$AppData\Release"
$writer.WriteLine("Progress: 70")
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/doopyelephant/GoogleDocs/refs/heads/master/GoogleDocs/DocsLogo.ico" -OutFile "$AppData\Release\GoogleDocs\icon.ico"
$writer.WriteLine("Progress: 80")
Set-Location "$AppData\Release\GoogleDocs"
$ExePath = "$AppData\Release\GoogleDocs\GoogleDocs.exe"
if($IsLinux)
{
$ExePath = "$AppData\Release\GoogleDocs\GoogleDocs"
}
$IcoPath = "$AppData\Release\GoogleDocs\icon.ico"
if($IsWindows)
{
    $WScript = New-Object -ComObject WScript.Shell
    $Shortcut = $WScript.CreateShortcut("$env:USERPROFILE\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\GoogleDocs.lnk")
    $Shortcut.TargetPath = $ExePath
    $Shortcut.WorkingDirectory = "$AppData\Release\GoogleDocs\"
    $Shortcut.IconLocation = "$IcoPath, 0"
    $Shortcut.Save()
    $writer.WriteLine("Progress: 90")
    # Define application details
    $AppName = "Google Docs"
    $RegistryKeyName = "GoogleDocs" # Unique key name for the registry
    $DisplayVersion = "0.1.0"
    $Publisher = "DoopyElephant"
    $InstallLocation = "$AppData\Release\GoogleDocs"
    $UninstallCommand = "powershell.exe -ExecutionPolicy Bypass -File $AppData\Release\GoogleDocs\Uninstall.ps1"
    $RegistryPath = "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\$RegistryKeyName"
    if (-not (Test-Path $RegistryPath))
    {
        New-Item -Path $RegistryPath -Force | Out-Null
    }
    New-ItemProperty -Path $RegistryPath -Name "DisplayName" -Value $AppName -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $RegistryPath -Name "DisplayVersion" -Value $DisplayVersion -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $RegistryPath -Name "Publisher" -Value $Publisher -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $RegistryPath -Name "InstallLocation" -Value $InstallLocation -PropertyType String -Force | Out-Null
    New-ItemProperty -Path $RegistryPath -Name "UninstallString" -Value $UninstallCommand -PropertyType String -Force | Out-Null
}
if($IsLinux)
{
    $DesktopEntry = "[Desktop Entry]`nType=Application`nTerminal=false`nName=Google Docs`nExec=$ExePath`nIcon=$IcoPath`nCategories=Office";
    Write-Output $DesktopEntry > "~/.local/share/applications/GoogleDocs.desktop"
}
    $writer.WriteLine("Progress: 95")
    Invoke-WebRequest -Uri "https://raw.githubusercontent.com/doopyelephant/GoogleDocs/refs/heads/master/GoogleDocs/Uninstall.ps1" -OutFile "$AppData\Release\GoogleDocs\Uninstall.ps1"
    $writer.WriteLine("Progress: 100")
