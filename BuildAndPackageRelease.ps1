$Projects = @(
    "GoogleDocs"
    "GoogleDocsInstaller"
)
foreach($Project in $Projects)
{
    $Platforms = @(
        "win-x64"
        "win-arm64"
        "win-x86"
        "linux-x64"
        "linux-arm64"
    )
    foreach ($Platform in $Platforms)
    {
        $ChildParams = @(
            "y"
            $Platform
        )
        $ChildParams | powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$PSScriptRoot\Build.ps1" -Project $Project
    }
}
foreach($Project in $Projects)
{
    Set-Location "./$Project/${Project}Builds"
    $zips = @(Get-ChildItem -Filter *.zip -File -ErrorAction SilentlyContinue)
    Set-Location "../../"
    mkdir "./Release"
    foreach($zip in $zips)
    {
    Copy-Item "./$Project/${Project}Builds/$zip" "./Release/"
    }
}