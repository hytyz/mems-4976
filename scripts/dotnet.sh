#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# shellcheck source=scripts/_podman-env.sh
source "${ROOT}/scripts/_podman-env.sh"

podman run --rm \
  -v "${ROOT}":/app \
  -w /app \
  --userns=keep-id \
  -p 5000:8080 \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet "$@"
