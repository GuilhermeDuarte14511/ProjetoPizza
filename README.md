# ProjetoPizza

Fundação full stack de uma plataforma de gestão para pizzaria, construída como monólito modular em .NET 10, PostgreSQL e React/TypeScript. O projeto traduz os artefatos de `designs` em um domínio consistente, sem assumir que cada tela representa uma tabela ou um microsserviço.

## O que está implementado

- Clean Architecture: Domain, Application, Infrastructure e Api.
- Módulos Core, Identity, Catalog, Inventory, Dining, Ordering, Production, Billing, Cashier, Devices, Notifications e Audit.
- Um `DbContext` PostgreSQL com schema por módulo, migration `InitialCreate`, índices, FKs e seed idempotente.
- Agregados e invariantes para mesas, sessões, pedidos, pizzas, contas, pagamentos e caixa.
- Endpoints administrativos de leitura e escrita, autenticação Identity/JWT, autorização por claims, rate limit, OpenAPI, health check, Problem Details e CORS.
- Painel React responsivo com todas as telas administrativas inventariadas, sem páginas placeholder.
- Cadastros administrativos em modais acessíveis, feedback por toast, erros HTTP tratados, enums localizados em português e transições com suporte a movimento reduzido.
- Fluxos operacionais para mesas, pedidos, cozinha, catálogo, pagamentos, caixa, dispositivos, usuários, perfis e configurações.
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
