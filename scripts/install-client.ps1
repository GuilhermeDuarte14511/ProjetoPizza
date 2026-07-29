[CmdletBinding()]
param(
    [string]$SourcePath,
    [string]$StatePath = (Join-Path $env:ProgramData "ProjetoPizza"),
    [switch]$CheckOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$repositoryUrl = "https://github.com/GuilhermeDuarte14511/ProjetoPizza.git"
$composeProjectName = "projeto-pizza"
$adminEmail = "admin@projetopizza.local"

function Write-Step {
    param([string]$Message)

    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Test-IsAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Refresh-ProcessPath {
    $machinePath = [Environment]::GetEnvironmentVariable("Path", "Machine")
    $userPath = [Environment]::GetEnvironmentVariable("Path", "User")
    $env:Path = "$machinePath;$userPath"
}

function Invoke-ExternalCommand {
    param(
        [Parameter(Mandatory)]
        [string]$FilePath,
        [Parameter(Mandatory)]
        [string[]]$Arguments
    )

    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "O comando '$FilePath $($Arguments -join ' ')' terminou com o código $LASTEXITCODE."
    }
}

function Get-WingetPath {
    $command = Get-Command winget.exe -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $candidate = Join-Path $env:LOCALAPPDATA "Microsoft\WindowsApps\winget.exe"
    if (Test-Path -LiteralPath $candidate) {
        return $candidate
    }

    return $null
}

function Install-WingetPackage {
    param(
        [Parameter(Mandatory)]
        [string]$Id,
        [Parameter(Mandatory)]
        [string]$DisplayName
    )

    $winget = Get-WingetPath
    if (-not $winget) {
        throw "O App Installer/winget não está disponível. Instale 'App Installer' pela Microsoft Store e execute este script novamente."
    }

    Write-Step "Instalando $DisplayName"
    Invoke-ExternalCommand -FilePath $winget -Arguments @(
        "install",
        "--id", $Id,
        "--exact",
        "--silent",
        "--accept-package-agreements",
        "--accept-source-agreements"
    )
    Refresh-ProcessPath
}

function Get-DockerPath {
    $command = Get-Command docker.exe -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $candidate = "C:\Program Files\Docker\Docker\resources\bin\docker.exe"
    if (Test-Path -LiteralPath $candidate) {
        return $candidate
    }

    return $null
}

function Test-DockerEngine {
    param([string]$DockerPath)

    if (-not $DockerPath) {
        return $false
    }

    & $DockerPath info --format "{{.ServerVersion}}" *> $null
    return $LASTEXITCODE -eq 0
}

function Wait-DockerEngine {
    param(
        [Parameter(Mandatory)]
        [string]$DockerPath,
        [int]$TimeoutSeconds = 180
    )

    $desktopPath = "C:\Program Files\Docker\Docker\Docker Desktop.exe"
    if (-not (Test-DockerEngine -DockerPath $DockerPath) -and (Test-Path -LiteralPath $desktopPath)) {
        Write-Step "Iniciando o Docker Desktop"
        Start-Process -FilePath $desktopPath | Out-Null
    }

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-DockerEngine -DockerPath $DockerPath) {
            Write-Progress -Activity "Aguardando Docker Desktop" -Completed
            return
        }

        $remaining = [Math]::Max(0, [int]($deadline - (Get-Date)).TotalSeconds)
        Write-Progress -Activity "Aguardando Docker Desktop" -Status "Restam até $remaining segundos"
        Start-Sleep -Seconds 3
    }

    Write-Progress -Activity "Aguardando Docker Desktop" -Completed
    throw "O Docker Desktop foi instalado, mas o mecanismo ainda não iniciou. Abra o Docker Desktop, conclua o aceite inicial ou reinicie o Windows e execute o instalador novamente."
}

function Ensure-Docker {
    $docker = Get-DockerPath
    if (-not $docker) {
        Install-WingetPackage -Id "Docker.DockerDesktop" -DisplayName "Docker Desktop"
        $docker = Get-DockerPath
    }

    if (-not $docker) {
        throw "O Docker foi instalado, mas o executável ainda não está disponível. Reinicie o Windows e execute o instalador novamente."
    }

    Wait-DockerEngine -DockerPath $docker

    $osType = & $docker info --format "{{.OSType}}"
    if ($LASTEXITCODE -ne 0 -or $osType.Trim() -ne "linux") {
        throw "O ProjetoPizza requer containers Linux. No Docker Desktop, selecione 'Switch to Linux containers' e execute novamente."
    }

    Invoke-ExternalCommand -FilePath $docker -Arguments @("compose", "version")
    return $docker
}

