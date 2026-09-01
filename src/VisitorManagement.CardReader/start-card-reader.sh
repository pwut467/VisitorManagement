#!/usr/bin/env bash
# CardReader is a Windows System Tray (WinForms) app — not supported on Linux.
set -euo pipefail
echo "VisitorManagement.CardReader รองรับเฉพาะ Windows (System Tray + PC/SC)"
echo "บน Windows ใช้: start-card-reader.bat หรือ"
echo "  dotnet run --project src/VisitorManagement.CardReader"
echo "  dotnet publish src/VisitorManagement.CardReader -c Release"
exit 1
