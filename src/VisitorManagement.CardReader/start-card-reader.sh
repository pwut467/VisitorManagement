#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
echo "เปิดโปรแกรมอ่านบัตรประชาชน ที่ http://127.0.0.1:5001"
echo "เสียบเครื่องอ่าน USB แล้วเสียบบัตรก่อนกด \"อ่านบัตรประชาชน\" ในเว็บ"

if command -v pcscd >/dev/null 2>&1; then
  if ! pgrep -x pcscd >/dev/null 2>&1; then
    echo "กำลังเปิด pcscd..."
    if command -v systemctl >/dev/null 2>&1 && systemctl is-system-running >/dev/null 2>&1; then
      sudo systemctl start pcscd.socket pcscd.service 2>/dev/null || sudo pcscd --disable-polkit || true
    else
      sudo pcscd --disable-polkit || true
    fi
  fi
fi

dotnet run -f net8.0 --launch-profile CardReader
