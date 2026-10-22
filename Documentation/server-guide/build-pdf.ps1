$source = Join-Path $PSScriptRoot "server-guide.html"
$target = Join-Path $PSScriptRoot "..\..\Spacebox\Multiplayer\Spacebox Server Guide.pdf"
$edge = @(
    "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe",
    "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe",
    "$env:ProgramFiles\Google\Chrome\Application\chrome.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1

if (-not $edge) {
    Write-Error "Microsoft Edge or Google Chrome is needed to print the PDF"
    exit 1
}

$target = [System.IO.Path]::GetFullPath($target)
$url = ([System.Uri](Resolve-Path $source).Path).AbsoluteUri
& $edge --headless=new --disable-gpu --no-pdf-header-footer --run-all-compositor-stages-before-draw --virtual-time-budget=5000 "--print-to-pdf=$target" $url | Out-Null
Start-Sleep -Seconds 2

if (Test-Path $target) {
    Write-Host "Created $target"
} else {
    Write-Error "The PDF was not created"
    exit 1
}
