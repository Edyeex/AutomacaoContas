param(
    [string]$HostName = "localhost",
    [int]$Port = 5432,
    [string]$AdminUser = "postgres",
    [string]$AppUser = "autodownload",
    [string]$AppPassword = "autodownload",
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
        [string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Comando falhou: $FilePath $($Arguments -join ' ')"
    }
}

$psql = Resolve-PostgresTool "psql"
$adminPassword = Read-Host "Senha do usuario PostgreSQL '$AdminUser'" -AsSecureString
$plainPassword = Convert-SecureStringToPlainText $adminPassword

try {
    $env:PGPASSWORD = $plainPassword

    Write-Host "Criando/atualizando usuario '$AppUser'..."
    Invoke-NativeCommand $psql @(
        "-h", $HostName,
        "-p", $Port,
        "-U", $AdminUser,
        "-d", "postgres",
        "-v", "ON_ERROR_STOP=1",
        "-c", "DO `$`$ BEGIN IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '$AppUser') THEN CREATE ROLE $AppUser LOGIN PASSWORD '$AppPassword'; ELSE ALTER ROLE $AppUser WITH LOGIN PASSWORD '$AppPassword'; END IF; END `$`$;"
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
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}
