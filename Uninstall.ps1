$ProgressPreference = 'SilentlyContinue'
$CustomIsWindows = [System.Environment]::OSVersion.Platform -eq 'Win32NT'
$CustomIsLinux = [Environment]::OSVersion.Platform -eq 'Unix'
if($CustomIsWindows)
{
    if (-NOT ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]"Administrator"))
    {
        $arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`""
        Start-Process powershell.exe -ArgumentList $arguments -Verb RunAs
        Exit
    }
    Remove-Item -Recurse -Force "$env:USERPROFILE/AppData/Local/GoogleDocs"  -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force "D:/AppData/Local/GoogleDocs/"  -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force "E:/AppData/Local/GoogleDocs/"  -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force "F:/AppData/Local/GoogleDocs/"  -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force "G:/AppData/Local/GoogleDocs/"  -ErrorAction SilentlyContinue
    Remove-Item -Force "$env:USERPROFILE/AppData/Roaming/Microsoft/Windows/Start Menu/Programs/GoogleDocs.lnk"
    $RegistryKeyName = "GoogleDocs"
    $RegistryPath = "HKLM:/Software/Microsoft/Windows/CurrentVersion/Uninstall/$RegistryKeyName"
    if (Test-Path $RegistryPath)
    {
        Remove-Item -Path $RegistryPath -Recurse -Force
        Write-Host "Successfully removed the program entry from Add/Remove Programs." -ForegroundColor Green
    }
    else
    {
        Write-Warning "The program registry key was not found."
    }
}
if($CustomIsLinux)
{
    Remove-Item -Recurse -Force "/opt/GoogleDocs"  -ErrorAction SilentlyContinue
    Remove-Item -Force "~/.local/share/applications/GoogleDocs.desktop"  -ErrorAction SilentlyContinue
}
