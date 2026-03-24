# Automatic Documentation Pipeline Plan

## Context
- The solution is a layered `.NET 10` application with multiple class library projects plus one ASP.NET Core API project.
- The repository already keeps manual architecture documentation under `docs/architecture`.
- The API already exposes Swagger, which creates an opportunity to unify conceptual docs, API reference, and REST reference in a single static site.
- The repository publication model is public, so GitHub Pages is an approved primary hosting option.

## Decision Summary
- Prefer `docfx` over `doxygen` for this repository.
- Generate documentation during CI and optionally expose a local developer command for manual preview/regeneration.
- Store source inputs in the repository and publish generated site artifacts through GitHub Pages as the default public hosting target.
- If GitHub Pages ever becomes unavailable, keep the generated site either:
  - as a build artifact only, or
  - committed to a dedicated branch such as `gh-pages`, or
  - committed under a repo folder only as a fallback.

## Comparison

### Why `docfx` fits better
- Native focus on `.NET` API documentation.
- Builds a single site from `.NET` assemblies, XML code comments, Markdown, and Swagger/OpenAPI.
- Matches the existing repository layout because `docs/architecture` can become conceptual content and Swagger can feed REST reference pages.
- Easier to keep a coherent developer portal rather than separate API/class reference output.

### Why `doxygen` is less suitable here
- It supports `C#` and XML-style comments, but its strongest fit is still polyglot/source-reference generation rather than a first-class `.NET` docs portal.
- It would add more friction to integrate the existing Markdown architecture docs and Swagger-based REST docs into one polished site.
- It is reasonable only if the primary objective is raw code reference output and diagrams, not a unified `.NET` documentation experience.

## Proposed Repository Layout
- `docs/`
  - `docfx.json`
  - `index.md`
  - `toc.yml`
  - `articles/` for hand-written conceptual docs
  - `api/` for generated API metadata/output inputs
  - `swagger/` for exported OpenAPI JSON if needed by the build
- Keep `docs/architecture` and gradually fold it into the DocFX table of contents instead of duplicating content.

## Command Placement

### Local developer commands
- Put the authoritative commands in `README.md`.
- Add a small script wrapper for consistency, for example:
  - `scripts/docs/build.sh`
  - `scripts/docs/serve.sh`
- Alternative: add `make docs` / `make docs-serve` only if the team already wants a Make-based workflow.

### CI commands
- Add a dedicated workflow such as `.github/workflows/docs.yml`, or a separate job inside the existing `ci.yml`.
- Prefer a separate workflow if documentation publishing should be independently rerunnable and less coupled to container deployment.

## CI/CD Proposal

### Pull requests
- Restore tools and dependencies.
- Build solution in `Release`.
- Generate XML documentation files.
- Export Swagger/OpenAPI from the API assembly.
- Run `docfx build` to validate the documentation site.
- Upload the generated site as an artifact for review.

### Main branch
- Run the same validation steps.
- Deploy the generated static site to GitHub Pages when enabled.

## Storage Strategy

### Version in Git
- Version the doc source files:
  - `docfx.json`
  - Markdown articles
  - TOC files
  - templates/assets if customized
- Do not version the generated `_site/` output by default.

### Generated outputs
- Local output: `docs/_site/`
- CI output: workflow artifact
- Published output: GitHub Pages deployment artifact or `gh-pages` branch

## GitHub Pages Feasibility
- GitHub Pages is free for public repositories on GitHub Free.
- GitHub Pages for private repositories requires paid capabilities; the docs indicate private Pages publishing requires an organization using GitHub Enterprise Cloud.
- For this public project, GitHub Pages is the primary recommendation.
- For a private project without the required paid plan, use CI artifacts or another hosting target.

## Fallback If GitHub Pages Is Not Used
- Keep documentation inside the repository as source Markdown only.
- Optionally commit generated static output under `docs/site/` or another folder, but only as a fallback because generated artifacts create noisy diffs and review overhead.
- Better fallback than committing generated HTML: publish CI artifacts on every main build so reviewers can download the site.

## Implementation Steps
1. Add XML documentation generation to all production `.csproj` files.
2. Create a DocFX docset under `docs/`.
3. Wire existing architecture Markdown into the DocFX navigation.
4. Export Swagger/OpenAPI JSON during docs build and include it in DocFX.
5. Add local scripts for `build` and `serve`.
6. Add CI validation for docs on pull requests.
7. Add GitHub Pages deployment on `main`.
8. Update `README.md` changelog and local docs commands.

## Risks
- XML comments may initially be sparse, producing thin API reference pages until the codebase is documented.
- DocFX is community-driven, so template/customization choices should stay conservative to reduce maintenance burden.
- Committing generated HTML into the main branch will likely create review noise and merge conflicts.

## Recommendation
- Adopt `docfx`.
- Store documentation source in `docs/`.
- Build docs in CI for every PR.
- Publish to GitHub Pages from `main`.
- If Pages is not available, keep generated docs as CI artifacts and keep only source docs in the main branch.
