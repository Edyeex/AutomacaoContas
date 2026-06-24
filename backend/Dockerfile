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
        unzip \
    && install -m 0755 -d /etc/apt/keyrings \
    && curl -fsSL https://dl.google.com/linux/linux_signing_key.pub \
        | gpg --dearmor -o /etc/apt/keyrings/google-linux-signing-keyring.gpg \
    && chmod a+r /etc/apt/keyrings/google-linux-signing-keyring.gpg \
    && echo "deb [arch=amd64 signed-by=/etc/apt/keyrings/google-linux-signing-keyring.gpg] http://dl.google.com/linux/chrome/deb/ stable main" \
        > /etc/apt/sources.list.d/google-chrome.list \
    && apt-get update \
    && apt-get install --yes --no-install-recommends \
        fonts-liberation \
        google-chrome-stable \
        xdg-utils \
    && chrome_major="$(google-chrome --product-version | cut -d '.' -f 1)" \
    && chromedriver_version="$(curl -fsSL "https://googlechromelabs.github.io/chrome-for-testing/LATEST_RELEASE_${chrome_major}")" \
    && curl -fsSL \
        -o /tmp/chromedriver.zip \
        "https://storage.googleapis.com/chrome-for-testing-public/${chromedriver_version}/linux64/chromedriver-linux64.zip" \
    && unzip -q /tmp/chromedriver.zip -d /tmp/chromedriver \
    && mv /tmp/chromedriver/chromedriver-linux64/chromedriver /usr/local/bin/chromedriver \
    && chmod +x /usr/local/bin/chromedriver \
    && rm -rf /tmp/chromedriver /tmp/chromedriver.zip \
    && rm -rf /var/lib/apt/lists/*

RUN google-chrome --version && chromedriver --version

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
