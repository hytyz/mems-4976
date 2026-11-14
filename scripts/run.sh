#!/usr/bin/env bash
# Start the whole MEMS solution (SQL Server, API, Admin, Public) through the .NET Aspire AppHost.
# Extra arguments are passed to `dotnet run`. Stop with Ctrl+C.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
APPHOST="${ROOT}/src/MEMS.AppHost"

fail() {
  echo "error: $*" >&2
  exit 1
}

# .NET 10 SDK
command -v dotnet >/dev/null 2>&1 || fail "the .NET SDK was not found. Install the .NET 10 SDK: https://dot.net/download"
sdk_major="$(dotnet --version 2>/dev/null | cut -d. -f1)"
[[ "${sdk_major}" =~ ^[0-9]+$ ]] || fail "could not read the .NET SDK version."
(( sdk_major >= 10 )) || fail "the .NET 10 SDK or later is required (found $(dotnet --version))."

# Container runtime: Aspire runs SQL Server in a container. Docker is used if it is running,
# otherwise Podman.
if command -v docker >/dev/null 2>&1 && docker info >/dev/null 2>&1; then
  echo "Container runtime: Docker"
elif command -v podman >/dev/null 2>&1 && podman info >/dev/null 2>&1; then
  export ASPIRE_CONTAINER_RUNTIME=podman
  echo "Container runtime: Podman"
else
  fail "no running container runtime. Start Docker Desktop (or Podman) and run this script again."
fi

# HTTPS development certificate: the apps use HTTPS endpoints.
if ! dotnet dev-certs https --check --trust >/dev/null 2>&1; then
  echo "warning: the HTTPS development certificate is missing or not trusted." >&2
  echo "         Run 'dotnet dev-certs https --trust' once if the browser shows certificate errors." >&2
fi

echo "Starting MEMS. Open the Aspire dashboard link printed below (login URL with a token)."
exec dotnet run --project "${APPHOST}" "$@"
