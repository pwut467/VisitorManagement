@echo off
REM Publish CardReader (Windows System Tray) for copying to a security PC
cd /d "%~dp0"
dotnet publish "%~dp0VisitorManagement.CardReader.csproj" -c Release -o "%~dp0bin\publish"
if errorlevel 1 (
  echo Publish failed.
  pause
  exit /b 1
)
echo.
echo Published to: %~dp0bin\publish
echo Run VisitorManagement.CardReader.exe on the PC with the USB card reader.
explorer "%~dp0bin\publish"
