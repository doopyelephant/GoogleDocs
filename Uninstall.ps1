Remove-Item -Recurse -Force "$env:USERPROFILE\AppData\Local\GoogleDocs"
Remove-Item -Force "$env:USERPROFILE\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\GoogleDocs.lnk"
$RegistryKeyName = "GoogleDocs"
$RegistryPath = "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\$RegistryKeyName"
if (Test-Path $RegistryPath) {
    Remove-Item -Path $RegistryPath -Recurper -Force
    Write-Host "Successfully removed the program entry from Add/Remove Programs." -ForegroundColor Green
} else {
    Write-Warning "The program registry key was not found."
}
