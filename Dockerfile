FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore DBAssistant.sln
RUN dotnet publish src/Api/DBAssistant.Api/DBAssistant.Api.csproj -c Release -o /app/publish/api --no-restore
RUN dotnet publish tools/DBAssistant.KnowledgeGenerator/DBAssistant.KnowledgeGenerator.csproj -c Release -o /app/publish/knowledge-generator --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

LABEL org.opencontainers.image.source="https://github.com/vitorrubio/dbassistant"
LABEL org.opencontainers.image.description="DBAssistant API for natural-language querying over connected MySQL databases."
LABEL org.opencontainers.image.licenses="MIT"

COPY --from=build /app/publish/api .
COPY --from=build /app/publish/knowledge-generator ./knowledge-generator
COPY docker/entrypoint.sh /app/entrypoint.sh

RUN chmod +x /app/entrypoint.sh && mkdir -p /app/knowledge/runtime

ENV ASPNETCORE_URLS=http://+:8080
ENV SCHEMA_KNOWLEDGE_DIRECTORY=/app/knowledge/runtime
ENV SCHEMA_KNOWLEDGE_FILE_PATH=/app/knowledge/runtime/schema-documents.json
ENV SCHEMA_KNOWLEDGE_EMBEDDING_INPUT_FILE_PATH=/app/knowledge/runtime/schema-embedding-input.jsonl
ENV SCHEMA_KNOWLEDGE_EMBEDDINGS_FILE_PATH=/app/knowledge/runtime/schema-embeddings.json
EXPOSE 8080

ENTRYPOINT ["/app/entrypoint.sh"]
