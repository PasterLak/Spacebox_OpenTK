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

function Get-GameVersion {
    param ([string]$projectRoot)
    $applicationFile = Join-Path (Split-Path $projectRoot -Parent) "Engine\Application.cs"
    if (-not (Test-Path $applicationFile)) {
        throw "Application.cs not found: $applicationFile"
    }

    $match = Select-String -Path $applicationFile -Pattern 'const\s+string\s+Version\s*=\s*"([^"]+)"' | Select-Object -First 1
    if ($match -eq $null) {
        throw "Version not found in $applicationFile"
    }
    return $match.Matches[0].Groups[1].Value
}

function Remove-ExcludedFiles {
    param ([string]$projectRoot, [string]$targetDir)
    $excludeFile = Join-Path $projectRoot "Build\exclude.txt"
    if (-not (Test-Path $excludeFile)) { return }

    Get-Content $excludeFile | ForEach-Object {
        $relativePath = $_.Trim()
        if ($relativePath -ne "") {
            $targetPath = Join-Path $targetDir $relativePath
            if (Test-Path $targetPath) {
                Remove-Item $targetPath -Recurse -Force -ErrorAction SilentlyContinue
                Write-Host "Excluded: $relativePath"
            } else {
                Write-Warning "Path not found (skip): $relativePath"
            }
        }
    }
}

function Get-UnixAttributes {
    param ([uint32]$mode)
    return [BitConverter]::ToInt32([BitConverter]::GetBytes([uint32](($mode -bor 0x8000) * 65536)), 0)
}

function Set-ZipUnixHost {
    param ([string]$zipPath)
    $bytes = [System.IO.File]::ReadAllBytes($zipPath)

    $end = -1
    for ($i = $bytes.Length - 22; $i -ge 0; $i--) {
        if ([BitConverter]::ToUInt32($bytes, $i) -eq 0x06054b50) { $end = $i; break }
    }
    if ($end -lt 0) { throw "Zip end record not found: $zipPath" }

    $count = [BitConverter]::ToUInt16($bytes, $end + 10)
    $position = [BitConverter]::ToUInt32($bytes, $end + 16)
    for ($n = 0; $n -lt $count; $n++) {
        if ([BitConverter]::ToUInt32($bytes, $position) -ne 0x02014b50) { throw "Broken central directory in $zipPath" }
        $bytes[$position + 5] = 3
        $nameLength = [BitConverter]::ToUInt16($bytes, $position + 28)
        $extraLength = [BitConverter]::ToUInt16($bytes, $position + 30)
        $commentLength = [BitConverter]::ToUInt16($bytes, $position + 32)
        $position += 46 + $nameLength + $extraLength + $commentLength
    }

    [System.IO.File]::WriteAllBytes($zipPath, $bytes)
}

function New-FlatZip {
    param ([string]$sourceDir, [string]$zipPath, [string[]]$extraFiles = @(), [string[]]$UnixExecutables = $null)
    Add-Type -AssemblyName System.IO.Compression
    Add-Type -AssemblyName System.IO.Compression.FileSystem

    if (Test-Path $zipPath) {
        Remove-Item $zipPath -Force
    }

    $unix = $null -ne $UnixExecutables
    $executableAttributes = Get-UnixAttributes 0x1ED
    $fileAttributes = Get-UnixAttributes 0x1A4

    $root = (Resolve-Path $sourceDir).Path.TrimEnd('\', '/')
    $zip = [System.IO.Compression.ZipFile]::Open($zipPath, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        $entries = @()
        Get-ChildItem $root -Recurse -File -Force | ForEach-Object {
            $entries += , @($_.FullName, $_.FullName.Substring($root.Length + 1).Replace('\', '/'))
        }
        foreach ($file in $extraFiles) {
            $entries += , @($file, (Split-Path $file -Leaf))
        }

        foreach ($item in $entries) {
            $entry = [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $item[0], $item[1], [System.IO.Compression.CompressionLevel]::Optimal)
            if ($unix) {
                $entry.ExternalAttributes = if ($UnixExecutables -contains $item[1]) { $executableAttributes } else { $fileAttributes }
            }
        }
    } finally {
        $zip.Dispose()
    }

    if ($unix) {
        Set-ZipUnixHost $zipPath
    }
}
