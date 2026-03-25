#!/bin/sh
set -eu

export SCHEMA_KNOWLEDGE_DIRECTORY="${SCHEMA_KNOWLEDGE_DIRECTORY:-/app/knowledge/runtime}"
export SCHEMA_KNOWLEDGE_FILE_PATH="${SCHEMA_KNOWLEDGE_FILE_PATH:-$SCHEMA_KNOWLEDGE_DIRECTORY/schema-documents.json}"
export SCHEMA_KNOWLEDGE_EMBEDDING_INPUT_FILE_PATH="${SCHEMA_KNOWLEDGE_EMBEDDING_INPUT_FILE_PATH:-$SCHEMA_KNOWLEDGE_DIRECTORY/schema-embedding-input.jsonl}"
export SCHEMA_KNOWLEDGE_EMBEDDINGS_FILE_PATH="${SCHEMA_KNOWLEDGE_EMBEDDINGS_FILE_PATH:-$SCHEMA_KNOWLEDGE_DIRECTORY/schema-embeddings.json}"

mkdir -p "$SCHEMA_KNOWLEDGE_DIRECTORY"

echo "Generating runtime schema knowledge artifacts..."

if dotnet /app/knowledge-generator/DBAssistant.KnowledgeGenerator.dll; then
  echo "Schema knowledge artifacts generated successfully."
else
  echo "Schema knowledge generation failed. Continuing with INFORMATION_SCHEMA fallback."
fi

exec dotnet /app/DBAssistant.Api.dll
