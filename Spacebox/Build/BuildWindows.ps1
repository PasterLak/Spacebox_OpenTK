# PowerShell script to build and package Spacebox project

. (Join-Path $PSScriptRoot "BuildCommon.ps1")

# ==== CONFIGURATION ====
$channel = "alpha"
$configuration = "Final"
$runtime = "win-x64"
$framework = "net8.0"
$selfContained = $true
# ========================

# Get paths
$scriptDir = $PSScriptRoot
$projectRoot = Find-ProjectRoot $scriptDir
$version = "$(Get-GameVersion $projectRoot) $channel"
$finalFolderName = "Spacebox_v$version"
$projectFile = Join-Path $projectRoot "Spacebox.csproj"
$publishRoot = Join-Path $projectRoot "bin\$configuration\$framework\$runtime"
$publishDir = Join-Path $publishRoot "publish"
$outputRoot = Join-Path (Split-Path $projectRoot -Parent) "Final"
$finalDir = Join-Path $outputRoot "Windows"
$zipPath = Join-Path $outputRoot "$finalFolderName.zip"

cls
Remove-Item -Path $publishRoot -Recurse -Force -ErrorAction SilentlyContinue

New-Item -Path "$publishDir" -ItemType Directory -Force | Out-Null


# Step 1: Publish project
Write-Host "Publishing project..."
# -p:OutputType=WinExe
$logTemp = Join-Path $env:TEMP "spacebox_build_log.txt"
$selfContainedArg = if ($selfContained) { "--self-contained" } else { "--no-self-contained" }
dotnet publish $projectFile -c $configuration -r $runtime $selfContainedArg *> $logTemp
if ($LASTEXITCODE -ne 0) {
    Write-Error "Publish failed, see $logTemp"
    exit 1
}




if (-Not (Test-Path $publishDir)) {
    Write-Error "Publish folder not found: $publishDir"
    exit 1
}

# Step 3: Move the published files into Final\Windows
Write-Host "Moving files to $finalDir..."
New-Item -Path $outputRoot -ItemType Directory -Force | Out-Null
if (Test-Path $finalDir) {
    Remove-Item $finalDir -Recurse -Force
}
Remove-Item -Path $zipPath -Force -ErrorAction SilentlyContinue
Move-Item -Path $publishDir -Destination $finalDir
Remove-Item -Path $publishRoot -Recurse -Force -ErrorAction SilentlyContinue

$logPath = Join-Path $outputRoot "log-windows.txt"
Move-Item -Path $logTemp -Destination $logPath -Force

# Step 4.5: Exclude files listed in exclude.txt (if exists)
Remove-ExcludedFiles $projectRoot $finalDir
Remove-UnusedAssets $projectRoot $finalDir


# Step 5: Create zip archive
if ($selfContained) {
    New-FlatZip $finalDir $zipPath
    Write-Host "Created archive: $zipPath"
} else {
    $extraData = Join-Path $projectRoot "Build\AddAfterBuild\Install_if_not_working.zip"
    New-FlatZip $finalDir $zipPath @($extraData)
    Write-Host "Created archive with folder and data.zip: $zipPath"
}


# Step 6: Open folder
if (-not $env:SPACEBOX_NO_EXPLORER) { Start-Process "explorer.exe" $outputRoot }

cls
Write-Host "--------------------------------------"
Write-Host ">>> Build and packaging complete! $version"
Write-Host "--------------------------------------"