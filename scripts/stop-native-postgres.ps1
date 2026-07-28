$ErrorActionPreference = "Stop"

$workspaceRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$dataPath = [System.IO.Path]::GetFullPath((Join-Path $workspaceRoot ".postgres-data"))
$pgCtl = "C:\Program Files\PostgreSQL\17\bin\pg_ctl.exe"

if (-not $dataPath.StartsWith("$workspaceRoot\", [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Diretório de dados fora do workspace."
}

if (-not (Test-Path -LiteralPath (Join-Path $dataPath "PG_VERSION"))) {
    Write-Output "Não existe cluster PostgreSQL local neste workspace."
    exit 0
}

& $pgCtl --pgdata=$dataPath status *> $null
if ($LASTEXITCODE -ne 0) {
    Write-Output "O PostgreSQL local já está parado."
    exit 0
}

& $pgCtl --pgdata=$dataPath --wait stop
if ($LASTEXITCODE -ne 0) {
    throw "Falha ao parar o PostgreSQL local."
}

Write-Output "PostgreSQL local parado."
