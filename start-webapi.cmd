@echo off
title Socium WebApi
cd /d "%~dp0src\backend\SociumApp\WebApi"
echo WebApi: https://localhost:7026
dotnet run --launch-profile https
pause
