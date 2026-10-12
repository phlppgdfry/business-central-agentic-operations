#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
command -v dotnet >/dev/null || { echo "Install the .NET 10 SDK first." >&2; exit 1; }
dotnet build --configuration Release
api_pid=""
dashboard_pid=""
cleanup() {
  trap - EXIT INT TERM
  if [ -n "$dashboard_pid" ]; then kill "$dashboard_pid" 2>/dev/null || true; fi
  if [ -n "$api_pid" ]; then kill "$api_pid" 2>/dev/null || true; fi
  wait 2>/dev/null || true
}
trap cleanup EXIT
trap 'exit 0' INT TERM
(cd src/AgenticOperations.Api && ASPNETCORE_ENVIRONMENT=Development exec dotnet bin/Release/net10.0/AgenticOperations.Api.dll --urls http://localhost:5057) &
api_pid=$!
ready=false
for attempt in {1..30}; do
  if curl --fail --silent http://localhost:5057/api/demo >/dev/null; then ready=true; break; fi
  if ! kill -0 "$api_pid" 2>/dev/null; then echo "API failed to start." >&2; exit 1; fi
  sleep 1
done
if [ "$ready" != true ]; then echo "API did not become ready." >&2; exit 1; fi
(cd src/AgenticOperations.Dashboard && ASPNETCORE_ENVIRONMENT=Development OperationsApi__BaseUrl=http://localhost:5057 exec dotnet bin/Release/net10.0/AgenticOperations.Dashboard.dll --urls http://localhost:5058) &
dashboard_pid=$!
echo "Dashboard: http://localhost:5058 | API: http://localhost:5057"
echo "Press Ctrl+C to stop both. Demo data resets when the API stops."
wait "$dashboard_pid"
