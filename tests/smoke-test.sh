#!/usr/bin/env bash
# Starts the server and two test clients, and checks they can see and message each other.
set -euo pipefail
cd "$(dirname "$0")/.."
PORT=${PORT:-7799}
OUT=$(mktemp -d)
dotnet build src/Server -nologo -v q >/dev/null
dotnet build src/TestClient -nologo -v q >/dev/null

dotnet run --no-build --project src/Server -- --port "$PORT" >"$OUT/server.log" 2>&1 &
SERVER=$!
trap 'kill $SERVER 2>/dev/null || true' EXIT
sleep 2

{ sleep 1; echo "/espera 2500"; echo "/sair"; } | dotnet run --no-build --project src/TestClient -- --port "$PORT" --name Ana --city Lagoa >"$OUT/ana.log" 2>&1 &
ANA=$!
sleep 1.5
{ echo "/enviar p1 ola Ana"; echo "/ping"; echo "/espera 800"; echo "/sair"; } | dotnet run --no-build --project src/TestClient -- --port "$PORT" --name Bia --city Serra >"$OUT/bia.log" 2>&1
wait $ANA

fail=0
check() { if grep -q "$2" "$OUT/$1"; then echo "ok   $1: $2"; else echo "FAIL $1: $2"; fail=1; fi; }
check ana.log "seu id: p1"
check ana.log "+ Bia (p2) entrou"
check ana.log "\[Bia\] chat: ola Ana"
check bia.log "+ Ana (p1) entrou"
check bia.log "pong"
check server.log "Bia -> p1: chat"
check server.log "Ana (p1) saiu"
if [ $fail -ne 0 ]; then echo "--- logs ---"; tail -n +1 "$OUT"/*.log; exit 1; fi
echo "Smoke test passou."
