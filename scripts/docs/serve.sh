#!/usr/bin/env bash
set -euo pipefail

readonly ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
readonly DOCFX_CONFIG_PATH="$ROOT_DIR/docs/docfx.json"
readonly DOCS_OUTPUT_PATH="${DOCS_OUTPUT_PATH:-$ROOT_DIR/docs/_site}"

"$ROOT_DIR/scripts/docs/build.sh"
dotnet tool run docfx "$DOCFX_CONFIG_PATH" --serve --output "$DOCS_OUTPUT_PATH"
