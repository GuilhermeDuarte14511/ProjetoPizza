[CmdletBinding()]
param(
    [string]$StatePath = (Join-Path $env:ProgramData "ProjetoPizza"),
    [string]$DestinationPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$metadataPath = Join-Path $StatePath "installation.json"
if (-not (Test-Path -LiteralPath $metadataPath)) {
    throw "Configuração do ProjetoPizza não encontrada em '$StatePath'."
}

$metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($DestinationPath)) {
    $DestinationPath = Join-Path $StatePath "backups"
}
$DestinationPath = [IO.Path]::GetFullPath($DestinationPath)
New-Item -ItemType Directory -Path $DestinationPath -Force | Out-Null

$docker = Get-Command docker.exe -ErrorAction SilentlyContinue
if (-not $docker) {
    $candidate = "C:\Program Files\Docker\Docker\resources\bin\docker.exe"
    if (-not (Test-Path -LiteralPath $candidate)) {
        throw "Docker Desktop não encontrado."
    }
    $dockerPath = $candidate
}
else {
    $dockerPath = $docker.Source
}

& $dockerPath inspect "projeto-pizza-postgres" *> $null
if ($LASTEXITCODE -ne 0) {
    throw "O container do PostgreSQL não está disponível. Inicie o ProjetoPizza antes do backup."
}

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$fileName = "projeto-pizza-$timestamp.dump"
$containerPath = "/tmp/$fileName"
$destinationFile = Join-Path $DestinationPath $fileName

try {
    Write-Host "Gerando backup consistente do PostgreSQL..." -ForegroundColor Cyan
    & $dockerPath exec `
        "projeto-pizza-postgres" `
        pg_dump `
        --username $metadata.databaseUser `
        --dbname $metadata.databaseName `
        --format custom `
        --file $containerPath
    if ($LASTEXITCODE -ne 0) {
        throw "O pg_dump falhou."
    }

    & $dockerPath cp "projeto-pizza-postgres:${containerPath}" $destinationFile
    if ($LASTEXITCODE -ne 0) {
        throw "Não foi possível copiar o backup para '$destinationFile'."
    }
}
finally {
    & $dockerPath exec "projeto-pizza-postgres" rm -f $containerPath *> $null
}

$hash = Get-FileHash -LiteralPath $destinationFile -Algorithm SHA256
Write-Host ""
Write-Host "Backup concluído." -ForegroundColor Green
Write-Host "Arquivo: $destinationFile"
Write-Host "SHA-256: $($hash.Hash)"
Write-Host ""
Write-Host "Copie este arquivo para outro disco ou armazenamento protegido."
