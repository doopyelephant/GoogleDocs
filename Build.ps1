param (
    [string]$Project = "GoogleDocs"
)

function Test-IsDotNetDll {
    param (
        [Parameter(Mandatory=$true)]
        [string]$Path
    )

    # Resolve to an absolute path to avoid scope bugs
    $AbsolutePath = Resolve-Path $Path -ErrorAction SilentlyContinue

    if (-not $AbsolutePath) {
        Write-Warning "File not found: $Path"
        return
    }

    try {
    # Attempt to read .NET assembly metadata
    $AssemblyName = [System.Reflection.AssemblyName]::GetAssemblyName($AbsolutePath)

    [PSCustomObject]@{
    Path     = $AbsolutePath
    Type     = ".NET Assembly (Managed)"
    Name     = $AssemblyName.Name
    Version  = $AssemblyName.Version
    }
    }
    catch [System.BadImageFormatException] {
        # Thrown specifically if the DLL is native/unmanaged
        [PSCustomObject]@{
            Path    = $AbsolutePath
            Type    = "Native DLL (Unmanaged)"
            Name    = "N/A"
            Version = "N/A"
        }
    }
    catch {
        # Catch other errors like access permissions
        Write-Error "Could not analyze file: $_"
    }
}

Set-Location $Project
if (Get-Command "dotnet" -ErrorAction SilentlyContinue) {
    Write-Host ".NET is installed."
    $CC = Read-Host -Prompt "Cross Compile? [y/n] (If running on current machine say n)"
    $arch = ""
    if($CC -eq "y")
    {
    $arch = Read-Host -Prompt "Arch? (win-x64, win-x86, win-arm64, linux-x64, linux-arm64, osx-x64, osx-arm64) (No OSX Support Yet(I don't have any hardware to test on) )"
    $arch = "-r $arch "
    }
    elseif($CC -eq "n")
    {

    }
    else{
        Write-Error "Invalid. Exiting..."
    }
    Remove-Item -Path "./Build" -Recurse -Force
    mkdir ./Build
    mkdir ./BuildDeps
    dotnet publish --self-contained -c Release -o "./BuildDeps" $arch
    Set-Location BuildDeps
    $dlls = Get-ChildItem -Filter *.dll
    $dotnetdlls = @()
    $notdotnetdlls = @()
    foreach ($dll in ($dlls -split '\s+')) {
        $isdotnet = Test-IsDotNetDll $dll
        Write-Host $isdotnet
        Write-Host $isdotnet.Type
        if($isdotnet.Type -match ".NET" -and !($dll -match $Project) `
    -and !($dll -match "^System\.") `
    -and !($dll -match "^Microsoft\.") `
    -and !($dll -match "^mscorlib") `
    -and !($dll -match "^netstandard") `
    -and !($dll -match "^WindowsBase") `
    -and !($dll -match "^PresentationFramework") `
    -and !($dll -match "^clr"))
        {

        Write-Host ".NET Assembly found"
        $dotnetdlls += "./$dll"
        }
        elseif(!($dll -match $Project)){
            Write-Host "Assembly is not .NET"
            $notdotnetdlls += "./$dll"
        }
    }
    Write-Host "Executing IlRepack /zeropekind /internalize out:../Build/$Project.dll ./$Project.dll $dotnetdlls"
    IlRepack /zeropekind /internalize /out:../Build/$Project.dll ./$Project.dll $dotnetdlls
    foreach($dll in $notdotnetdlls)
    {
        Copy-Item $dll "../Build/"
    }
    Set-Location ..
    $jsons = @(Get-ChildItem -Filter *.json -File -ErrorAction SilentlyContinue)
    $scripts = @(Get-ChildItem -Filter *.ps1 -File -ErrorAction SilentlyContinue)
    $pys = @(Get-ChildItem -Filter *.py -File -ErrorAction SilentlyContinue)
    $files = $jsons + $scripts + $pys
    foreach ($file in $files)
    {
    Copy-Item $file "./Build"
    }
    Copy-Item "./BuildDeps/$Project.exe" "./Build/"
    Remove-Item "./BuildDeps" -R
} else {
    Write-Error ".NET is NOT installed. Exiting..."
}
Set-Location ".."
