# ProjetoPizza

Fundação full stack de uma plataforma de gestão para pizzaria, construída como monólito modular em .NET 10, PostgreSQL e React/TypeScript. O projeto traduz os artefatos de `designs` em um domínio consistente, sem assumir que cada tela representa uma tabela ou um microsserviço.

## O que está implementado

- Clean Architecture: Domain, Application, Infrastructure e Api.
- Módulos Core, Identity, Catalog, Inventory, Dining, Ordering, Production, Billing, Cashier, Devices, Notifications e Audit.
- Um `DbContext` PostgreSQL com schemas por módulo, migrations versionadas, índices, FKs e seed idempotente.
- Agregados e invariantes para mesas, sessões, pedidos, pizzas, contas, pagamentos e caixa.
- Endpoints administrativos de leitura e escrita, autenticação Identity/JWT, autorização por claims, rate limit, OpenAPI, health check, Problem Details e CORS.
- Painel React responsivo com todas as telas administrativas inventariadas e jornada do tablet da mesa em `/mesa`.
- Cadastros administrativos em modais acessíveis, feedback por toast, erros HTTP tratados, enums localizados em português e transições com suporte a movimento reduzido.
- Fluxos operacionais para mesas, pedidos, cozinha, catálogo, pagamentos, caixa, dispositivos, usuários, perfis e configurações.
- Jornada do cliente com ativação segura do tablet, cardápio real, montagem de pizza com adicionais por sabor, carrinho, acompanhamento, chamado e solicitação de conta.
- Testes unitários de Domain/Application e teste de integração PostgreSQL preparado com Testcontainers.
- Inventário completo das referências visuais e documentação das decisões.

## Estrutura

```text
src/
  ProjetoPizza.Domain
  ProjetoPizza.Application
  ProjetoPizza.Infrastructure
  ProjetoPizza.Api
  ProjetoPizza.Web
tests/
  ProjetoPizza.Domain.Tests
  ProjetoPizza.Application.Tests
  ProjetoPizza.IntegrationTests
docs/
designs/
```

As dependências seguem `Domain <- Application <- Infrastructure <- Api`. O Web conversa apenas por HTTP.

## Início rápido

Pré-requisitos: .NET SDK 10, Node.js/npm e Docker com Compose.

```powershell
Copy-Item .env.example .env
# Troque as senhas e a chave por valores exclusivamente locais.
docker compose up -d

$env:ConnectionStrings__PostgreSql = "Host=localhost;Port=5432;Database=projeto_pizza;Username=projeto_pizza;Password=<senha-local>"
dotnet tool restore
dotnet run --project src/ProjetoPizza.Api -- --seed
dotnet run --project src/ProjetoPizza.Api --urls http://localhost:5080
```

Em outro terminal:

```powershell
Set-Location src/ProjetoPizza.Web
npm install
npm run dev
```

Abra `http://localhost:5173`. Com `VITE_API_URL` definido, o login e as telas usam exclusivamente a API. Sem essa variável, mocks tipados permitem desenvolvimento visual isolado.

Se Docker não estiver disponível no Windows, o PostgreSQL 17 instalado localmente pode ser iniciado em uma porta isolada:

```powershell
$env:POSTGRES_PASSWORD = "<senha-local>"
.\scripts\start-native-postgres.ps1
$env:ConnectionStrings__PostgreSql = "Host=localhost;Port=55432;Database=projeto_pizza;Username=projeto_pizza;Password=<senha-local>"
$env:Authentication__SigningKey = "<chave-local-com-pelo-menos-32-caracteres>"
$env:DevelopmentSeed__AdminPassword = "<senha-forte-do-admin-local>"
dotnet run --project src/ProjetoPizza.Api -- --seed
```

## Instalação na máquina do cliente

### Topologia recomendada

A instalação local da pizzaria utiliza uma máquina principal como servidor. Essa máquina executa o PostgreSQL, a API e o frontend. Computadores administrativos e tablets acessam o sistema pela rede local.

```text
Tablets e computadores
        |
        | Wi-Fi/LAN
        v
Máquina servidor da pizzaria
  - Frontend: portas 80/443 em produção
  - API: porta 5080
  - PostgreSQL: porta 5432, restrita ao servidor
```

A máquina servidor deve possuir:

- Windows 11 Pro ou Windows Server atualizado;
- processador com pelo menos 4 núcleos;
- 8 GB de RAM, preferencialmente 16 GB;
- SSD com ao menos 20 GB livres;
- IP fixo ou reserva de endereço no roteador;
- nobreak e rotina de backup;
- tablets conectados à mesma rede local.

