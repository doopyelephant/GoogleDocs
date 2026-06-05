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

if (Get-Command "dotnet" -ErrorAction SilentlyContinue) {
    Write-Host ".NET is installed."
    $CC = Read-Host -Prompt "Cross Compile? [y/n] (If running on current machine say n)"
    $arch = ""
    if($CC -eq "y")
    {
    $arch = Read-Host -Prompt "Arch? (win-x64, win-x86, win-arm64, linux-x64, linux-arm64, osx-x64, osx-arm64) (No OSX Support)"
    $arch = "-r $arch "
    }
    elseif($CC -eq "n")
    {

    }
    else{
        Write-Error "Invalid. Exiting..."
    }
    Remove-Item -Path "./BuildDeps" -Recurse -Force
    Remove-Item -Path "./Build" -Recurse -Force
    mkdir ./Build
    mkdir ./BuildDeps
    dotnet publish --self-contained -c Release -o "./BuildDeps" $arch
    cd BuildDeps
    $dlls = Get-ChildItem -Filter *.dll
    foreach ($dll in ($dlls -split '\s+')) {
        $isdotnet = Test-IsDotNetDll $dll.FullName
        Write-Host $isdotnet
    }
    IlRepack /out:../Build/GoogleDocs.exe ./GoogleDocs.exe $dlls
    cd ..

} else {
    Write-Error ".NET is NOT installed. Exiting..."
}
