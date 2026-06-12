param (
    [string]$Drive = "C:\",
    [string]$Mode = "External"
)
$AppData = "$env:USERPROFILE\AppData\Local\GoogleDocs"
$Release = "$AppData\Release.zip"
Invoke-WebRequest -Uri "https://github.com/doopyelephant/GoogleDocs/releases/download/v0.1.0-alpha/GoogleDocs-windows-x64.zip" -OutFile "$Release"
Invoke-WebRequest -Uri "https://github.com/doopyelephant/GoogleDocs/releases/download/v0.1.0-alpha/GoogleDocs-windows-x64.zip" -OutFile "$Release"
Write-Host "60"
Expand-Archive -Path $Release -DestinationPath "$AppData\Release"
Write-Host "70"
Set-Location "$AppData\Release\GoogleDocs"
$ExePath = ".\GoogleDocs.exe"
$IcoPath = ".\icon.ico"
$Signature = @"
[DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Unicode)]
public static extern IntPtr BeginUpdateResource(string pFileName, bool bDeleteExistingResources);

[DllImport("kernel32.dll", SetLastError=true)]
public static extern bool UpdateResource(IntPtr hUpdate, string lpType, string lpName, ushort wLanguage, byte[] lpData, uint cbData);

[DllImport("kernel32.dll", SetLastError=true)]
public static extern bool EndUpdateResource(IntPtr hUpdate, bool fDiscard);
"@
$Kernel32 = Add-Type -MemberDefinition $Signature -Name Kernel32 -Namespace Win32 -PassThru
$IcoBytes = [System.IO.File]::ReadAllBytes($IcoPath)
$hUpdate = $Kernel32::BeginUpdateResource($ExePath, $false)
$Result = $Kernel32::UpdateResource($hUpdate, "#3", "101", 1033, $IcoBytes, $IcoBytes.Length)
$Kernel32::EndUpdateResource($hUpdate, $false)
Write-Host "80"