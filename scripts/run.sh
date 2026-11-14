#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# shellcheck source=scripts/_podman-env.sh
source "${ROOT}/scripts/_podman-env.sh"

podman build -t municipal-elections-dev "${ROOT}"
podman run --rm -it \
  -v "${ROOT}":/app \
  -w /app \
  --userns=keep-id \
  -p 5000:8080 \
  municipal-elections-dev
