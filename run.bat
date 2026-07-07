@echo off
title Khoi dong PrintNow Backend and Ngrok
cls

echo ================================================================
echo          KHOI DONG PRINTNOW BACKEND AND NGROK
echo ================================================================
echo.

echo [+] Dang khoi chay Ngrok o cua so moi...
start "PrintNow Ngrok" cmd /k "cd /d D:\tools\ngrok && ngrok http 5195"

echo [+] Dang chay backend PrintNow.Web...
cd /d "%~dp0PrintNow.Web"
dotnet run

pause