function Ensure-Git {
    $git = Get-Command git.exe -ErrorAction SilentlyContinue
    if (-not $git) {
        Install-WingetPackage -Id "Git.Git" -DisplayName "Git"
        $git = Get-Command git.exe -ErrorAction SilentlyContinue
    }

    if (-not $git) {
        throw "O Git foi instalado, mas ainda não está disponível. Reinicie o PowerShell e execute o instalador novamente."
    }

    return $git.Source
}

function Test-DotNetSdk10 {
    $dotnet = Get-Command dotnet.exe -ErrorAction SilentlyContinue
    if (-not $dotnet) {
        return $false
    }

    $sdks = & $dotnet.Source --list-sdks
    return @($sdks | Where-Object { $_ -match "^10\." }).Count -gt 0
}

function Ensure-DotNetSdk10 {
    if (-not (Test-DotNetSdk10)) {
        Install-WingetPackage -Id "Microsoft.DotNet.SDK.10" -DisplayName ".NET SDK 10"
    }

    if (-not (Test-DotNetSdk10)) {
        throw "O .NET SDK 10 foi instalado, mas ainda não está disponível. Reinicie o PowerShell e execute novamente."
    }
}

function Test-CompatibleNode {
    $node = Get-Command node.exe -ErrorAction SilentlyContinue
    if (-not $node) {
        return $false
    }

    $version = (& $node.Source --version).TrimStart([char]"v")
    $major = ($version -split "\.")[0] -as [int]
    return $major -ge 22
}

function Ensure-CompatibleNode {
    if (-not (Test-CompatibleNode)) {
        Install-WingetPackage -Id "OpenJS.NodeJS.22" -DisplayName "Node.js 22"
    }

    if (-not (Test-CompatibleNode)) {
        throw "O Node.js compatível foi instalado, mas ainda não está disponível. Reinicie o PowerShell e execute novamente."
    }
}

function Enable-ContainerPrerequisites {
    $operatingSystem = Get-CimInstance Win32_OperatingSystem
    if ([int]$operatingSystem.ProductType -ne 1) {
        throw "O instalador automatizado usa Docker Desktop e é suportado em Windows 11 Pro. Para Windows Server, utilize a instalação manual com PostgreSQL e IIS descrita no README."
    }

    $processor = Get-CimInstance Win32_Processor | Select-Object -First 1
    if ($processor -and
        -not $processor.VirtualizationFirmwareEnabled -and
        -not (Get-CimInstance Win32_ComputerSystem).HypervisorPresent) {
        Write-Warning "A virtualização de hardware parece desativada. Ative Intel VT-x/AMD-V na BIOS/UEFI antes de iniciar o Docker Desktop."
    }

    $restartRequired = $false
    foreach ($featureName in @("Microsoft-Windows-Subsystem-Linux", "VirtualMachinePlatform")) {
        $feature = Get-WindowsOptionalFeature -Online -FeatureName $featureName
        if ($feature.State -eq "EnablePending") {
            $restartRequired = $true
        }
        elseif ($feature.State -ne "Enabled") {
            Write-Step "Ativando o componente do Windows: $featureName"
            $result = Enable-WindowsOptionalFeature `
                -Online `
                -FeatureName $featureName `
                -All `
                -NoRestart
            if ($result.RestartNeeded) {
                $restartRequired = $true
            }
        }
    }

    return $restartRequired
}

function Resolve-ProjectSource {
    param(
        [string]$RequestedPath,
        [switch]$CloneIfMissing
    )

    $repositoryCandidate = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
    if (Test-Path -LiteralPath (Join-Path $repositoryCandidate "compose.yaml")) {
        return $repositoryCandidate
    }

    $target = if ([string]::IsNullOrWhiteSpace($RequestedPath)) {
        "C:\ProjetoPizza"
    }
    else {
        [System.IO.Path]::GetFullPath($RequestedPath)
    }

    if (Test-Path -LiteralPath (Join-Path $target "compose.yaml")) {
        return $target
    }

    if (-not $CloneIfMissing) {
        return $target
    }

    if (Test-Path -LiteralPath $target) {
        $entries = @(Get-ChildItem -LiteralPath $target -Force)
        if ($entries.Count -gt 0) {
            throw "A pasta '$target' já existe, não está vazia e não contém o ProjetoPizza."
        }
    }

    $git = Ensure-Git
    Write-Step "Baixando o ProjetoPizza"
    Invoke-ExternalCommand -FilePath $git -Arguments @("clone", "--branch", "main", "--single-branch", $repositoryUrl, $target)
    return $target
}

