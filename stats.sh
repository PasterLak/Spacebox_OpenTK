#!/bin/bash

echo "<--------------------------------------------->"
echo "          PROJECT STATISTICS                   "
echo "<--------------------------------------------->"


cs_files=$(find "P:/C#/Spacebox_OpenTK" -type f -name "*.cs" | wc -l)
echo "  CS Files: $cs_files"


cs_lines=$(find "P:/C#/Spacebox_OpenTK" -type f -name "*.cs" -exec cat {} + | wc -l)
echo "  CS lines: $cs_lines"


json_files=$(find "P:/C#/Spacebox_OpenTK/Spacebox/GameSets" -type f -name "*.json" | wc -l)
echo "  Json Files: $json_files"


glsl_files=$(find "P:/C#/Spacebox_OpenTK/Spacebox/Shaders" -type f -name "*.glsl" | wc -l)
echo "  GLSL Files: $glsl_files"

echo "<--------------------------------------------->"


big_files_500=$(find "P:/C#/Spacebox_OpenTK" -type f -name "*.cs" -exec sh -c 'lines=$(wc -l < "{}"); [ "$lines" -gt 500 ] && basename "{}" .cs' \;)
big_files_500_count=$(echo "$big_files_500" | grep -c .)
echo "  Files with more than 500 lines: $big_files_500_count"
echo "File names:"
echo "$big_files_500" | sed 's/^/- /'


big_files_1000=$(find "P:/C#/Spacebox_OpenTK" -type f -name "*.cs" -exec sh -c 'lines=$(wc -l < "{}"); [ "$lines" -gt 1000 ] && basename "{}" .cs' \;)
big_files_1000_count=$(echo "$big_files_1000" | grep -c .)
echo "  Files with more than 1000 lines: $big_files_1000_count"
echo "File names:"
echo "$big_files_1000" | sed 's/^/- /'

echo "<--------------------------------------------->"
