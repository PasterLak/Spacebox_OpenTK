
$filesCount = (Get-ChildItem -Path "P:\C#\Spacebox_OpenTK" -Recurse -Filter *.cs).Count


$linesCount = (Get-ChildItem -Path "P:\C#\Spacebox_OpenTK" -Recurse -Filter *.cs | 
               Get-Content | Measure-Object -Line).Lines

$shadersCount = (Get-ChildItem -Path "P:\C#\Spacebox_OpenTK\Spacebox\Shaders" -Recurse -Filter *.glsl).Count
$jsonCount = (Get-ChildItem -Path "P:\C#\Spacebox_OpenTK\Spacebox\GameSets" -Recurse -Filter *.json).Count



$bigFiles0 = Get-ChildItem -Path "P:\C#\Spacebox_OpenTK" -Recurse -Filter *.cs |
    Where-Object { (Get-Content $_.FullName | Measure-Object -Line).Lines -gt 500 }


$bigFilesCount0 = $bigFiles0.Count


$bigFileNames0 = $bigFiles0 | Select-Object -ExpandProperty BaseName





$bigFiles = Get-ChildItem -Path "P:\C#\Spacebox_OpenTK" -Recurse -Filter *.cs |
    Where-Object { (Get-Content $_.FullName | Measure-Object -Line).Lines -gt 1000 }


$bigFilesCount = $bigFiles.Count


$bigFileNames = $bigFiles | Select-Object -ExpandProperty BaseName





Write-Host "<--------------------------------------------->"
Write-Host "	PROJECT STATISTICS	"
Write-Host "<--------------------------------------------->"

Write-Host "  CS Files: $filesCount"
Write-Host "  CS lines: $linesCount"
Write-Host "  Json Files: $jsonCount"
Write-Host "  GLSL Files: $shadersCount"
Write-Host "<--------------------------------------------->"
Write-Host "  Files with more than 500 lines: $bigFilesCount0"
Write-Host "File names:"
$bigFileNames0 | ForEach-Object { Write-Host "- $_" }

Write-Host "  Files with more than 1000 lines: $bigFilesCount"
Write-Host "File names:"
$bigFileNames | ForEach-Object { Write-Host "- $_" }

Write-Host "<--------------------------------------------->"