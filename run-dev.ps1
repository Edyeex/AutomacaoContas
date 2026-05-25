param(
    [switch]$StopExisting
)

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$BackendProject = Join-Path $Root "backend\src\AutoDownload.Api\AutoDownload.Api.csproj"
$FrontendPath = Join-Path $Root "autodownload"
$ApiUrl = "http://localhost:5080/api"
$ApiPort = 5080
$FrontendPort = 3000

function Stop-ProcessTree {
    param([int]$ProcessId)

    $children = Get-CimInstance Win32_Process -Filter "ParentProcessId=$ProcessId" -ErrorAction SilentlyContinue
    foreach ($child in $children) {
        Stop-ProcessTree -ProcessId $child.ProcessId
    }

    Stop-Process -Id $ProcessId -Force -ErrorAction SilentlyContinue
}

function Assert-Command {
    param(
        [string]$Name,
        [string]$InstallHint
    )

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Comando '$Name' nao encontrado. $InstallHint"
    }
}

function Assert-Port {
    param(
        [int]$Port,
        [string]$Name
    )

    $connections = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
    if (-not $connections) {
        return
    }

    $processIds = $connections | Select-Object -ExpandProperty OwningProcess -Unique
    foreach ($processId in $processIds) {
        if ($processId -eq 0) {
            continue
        }

        $processInfo = Get-CimInstance Win32_Process -Filter "ProcessId=$processId" -ErrorAction SilentlyContinue
        $commandLine = if ($processInfo) { $processInfo.CommandLine } else { "" }

        if ($StopExisting) {
            Write-Host "Parando processo antigo na porta $Port ($Name): PID $processId" -ForegroundColor Yellow
            Stop-ProcessTree -ProcessId $processId
            continue
        }

        Write-Host "A porta $Port ($Name) ja esta em uso pelo PID $processId." -ForegroundColor Yellow
        if ($commandLine) {
            Write-Host $commandLine -ForegroundColor DarkGray
        }
        throw "Feche esse processo ou rode: .\run-dev.ps1 -StopExisting"
    }

    Start-Sleep -Seconds 1
}

Assert-Command -Name "dotnet" -InstallHint "Instale o .NET SDK 10."
Assert-Command -Name "npm" -InstallHint "Instale o Node.js."

$NpmCommand = Get-Command "npm.cmd" -ErrorAction SilentlyContinue
if (-not $NpmCommand) {
    $NpmCommand = Get-Command "npm" -ErrorAction SilentlyContinue
}

if (-not (Test-Path $BackendProject)) {
    throw "Projeto backend nao encontrado em: $BackendProject"
}

if (-not (Test-Path (Join-Path $FrontendPath "package.json"))) {
    throw "Projeto frontend nao encontrado em: $FrontendPath"
}

if (-not (Test-Path (Join-Path $FrontendPath "node_modules"))) {
    throw "Dependencias do frontend nao instaladas. Rode uma vez: cd .\autodownload; npm install"
}

Assert-Port -Port $ApiPort -Name "backend"
Assert-Port -Port $FrontendPort -Name "frontend"

$apiProcess = $null
$frontendProcess = $null
$previousApiUrl = $env:NEXT_PUBLIC_API_URL

try {
    Write-Host "Iniciando backend em http://localhost:$ApiPort ..." -ForegroundColor Cyan
    $apiProcess = Start-Process `
        -FilePath "dotnet" `
        -ArgumentList @("run", "--project", $BackendProject, "--launch-profile", "http") `
        -WorkingDirectory $Root `
        -NoNewWindow `
        -PassThru

    Start-Sleep -Seconds 2

    Write-Host "Iniciando frontend em http://localhost:$FrontendPort ..." -ForegroundColor Cyan
    $env:NEXT_PUBLIC_API_URL = $ApiUrl
    $frontendProcess = Start-Process `
        -FilePath $NpmCommand.Source `
        -ArgumentList @("run", "dev", "--", "-p", "$FrontendPort") `
        -WorkingDirectory $FrontendPath `
        -NoNewWindow `
        -PassThru

    Write-Host ""
    Write-Host "AutoDownload rodando:" -ForegroundColor Green
    Write-Host "  Frontend: http://localhost:$FrontendPort"
    Write-Host "  Backend:  http://localhost:$ApiPort/api"
    Write-Host ""
    Write-Host "Pressione Ctrl+C para parar tudo." -ForegroundColor Yellow
    Write-Host ""

    while ($true) {
        $apiProcess.Refresh()
        $frontendProcess.Refresh()

        if ($apiProcess.HasExited) {
            throw "Backend encerrou com codigo $($apiProcess.ExitCode)."
        }

        if ($frontendProcess.HasExited) {
            throw "Frontend encerrou com codigo $($frontendProcess.ExitCode)."
        }

        Start-Sleep -Seconds 1
    }
}
finally {
    if ($null -eq $previousApiUrl) {
        Remove-Item Env:\NEXT_PUBLIC_API_URL -ErrorAction SilentlyContinue
    }
    else {
        $env:NEXT_PUBLIC_API_URL = $previousApiUrl
    }

    Write-Host ""
    Write-Host "Parando AutoDownload..." -ForegroundColor Yellow

    if ($frontendProcess -and -not $frontendProcess.HasExited) {
        Stop-ProcessTree -ProcessId $frontendProcess.Id
    }

    if ($apiProcess -and -not $apiProcess.HasExited) {
        Stop-ProcessTree -ProcessId $apiProcess.Id
    }

    Write-Host "Processos encerrados." -ForegroundColor Green
}
