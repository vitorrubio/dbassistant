# Fix Plan - Hide Response Metadata When Details Are Disabled (2026-03-24)

## Goal
Ensure `schemaContextSource` and `executed` are omitted from the API response when `showDetails` is `false` or omitted.

## Scope
- Update the response contract to support conditional omission.
- Adjust the use case output for detail-disabled responses.
- Update API integration tests and README examples.
- Validate locally and re-check production after deployment.

## Steps
- [x] Inspect current API response serialization and tests.
- [x] Implement conditional omission for metadata fields.
- [x] Update automated tests and documentation.
- [ ] Run local verification, commit, push, and validate production.