Não utilize `localhost` nos tablets: esse endereço aponta para o próprio tablet. Use o IP fixo ou nome DNS da máquina servidor, por exemplo `http://192.168.1.20`.

### Pré-requisitos do instalador

Para a instalação piloto:

1. Git;
2. .NET SDK 10;
3. Node.js 22 com npm;
4. Docker com Compose, ou PostgreSQL 17 instalado nativamente.

Para a instalação definitiva, o servidor não precisa manter o SDK nem o Node.js depois que os artefatos forem publicados. Instale o ASP.NET Core Hosting Bundle 10, IIS e o módulo URL Rewrite. O PostgreSQL 17 pode ser nativo ou executado em container com volume persistente.

### 1. Obter o sistema

Abra o PowerShell como administrador:

```powershell
Set-Location C:\
git clone https://github.com/GuilhermeDuarte14511/ProjetoPizza.git
Set-Location C:\ProjetoPizza
git switch main
```

Em atualizações futuras, utilize uma versão marcada ou um commit homologado. Evite instalar diretamente um commit ainda não testado.

### 2. Configurar senhas e endereço do servidor

Crie o arquivo local de ambiente:

```powershell
Copy-Item .env.example .env
notepad .env
```

Substitua todos os valores de desenvolvimento:

- `POSTGRES_PASSWORD`: senha exclusiva do banco;
- `ConnectionStrings__PostgreSql`: a mesma senha na connection string;
- `Authentication__SigningKey`: chave aleatória com no mínimo 32 caracteres;
- `DevelopmentSeed__AdminPassword`: senha temporária e forte do primeiro administrador;
- `VITE_API_URL`: endereço da API usando o IP fixo do servidor.

Exemplo de endereço, sem credenciais:

```text
VITE_API_URL=http://192.168.1.20:5080
```

O arquivo `.env` é ignorado pelo Git. Nunca envie esse arquivo por e-mail, não o adicione ao repositório e limite sua leitura ao usuário responsável pelo serviço.

### 3. Subir o PostgreSQL

Com Docker:

```powershell
docker compose up -d postgres
docker compose ps
```

O status deve ficar `healthy`. Os dados são persistidos no volume `projeto-pizza-postgres-data`; remover o container não remove o volume, mas `docker compose down -v` apaga o banco e não deve ser usado na máquina do cliente.

Se a instalação utilizar PostgreSQL nativo, crie o banco e o usuário definidos na connection string. A porta do banco deve ficar acessível somente na própria máquina servidor.

### 4. Aplicar banco e carga inicial

Carregue os valores definidos no `.env` na sessão atual do PowerShell:

```powershell
$values = @{}
Get-Content .env | ForEach-Object {
    if ($_ -and -not $_.TrimStart().StartsWith("#")) {
        $separator = $_.IndexOf("=")
        if ($separator -gt 0) {
            $values[$_.Substring(0, $separator)] = $_.Substring($separator + 1)
        }
    }
}

$env:ConnectionStrings__PostgreSql = $values["ConnectionStrings__PostgreSql"]
$env:Authentication__SigningKey = $values["Authentication__SigningKey"]
$env:DevelopmentSeed__AdminPassword = $values["DevelopmentSeed__AdminPassword"]
```

Depois execute:

```powershell
dotnet tool restore
dotnet restore ProjetoPizza.sln
dotnet run --project src/ProjetoPizza.Api -- --seed
```

O comando aplica todas as migrations de forma incremental e executa a carga idempotente. Na entrega atual, o seed contém massas de demonstração e é indicado para instalação piloto ou homologação. Antes de uma implantação comercial definitiva, deve ser preparada uma carga inicial específica da unidade, sem pedidos e dispositivos de demonstração.

### 5. Instalação piloto para validação no cliente

Instale as dependências do frontend:

```powershell
Set-Location C:\ProjetoPizza\src\ProjetoPizza.Web
npm ci
Set-Location C:\ProjetoPizza
```

Inicie API e frontend:

```powershell
.\scripts\start-local.ps1 -ApiPort 5080 -WebPort 5173 -DatabasePort 5432
```

O script:

- lê o `.env` sem versionar credenciais;
- inicia a API em todas as interfaces da rede;
- inicia o frontend;
- configura a origem CORS da rede local;
- aguarda o health check antes de concluir;
- exibe os endereços local e do tablet.

Libere as portas somente no perfil de rede privada:

```powershell
New-NetFirewallRule -DisplayName "ProjetoPizza Web" -Direction Inbound -Protocol TCP -LocalPort 5173 -Action Allow -Profile Private
New-NetFirewallRule -DisplayName "ProjetoPizza API" -Direction Inbound -Protocol TCP -LocalPort 5080 -Action Allow -Profile Private
```

Acesse:

