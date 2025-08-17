$packageName = "RLMatrix"
$packageVersion = "0.5.2"
$netTarget = "netstandard2.0"
$tempDir = ".\Temp"
$dllDir = ".\Assets\Plugins"
$nugetPath = "C:\nuget.exe"
if (!(Test-Path $nugetPath)) {
    Write-Error "NuGet.exe not found at $nugetPath. Please ensure it's installed there or update the path."
    exit 1
}
if (!(Test-Path $tempDir)) {
    New-Item -ItemType "directory" -Path $tempDir
}
& $nugetPath install $packageName -Version $packageVersion -OutputDirectory $tempDir
if (!(Test-Path $dllDir)) {
    New-Item -ItemType "directory" -Path $dllDir
}
Get-ChildItem -Path $tempDir -Directory | ForEach-Object {
    $packagePath = Join-Path $_.FullName "lib\$netTarget"
    if (Test-Path $packagePath) {
        Get-ChildItem -Path $packagePath -Filter "*.dll" | ForEach-Object {
            $destinationPath = Join-Path $dllDir $_.Name
            if (!(Test-Path $destinationPath)) {
                Copy-Item -Path $_.FullName -Destination $destinationPath
            }
        }
    }
}
Remove-Item $tempDir -Recurse -Force