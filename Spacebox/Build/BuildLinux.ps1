. (Join-Path $PSScriptRoot "BuildCommon.ps1")

$channel = "alpha"
$configuration = "Final"
$runtime = "linux-x64"
$framework = "net8.0"

$projectRoot = Find-ProjectRoot $PSScriptRoot
$version = "$(Get-GameVersion $projectRoot) $channel"
$finalFolderName = "Spacebox_v$($version)_linux"
$projectFile = Join-Path $projectRoot "Spacebox.csproj"
$publishRoot = Join-Path $projectRoot "bin\$configuration\$framework\$runtime"
$publishDir = Join-Path $publishRoot "publish"
$outputRoot = Join-Path (Split-Path $projectRoot -Parent) "Final"
$finalDir = Join-Path $outputRoot "Linux"
$zipPath = Join-Path $outputRoot "$finalFolderName.zip"
$logPath = Join-Path $outputRoot "log-linux.txt"

Remove-Item -Path $publishRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -Path $publishRoot -ItemType Directory -Force | Out-Null
New-Item -Path $outputRoot -ItemType Directory -Force | Out-Null

Write-Host "Publishing project for $runtime..."
dotnet publish $projectFile -c $configuration -r $runtime --self-contained *> $logPath
if ($LASTEXITCODE -ne 0 -or -not (Test-Path (Join-Path $publishDir "Spacebox"))) {
    Write-Error "Publish failed, see $logPath"
    exit 1
}

if (Test-Path $finalDir) { Remove-Item $finalDir -Recurse -Force }
Remove-Item -Path $zipPath -Force -ErrorAction SilentlyContinue
Move-Item -Path $publishDir -Destination $finalDir
Remove-Item -Path $publishRoot -Recurse -Force -ErrorAction SilentlyContinue

Remove-ExcludedFiles $projectRoot $finalDir
Remove-UnusedAssets $projectRoot $finalDir

New-FlatZip $finalDir $zipPath -UnixExecutables @("Spacebox", "start.sh", "start-server.sh")

if (-not $env:SPACEBOX_NO_EXPLORER) { Start-Process "explorer.exe" $outputRoot }

Write-Host "--------------------------------------"
Write-Host ">>> Linux build complete! $version"
Write-Host ">>> $zipPath"
Write-Host ">>> On Linux: unzip, then run './start.sh' (game) or './start-server.sh' (server)"
Write-Host "--------------------------------------"
