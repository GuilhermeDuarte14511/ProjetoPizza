[CmdletBinding()]
param(
    [string]$StatePath = (Join-Path $env:ProgramData "ProjetoPizza"),
    [switch]$RevealSecrets,
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$metadataPath = Join-Path $StatePath "installation.json"
$credentialPath = Join-Path $StatePath "installation-secrets.clixml"

if (-not (Test-Path -LiteralPath $metadataPath) -or -not (Test-Path -LiteralPath $credentialPath)) {
    throw "A instalação protegida não foi encontrada em '$StatePath'."
}

$metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
try {
    $credentials = Import-Clixml -LiteralPath $credentialPath
}
catch {
    throw "As credenciais só podem ser abertas pelo mesmo usuário do Windows que realizou a instalação."
}

Write-Host "ProjetoPizza - dados da instalação" -ForegroundColor Cyan
Write-Host ""
Write-Host "Instalado em:    $($metadata.installedAt)"
Write-Host "Sistema:         $($metadata.applicationUrl)"
Write-Host "Tablet:          $($metadata.tabletUrl)"
Write-Host "Health check:    $($metadata.healthUrl)"
Write-Host "Administrador:   $($credentials.InitialAdministrator.UserName)"
Write-Host "Usuário do banco: $($credentials.Database.UserName)"
Write-Host "Pasta do sistema: $($metadata.sourcePath)"
Write-Host "Dados protegidos: $StatePath"

if (-not $RevealSecrets) {
    Write-Host ""
    Write-Host "As senhas permanecem ocultas." -ForegroundColor Yellow
    Write-Host "Para exibi-las, execute novamente com -RevealSecrets."
    exit 0
}

if (-not $Force) {
    Write-Host ""
    Write-Warning "As senhas ficarão visíveis na tela e poderão aparecer no histórico de suporte."
    $confirmation = Read-Host "Digite EXIBIR para continuar"
    if ($confirmation -cne "EXIBIR") {
        Write-Host "Exibição cancelada."
        exit 0
    }
}

Write-Host ""
Write-Host "Senha do banco:        $($credentials.Database.GetNetworkCredential().Password)" -ForegroundColor Yellow
Write-Host "Senha inicial do admin: $($credentials.InitialAdministrator.GetNetworkCredential().Password)" -ForegroundColor Yellow
Write-Host ""
Write-Host "A senha exibida do administrador é a senha inicial. Se ela foi alterada no sistema, consulte a senha atual com o responsável." -ForegroundColor DarkYellow
