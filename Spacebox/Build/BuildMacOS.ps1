. (Join-Path $PSScriptRoot "BuildCommon.ps1")

$channel = "alpha"
$configuration = "Final"
$framework = "net8.0"
$targets = @(
    @{ Runtime = "osx-x64"; Folder = "MacOS-Intel"; Suffix = "macos_intel" },
    @{ Runtime = "osx-arm64"; Folder = "MacOS-AppleSilicon"; Suffix = "macos_apple_silicon" }
)

$projectRoot = Find-ProjectRoot $PSScriptRoot
$version = "$(Get-GameVersion $projectRoot) $channel"
$projectFile = Join-Path $projectRoot "Spacebox.csproj"
$outputRoot = Join-Path (Split-Path $projectRoot -Parent) "Final"
New-Item -Path $outputRoot -ItemType Directory -Force | Out-Null

foreach ($target in $targets) {
    $runtime = $target.Runtime
    $publishRoot = Join-Path $projectRoot "bin\$configuration\$framework\$runtime"
    $publishDir = Join-Path $publishRoot "publish"
    $finalDir = Join-Path $outputRoot $target.Folder
    $zipPath = Join-Path $outputRoot "Spacebox_v$($version)_$($target.Suffix).zip"
    $logPath = Join-Path $outputRoot "log-$($target.Suffix).txt"

    Remove-Item -Path $publishRoot -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -Path $publishRoot -ItemType Directory -Force | Out-Null

    Write-Host "Publishing project for $runtime..."
    dotnet publish $projectFile -c $configuration -r $runtime --self-contained *> $logPath
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path (Join-Path $publishDir "Spacebox"))) {
        Write-Error "Publish failed for $runtime, see $logPath"
        exit 1
    }

    if (Test-Path $finalDir) { Remove-Item $finalDir -Recurse -Force }
    Remove-Item -Path $zipPath -Force -ErrorAction SilentlyContinue
    Move-Item -Path $publishDir -Destination $finalDir
    Remove-Item -Path $publishRoot -Recurse -Force -ErrorAction SilentlyContinue

    Remove-ExcludedFiles $projectRoot $finalDir
    Remove-UnusedAssets $projectRoot $finalDir

    New-FlatZip $finalDir $zipPath -UnixExecutables @("Spacebox", "start.sh", "start-server.sh")
    Write-Host ">>> $runtime build complete: $zipPath"
}

if (-not $env:SPACEBOX_NO_EXPLORER) { Start-Process "explorer.exe" $outputRoot }

Write-Host "--------------------------------------"
Write-Host ">>> macOS builds complete! $version (untested: built on Windows)"
Write-Host ">>> On a Mac: unzip, run 'xattr -dr com.apple.quarantine .' in the folder once, then './start.sh'"
Write-Host "--------------------------------------"
