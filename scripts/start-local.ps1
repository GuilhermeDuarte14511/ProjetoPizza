param(
    [int]$ApiPort = 5080,
    [int]$WebPort = 5173,
    [int]$DatabasePort = 55432
)

$ErrorActionPreference = "Stop"

$workspaceRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$environmentPath = Join-Path $workspaceRoot ".env"
$environmentExamplePath = Join-Path $workspaceRoot ".env.example"
$webPath = Join-Path $workspaceRoot "src\ProjetoPizza.Web"
$apiOutputPath = Join-Path $workspaceRoot "api-local.stdout.log"
$apiErrorPath = Join-Path $workspaceRoot "api-local.stderr.log"
$webOutputPath = Join-Path $workspaceRoot "web-local.stdout.log"
$webErrorPath = Join-Path $workspaceRoot "web-local.stderr.log"

function Test-LocalPort {
    param([int]$Port)

    $client = [System.Net.Sockets.TcpClient]::new()
    try {
        $connection = $client.ConnectAsync("localhost", $Port)
        return $connection.Wait(500) -and $client.Connected
    }
    catch {
        return $false
    }
    finally {
        $client.Dispose()
    }
}

function Read-LocalEnvironment {
    $sourcePath = if (Test-Path -LiteralPath $environmentPath) {
        $environmentPath
    }
    else {
        $environmentExamplePath
    }

    $values = @{}
    foreach ($line in Get-Content -LiteralPath $sourcePath) {
        if ([string]::IsNullOrWhiteSpace($line) -or $line.TrimStart().StartsWith("#")) {
            continue
        }

        $separator = $line.IndexOf("=")
        if ($separator -le 0) {
            continue
        }

        $values[$line.Substring(0, $separator)] = $line.Substring($separator + 1)
    }

    return $values
}

if (-not (Test-LocalPort -Port $DatabasePort)) {
    throw "PostgreSQL não está disponível em localhost:$DatabasePort."
}

$localEnvironment = Read-LocalEnvironment
$connectionString = $localEnvironment["ConnectionStrings__PostgreSql"]
if ([string]::IsNullOrWhiteSpace($connectionString)) {
    throw "ConnectionStrings__PostgreSql não foi definida no ambiente local."
}

$connectionString = [regex]::Replace($connectionString, "Port=\d+", "Port=$DatabasePort")
$apiUrl = "http://localhost:$ApiPort"
$webUrl = "http://localhost:$WebPort"

if (-not (Test-LocalPort -Port $ApiPort)) {
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:ConnectionStrings__PostgreSql = $connectionString
    $env:Authentication__SigningKey = $localEnvironment["Authentication__SigningKey"]

    $apiProcess = Start-Process `
        -FilePath "dotnet.exe" `
        -ArgumentList "run", "--no-build", "--project", "src/ProjetoPizza.Api", "--urls", $apiUrl `
        -WorkingDirectory $workspaceRoot `
        -RedirectStandardOutput $apiOutputPath `
        -RedirectStandardError $apiErrorPath `
        -WindowStyle Hidden `
        -PassThru

    $apiReady = $false
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        try {
            $response = Invoke-WebRequest -Uri "$apiUrl/api/v1/health" -UseBasicParsing -TimeoutSec 1
            if ($response.StatusCode -eq 200) {
                $apiReady = $true
                break
            }
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    }

    if (-not $apiReady) {
        Stop-Process -Id $apiProcess.Id -ErrorAction SilentlyContinue
        throw "A API não ficou saudável. Consulte api-local.stderr.log."
    }
}

if (-not (Test-LocalPort -Port $WebPort)) {
    $env:VITE_API_URL = $apiUrl
    $webProcess = Start-Process `
        -FilePath "npm.cmd" `
        -ArgumentList "run", "dev", "--", "--host", "localhost", "--port", $WebPort `
        -WorkingDirectory $webPath `
        -RedirectStandardOutput $webOutputPath `
        -RedirectStandardError $webErrorPath `
        -WindowStyle Hidden `
        -PassThru

    $webReady = $false
    for ($attempt = 0; $attempt -lt 30; $attempt++) {
        if (Test-LocalPort -Port $WebPort) {
            $webReady = $true
            break
        }
        Start-Sleep -Milliseconds 500
    }

    if (-not $webReady) {
        Stop-Process -Id $webProcess.Id -ErrorAction SilentlyContinue
        throw "O frontend não iniciou. Consulte web-local.stderr.log."
    }
}

Write-Output "API disponível em $apiUrl"
Write-Output "Frontend disponível em $webUrl"
