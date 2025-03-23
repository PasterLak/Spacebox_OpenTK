# PowerShell script to build and package Spacebox project

# ==== CONFIGURATION ====
$version = "0.1.2 alpha"
$configuration = "Release"
$runtime = "win-x64"
$framework = "net8.0"
$finalFolderName = "Spacebox_v$version"
# ========================

# Find the project root by looking for Spacebox.csproj
function Find-ProjectRoot {
    param ([string]$startDir)
    $dir = Get-Item $startDir
    while ($dir -ne $null) {
        $csprojPath = Join-Path $dir.FullName "Spacebox.csproj"
        if (Test-Path $csprojPath) {
            return $dir.FullName
        }
        $dir = $dir.Parent
    }
    throw "Spacebox.csproj not found."
}

# Get paths
$scriptDir = $PSScriptRoot
$projectRoot = Find-ProjectRoot $scriptDir
$projectFile = Join-Path $projectRoot "Spacebox.csproj"
$publishRoot = Join-Path $projectRoot "bin\$configuration\$framework\$runtime"
$publishDir = Join-Path $publishRoot "publish"
$openAlDll = Join-Path $projectRoot "Build\AddAfterBuild\OpenAL32.dll"
$finalDir = Join-Path $publishRoot $finalFolderName
$zipPath = Join-Path $publishRoot "$finalFolderName.zip"

cls
Remove-Item -Path "$projectRoot\bin\$configuration" -Recurse -Force -ErrorAction SilentlyContinue

New-Item -Path "$publishDir" -ItemType Directory -Force | Out-Null


# Step 1: Publish project
Write-Host "Publishing project..."

$logTemp = Join-Path $env:TEMP "spacebox_build_log.txt"
dotnet publish $projectFile -c $configuration -r $runtime --no-self-contained *> $logTemp




if (-Not (Test-Path $publishDir)) {
    Write-Error "Publish folder not found: $publishDir"
    exit 1
}

# Step 2: Copy OpenAL32.dll
if (Test-Path $openAlDll) {
    Copy-Item $openAlDll -Destination $publishDir -Force
    Write-Host "Copied OpenAL32.dll to $publishDir"
} else {
    Write-Warning "OpenAL32.dll not found at $openAlDll"
}

# Step 3: Remove everything in publishRoot except publish folder
Write-Host "Cleaning up intermediate build files..."
Get-ChildItem $publishRoot -Exclude "publish" | Remove-Item -Recurse -Force

# Step 4: Rename publish folder
if (Test-Path $finalDir) {
    Remove-Item $finalDir -Recurse -Force
}
Rename-Item -Path $publishDir -NewName $finalFolderName

$logPath = Join-Path $publishRoot "log.txt"
Move-Item -Path $logTemp -Destination $logPath -Force

# Step 4.5: Exclude files listed in exclude.txt (if exists)
$excludeFile = Join-Path $projectRoot "Build\exclude.txt"
if (Test-Path $excludeFile) {
    Write-Host "Processing exclude.txt..."

    Get-Content $excludeFile | ForEach-Object {
        $relativePath = $_.Trim()
        if ($relativePath -ne "") {
            $targetPath = Join-Path $finalDir $relativePath
            if (Test-Path $targetPath) {
                Remove-Item $targetPath -Recurse -Force -ErrorAction SilentlyContinue
                Write-Host "Excluded: $relativePath"
            } else {
                Write-Warning "Path not found (skip): $relativePath"
            }
        }
    }
}


# Step 5: Create zip archive
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

$extraData = Join-Path $projectRoot "Build\AddAfterBuild\Install_if_not_working.zip"
Compress-Archive -Path $finalDir, $extraData -DestinationPath $zipPath
Write-Host "Created archive with folder and data.zip: $zipPath"


# Step 6: Open folder
Start-Process "explorer.exe" $publishRoot

cls
Write-Host "--------------------------------------"
Write-Host ">>> Build and packaging complete! $version"
Write-Host "--------------------------------------"