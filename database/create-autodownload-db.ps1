param(
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$AdminUser = "postgres",
    [string]$AppUser = "autodownload",
    [string]$AppPassword = $env:AUTODOWNLOAD_DB_PASSWORD,
    [string]$Database = "autodownload"
)

$ErrorActionPreference = "Stop"

function Resolve-PostgresTool {
    param([string]$ToolName)

    $command = Get-Command $ToolName -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $candidates = @(
        "C:\Program Files\PostgreSQL\18\bin\$ToolName.exe",
        "C:\Program Files\PostgreSQL\17\bin\$ToolName.exe",
        "C:\Program Files\PostgreSQL\16\bin\$ToolName.exe",
        "C:\Program Files\PostgreSQL\15\bin\$ToolName.exe"
    )

    foreach ($candidate in $candidates) {
        if (Test-Path $candidate) {
            return $candidate
        }
    }

    throw "Nao encontrei $ToolName.exe. Instale o PostgreSQL ou adicione a pasta bin ao PATH."
}

function Convert-SecureStringToPlainText {
    param([securestring]$SecureValue)

    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureValue)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    }
}

function Invoke-NativeCommand {
    param(
        [string]$FilePath,
        [string[]]$Arguments,
        [switch]$RedactArguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        if ($RedactArguments) {
            throw "Comando falhou: $FilePath (argumentos omitidos por seguranca)"
        }

        throw "Comando falhou: $FilePath $($Arguments -join ' ')"
    }
}

function New-RandomSecret {
    param([int]$ByteLength = 48)

    $bytes = New-Object byte[] $ByteLength
    $generator = [Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $generator.GetBytes($bytes)
        return [Convert]::ToBase64String($bytes)
    }
    finally {
        $generator.Dispose()
    }
}

foreach ($identifier in @($AppUser, $Database)) {
    if ($identifier -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') {
        throw "Identificador PostgreSQL invalido: '$identifier'."
    }
}

if ([string]::IsNullOrWhiteSpace($AppPassword)) {
    $secureAppPassword = Read-Host "Defina a senha do usuario PostgreSQL '$AppUser'" -AsSecureString
    $AppPassword = Convert-SecureStringToPlainText $secureAppPassword
}

if ([string]::IsNullOrWhiteSpace($AppPassword)) {
    throw "A senha do usuario PostgreSQL da aplicacao nao pode ficar vazia."
}

$psql = Resolve-PostgresTool "psql"
$adminPassword = Read-Host "Senha do usuario PostgreSQL '$AdminUser'" -AsSecureString
$plainPassword = Convert-SecureStringToPlainText $adminPassword
$escapedSqlPassword = $AppPassword.Replace("'", "''")
$escapedConnectionPassword = $AppPassword.Replace('"', '""')
$connectionString = "Host=$HostName;Port=$Port;Database=$Database;Username=$AppUser;Password=`"$escapedConnectionPassword`""
$signingKey = New-RandomSecret
$apiProject = ".\backend\src\AutoDownload.Api\AutoDownload.Api.csproj"
$previousPgPassword = $env:PGPASSWORD
$previousConnectionString = $env:ConnectionStrings__AutoDownload
$previousSigningKey = $env:Security__AccessToken__SigningKey

try {
    $env:PGPASSWORD = $plainPassword
    $env:ConnectionStrings__AutoDownload = $connectionString
    $env:Security__AccessToken__SigningKey = $signingKey

    Write-Host "Criando/atualizando usuario '$AppUser'..."
    Invoke-NativeCommand -FilePath $psql -RedactArguments -Arguments @(
        "-h", $HostName,
        "-p", $Port,
        "-U", $AdminUser,
        "-d", "postgres",
        "-v", "ON_ERROR_STOP=1",
        "-c", "DO `$`$ BEGIN IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '$AppUser') THEN CREATE ROLE $AppUser LOGIN PASSWORD '$escapedSqlPassword'; ELSE ALTER ROLE $AppUser WITH LOGIN PASSWORD '$escapedSqlPassword'; END IF; END `$`$;"
    )

    $databaseExists = & $psql @(
        "-h", $HostName,
        "-p", $Port,
        "-U", $AdminUser,
        "-d", "postgres",
        "-tAc", "SELECT 1 FROM pg_database WHERE datname = '$Database';"
    )

    if ($LASTEXITCODE -ne 0) {
        throw "Nao foi possivel consultar os bancos existentes."
    }

    if (($databaseExists | Out-String).Trim() -ne "1") {
        Write-Host "Criando banco '$Database'..."
        Invoke-NativeCommand $psql @(
            "-h", $HostName,
            "-p", $Port,
            "-U", $AdminUser,
            "-d", "postgres",
            "-v", "ON_ERROR_STOP=1",
            "-c", "CREATE DATABASE $Database OWNER $AppUser;"
        )
    }
    else {
        Write-Host "Banco '$Database' ja existe."
    }

    Write-Host "Garantindo permissoes..."
    Invoke-NativeCommand $psql @(
        "-h", $HostName,
        "-p", $Port,
        "-U", $AdminUser,
        "-d", $Database,
        "-v", "ON_ERROR_STOP=1",
        "-c", "GRANT ALL PRIVILEGES ON DATABASE $Database TO $AppUser; GRANT ALL ON SCHEMA public TO $AppUser;"
    )

    Write-Host "Salvando configuracoes locais fora do repositorio..."
    Invoke-NativeCommand -FilePath "dotnet" -RedactArguments -Arguments @(
        "user-secrets", "set",
        "ConnectionStrings:AutoDownload", $connectionString,
        "--project", $apiProject
    )
    Invoke-NativeCommand -FilePath "dotnet" -RedactArguments -Arguments @(
        "user-secrets", "set",
        "Security:AccessToken:SigningKey", $signingKey,
        "--project", $apiProject
    )

    Write-Host "Verificando dotnet-ef..."
    $efVersion = & dotnet ef --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Aplicando migrations do Entity Framework..."
        Invoke-NativeCommand "dotnet" @(
            "ef", "database", "update",
            "--project", ".\backend\src\AutoDownload.Infrastructure\AutoDownload.Infrastructure.csproj",
            "--startup-project", ".\backend\src\AutoDownload.Api\AutoDownload.Api.csproj"
        )
    }
    else {
        Write-Host "dotnet-ef nao esta instalado. A API tambem aplica as migrations automaticamente ao iniciar."
    }

    Write-Host ""
    Write-Host "Banco pronto."
    Write-Host "Agora rode a API para executar o seed:"
    Write-Host "dotnet run --project .\backend\src\AutoDownload.Api\AutoDownload.Api.csproj --launch-profile http"
}
finally {
    if ($null -eq $previousPgPassword) {
        Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    }
    else {
        $env:PGPASSWORD = $previousPgPassword
    }

    if ($null -eq $previousConnectionString) {
        Remove-Item Env:\ConnectionStrings__AutoDownload -ErrorAction SilentlyContinue
    }
    else {
        $env:ConnectionStrings__AutoDownload = $previousConnectionString
    }

    if ($null -eq $previousSigningKey) {
        Remove-Item Env:\Security__AccessToken__SigningKey -ErrorAction SilentlyContinue
    }
    else {
        $env:Security__AccessToken__SigningKey = $previousSigningKey
    }
}
