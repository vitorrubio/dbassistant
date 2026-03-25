# Runtime Schema RAG Generator Plan

## Goal
Upgrade the existing `DBAssistant.KnowledgeGenerator` so it emits retrieval-ready schema artifacts at runtime without committing or packaging generated artifacts.

## Scope
- Keep the current generator and evolve it.
- Generate enriched schema documents plus embedding-input JSONL.
- Keep API fallback to `INFORMATION_SCHEMA`.
- Run the generator before the API inside the container.
- Add automated tests for corpus generation and operational wiring.

## Steps
1. Inspect the existing metadata reader and artifact shape.
2. Extend the document model with retrieval-oriented fields and new doc types.
3. Generate overview, table, relationship, join-path, and lookup/fact/view documents.
4. Emit `schema-documents.json` and `schema-embedding-input.jsonl`.
5. Update the runtime consumer to read the new shape and score documents with hybrid lexical/semantic signals.
6. Remove pre-generated knowledge artifacts from git/build/deploy flows.
7. Add container startup generation plus concise documentation and tests.
