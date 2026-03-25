# Technical Decisions

## 1. Cache
### Decision
There is no distributed cache in the current version.

### Rationale
- Responses depend on transactional data that may change frequently.
- Premature caching can return stale answers without a robust invalidation policy.
- The initial focus was correctness first: read-only SQL, hybrid schema context, and safe execution.

### Suggested Next Step
Introduce selective caching for low-volatility repeated questions with a short TTL and a key derived from question hash plus schema version.

## 2. RAG (Retrieval-Augmented Generation)
### Decision
RAG was removed from the solution. Schema context is now built directly from live `INFORMATION_SCHEMA` metadata for every request.

### Rationale
- A local benchmark executed all 13 prompts from `ACCEPTANCE.md` twice, once with RAG and once without it.
- The non-RAG path was faster in 11 of 13 prompts, with average latency around 6.84 seconds versus 9.50 seconds with RAG.
- RAG introduced an extra OpenAI embeddings call and a second operational artifact pipeline with little practical benefit for this schema size.
- RAG also changed model behavior in undesirable ways by over-shaping the prompt context, which made some answers less faithful to the intended business interpretation.

### Risk Mitigation
- Keep schema assembly simple and authoritative by reading directly from `INFORMATION_SCHEMA`.
- Add only narrow interpretation hints derived from explicit schema fields such as `company`, instead of restoring a secondary retrieval pipeline.
- Monitor prompt size and latency; if schema scale grows materially, reassess targeted schema summarization from measured evidence rather than reintroducing full RAG by default.
- Keep the real-LLM evaluation suite opt-in through `RUN_REAL_LLM_EVALUATION_TESTS=true` so the default local and CI test run remains deterministic.

## 3. Synchronous vs. Asynchronous Processing
### Decision
The current flow is synchronous from SQL generation through optional execution.

### Rationale
- It keeps the API contract simple for consumers.
- It is suitable for short to medium analytical queries.
- Troubleshooting is easier when request and response stay correlated in a single HTTP interaction.

### When to Evolve
- Long-running queries or high concurrency.
- Need for queues, retries, callbacks, or polling.
- Cost-control requirements that benefit from background orchestration.

## 4. Risk Mitigation
- **SQL safety risk**: domain validation accepts read-only SQL only.
- **OpenAI availability risk**: external-service failures are handled in the gateway and use-case layers.
- **Schema drift risk**: direct `INFORMATION_SCHEMA` reads keep the assistant aligned with the live schema at request time.
- **Secret exposure risk**: environment variables and managed secret stores keep sensitive values out of the codebase.
- **Regression risk**: unit tests, layer-specific integration tests, and API integration tests with TestServer are in place.
- **Low-value answer risk**: a second model call transforms raw SQL results into `resultsAsText` when interpretation matters more than the raw table.

## 5. GitHub Actions
### Why It Is Used
- A single pipeline restores, builds, tests, packages, and deploys the application.
- It gives fast feedback for branch work and standardizes release quality.
- It integrates naturally with repository secrets and permissions.

### Benefits
- Lower variation between development, CI, and delivery environments.
- Quality evidence through automated execution.
- Lower operational effort for frequent releases.

## 6. Containers
### Why They Are Used
- Predictable packaging of the application and dependencies.
- Portability across local development, CI, and cloud hosting.
- Simpler horizontal scaling.

### Benefits
- Fewer environment-specific issues.
- Faster deployment and safer rollback through versioned images.

## 7. GitHub Container Registry (GHCR)
### Why It Is Used
- It is integrated with the code host and pipeline.
- Access control is aligned with the repository.
- It reduces friction for versioning and distributing images.

### Benefits
- Centralized governance of build artifacts.
- Lower operational complexity than maintaining a separate registry.

## 8. Azure Hosting Plan
### Decision
The application is deployed to Azure Container Apps through the CI workflow.

### Rationale
- It is a managed container platform for HTTP services with native autoscaling.
- It fits a stateless API with variable load.
- It reduces operational burden compared with self-managed cluster orchestration.

### Note
The exact billing SKU should be confirmed in the organization Azure subscription; the repository reveals the target platform, not the finance configuration.

## 9. Environment Variables and Secrets
### Current Approach
- `.env` is used for local development and local testing.
- `.env_copy-me` is the versioned template without sensitive values.
- GitHub Secrets store CI and deployment credentials.
- Azure Container App Secrets store runtime secrets such as the client API key.

### Why This Approach
- Sensitive configuration stays separate from source code.
- Credential rotation does not require code changes.
- The risk of leaking secrets through commits or pull requests is reduced.

## 10. Application Costs
### 10.1 Main Cost Drivers
- OpenAI API usage.
- Azure Container Apps runtime compute.
- External or managed MySQL usage.
- GHCR image storage and transfer.
- GitHub Actions CI minutes.

### 10.2 Cost-Control Strategies
- Keep model context small through focused prompt construction and question-specific SQL planning.
- Define token and timeout boundaries.
- Prefer autoscaling and on-demand compute behavior.
- Reuse immutable SHA-tagged images and avoid unnecessary rebuilds.
- Measure cost per request through observability of tokens, latency, and request volume.

### 10.3 Cost Summary
OpenAI is likely to be the dominant cost driver at scale, followed by API compute and database usage. The current stateless containerized design supports incremental cost control.
