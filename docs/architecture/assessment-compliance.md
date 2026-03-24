# Assessment Compliance

## Goal
This document maps the original development assessment requirements to the current `dbassistant` implementation so an evaluator can verify coverage quickly.

## Requirement Mapping

### LLM-based API for natural-language-to-SQL
- Implemented through `POST /api/assistant/query`.
- SQL planning uses the OpenAI Responses API with function-calling style output submission.
- SQL execution remains guarded by domain validation plus runtime database execution checks.

### Prompt engineering and guardrails
- Prompt rules explicitly block non-read-only SQL.
- The model is instructed to refuse questions that the schema cannot answer correctly.
- Runtime execution warnings are treated as failures instead of silent empty results.

### Function calling
- The SQL planning flow now uses a function tool named `submit_query_plan` instead of only free-form JSON output.

### RAG and vector search
- Schema knowledge is retrieved from `knowledge/schema-index.json`.
- Retrieval uses embeddings and cosine-similarity ranking.
- `INFORMATION_SCHEMA` remains the authoritative fallback for schema completeness.

### Intelligent cache
- SQL plan generation is cached in memory for repeated prompts.
- Schema retrieval results are cached in memory for repeated questions.
- Embeddings for schema documents are persisted locally in `knowledge/schema-index.embeddings.json`.

### Architecture deliverables
- `solution-overview.md`
- `architecture-diagrams.md`
- `technical-document.md`
- `technical-decisions.md`

### Executable API surface
- Swagger is enabled and enriched with endpoint summaries, response annotations, and request or response examples.

### Cloud publication and CI/CD
- The solution builds into a Docker image.
- CI publishes images to GHCR.
- Deployment targets Azure Container Apps on `master` or `main`.

### Quality validation
- Unit tests cover domain and use-case behavior.
- API integration tests cover controller behavior through TestServer.
- Acceptance tests compare deterministic SQL results against direct database execution.
- Evaluation tests exist for the official challenge prompts using the real LLM path, gated by environment configuration.
