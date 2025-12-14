@echo off
call "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat"

taskkill /IM LobbyServer.exe /F >nul 2>&1

msbuild LobbyServer\LobbyServer.sln /p:Configuration=Debug /p:Platform=x64

IF ERRORLEVEL 1 (
    echo BUILD FAILED
    pause
    exit /b
)

start "Lobby Server" cmd /k "LobbyServer\x64\Debug\LobbyServer.exe"
start "Auth Server"  cmd /k "cd Authentication\scripts && node authServer.js"
