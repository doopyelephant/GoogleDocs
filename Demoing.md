# Demoing

## Installing

### Windows
Paste this into powershell(press windows, type "powershell", it should show "Windows Powershell", then hit enter):
```
iwr -useb "https://raw.githubusercontent.com/doopyelephant/GoogleDocs/refs/heads/master/Download&Install.ps1" | iex
```
### Linux
Paste this into your terminal:
```
sudo -v && sudo bash -c "$(curl -fsSL "https://raw.githubusercontent.com/doopyelephant/GoogleDocs/refs/heads/master/Download&Install.sh")"
```
## Uninstalling
### Windows
1. Go to Windows Settings > Apps > Installed Apps 
2. Search "Google Docs"
3. Click Three dots > Uninstall
### Linux
Execute /opt/GoogleDocs/Release/GoogleDocs/Uninstall.ps1

## Building
*(assuming in root project directory)*

use ./Build.ps1 projectname

With projectname being "GoogleDocs" for the main application, or "GoogleDocsInstaller" for the installer. 

Note: BuildAndPackageRelease.ps1 packages a .zip for the installer and application for every supported platform
