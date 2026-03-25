# Solution Overview

DB Assistant is a layered .NET 10 solution that turns natural-language questions into safe read-only MySQL queries. The architecture follows a pragmatic Clean Architecture and DDD style, keeping domain rules and use-case orchestration isolated from infrastructure details such as OpenAI integration, schema discovery, and SQL execution.

## Layer Summary
- `Domain`: domain entities, validation guards, and repository or service contracts.
- `UseCases`: orchestration of the natural-language-to-SQL workflow.
- `Data`: live schema discovery and read-only query execution against MySQL.
- `Services`: OpenAI integration and schema-knowledge search.
- `Api`: HTTP controllers, middleware, and Swagger surface.

## Request Flow
1. A client sends `POST /api/assistant/query` with a natural-language question.
2. The API key middleware validates access before the controller is reached.
3. The use case assembles hybrid schema context from the local knowledge index and `INFORMATION_SCHEMA`.
4. The OpenAI gateway generates read-only SQL and, after execution, summarizes the result as `resultsAsText`.
5. The domain guard validates that only safe read-only SQL is accepted.
6. When execution is enabled, the data layer returns tabular results from MySQL.

## Documentation Set
- [Architecture Diagrams](architecture-diagrams.md): rendered architecture diagrams and Mermaid source links.
- [Technical Document](technical-document.md): request flow, architectural decisions, scalability, and security.
- [Technical Decisions](technical-decisions.md): rationale for cache, RAG, deployment, secrets, and costs.

The Mermaid source files are stored alongside the English documentation so the diagrams can be reviewed and regenerated when the architecture changes.

## Low Level Tech Docs
- [GitHub Pages Documentation Site](https://vitorrubio.github.io/dbassistant/): published static technical documentation generated with DocFX, including the low-level .NET API reference and REST API reference.
