# Imagem da API para Render Free / Koyeb (512 MB). Multi-stage, Alpine, sem símbolos de debug.
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
WORKDIR /src

COPY src/FloristHub.DAL/FloristHub.DAL.csproj src/FloristHub.DAL/
COPY src/FloristHub.Services/FloristHub.Services.csproj src/FloristHub.Services/
COPY src/FloristHub.Api/FloristHub.Api.csproj src/FloristHub.Api/
COPY Directory.Build.props Directory.Packages.props ./

RUN dotnet restore src/FloristHub.Api/FloristHub.Api.csproj

COPY src/ src/
RUN dotnet publish src/FloristHub.Api/FloristHub.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false \
    /p:DebugType=none \
    /p:DebugSymbols=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
WORKDIR /app

RUN apk add --no-cache curl \
    && mkdir -p /app/uploads \
    && chown -R app:app /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0 \
    PORT=8080

COPY --from=build --chown=app:app /app/publish .

USER app
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD ["sh", "-c", "curl -fsS \"http://127.0.0.1:${PORT:-8080}/health\" >/dev/null"]

ENTRYPOINT ["dotnet", "FloristHub.Api.dll"]
