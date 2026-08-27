#!/usr/bin/env bash
# Per-boot Cloud Agent start: CardReader (bg) + Web (fg, InMemory DB)
set -euo pipefail
cd "$(dirname "$0")/.."

if command -v pcscd >/dev/null 2>&1 && ! pgrep -x pcscd >/dev/null 2>&1; then
  sudo -n pcscd --disable-polkit >/tmp/pcscd.log 2>&1 || true
fi

if ! curl -sf --max-time 1 http://127.0.0.1:5001/health >/dev/null 2>&1; then
  echo "Starting VisitorManagement.CardReader on http://127.0.0.1:5001"
  nohup dotnet run --project src/VisitorManagement.CardReader --no-launch-profile --urls http://127.0.0.1:5001 \
    >/tmp/visitor-card-reader.log 2>&1 </dev/null &
  disown || true
  for _ in $(seq 1 45); do
    curl -sf --max-time 1 http://127.0.0.1:5001/health >/dev/null 2>&1 && break
    sleep 1
  done
fi

if ! curl -sf --max-time 1 http://127.0.0.1:5001/health >/dev/null 2>&1; then
  echo "WARNING: CardReader failed to start. See /tmp/visitor-card-reader.log" >&2
fi

if curl -sf --max-time 1 http://127.0.0.1:5088/ >/dev/null 2>&1; then
  echo "VisitorManagement.Web already listening on http://127.0.0.1:5088"
  # Keep start attached so the environment start phase has a long-lived process.
  exec tail -f /dev/null
fi

export Database__Provider=InMemory
export ASPNETCORE_ENVIRONMENT=Development
exec dotnet run --project src/VisitorManagement.Web --urls http://127.0.0.1:5088 --no-launch-profile
