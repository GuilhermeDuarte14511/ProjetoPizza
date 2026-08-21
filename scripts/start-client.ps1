[CmdletBinding()]
param(
    [string]$StatePath = (Join-Path $env:ProgramData "ProjetoPizza")
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

function Get-DockerPath {
    $machinePath = [Environment]::GetEnvironmentVariable("Path", "Machine")
    $userPath = [Environment]::GetEnvironmentVariable("Path", "User")
    $env:Path = "$machinePath;$userPath"

    $command = Get-Command docker.exe -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $candidate = "C:\Program Files\Docker\Docker\resources\bin\docker.exe"
    if (Test-Path -LiteralPath $candidate) {
        return $candidate
    }

    throw "Docker Desktop não encontrado."
}

function Test-DockerEngine {
    param([string]$DockerPath)

    & $DockerPath info --format "{{.ServerVersion}}" *> $null
    return $LASTEXITCODE -eq 0
}

function Wait-DockerEngine {
    param([string]$DockerPath)

    if (-not (Test-DockerEngine -DockerPath $DockerPath)) {
        $desktopPath = "C:\Program Files\Docker\Docker\Docker Desktop.exe"
        if (-not (Test-Path -LiteralPath $desktopPath)) {
            throw "Docker Desktop não encontrado."
        }
        Start-Process -FilePath $desktopPath | Out-Null
    }

    $deadline = (Get-Date).AddMinutes(3)
    while ((Get-Date) -lt $deadline) {
        if (Test-DockerEngine -DockerPath $DockerPath) {
            return
        }
        Start-Sleep -Seconds 3
    }

    throw "Docker Desktop não ficou disponível em três minutos."
}

$metadataPath = Join-Path $StatePath "installation.json"
$runtimeEnvironmentPath = Join-Path $StatePath "runtime.env"
if (-not (Test-Path -LiteralPath $metadataPath) -or -not (Test-Path -LiteralPath $runtimeEnvironmentPath)) {
    throw "Configuração do ProjetoPizza não encontrada em '$StatePath'. Execute install-client.ps1."
}

$metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
$composeFile = Join-Path $metadata.sourcePath "compose.yaml"
if (-not (Test-Path -LiteralPath $composeFile)) {
    throw "compose.yaml não encontrado em '$($metadata.sourcePath)'."
}

$docker = Get-DockerPath
Wait-DockerEngine -DockerPath $docker

& $docker compose `
    --project-name $metadata.composeProjectName `
    --env-file $runtimeEnvironmentPath `
    --file $composeFile `
    --profile client `
    run --rm api --migrate
if ($LASTEXITCODE -ne 0) {
    throw "Não foi possível aplicar as migrations do PostgreSQL."
}

& $docker compose `
    --project-name $metadata.composeProjectName `
    --env-file $runtimeEnvironmentPath `
    --file $composeFile `
    --profile client `
    up -d
if ($LASTEXITCODE -ne 0) {
    throw "Não foi possível iniciar os containers do ProjetoPizza."
}

$deadline = (Get-Date).AddMinutes(2)
$lastError = $null
while ((Get-Date) -lt $deadline) {
    try {
        $response = Invoke-WebRequest -Uri $metadata.healthUrl -UseBasicParsing -TimeoutSec 4
        if ($response.StatusCode -eq 200) {
            Write-Host "ProjetoPizza disponível em $($metadata.applicationUrl)" -ForegroundColor Green
            exit 0
        }
    }
    catch {
        $lastError = $_.Exception.Message
    }
    Start-Sleep -Seconds 3
}

throw "Os containers iniciaram, mas o health check falhou: $lastError"