```text
Administração: http://IP-DO-SERVIDOR:5173
Tablet:        http://IP-DO-SERVIDOR:5173/mesa
API:           http://IP-DO-SERVIDOR:5080/api/v1/health
```

Esse modo utiliza o servidor de desenvolvimento do Vite. Ele serve para implantação piloto, treinamento e homologação presencial, mas não deve ser a forma definitiva de inicialização automática.

### 6. Publicação definitiva

Na máquina de build ou no servidor durante a instalação:

```powershell
Set-Location C:\ProjetoPizza
dotnet publish src/ProjetoPizza.Api -c Release -o C:\ProjetoPizza\publish\api

Set-Location src\ProjetoPizza.Web
$env:VITE_API_URL = "http://IP-DO-SERVIDOR:5080"
npm ci
npm run build
New-Item -ItemType Directory -Path C:\ProjetoPizza\publish\web -Force | Out-Null
Copy-Item .\dist\* C:\ProjetoPizza\publish\web -Recurse -Force
```

No IIS:

1. crie um Application Pool sem Managed Runtime para a API;
2. publique `C:\ProjetoPizza\publish\api` na porta `5080`;
3. configure as variáveis `ConnectionStrings__PostgreSql`, `Authentication__SigningKey`, `AllowedHosts` e `Cors__AllowedOrigins__0`;
4. proteja a configuração com ACL para que somente administradores e a identidade do Application Pool possam lê-la;
5. publique `C:\ProjetoPizza\publish\web` como site estático nas portas `80` e, preferencialmente, `443`;
6. configure o URL Rewrite do frontend para devolver `index.html` nas rotas da SPA;
7. use certificado HTTPS válido quando a rede ou a política do cliente exigir.

Antes de iniciar a nova versão, carregue na sessão do PowerShell as mesmas variáveis seguras usadas pela API e aplique somente as migrations:

```powershell
C:\ProjetoPizza\publish\api\ProjetoPizza.Api.exe --migrate
```

Não execute `--seed` novamente em uma base comercial sem revisar a carga correspondente. A aplicação do IIS e o site devem iniciar automaticamente com o Windows.

### 7. Vincular os tablets

1. abra o painel administrativo pelo endereço de rede;
2. acesse **Configurações > Tablets**;
3. escolha **Adicionar novo tablet** ou **Vincular**;
4. selecione a mesa;
5. leia o QR Code ou digite no tablet a URL exibida;
6. confirme que o cardápio e a mesa correta foram carregados.

O vínculo é de uso único e expira. O tablet deve permanecer na mesma rede do servidor.

### 8. Verificação pós-instalação

Confirme:

- PostgreSQL saudável e volume persistente;
- `GET /api/v1/health` respondendo HTTP 200;
- login administrativo;
- abertura e fechamento de caixa;
- abertura de mesa;
- ativação do tablet;
- envio de pedido até a cozinha;
- solicitação de conta e pagamento;
- exportação de PDF e Excel;
- acesso após reiniciar a máquina servidor.

### Backup, atualização e recuperação

Antes de cada atualização:

1. faça backup do PostgreSQL com `pg_dump`;
2. copie os diretórios publicados da versão atual;
3. registre o commit ou tag instalado;
4. pare os sites no IIS;
5. publique os novos artefatos;
6. execute `ProjetoPizza.Api.exe --migrate`;
7. inicie os sites e valide o health check.

Mantenha backups fora do disco principal e teste periodicamente a restauração. Em caso de falha do aplicativo, restaure os artefatos anteriores. Não reverta migrations manualmente nem restaure o banco sem avaliar os dados gravados após a atualização.

## Qualidade

```powershell
dotnet restore ProjetoPizza.sln
dotnet build ProjetoPizza.sln --no-restore
dotnet test ProjetoPizza.sln --no-build

Set-Location src/ProjetoPizza.Web
npm run lint
npm test
npm run build
npm run test:e2e
```

## Documentação

- [Inventário dos designs](docs/design-inventory.md)
- [Cobertura das telas administrativas](docs/admin-screen-coverage.md)
- [Arquitetura](docs/architecture.md)
- [Modelo de domínio](docs/domain-model.md)
- [Modelo de banco](docs/database-model.md)
- [Guia de desenvolvimento](docs/development.md)
- [Experiência e arquitetura do frontend](docs/frontend-experience.md)

## Limites desta entrega

Integrações com adquirentes TEF/Pix, impressão física, `pg_dump`/armazenamento de backups e notificações externas dependem da escolha de provedores. A aplicação administrativa não simula sucesso dessas integrações. Consulte [a cobertura administrativa](docs/admin-screen-coverage.md) e as decisões pendentes em `docs/architecture.md`.