function Get-DefaultServerAddress {
    $route = Get-NetRoute -DestinationPrefix "0.0.0.0/0" -ErrorAction SilentlyContinue |
        Sort-Object RouteMetric |
        Select-Object -First 1
    if (-not $route) {
        return "localhost"
    }

    $address = Get-NetIPAddress -AddressFamily IPv4 -InterfaceIndex $route.InterfaceIndex -ErrorAction SilentlyContinue |
        Where-Object { $_.IPAddress -notlike "169.254.*" } |
        Select-Object -ExpandProperty IPAddress -First 1

    if ($address) {
        return $address
    }

    return "localhost"
}

function Read-TextValue {
    param(
        [Parameter(Mandatory)]
        [string]$Prompt,
        [string]$DefaultValue,
        [Parameter(Mandatory)]
        [scriptblock]$Validator,
        [Parameter(Mandatory)]
        [string]$ValidationMessage
    )

    while ($true) {
        $label = if ([string]::IsNullOrWhiteSpace($DefaultValue)) {
            $Prompt
        }
        else {
            "$Prompt [$DefaultValue]"
        }
        $value = Read-Host $label
        if ([string]::IsNullOrWhiteSpace($value)) {
            $value = $DefaultValue
        }

        if (& $Validator $value) {
            return $value
        }

        Write-Warning $ValidationMessage
    }
}

function Read-ConfirmedSecret {
    param(
        [Parameter(Mandatory)]
        [string]$Prompt,
        [Parameter(Mandatory)]
        [scriptblock]$Validator,
        [Parameter(Mandatory)]
        [string]$ValidationMessage
    )

    while ($true) {
        $firstSecure = Read-Host $Prompt -AsSecureString
        $secondSecure = Read-Host "Confirme a senha" -AsSecureString
        $first = [Net.NetworkCredential]::new("", $firstSecure).Password
        $second = [Net.NetworkCredential]::new("", $secondSecure).Password

        if ($first -ne $second) {
            Write-Warning "As senhas não são iguais."
            continue
        }

        if (-not (& $Validator $first)) {
            Write-Warning $ValidationMessage
            continue
        }

        return $first
    }
}

function Read-YesNo {
    param(
        [Parameter(Mandatory)]
        [string]$Prompt,
        [bool]$DefaultYes = $true
    )

    $suffix = if ($DefaultYes) { "[S/n]" } else { "[s/N]" }
    while ($true) {
        $answer = (Read-Host "$Prompt $suffix").Trim().ToLowerInvariant()
        if ([string]::IsNullOrWhiteSpace($answer)) {
            return $DefaultYes
        }
        if ($answer -in @("s", "sim", "y", "yes")) {
            return $true
        }
        if ($answer -in @("n", "nao", "não", "no")) {
            return $false
        }

        Write-Warning "Responda S para sim ou N para não."
    }
}

function New-RandomSigningKey {
    $bytes = New-Object byte[] 48
    $generator = [Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $generator.GetBytes($bytes)
        return [Convert]::ToBase64String($bytes)
    }
    finally {
        $generator.Dispose()
    }
}

function ConvertTo-EnvValue {
    param([Parameter(Mandatory)][string]$Value)

    if ($Value.Contains("'") -or $Value.Contains("`r") -or $Value.Contains("`n")) {
        throw "A configuração contém um caractere que não pode ser salvo com segurança."
    }

    return "'$Value'"
}

function Set-PrivateAcl {
    param(
        [Parameter(Mandatory)]
        [string]$Path,
        [switch]$Directory
    )

    $currentSid = [Security.Principal.WindowsIdentity]::GetCurrent().User
    $systemSid = [Security.Principal.SecurityIdentifier]::new("S-1-5-18")
    $administratorsSid = [Security.Principal.SecurityIdentifier]::new("S-1-5-32-544")

    if ($Directory) {
        $security = [Security.AccessControl.DirectorySecurity]::new()
        $inheritance = [Security.AccessControl.InheritanceFlags]"ContainerInherit, ObjectInherit"
    }
    else {
        $security = [Security.AccessControl.FileSecurity]::new()
        $inheritance = [Security.AccessControl.InheritanceFlags]::None
    }

    $security.SetAccessRuleProtection($true, $false)
    foreach ($sid in @($currentSid, $systemSid, $administratorsSid)) {
        $rule = [Security.AccessControl.FileSystemAccessRule]::new(
            $sid,
            [Security.AccessControl.FileSystemRights]::FullControl,
            $inheritance,
            [Security.AccessControl.PropagationFlags]::None,
            [Security.AccessControl.AccessControlType]::Allow)
        [void]$security.AddAccessRule($rule)
    }

    Set-Acl -LiteralPath $Path -AclObject $security
}

