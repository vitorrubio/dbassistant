# DB Assistant

## Overview
DB Assistant is a layered .NET 8 solution that exposes a REST API for translating natural language questions into safe read-only MySQL queries for the FinTechX Northwind database. The current implementation uses a hybrid schema-context strategy: it searches an indexed schema knowledge base first and then falls back to live metadata from `INFORMATION_SCHEMA`, so newly created tables remain discoverable even when they are not yet indexed in the RAG layer.

## Local Development
Clone the repository and create your local environment file from `.env_copy-me`. Fill the placeholders in `.env` with your MySQL and OpenAI credentials before running the API.

```bash
git clone <repository-url>
cd dbassistant
cp .env_copy-me .env
docker compose up -d mysql
dotnet restore DBAssistant.sln
dotnet build DBAssistant.sln
dotnet test DBAssistant.sln
dotnet run --project src/Api/DBAssistant.Api/DBAssistant.Api.csproj
```

The API exposes Swagger at `/swagger` and the first endpoint at `POST /api/assistant/query`.

The response JSON currently contains:

```json
{
  "sql": "SELECT ...",
  "explanation": "Why the query was generated this way.",
  "schemaContextSource": "rag+information_schema",
  "executed": true,
  "columns": ["ColumnA", "ColumnB"],
  "rows": [
    {
      "ColumnA": "value",
      "ColumnB": 10
    }
  ]
}
```

`OPENAI_MODEL` defaults to `gpt-5.4`. The local RAG bootstrap reads from `knowledge/schema-index.json`; if no relevant schema knowledge is found, the application uses `INFORMATION_SCHEMA` directly.

## Changelog
### 2026-03-23
- Bootstrapped the solution structure, environment templates, Docker assets, CI workflow, Swagger API, and the first read-only SQL assistant flow.
- Added hybrid schema context assembly with RAG-first lookup and `INFORMATION_SCHEMA` fallback, and fixed the default OpenAI model to `gpt-5.4`.
