FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore DBAssistant.sln
RUN dotnet publish src/Api/DBAssistant.Api/DBAssistant.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

LABEL org.opencontainers.image.source="https://github.com/vitorrubio/dbassistant"
LABEL org.opencontainers.image.description="DBAssistant API for natural-language querying over connected MySQL databases."
LABEL org.opencontainers.image.licenses="MIT"

COPY --from=build /app/publish .
COPY --from=build /src/knowledge ./knowledge

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "DBAssistant.Api.dll"]