function Save-InstallationState {
    param(
        [Parameter(Mandatory)]
        [string]$TargetStatePath,
        [Parameter(Mandatory)]
        [hashtable]$Settings
    )

    if (-not (Test-Path -LiteralPath $TargetStatePath)) {
        New-Item -ItemType Directory -Path $TargetStatePath -Force | Out-Null
    }
    Set-PrivateAcl -Path $TargetStatePath -Directory

    $runtimeEnvironmentPath = Join-Path $TargetStatePath "runtime.env"
    $credentialPath = Join-Path $TargetStatePath "installation-secrets.clixml"
    $metadataPath = Join-Path $TargetStatePath "installation.json"
    $informationPath = Join-Path $TargetStatePath "LEIA-ME.txt"

    $environmentLines = @(
        "POSTGRES_DB=$(ConvertTo-EnvValue $Settings.DatabaseName)",
        "POSTGRES_USER=$(ConvertTo-EnvValue $Settings.DatabaseUser)",
        "POSTGRES_PASSWORD=$(ConvertTo-EnvValue $Settings.DatabasePassword)",
        "POSTGRES_PORT=$(ConvertTo-EnvValue ([string]$Settings.DatabasePort))",
        "WEB_PORT=$(ConvertTo-EnvValue ([string]$Settings.WebPort))",
        "APP_ORIGIN=$(ConvertTo-EnvValue $Settings.ApplicationUrl)",
        "LOCAL_APP_ORIGIN=$(ConvertTo-EnvValue $Settings.LocalApplicationUrl)",
        "LOOPBACK_APP_ORIGIN=$(ConvertTo-EnvValue $Settings.LoopbackApplicationUrl)",
        "Authentication__SigningKey=$(ConvertTo-EnvValue $Settings.SigningKey)",
        "DevelopmentSeed__AdminPassword=$(ConvertTo-EnvValue $Settings.AdminPassword)"
    )
    $utf8WithoutBom = [Text.UTF8Encoding]::new($false)
    [IO.File]::WriteAllText($runtimeEnvironmentPath, ($environmentLines -join [Environment]::NewLine), $utf8WithoutBom)

    $credentialBundle = [PSCustomObject]@{
        SchemaVersion = 1
        Database = [PSCredential]::new(
            $Settings.DatabaseUser,
            (ConvertTo-SecureString $Settings.DatabasePassword -AsPlainText -Force))
        InitialAdministrator = [PSCredential]::new(
            $adminEmail,
            (ConvertTo-SecureString $Settings.AdminPassword -AsPlainText -Force))
        SigningKey = ConvertTo-SecureString $Settings.SigningKey -AsPlainText -Force
        CreatedAt = [DateTimeOffset]::Now
    }
    $credentialBundle | Export-Clixml -LiteralPath $credentialPath -Force

    $metadata = [ordered]@{
        schemaVersion = 1
        installedAt = [DateTimeOffset]::Now.ToString("O")
        sourcePath = $Settings.SourcePath
        statePath = $TargetStatePath
        composeProjectName = $composeProjectName
        serverAddress = $Settings.ServerAddress
        webPort = $Settings.WebPort
        databasePort = $Settings.DatabasePort
        databaseName = $Settings.DatabaseName
        databaseUser = $Settings.DatabaseUser
        applicationUrl = $Settings.ApplicationUrl
        tabletUrl = "$($Settings.ApplicationUrl)/mesa"
        healthUrl = "$($Settings.ApplicationUrl)/backend/api/v1/health"
        initialAdministrator = $adminEmail
        initialDataLoaded = $Settings.LoadInitialData
    }
    [IO.File]::WriteAllText($metadataPath, ($metadata | ConvertTo-Json -Depth 4), $utf8WithoutBom)

    $information = @"
PROJETOPIZZA - DADOS DA INSTALAÇÃO

Sistema:       $($Settings.ApplicationUrl)
Tablet:        $($Settings.ApplicationUrl)/mesa
Saúde da API:  $($Settings.ApplicationUrl)/backend/api/v1/health
Administrador: $adminEmail
Banco:         $($Settings.DatabaseName)
Usuário banco: $($Settings.DatabaseUser)

As senhas NÃO estão gravadas neste texto.
Elas estão criptografadas para o usuário do Windows que realizou a instalação.

Para consultar:
powershell -ExecutionPolicy Bypass -File "$($Settings.SourcePath)\scripts\show-client-configuration.ps1"
"@
    [IO.File]::WriteAllText($informationPath, $information, $utf8WithoutBom)

    foreach ($path in @($runtimeEnvironmentPath, $credentialPath, $metadataPath, $informationPath)) {
        Set-PrivateAcl -Path $path
    }

    return @{
        RuntimeEnvironmentPath = $runtimeEnvironmentPath
        CredentialPath = $credentialPath
        MetadataPath = $metadataPath
    }
}

