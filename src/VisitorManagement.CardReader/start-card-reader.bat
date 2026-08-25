@echo off
title Visitor Thai ID Card Reader
cd /d "%~dp0"
echo เปิดโปรแกรมอ่านบัตรประชาชน ที่ http://127.0.0.1:5001
echo เสียบเครื่องอ่าน USB แล้วเสียบบัตรก่อนกด "อ่านบัตรประชาชน" ในเว็บ
dotnet run --launch-profile CardReader
pause
