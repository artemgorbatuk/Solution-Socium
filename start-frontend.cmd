@echo off
title Socium Frontend
cd /d "%~dp0src\frontend\SociumWeb"
echo Frontend: http://127.0.0.1:4200
call npx ng serve --host 127.0.0.1 --port 4200
pause