function Get-ExistingSecrets {
    param([string]$TargetStatePath)

    $credentialPath = Join-Path $TargetStatePath "installation-secrets.clixml"
    if (-not (Test-Path -LiteralPath $credentialPath)) {
        return $null
    }

    try {
        return Import-Clixml -LiteralPath $credentialPath
    }
    catch {
        throw "Já existe uma instalação, mas as credenciais não puderam ser descriptografadas por este usuário do Windows. Entre com o usuário que fez a instalação ou preserve '$credentialPath' e procure o responsável técnico."
    }
}

function Set-PrivateFirewallRule {
    param([int]$Port)

    $displayName = "ProjetoPizza Web ($Port)"
    $existing = Get-NetFirewallRule -DisplayName $displayName -ErrorAction SilentlyContinue
    if ($existing) {
        Set-NetFirewallRule -DisplayName $displayName -Enabled True -Profile Private -Action Allow | Out-Null
        return
    }

    New-NetFirewallRule `
        -DisplayName $displayName `
        -Direction Inbound `
        -Protocol TCP `
        -LocalPort $Port `
        -Action Allow `
        -Profile Private | Out-Null
}

function Register-StartupTask {
    param([string]$ProjectSource)

    $scriptPath = Join-Path $ProjectSource "scripts\start-client.ps1"
    $arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$scriptPath`""
    $action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument $arguments
    $trigger = New-ScheduledTaskTrigger -AtLogOn
    $principal = New-ScheduledTaskPrincipal `
        -UserId ([Security.Principal.WindowsIdentity]::GetCurrent().Name) `
        -LogonType Interactive `
        -RunLevel Highest
    $settings = New-ScheduledTaskSettingsSet `
        -AllowStartIfOnBatteries `
        -DontStopIfGoingOnBatteries `
        -StartWhenAvailable `
        -ExecutionTimeLimit ([TimeSpan]::Zero) `
        -MultipleInstances IgnoreNew

    Register-ScheduledTask `
        -TaskName "ProjetoPizza - Iniciar sistema" `
        -Description "Inicia os containers do ProjetoPizza após o logon do servidor." `
        -Action $action `
        -Trigger $trigger `
        -Principal $principal `
        -Settings $settings `
        -Force | Out-Null
}

function New-ApplicationShortcuts {
    param(
        [string]$ApplicationUrl,
        [string]$TabletUrl
    )

    $desktop = [Environment]::GetFolderPath("CommonDesktopDirectory")
    $utf8WithoutBom = [Text.UTF8Encoding]::new($false)
    [IO.File]::WriteAllText(
        (Join-Path $desktop "ProjetoPizza - Administração.url"),
        "[InternetShortcut]`r`nURL=$ApplicationUrl`r`n",
        $utf8WithoutBom)
    [IO.File]::WriteAllText(
        (Join-Path $desktop "ProjetoPizza - Tablet.url"),
        "[InternetShortcut]`r`nURL=$TabletUrl`r`n",
        $utf8WithoutBom)
}

function Wait-ApplicationHealth {
    param(
        [Parameter(Mandatory)]
        [string]$HealthUrl,
        [int]$TimeoutSeconds = 120
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $lastError = $null
    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-WebRequest -Uri $HealthUrl -UseBasicParsing -TimeoutSec 4
            if ($response.StatusCode -eq 200) {
                Write-Progress -Activity "Validando o ProjetoPizza" -Completed
                return
            }
        }
        catch {
            $lastError = $_.Exception.Message
        }

        Write-Progress -Activity "Validando o ProjetoPizza" -Status "Aguardando health check"
        Start-Sleep -Seconds 3
    }

    Write-Progress -Activity "Validando o ProjetoPizza" -Completed
    throw "O sistema não respondeu ao health check '$HealthUrl'. Último erro: $lastError"
}

