#!/usr/bin/env bash
# Build the dev container image and run the app with watch on port 5000.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# Keep Podman's container storage rooted in the real home directory even when
# these scripts are launched from the VS Code snap.
# shellcheck source=scripts/_podman-env.sh
source "${ROOT}/scripts/_podman-env.sh"

podman build -t municipal-elections-dev "${ROOT}"
podman run --rm -it \
  -v "${ROOT}":/app \
  -w /app \
  --userns=keep-id \
  -p 5000:8080 \
  municipal-elections-dev
