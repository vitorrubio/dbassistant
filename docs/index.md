# DB Assistant Documentation

DB Assistant is a layered `.NET 10` solution that turns natural-language questions into safe read-only MySQL queries. This documentation site combines architecture articles, generated `.NET` API reference, and the exported REST API description into one static portal.

## Sections

- [Architecture overview](architecture/solution-overview.md)
- [REST API reference](swagger/dbassistant-api.swagger.json)
- [.NET API reference](api/toc.yml)

## Local Build

Run the local documentation pipeline from the repository root:

```bash
./scripts/docs/build.sh
```

To preview the generated site locally:

```bash
./scripts/docs/serve.sh
```

Both scripts restore the local .NET tools defined in `.config/dotnet-tools.json`, build the solution, export the Swagger 2.0 description, and run DocFX.