if ($env:OS -ne "Windows_NT") {
    throw "Este instalador foi criado para Windows 11 Pro ou Windows Server."
}

if (-not $CheckOnly -and -not (Test-IsAdministrator)) {
    Write-Host "Solicitando permissão de administrador..." -ForegroundColor Yellow
    $arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`" -StatePath `"$StatePath`""
    if (-not [string]::IsNullOrWhiteSpace($SourcePath)) {
        $arguments += " -SourcePath `"$SourcePath`""
    }
    Start-Process -FilePath "powershell.exe" -ArgumentList $arguments -Verb RunAs | Out-Null
    exit 0
}

$resolvedSource = Resolve-ProjectSource -RequestedPath $SourcePath -CloneIfMissing:(-not $CheckOnly)
$requiredFiles = @(
    "compose.yaml",
    "docker\api.Dockerfile",
    "docker\web.Dockerfile",
    "docker\nginx.conf"
)
$missingFiles = @($requiredFiles | Where-Object { -not (Test-Path -LiteralPath (Join-Path $resolvedSource $_)) })

if ($CheckOnly) {
    Write-Host "ProjetoPizza - diagnóstico de instalação" -ForegroundColor Cyan
    Write-Host "Origem: $resolvedSource"
    Write-Host "Administrador: $(Test-IsAdministrator)"
    Write-Host "winget: $(if (Get-WingetPath) { 'disponível' } else { 'ausente' })"
    Write-Host "Docker CLI: $(if (Get-DockerPath) { 'disponível' } else { 'ausente' })"
    Write-Host "Docker Engine: $(if (Test-DockerEngine -DockerPath (Get-DockerPath)) { 'ativo' } else { 'inativo' })"
    Write-Host ".NET SDK 10 local: $(if (Test-DotNetSdk10) { 'disponível' } else { 'opcional/ausente' })"
    Write-Host "Node.js local compatível: $(if (Test-CompatibleNode) { 'disponível' } else { 'opcional/ausente' })"
    Write-Host "Arquivos obrigatórios ausentes: $($missingFiles.Count)"
    exit $(if ($missingFiles.Count -eq 0) { 0 } else { 1 })
}

if ($missingFiles.Count -gt 0) {
    throw "A cópia do projeto está incompleta. Arquivos ausentes: $($missingFiles -join ', ')."
}

Clear-Host
Write-Host "======================================================" -ForegroundColor DarkCyan
Write-Host "       INSTALADOR INTERATIVO - PROJETOPIZZA" -ForegroundColor Cyan
Write-Host "======================================================" -ForegroundColor DarkCyan
Write-Host ""
Write-Host "Este assistente instalará e iniciará PostgreSQL, API e frontend em containers."
Write-Host "Nenhuma senha real será adicionada ao Git."
Write-Host "As credenciais serão protegidas pelo Windows e só poderão ser abertas pelo usuário instalador."

if (Enable-ContainerPrerequisites) {
    Write-Host ""
    Write-Host "Os componentes WSL 2 foram ativados e o Windows precisa ser reiniciado." -ForegroundColor Yellow
    Write-Host "Após reiniciar, execute novamente este mesmo instalador; ele continuará sem recriar os componentes."
    exit 3010
}

$dockerPath = Ensure-Docker
$installDevelopmentTools = Read-YesNo `
    -Prompt "Instalar também .NET SDK 10 e Node.js 22+ no Windows para manutenção local? (não é necessário para os containers)" `
    -DefaultYes $false
if ($installDevelopmentTools) {
    Ensure-DotNetSdk10
    Ensure-CompatibleNode
}

$existingSecrets = Get-ExistingSecrets -TargetStatePath $StatePath
$existingMetadata = $null
$existingMetadataPath = Join-Path $StatePath "installation.json"
if (Test-Path -LiteralPath $existingMetadataPath) {
    $existingMetadata = Get-Content -LiteralPath $existingMetadataPath -Raw | ConvertFrom-Json
}

& $dockerPath volume inspect "projeto-pizza-postgres-data" *> $null
$databaseVolumeExists = $LASTEXITCODE -eq 0
if ($databaseVolumeExists -and -not $existingSecrets) {
    throw "O volume do PostgreSQL já existe, mas o arquivo local de credenciais não foi encontrado. O instalador não substituirá a senha para evitar perda de acesso. Preserve o volume, restaure as credenciais ou faça backup antes de uma reinstalação."
}

$reuseSecrets = $false
if ($existingSecrets) {
    $reuseSecrets = Read-YesNo -Prompt "Foi encontrada uma instalação anterior. Reutilizar as credenciais protegidas?" -DefaultYes $true
    if (-not $reuseSecrets) {
        throw "A troca automatizada das credenciais de um banco existente foi bloqueada para evitar perda de acesso. Faça backup e execute o procedimento de rotação de credenciais antes de reinstalar."
    }
}

$defaultAddress = if ($existingMetadata) { [string]$existingMetadata.serverAddress } else { Get-DefaultServerAddress }
$defaultWebPort = if ($existingMetadata) { [string]$existingMetadata.webPort } else { "8080" }
$defaultDatabasePort = if ($existingMetadata) { [string]$existingMetadata.databasePort } else { "5432" }
$serverAddress = Read-TextValue `
    -Prompt "IP fixo ou nome do servidor na rede" `
    -DefaultValue $defaultAddress `
    -Validator { param($value) $value -match "^[A-Za-z0-9.-]+$" } `
    -ValidationMessage "Informe somente um IPv4 ou nome DNS, sem http://, barras ou espaços."
$webPortText = Read-TextValue `
    -Prompt "Porta do sistema" `
    -DefaultValue $defaultWebPort `
    -Validator { param($value) ($value -as [int]) -and [int]$value -ge 80 -and [int]$value -le 65535 } `
    -ValidationMessage "Informe uma porta entre 80 e 65535."
$databasePortText = Read-TextValue `
    -Prompt "Porta local do PostgreSQL" `
    -DefaultValue $defaultDatabasePort `
    -Validator { param($value) ($value -as [int]) -and [int]$value -ge 1024 -and [int]$value -le 65535 } `
    -ValidationMessage "Informe uma porta entre 1024 e 65535."
if ($reuseSecrets) {
    if (-not $existingMetadata -or [string]::IsNullOrWhiteSpace([string]$existingMetadata.databaseName)) {
        throw "A instalação anterior não contém metadados suficientes para uma reinstalação segura."
    }
    $databaseName = [string]$existingMetadata.databaseName
    $databaseUser = $existingSecrets.Database.UserName
    $databasePassword = $existingSecrets.Database.GetNetworkCredential().Password
    $adminPassword = $existingSecrets.InitialAdministrator.GetNetworkCredential().Password
    $signingKey = [Net.NetworkCredential]::new("", $existingSecrets.SigningKey).Password
    Write-Host "Credenciais existentes carregadas com segurança." -ForegroundColor Green
}
else {
    $databaseName = Read-TextValue `
        -Prompt "Nome do banco" `
        -DefaultValue "projeto_pizza" `
        -Validator { param($value) $value -match "^[a-z][a-z0-9_]{2,62}$" } `
        -ValidationMessage "Use de 3 a 63 letras minúsculas, números ou sublinhados, começando por letra."
    $databaseUser = Read-TextValue `
        -Prompt "Login do banco PostgreSQL" `
        -DefaultValue "projeto_pizza" `
        -Validator { param($value) $value -match "^[a-z][a-z0-9_]{2,30}$" } `
        -ValidationMessage "Use de 3 a 31 letras minúsculas, números ou sublinhados, começando por letra."
    $databasePassword = Read-ConfirmedSecret `
        -Prompt "Senha do banco (mínimo 12 caracteres)" `
        -Validator { param($value) $value -match "^[A-Za-z0-9!@#%_+=.,-]{12,128}$" } `
        -ValidationMessage "Use de 12 a 128 caracteres. Permitidos: letras, números e ! @ # % _ + = . , -"
    $adminPassword = Read-ConfirmedSecret `
        -Prompt "Senha inicial de $adminEmail" `
        -Validator {
            param($value)
            $value -match "^[A-Za-z0-9!@#%_+=.,-]{10,128}$" -and
            $value -cmatch "[A-Z]" -and
            $value -cmatch "[a-z]" -and
            $value -match "\d" -and
            $value -match "[^A-Za-z0-9]"
        } `
        -ValidationMessage "Use de 10 a 128 caracteres, com maiúscula, minúscula, número e um dos símbolos ! @ # % _ + = . , -"
    $signingKey = New-RandomSigningKey
}

$loadInitialData = Read-YesNo `
    -Prompt "Aplicar a carga inicial idempotente (necessária no primeiro uso e contém dados demonstrativos)?" `
    -DefaultYes $true
$registerStartup = Read-YesNo `
    -Prompt "Iniciar o ProjetoPizza automaticamente no logon deste usuário?" `
    -DefaultYes $true

$webPort = [int]$webPortText
$databasePort = [int]$databasePortText
$applicationUrl = if ($webPort -eq 80) { "http://${serverAddress}" } else { "http://${serverAddress}:$webPort" }
$localhostApplicationUrl = if ($webPort -eq 80) { "http://localhost" } else { "http://localhost:$webPort" }
$loopbackApplicationUrl = if ($webPort -eq 80) { "http://127.0.0.1" } else { "http://127.0.0.1:$webPort" }
$healthUrl = "$loopbackApplicationUrl/backend/api/v1/health"

Write-Host ""
Write-Host "Resumo da instalação" -ForegroundColor Cyan
Write-Host "  Sistema:        $applicationUrl"
Write-Host "  Tablet:         $applicationUrl/mesa"
Write-Host "  Banco:          $databaseName"
Write-Host "  Usuário banco:  $databaseUser"
Write-Host "  Senhas:         protegidas e não exibidas"
if (-not (Read-YesNo -Prompt "Confirmar e iniciar a instalação?" -DefaultYes $true)) {
    Write-Host "Instalação cancelada. Nenhuma configuração foi alterada."
    exit 0
}

Write-Step "Salvando a configuração local protegida"
$state = Save-InstallationState -TargetStatePath $StatePath -Settings @{
    SourcePath = $resolvedSource
    ServerAddress = $serverAddress
    WebPort = $webPort
    DatabasePort = $databasePort
    DatabaseName = $databaseName
    DatabaseUser = $databaseUser
    DatabasePassword = $databasePassword
    AdminPassword = $adminPassword
    SigningKey = $signingKey
    ApplicationUrl = $applicationUrl
    LocalApplicationUrl = $localhostApplicationUrl
    LoopbackApplicationUrl = $loopbackApplicationUrl
    LoadInitialData = $loadInitialData
}

$composeFile = Join-Path $resolvedSource "compose.yaml"
$composeArguments = @(
    "compose",
    "--project-name", $composeProjectName,
    "--env-file", $state.RuntimeEnvironmentPath,
    "--file", $composeFile
)

Write-Step "Subindo o PostgreSQL"
Invoke-ExternalCommand -FilePath $dockerPath -Arguments ($composeArguments + @("up", "-d", "postgres"))

Write-Step "Construindo a API e o frontend"
Invoke-ExternalCommand -FilePath $dockerPath -Arguments ($composeArguments + @("--profile", "client", "build", "api", "web"))

Write-Step "Aplicando migrations e carga inicial"
$databaseCommand = if ($loadInitialData) { "--seed" } else { "--migrate" }
Invoke-ExternalCommand -FilePath $dockerPath -Arguments ($composeArguments + @("--profile", "client", "run", "--rm", "api", $databaseCommand))

Write-Step "Iniciando o sistema"
Invoke-ExternalCommand -FilePath $dockerPath -Arguments ($composeArguments + @("--profile", "client", "up", "-d"))

Write-Step "Configurando acesso na rede privada"
Set-PrivateFirewallRule -Port $webPort
New-ApplicationShortcuts -ApplicationUrl $applicationUrl -TabletUrl "$applicationUrl/mesa"
if ($registerStartup) {
    Register-StartupTask -ProjectSource $resolvedSource
}

Write-Step "Executando diagnóstico final"
Wait-ApplicationHealth -HealthUrl $healthUrl
Invoke-ExternalCommand -FilePath $dockerPath -Arguments ($composeArguments + @("--profile", "client", "ps"))

$databasePassword = $null
$adminPassword = $null
$signingKey = $null

Write-Host ""
Write-Host "======================================================" -ForegroundColor Green
Write-Host "           INSTALAÇÃO CONCLUÍDA COM SUCESSO" -ForegroundColor Green
Write-Host "======================================================" -ForegroundColor Green
Write-Host "Administração: $applicationUrl"
Write-Host "Tablet:        $applicationUrl/mesa"
Write-Host "Login inicial: $adminEmail"
Write-Host "Dados locais:  $StatePath"
Write-Host ""
Write-Host "Para consultar os dados protegidos:"
Write-Host "powershell -ExecutionPolicy Bypass -File `"$resolvedSource\scripts\show-client-configuration.ps1`""
Write-Host ""
Write-Host "Troque a senha inicial do administrador após o primeiro acesso." -ForegroundColor Yellow
