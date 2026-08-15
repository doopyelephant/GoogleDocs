$ProgressPreference = 'SilentlyContinue'
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
$Release = "$AppData\InstallerRelease.zip"
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
    Write-Host "Using Windows Version"
}
else{
    Write-Host "Not using windows version"
}
if($IsLinux)
{
    $OS = "linux"
    Write-Host "Using linux version"
}
else{
    Write-Host "Not using linux version"
}
$libc = ldd /bin/ls
if($libc -match "musl")
{
    $OSarch = "$OSarch-musl"
    Write-Host "Using musl libc"
}
else{
    Write-Host "Skipping musl"
}
$installzip = "https://github.com/doopyelephant/GoogleDocs/releases/download/v0.5.0-alpha/GoogleDocsInstaller-${OS}-${OSarch}.zip"
Write-Host "Downloading installer from ${installzip}"
Invoke-WebRequest -Uri $installzip -OutFile "$Release"
mkdir "$AppData\InstallerRelease"
Expand-Archive -Path $Release -DestinationPath "$AppData\InstallerRelease"
Remove-Item -Force $Release
if($IsWindows)
{
    & "$AppData\InstallerRelease\Installer\GoogleDocsInstaller.exe"
}
if($IsLinux)
{
    $installerpath = "$AppData\InstallerRelease\Installer\GoogleDocsInstaller"
    chmod +x $installerpath
    & $installerpath
}