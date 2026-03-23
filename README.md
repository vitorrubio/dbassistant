# DB Assistant

## Overview
DB Assistant is a layered .NET 8 solution that exposes a REST API for translating natural language questions into safe read-only MySQL queries for the FinTechX Northwind database. The current bootstrap delivers the initial Clean Architecture / DDD structure, Swagger documentation, Docker assets, CI workflow, and the first vertical slice for SQL generation and optional execution.

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

## Changelog
### 2026-03-23
- Bootstrapped the solution structure, environment templates, Docker assets, CI workflow, Swagger API, and the first read-only SQL assistant flow.
