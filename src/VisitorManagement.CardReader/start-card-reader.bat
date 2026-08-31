@echo off
title Visitor Thai ID Card Reader
cd /d "%~dp0"
echo เปิดโปรแกรมอ่านบัตรประชาชน ใน System Tray
echo API: http://127.0.0.1:5001
echo คลิกขวาที่ไอคอนถาดระบบเพื่อดูสถานะหรือออกจากโปรแกรม
dotnet run -f net8.0-windows --project "%~dp0VisitorManagement.CardReader.csproj" --no-launch-profile
if errorlevel 1 pause
