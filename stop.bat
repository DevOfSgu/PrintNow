@echo off
title Tat PrintNow Backend and Ngrok
cls

echo ================================================================
echo               TAT PRINTNOW BACKEND AND NGROK
echo ================================================================
echo.

echo [+] Dang dung tat ca tien trinh dotnet...
taskkill /f /im dotnet.exe 2>nul
taskkill /f /im PrintNow.Web.exe 2>nul

echo [+] Dang dung tat ca tien trinh ngrok...
taskkill /f /im ngrok.exe 2>nul

echo.
echo ================================================================
echo [+] Da dung tat ca cac dich vu thanh cong!
echo ================================================================
echo.
pause
