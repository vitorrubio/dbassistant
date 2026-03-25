# Technical Document

## 1. Overview
DB Assistant is a .NET 10 REST API that transforms natural-language questions into read-only MySQL queries. The solution is organized into `Domain`, `Data`, `Services`, `UseCases`, and `Api` layers to keep coupling low, preserve testability, and support operational safety.

The main goal is to accelerate data analysis without exposing end users to raw SQL complexity and without allowing write operations against the database.

## 2. Request Flow
1. The client sends `POST /api/assistant/query` with the question and, optionally, `executeSql` and `showDetails`.
2. Middleware validates the API key, using `x-api-key` by default.
3. `AssistantController` delegates to `IProcessNaturalLanguageQueryUseCase`.
4. The use case builds schema context through `ISchemaContextAssembler`:
   - It queries the local runtime knowledge index in `knowledge/runtime/schema-documents.json` together with the persisted vectors in `knowledge/runtime/schema-embeddings.json` for RAG hints.
   - It falls back to live `INFORMATION_SCHEMA` metadata to cover tables that are not yet indexed.
5. `ISqlGenerationGateway` uses OpenAI to generate structured SQL from the question and schema context.
6. The domain validates the generated statement and blocks forbidden commands, allowing read-only SQL only.
7. When `executeSql` is omitted, it defaults to `true`. When it is `false`, the SQL is validated but not executed.
8. When execution occurs, `ISqlQueryExecutor` returns columns and rows from MySQL.
9. After execution, `ISqlGenerationGateway` makes a second model call to convert raw tabular data into `resultsAsText`, which may be short Markdown text or a Markdown table.
10. When `showDetails` is omitted, it defaults to `false`. In that mode the API hides `sql`, `explanation`, `schemaContextSource`, and `executed`. When `showDetails=true`, those fields are returned.
11. The API returns tabular results together with a human-friendly summary.

## 3. Architectural Decisions
- Clean Architecture plus lightweight DDD keeps domain rules and ports in the center and infrastructure details at the edges.
- Direct SQL access is preferred over an ORM because the product depends on dynamic analytical queries.
- Interface-driven contracts make it easier to test each layer and swap external gateways.
- A hybrid RAG plus live-metadata pipeline improves prompt quality without drifting away from the real database state.

## 4. Scalability Strategy
### 4.1 Horizontal API Scalability
The API is stateless at runtime, so multiple container replicas can serve requests without in-memory state coordination.

### 4.2 Cost Scalability
Azure Container Apps can scale replica count based on demand, keeping low-load operating costs under control.

### 4.3 Context Scalability
The runtime-generated `schema-documents.json` and `schema-embeddings.json` artifacts reduce the amount of schema context sent to the model for most requests and avoid rebuilding the full semantic index inside the first user request after a restart. `INFORMATION_SCHEMA` preserves functional completeness when the local RAG artifacts are missing, outdated, or unavailable.

### 4.4 Delivery Scalability
GitHub Actions automates restore, build, test, package, and deployment steps. Immutable `sha-<commit>` image tags improve traceability and rollback safety.

## 5. Security Strategy
### 5.1 Access Security
- The assistant endpoint is protected by a configurable API key header.
- Requests without a valid key are rejected with `401 Unauthorized`.

### 5.2 Data Security
- Domain guardrails accept read-only SQL only.
- There is no write or DDL endpoint.
- Database credentials are loaded from environment variables rather than source code.

### 5.3 Supply Chain Security
- The CI pipeline produces reproducible builds.
- Container images are versioned and published to GitHub Container Registry.
- Deployments target images tagged with the commit SHA.

### 5.4 Secret Management
- Sensitive values are stored in GitHub Secrets and Azure Container App Secrets.
- The workflow injects the client API key through `secretref`, avoiding hardcoded runtime secrets.

## 6. Observability and Operations
The current baseline already provides traceability through image tags and automated tests. Recommended next steps are structured logs, per-stage latency metrics for RAG, OpenAI, and database calls, and alerts for authentication failures or external dependency outages.
