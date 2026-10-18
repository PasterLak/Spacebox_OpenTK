@echo off
cd /d "%~dp0"
title Spacebox Server
start "" /b /wait "%~dp0Spacebox.exe" --server %*
