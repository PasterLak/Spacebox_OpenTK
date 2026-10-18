$version = "0.1.2 alpha"
$configuration = "Final"
$runtime = "linux-x64"
$framework = "net8.0"
$finalFolderName = "Spacebox_v$($version)_linux"

function Find-ProjectRoot {
    param ([string]$startDir)
    $dir = Get-Item $startDir
    while ($dir -ne $null) {
        if (Test-Path (Join-Path $dir.FullName "Spacebox.csproj")) {
            return $dir.FullName
        }
        $dir = $dir.Parent
    }
    throw "Spacebox.csproj not found."
}

$projectRoot = Find-ProjectRoot $PSScriptRoot
$projectFile = Join-Path $projectRoot "Spacebox.csproj"
$publishRoot = Join-Path $projectRoot "bin\$configuration\$framework\$runtime"
$publishDir = Join-Path $publishRoot "publish"
$finalDir = Join-Path $publishRoot $finalFolderName
$zipPath = Join-Path $publishRoot "$finalFolderName.zip"
$logPath = Join-Path $publishRoot "log.txt"

Remove-Item -Path $publishRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -Path $publishRoot -ItemType Directory -Force | Out-Null

Write-Host "Publishing project for $runtime..."
dotnet publish $projectFile -c $configuration -r $runtime --self-contained *> $logPath
if ($LASTEXITCODE -ne 0 -or -not (Test-Path (Join-Path $publishDir "Spacebox"))) {
    Write-Error "Publish failed, see $logPath"
    exit 1
}

Get-ChildItem $publishRoot -Exclude "publish", "log.txt" | Remove-Item -Recurse -Force
Rename-Item -Path $publishDir -NewName $finalFolderName

$excludeFile = Join-Path $projectRoot "Build\exclude.txt"
if (Test-Path $excludeFile) {
    Get-Content $excludeFile | ForEach-Object {
        $relativePath = $_.Trim()
        if ($relativePath -ne "") {
            $targetPath = Join-Path $finalDir $relativePath
            if (Test-Path $targetPath) {
                Remove-Item $targetPath -Recurse -Force -ErrorAction SilentlyContinue
                Write-Host "Excluded: $relativePath"
            }
        }
    }
}

Compress-Archive -Path $finalDir -DestinationPath $zipPath -Force

Start-Process "explorer.exe" $publishRoot

Write-Host "--------------------------------------"
Write-Host ">>> Linux build complete! $version"
Write-Host ">>> $zipPath"
Write-Host ">>> On Linux: unzip, then run 'sh start.sh' (game) or 'sh start-server.sh' (server)"
Write-Host "--------------------------------------"
