# Desenvolvimento

## Pré-requisitos

- .NET SDK 10, respeitando o `global.json`
- Node.js 22 ou compatível com Vite 8
- npm
- Docker com Compose, ou PostgreSQL 17 acessível localmente

## Configuração local

No PowerShell, a partir da raiz:

```powershell
Copy-Item .env.example .env
```

Altere somente o arquivo `.env`, que está ignorado pelo Git. Use uma senha local e nunca versione credenciais. Suba o banco:

```powershell
docker compose up -d
docker compose ps
```

Exporte a connection string para a API na sessão atual:

```powershell
$env:ConnectionStrings__PostgreSql = "Host=localhost;Port=5432;Database=projeto_pizza;Username=projeto_pizza;Password=<senha-local>"
$env:Authentication__SigningKey = "<chave-local-com-pelo-menos-32-caracteres>"
$env:DevelopmentSeed__AdminPassword = "<senha-forte-do-admin-local>"
```

Restaure ferramentas, aplique a migration e execute o seed:

```powershell
dotnet tool restore
dotnet run --project src/ProjetoPizza.Api -- --seed
```

Sem Docker, mas com PostgreSQL 17 instalado no Windows, use o cluster isolado do workspace:

```powershell
$env:POSTGRES_PASSWORD = "<senha-local>"
.\scripts\start-native-postgres.ps1
$env:ConnectionStrings__PostgreSql = "Host=localhost;Port=55432;Database=projeto_pizza;Username=projeto_pizza;Password=<senha-local>"
dotnet run --project src/ProjetoPizza.Api -- --seed
```

Para encerrá-lo:

```powershell
.\scripts\stop-native-postgres.ps1
```

## Executar

API:

```powershell
dotnet run --project src/ProjetoPizza.Api --urls http://localhost:5080
```

Web, em outro terminal:

```powershell
Set-Location src/ProjetoPizza.Web
npm install
npm run dev
```

A interface usa `VITE_API_URL` quando definido. Nesse modo, falhas de autenticação removem a sessão e o acesso administrativo exige JWT. Sem essa variável, as páginas usam mocks tipados e centralizados sem iniciar requisições de rede, permitindo trabalho visual isolado.

O seed cria o usuário `admin@projetopizza.local`. A senha é obrigatoriamente lida de `DevelopmentSeed__AdminPassword`; nenhuma senha padrão fica no código.

O tablet do cliente fica em `http://localhost:5173/mesa`. No seed de desenvolvimento, `DEV-TABLET-002` vincula a Mesa 2 e `DEV-TABLET-003` vincula a Mesa 3. A mesa precisa ter atendimento aberto e, por padrão, pedidos exigem um turno de caixa aberto. Esses códigos são massa local e não devem ser reutilizados em produção.

O fluxo preferencial está em `/admin/devices`: use **Adicionar novo tablet** ou **Vincular**, selecione a mesa e abra no aparelho o QR Code ou a URL exibida. O link expira em 30 minutos, funciona uma única vez e exige que a mesa esteja aberta. Para testar em outro aparelho da rede, abra o painel pelo IP da máquina (por exemplo, `http://192.168.x.x:5173`) antes de gerar o link; `localhost` aponta para o próprio tablet.

## Rotas úteis

- `GET /api/v1/system/info`
- `GET /api/v1/health`
- `GET /openapi/v1.json`
- `GET /api/v1/admin/dashboard`
- `GET /api/v1/admin/tables`
- `GET /api/v1/admin/tables/{id}`
- `GET /api/v1/admin/categories`
- `GET /api/v1/admin/products`
- `GET /api/v1/admin/pizza-sizes`
- `GET /api/v1/admin/pizza-flavors`
- `GET /api/v1/admin/service-calls`
- `GET /api/v1/admin/kitchen/tickets`
- `POST /api/v1/auth/login`
- `GET|POST|PUT /api/v1/admin/...` para os casos de uso descritos em `docs/admin-screen-coverage.md`, incluindo catálogo de sabores
- `POST /api/v1/client/sessions` para ativar um tablet provisionado
- `POST /api/v1/admin/devices/tablets` para cadastrar e provisionar um tablet
- `POST /api/v1/admin/devices/{id}/provision` para vincular novamente e renovar o link
- `GET /api/v1/client/bootstrap`
- `POST /api/v1/client/orders`
- `POST /api/v1/client/service-calls`
- `POST /api/v1/client/bill-requests`

Os endpoints administrativos exigem bearer token. `AdminAccess`/`AdminWrite` e `OperationsAccess`/`OperationsWrite` verificam claims específicas; o login possui limite de tentativas por IP.
Após a ativação, os endpoints do cliente exigem o token opaco no cabeçalho `X-Device-Session`. A ativação também possui limite de tentativas por IP.

## Validação

Backend:

```powershell
dotnet restore ProjetoPizza.sln
dotnet build ProjetoPizza.sln --no-restore
dotnet test ProjetoPizza.sln --no-build
dotnet format ProjetoPizza.sln --verify-no-changes --no-restore
dotnet list ProjetoPizza.sln package --vulnerable --include-transitive
```

Frontend:

```powershell
Set-Location src/ProjetoPizza.Web
npm install
npm run lint
npm test
npm run build
npm run test:e2e
npm audit
```

O teste de integração com Testcontainers depende de um daemon Docker e é habilitado explicitamente:

```powershell
$env:RUN_DOCKER_TESTS = "1"
dotnet test tests/ProjetoPizza.IntegrationTests
```

Sem essa variável, o cenário é reportado como ignorado. A migration e o seed também podem ser validados no cluster nativo descrito acima.

Para o E2E do tablet com API e banco reais, permita a origem e o host isolados usados pelo Playwright antes de iniciar a API:

```powershell
$env:AllowedHosts = "localhost;127.0.0.1"
$env:Cors__AllowedOrigins__0 = "http://127.0.0.1:4175"

Set-Location src/ProjetoPizza.Web
npm run test:e2e:client
```

## Convenções

- Domain não referencia EF Core, HTTP nem detalhes de infraestrutura.
- Application depende apenas de Domain e define portas específicas.
- Infrastructure implementa persistência e integrações.
- Api é a composition root e converte falhas para Problem Details.
- Código e conceitos de domínio usam inglês; textos de interface e documentação usam português.
- Novas invariantes pertencem ao agregado quando não exigem I/O; regras com leitura externa são coordenadas por casos de uso.
- Não introduza Repository genérico, Unit of Work duplicado, MediatR ou AutoMapper sem uma necessidade comprovada.
- Variantes `*_atualizada` em `designs` têm precedência; os artefatos originais nunca devem ser alterados.
