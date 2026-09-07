@echo off
chcp 65001 >nul
title MCP for Unity - setup
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0setup-unity-mcp.ps1"
echo.
echo ----------------------------------------------------------
echo Finished. Log file: %~dp0setup-unity-mcp-log.txt
echo ----------------------------------------------------------
pause
