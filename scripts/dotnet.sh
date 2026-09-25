#!/usr/bin/env bash
# Run dotnet (and dotnet ef/codegen via tool manifest) inside the dev container.
set -euo pipefail

# Resolve repository root (one level up from the scripts directory).
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# Keep Podman's container storage rooted in the real home directory even when
# these scripts are launched from the VS Code snap.
# shellcheck source=scripts/_podman-env.sh
source "${ROOT}/scripts/_podman-env.sh"

podman run --rm \
  -v "${ROOT}":/app \
  -w /app \
  --userns=keep-id \
  -p 5000:8080 \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet "$@"
