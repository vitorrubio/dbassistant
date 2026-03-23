# Feature Plan: Bootstrap DB Assistant

## Context
- Read `AGENTS.md` and `DBASSISTANT.md`.
- Goal: start the project with a Clean Architecture / DDD-aligned skeleton for a DB Assistant API backed by MySQL and OpenAI.
- Constraint: `.env` must be created with placeholders only and wait for the user to fill values.

## Planned Steps
- [x] Read repository instructions and project specification.
- [x] Inspect current repository state and available .NET SDK.
- [x] Create feature branch for the bootstrap work.
- [x] Create `.agents` memory structure for planning and internal notes.
- [x] Add repository bootstrap files: `.gitignore`, `.env`, `.env_copy-me`, `README.md`.
- [x] Generate solution and projects for Domain, Data, Services, UseCases, Api, and test projects.
- [x] Wire references, dependency injection, configuration and Swagger.
- [x] Implement an initial vertical slice for a read-only natural language query contract.
- [x] Add Docker and Docker Compose files for API and MySQL integration.
- [ ] Run build and tests. Blocked: local .NET runtime is missing, so `dotnet new/build/test` cannot execute in this environment yet.
- [ ] Summarize pending architecture/business decisions that require user confirmation.

## Initial Assumptions
- Use the installed .NET SDK `8.0.418`, since that is the latest available in the current environment.
- Bootstrap a REST API with a minimal first endpoint and placeholders for OpenAI/MySQL integration.
- Enforce read-only query intent in the application contracts and validation layer from the start.
- Use the OpenAI Responses API shape for the first gateway implementation, keeping the model configurable in `.env`.

## Pending User Decisions
- Exact API contract for the first business endpoint beyond the bootstrap version.
- Preferred OpenAI model family and whether responses should return raw SQL, executed results, natural-language summaries, or all of them.
- Database schema exposure strategy for prompt/RAG context.
