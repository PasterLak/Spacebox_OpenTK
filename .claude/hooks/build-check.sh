#!/usr/bin/env bash
# PostToolUse hook: after a .cs file is edited, build the affected project and
# report compile errors back to Claude (exit code 2 feeds stderr to the agent).

file_path=$(jq -r '.tool_input.file_path // empty')
[[ "$file_path" == *.cs ]] || exit 0

root="${CLAUDE_PROJECT_DIR:-$(pwd)}"
rel="${file_path#"$root"/}"

case "$rel" in
  Spacebox.Tests/*) project="Spacebox.Tests/Spacebox.Tests.csproj" ;;
  Spacebox.Benchmarks/*) project="Spacebox.Benchmarks/Spacebox.Benchmarks.csproj" ;;
  LegacyWorldConverter/*) project="LegacyWorldConverter/LegacyWorldConverter.csproj" ;;
  Engine/*|Spacebox/*|Spacebox.Core/*) project="Spacebox/Spacebox.csproj" ;;
  *) exit 0 ;;
esac

dotnet_bin=$(command -v dotnet || echo "$HOME/.dotnet/dotnet")
[[ -x "$dotnet_bin" ]] || exit 0

errors=$("$dotnet_bin" build "$root/$project" -v q -nologo -clp:ErrorsOnly 2>&1 | grep -E ': error ' | sort -u)

if [[ -n "$errors" ]]; then
  echo "Build of $project failed after editing $rel:" >&2
  echo "$errors" | sed "s|$root/||g" | head -30 >&2
  exit 2
fi
exit 0
