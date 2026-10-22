#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
PORT="${SPACEBOX_AGENT_PORT:-47800}"
BASE="http://127.0.0.1:$PORT"
CONFIG="${SPACEBOX_AGENT_CONFIG:-Release}"
EXE_DIR="$ROOT/Spacebox/bin/$CONFIG/net8.0"

get() {
  local path="$1"; shift
  local args=()
  for kv in "$@"; do args+=(--data-urlencode "$kv"); done
  curl -sS -G "${args[@]}" "$BASE$path"
  echo
}

post() {
  local path="$1"; shift
  local args=()
  for kv in "$@"; do args+=(--data-urlencode "$kv"); done
  curl -sS -G -X POST "${args[@]}" "$BASE$path"
  echo
}

usage() {
  cat <<'EOF'
Usage: Tools/agent/game.sh <command> [args]

  start [world] [--no-build] [--fresh] [--mode survival] [--seed N]   build Release, launch, wait until the world is loaded
  menu [--no-build]          launch to the main menu with the API on
  stop                       quit the game
  help                       list all API routes
  state                      scene, player, crosshair target, open windows
  ui [window] | uiflat       ImGui text on screen | just the strings
  canvas [1]                 CanvasUI layers + layout check (1 = list controls)
  winsize <w> <h>            resize the game window
  click <text> [window] [index] | hover <text> | clickxy <x> <y>
  key <Key> [tap|hold|release] [frames]      e.g. key E, key W hold 60
  action <name> [tap|hold|release] [frames]  e.g. action inventory, action forward hold 120
  mouse [left|right] [click|hold|release] [frames]
  type <text>                type into the focused ImGui field
  release                    release all injected input
  cmd <console command>      e.g. cmd "give default:drill 1", cmd "tp 0 0 0"
  look | lookat <x> <y> <z> | turn <yaw> <pitch> | turnby <dyaw> <dpitch>
  block <x> <y> <z> | blocks [radius] [idFilter] | entities [radius]
  goto <x> <y> <z> [speed] | nav | navstop
  inv | select <slot>
  shot [path]                save a PNG screenshot, prints its path
  log [since]                console messages
  wait <ms> | waitfor <world|idle|nav> [timeoutSec]
  get <path> [k=v...] | post <path> [k=v...]
EOF
}

is_up() { curl -s -o /dev/null --max-time 1 "$BASE/" ; }

launch() {
  local build=1 extra=()
  for a in "$@"; do
    if [[ "$a" == "--no-build" ]]; then build=0; else extra+=("$a"); fi
  done
  if is_up; then echo "Game API already running on $BASE"; return 0; fi
  if [[ $build == 1 ]]; then
    dotnet build "$ROOT/Spacebox/Spacebox.csproj" -c "$CONFIG" -v q -nologo | tail -n 3
  fi
  if [[ -f "$EXE_DIR/Spacebox.exe" ]] && command -v powershell.exe > /dev/null; then
    local quoted="'--agent','$PORT'"
    for a in "${extra[@]}"; do quoted+=",'${a//\'/\'\'}'"; done
    powershell.exe -NoProfile -Command "Start-Process -FilePath '$(cygpath -w "$EXE_DIR/Spacebox.exe")' -WorkingDirectory '$(cygpath -w "$EXE_DIR")' -ArgumentList $quoted" < /dev/null
  else
    (cd "$EXE_DIR" && nohup ./Spacebox --agent "$PORT" "${extra[@]}" < /dev/null > /dev/null 2>&1 &)
  fi
  for _ in $(seq 1 120); do
    if is_up; then return 0; fi
    sleep 0.5
  done
  echo "Game did not open the API on $BASE (see Tools/agent/game.log)" >&2
  return 1
}

cmd="${1:-help}"; shift || true
case "$cmd" in
  start)
    world="AgentWorld"
    if [[ $# -gt 0 && "$1" != --* ]]; then world="$1"; shift; fi
    args=(--agent-world "$world")
    while [[ $# -gt 0 ]]; do
      case "$1" in
        --fresh) args+=(--agent-fresh); shift ;;
        --mode) args+=(--agent-mode "$2"); shift 2 ;;
        --seed) args+=(--agent-seed "$2"); shift 2 ;;
        *) args+=("$1"); shift ;;
      esac
    done
    launch "${args[@]}"
    get /wait until=world timeout=180 ;;
  menu) launch "$@" ;;
  stop)
    post /quit
    for _ in $(seq 1 40); do is_up || exit 0; sleep 0.5; done
    echo "Game is still running" >&2; exit 1 ;;
  help) get / ;;
  state) get /state ;;
  ui) if [[ $# -gt 0 ]]; then get /ui "window=$1"; else get /ui; fi ;;
  uiflat) get /ui flat=1 ;;
  canvas) get /canvas ${1:+"tree=$1"} ;;
  winsize) post /window/size "w=$1" "h=$2" ;;
  click) post /ui/click "text=$1" ${2:+"window=$2"} ${3:+"index=$3"} ;;
  hover) post /ui/hover "text=$1" ${2:+"window=$2"} ;;
  clickxy) post /ui/click "x=$1" "y=$2" ;;
  key) post /input/key "key=$1" "mode=${2:-tap}" ${3:+"frames=$3"} ;;
  action) post /input/action "name=$1" "mode=${2:-tap}" ${3:+"frames=$3"} ;;
  mouse) post /input/mouse "button=${1:-left}" "mode=${2:-click}" ${3:+"frames=$3"} ;;
  type) post /input/text "text=$*" ;;
  release) post /input/release ;;
  cmd) post /command "cmd=$*" ;;
  look) get /look ;;
  lookat) post /look "x=$1" "y=$2" "z=$3" ;;
  turn) post /look "yaw=$1" "pitch=$2" ;;
  turnby) post /look "dyaw=$1" "dpitch=${2:-0}" ;;
  block) get /block "x=$1" "y=$2" "z=$3" ;;
  blocks) get /blocks "radius=${1:-8}" ${2:+"id=$2"} ;;
  entities) get /entities "radius=${1:-2000}" ;;
  goto) post /goto "x=$1" "y=$2" "z=$3" "speed=${4:-8}" ;;
  nav) get /goto ;;
  navstop) post /goto/stop ;;
  inv) get /inventory ;;
  select) post /inventory/select "slot=$1" ;;
  shot) post /screenshot ${1:+"path=$1"} ;;
  log) get /log "since=${1:-0}" ;;
  wait) get /wait "ms=${1:-500}" ;;
  waitfor) get /wait "until=$1" "timeout=${2:-60}" ;;
  get) p="$1"; shift; get "$p" "$@" ;;
  post) p="$1"; shift; post "$p" "$@" ;;
  *) usage; exit 1 ;;
esac
