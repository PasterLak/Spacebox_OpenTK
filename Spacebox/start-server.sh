#!/usr/bin/env sh
cd "$(dirname "$0")" || exit 1
if [ -x ./Spacebox ]; then
    exec ./Spacebox --server "$@"
fi
exec dotnet Spacebox.dll --server "$@"
