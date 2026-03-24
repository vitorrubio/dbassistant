#!/usr/bin/env bash
set -euo pipefail

readonly ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
readonly CONFIGURATION="${CONFIGURATION:-Release}"
readonly API_PROJECT_PATH="$ROOT_DIR/src/Api/DBAssistant.Api/DBAssistant.Api.csproj"
readonly DOCFX_CONFIG_PATH="$ROOT_DIR/docs/docfx.json"
readonly SWAGGER_OUTPUT_DIR="$ROOT_DIR/docs/swagger"
readonly SWAGGER_OUTPUT_PATH="$SWAGGER_OUTPUT_DIR/dbassistant-api.swagger.json"
readonly DOCS_OUTPUT_PATH="${DOCS_OUTPUT_PATH:-$ROOT_DIR/docs/_site}"
readonly SWAGGER_URL="${SWAGGER_URL:-http://127.0.0.1:5080/swagger/v1/swagger.json}"
readonly APP_URLS="${APP_URLS:-http://127.0.0.1:5080}"
readonly APP_LOG_PATH="$ROOT_DIR/docs/swagger/api-docs-build.log"

export API_KEY_HEADER_NAME="${API_KEY_HEADER_NAME:-x-api-key}"
export API_KEY_HEADER_VALUE="${API_KEY_HEADER_VALUE:-docs-build-key}"
export ASPNETCORE_URLS="$APP_URLS"

mkdir -p "$SWAGGER_OUTPUT_DIR"

dotnet tool restore
dotnet restore "$ROOT_DIR/DBAssistant.sln"
dotnet build "$ROOT_DIR/DBAssistant.sln" --configuration "$CONFIGURATION" --no-restore

dotnet run --project "$API_PROJECT_PATH" --configuration "$CONFIGURATION" --no-build --no-launch-profile >"$APP_LOG_PATH" 2>&1 &
api_process_id=$!

cleanup() {
  if kill -0 "$api_process_id" >/dev/null 2>&1; then
    kill "$api_process_id" >/dev/null 2>&1 || true
    wait "$api_process_id" >/dev/null 2>&1 || true
  fi
}

trap cleanup EXIT

for _ in $(seq 1 30); do
  if curl --silent --fail "$SWAGGER_URL" --output "$SWAGGER_OUTPUT_PATH"; then
    break
  fi

  sleep 1
done

if [ ! -s "$SWAGGER_OUTPUT_PATH" ]; then
  cat "$APP_LOG_PATH"
  exit 1
fi

dotnet tool run docfx "$DOCFX_CONFIG_PATH" --output "$DOCS_OUTPUT_PATH"
