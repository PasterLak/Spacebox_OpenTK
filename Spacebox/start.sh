#!/usr/bin/env sh
cd "$(dirname "$0")" || exit 1

if [ -f ./Spacebox ]; then
    [ -x ./Spacebox ] || chmod +x ./Spacebox
    exec ./Spacebox "$@"
fi

exec dotnet Spacebox.dll "$@"
