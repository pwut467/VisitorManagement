#!/usr/bin/env bash
# Idempotent Cloud Agent install for VisitorManagement
set -euo pipefail
cd "$(dirname "$0")/.."

dotnet --info >/dev/null
dotnet restore VisitorManagement.sln
dotnet build VisitorManagement.sln -c Debug --no-restore

# Optional PC/SC packages for CardReader agent (safe if already installed)
if command -v apt-get >/dev/null 2>&1; then
  if ! dpkg -s libpcsclite1 >/dev/null 2>&1; then
    sudo -n apt-get update -qq
    sudo -n DEBIAN_FRONTEND=noninteractive apt-get install -y -qq libpcsclite1 pcscd libccid || true
  fi
fi

echo "cloud-agent-install: OK"
