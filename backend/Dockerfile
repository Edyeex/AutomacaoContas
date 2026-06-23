# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY src/AutoDownload.Domain/AutoDownload.Domain.csproj src/AutoDownload.Domain/
COPY src/AutoDownload.Application/AutoDownload.Application.csproj src/AutoDownload.Application/
COPY src/AutoDownload.Infrastructure/AutoDownload.Infrastructure.csproj src/AutoDownload.Infrastructure/
COPY src/AutoDownload.Api/AutoDownload.Api.csproj src/AutoDownload.Api/

RUN dotnet restore src/AutoDownload.Api/AutoDownload.Api.csproj

COPY src/ src/

RUN dotnet publish src/AutoDownload.Api/AutoDownload.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

USER root
RUN apt-get update \
    && apt-get install --yes --no-install-recommends \
        ca-certificates \
        chromium \
        chromium-driver \
        fonts-liberation \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

RUN mkdir -p /app/App_Data/bills /app/App_Data/demo-bills \
    && chown -R $APP_UID:$APP_UID /app

COPY --from=build --chown=$APP_UID:$APP_UID /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=10000 \
    DOTNET_EnableDiagnostics=0 \
    HOME=/app/App_Data \
    Automation__Demo__StorageDirectory=/app/App_Data/demo-bills \
    Automation__VeroInternet__DownloadDirectory=/app/App_Data/bills \
    Automation__VeroInternet__Headless=true \
    Automation__RmsTelecom__DownloadDirectory=/app/App_Data/bills \
    Automation__RmsTelecom__Headless=true

EXPOSE 10000

USER $APP_UID

ENTRYPOINT ["dotnet", "AutoDownload.Api.dll"]
