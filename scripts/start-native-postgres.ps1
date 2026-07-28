param(
    [int]$Port = $(if ($env:POSTGRES_PORT) { [int]$env:POSTGRES_PORT } else { 55432 }),
    [string]$Database = $(if ($env:POSTGRES_DB) { $env:POSTGRES_DB } else { "projeto_pizza" }),
    [string]$DatabaseUser = $(if ($env:POSTGRES_USER) { $env:POSTGRES_USER } else { "projeto_pizza" })
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($env:POSTGRES_PASSWORD)) {
    throw "Defina POSTGRES_PASSWORD somente na sessão atual antes de iniciar o PostgreSQL."
}

$workspaceRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$dataPath = [System.IO.Path]::GetFullPath((Join-Path $workspaceRoot ".postgres-data"))
$passwordFile = [System.IO.Path]::GetFullPath((Join-Path $workspaceRoot ".postgres-bootstrap-password"))
$logPath = [System.IO.Path]::GetFullPath((Join-Path $workspaceRoot ".postgres-local.log"))

if (-not $dataPath.StartsWith("$workspaceRoot\", [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Diretório de dados fora do workspace."
}

$postgresBin = "C:\Program Files\PostgreSQL\17\bin"
$initDb = Join-Path $postgresBin "initdb.exe"
$pgCtl = Join-Path $postgresBin "pg_ctl.exe"
$psql = Join-Path $postgresBin "psql.exe"

foreach ($executable in @($initDb, $pgCtl, $psql)) {
    if (-not (Test-Path -LiteralPath $executable)) {
        throw "PostgreSQL 17 não encontrado em $postgresBin."
    }
}

if (-not (Test-Path -LiteralPath (Join-Path $dataPath "PG_VERSION"))) {
    try {
        Set-Content -LiteralPath $passwordFile -Value $env:POSTGRES_PASSWORD -NoNewline
        & $initDb --pgdata=$dataPath --username=$DatabaseUser --pwfile=$passwordFile --auth-host=scram-sha-256 --auth-local=trust --encoding=UTF8 --locale=C
        if ($LASTEXITCODE -ne 0) {
            throw "Falha ao inicializar o cluster PostgreSQL."
        }
    }
    finally {
        if (Test-Path -LiteralPath $passwordFile) {
            Remove-Item -LiteralPath $passwordFile -Force
        }
    }
}

& $pgCtl --pgdata=$dataPath status *> $null
if ($LASTEXITCODE -ne 0) {
    & $pgCtl --pgdata=$dataPath --log=$logPath --options="-p $Port -h localhost" --wait start
    if ($LASTEXITCODE -ne 0) {
        throw "Falha ao iniciar o PostgreSQL local."
    }
}

$env:PGPASSWORD = $env:POSTGRES_PASSWORD
$databaseExists = & $psql --host localhost --port $Port --username $DatabaseUser --dbname postgres --no-password --tuples-only --no-align --command "select 1 from pg_database where datname = '$Database';"
if ($databaseExists -ne "1") {
    & $psql --host localhost --port $Port --username $DatabaseUser --dbname postgres --no-password --command "create database `"$Database`";"
}

Write-Output "PostgreSQL local disponível em localhost:$Port/$Database."
Write-Output "Defina ConnectionStrings__PostgreSql na sessão atual antes de executar a API."
