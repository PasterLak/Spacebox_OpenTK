#!/bin/bash
cd /Users/vladis/Documents/Projects/C#/Spacebox_OpenTK

dotnet publish Spacebox/Spacebox.csproj -c Release -r osx-x64 /p:EnableMacAppBundle=true
echo "->>>> Spacebox.app created in  publish!"

open ./Spacebox/bin/Release/net8.0/osx-x64/publish