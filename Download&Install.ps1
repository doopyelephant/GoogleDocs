$ProgressPreference = 'SilentlyContinue'
$AppData = "$env:USERPROFILE\AppData\Local\GoogleDocs"
$Release = "$AppData\InstallerRelease.zip"
Invoke-WebRequest -Uri "https://github.com/doopyelephant/GoogleDocs/releases/download/v0.2.0-alpha/GoogleDocsInstaller-windows-x64.zip" -OutFile "$Release"
mkdir "$AppData\InstallerRelease"
Expand-Archive -Path $Release -DestinationPath "$AppData\InstallerRelease"
Remove-Item -Force $Release
& "$AppData\InstallerRelease\Installer\GoogleDocsInstaller.exe"