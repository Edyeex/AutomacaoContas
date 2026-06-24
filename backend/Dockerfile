# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
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

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS runtime

USER root
RUN apt-get update \
    && apt-get install --yes --no-install-recommends \
        ca-certificates \
        curl \
        gnupg \
        fonts-liberation \
        libasound2 \
        libatk-bridge2.0-0 \
        libgbm1 \
        libgtk-3-0 \
        libnss3 \
        libxss1 \
        xdg-utils \
    && install -m 0755 -d /etc/apt/keyrings \
    && curl -fsSL https://dl.google.com/linux/linux_signing_key.pub \
        | gpg --dearmor -o /etc/apt/keyrings/google-linux-signing-keyring.gpg \
    && chmod a+r /etc/apt/keyrings/google-linux-signing-keyring.gpg \
    && echo "deb [arch=amd64 signed-by=/etc/apt/keyrings/google-linux-signing-keyring.gpg] http://dl.google.com/linux/chrome/deb/ stable main" \
        > /etc/apt/sources.list.d/google-chrome.list \
    && apt-get update \
    && apt-get install --yes --no-install-recommends google-chrome-stable \
    && rm -rf /var/lib/apt/lists/*

RUN google-chrome --version

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
