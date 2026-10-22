#!/usr/bin/env sh
cd "$(dirname "$0")" || exit 1

if [ -f ./Spacebox ]; then
    [ -x ./Spacebox ] || chmod +x ./Spacebox
    set -- ./Spacebox --server "$@"
else
    set -- dotnet Spacebox.dll --server "$@"
fi

exec "$@"
