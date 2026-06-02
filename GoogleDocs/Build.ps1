if (Get-Command "dotnet" -ErrorAction SilentlyContinue) {
    Write-Host ".NET is installed."
    $CC = Read-Host -Prompt "Cross Compile? [y/n] (If running on current machine say n)"
    if($CC -eq "y")
    {
    $arch = Read-Host -Prompt "Arch? (win-x64, win-x86, win-arm64, linux-x64, linux-arm64, osx-x64, osx-arm64) (No OSX Support)"

    }
    elseif($CC -eq "n")
    {

    }
    else{
        Write-Error "Invalid. Exiting..."
    }

} else {
    Write-Error ".NET is NOT installed. Exiting..."
}
