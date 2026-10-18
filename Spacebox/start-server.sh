#!/usr/bin/env sh
cd "$(dirname "$0")" || exit 1

if [ -f ./Spacebox ]; then
    [ -x ./Spacebox ] || chmod +x ./Spacebox
    set -- ./Spacebox --server "$@"
else
    set -- dotnet Spacebox.dll --server "$@"
fi

if [ -z "$DISPLAY" ] && [ -z "$WAYLAND_DISPLAY" ]; then
    if command -v xvfb-run >/dev/null 2>&1; then
        exec xvfb-run -a "$@"
    fi
    echo "No display found. Install xvfb (xvfb-run) to run the server without a desktop." >&2
fi

exec "$@"
