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

## Instalação automatizada na máquina do cliente

O modo recomendado usa o instalador interativo `scripts/install-client.ps1`. Ele foi pensado para a pessoa responsável pela implantação executar uma vez na máquina que será o servidor da pizzaria. O script prepara PostgreSQL, API e frontend em containers, aplica o banco, valida a saúde do sistema e configura a inicialização automática.

O fluxo completo é:

```text
Baixar o instalador
        |
        v
Validar Windows, WSL 2, virtualização e Docker
        |
        v
Perguntar configurações e credenciais
        |
        v
Salvar configuração local protegida
        |
        v
Construir e iniciar PostgreSQL + API + Web
        |
        v
Aplicar migrations/seed e executar health check
        |
        v
Exibir URLs e criar atalhos
```

Na máquina do cliente não é necessário instalar manualmente Node.js, npm ou o SDK .NET. Eles já existem nas imagens usadas para construir a aplicação. A instalação local dessas ferramentas é opcional e serve somente para manutenção ou desenvolvimento.

### O que será executado

| Componente | Função | Acesso |
|---|---|---|
| PostgreSQL 17 | Armazena dados, usuários, pedidos e configurações | Somente no servidor, via `127.0.0.1` |
| API .NET 10 | Executa regras de negócio, autenticação, migrations e integrações internas | Somente pela rede privada dos containers |
| Nginx + React | Entrega o painel e a interface dos tablets e encaminha `/backend` para a API | Porta escolhida para a rede local, padrão `8080` |
| Docker Desktop | Mantém os três containers e seus reinícios | Executado na máquina servidor |

Os dados do PostgreSQL ficam no volume persistente `projeto-pizza-postgres-data`. Reconstruir ou atualizar os containers não apaga esse volume.

### O que a máquina precisa ter

- Windows 11 Pro 64 bits atualizado;
- usuário com permissão de administrador;
- virtualização Intel VT-x ou AMD-V habilitada na BIOS/UEFI;
- conexão com a internet durante a instalação;
- 8 GB de RAM, preferencialmente 16 GB;
- SSD com pelo menos 20 GB livres;
- rede configurada como **Privada** no Windows;
- IP fixo ou reserva de IP no roteador para a máquina servidor.

Antes de começar:

1. confirme que o Windows está atualizado;
2. configure a rede como **Privada**;
3. reserve o IP do servidor no roteador;
4. tenha em mãos o login e a senha que serão usados no banco;
5. defina uma senha forte para o administrador inicial;
6. feche programas que estejam usando as portas `8080` ou `5432`, quando possível.

O instalador usa o `winget` para baixar:

- Git, caso o projeto ainda não esteja na máquina;
- Docker Desktop;
- opcionalmente, .NET SDK 10 e Node.js 22+ para manutenção local.

Depois, o Docker baixa as imagens oficiais PostgreSQL 17, .NET 10, Node.js 22 e Nginx utilizadas na construção e execução dos containers.

Node.js e o SDK .NET são utilizados apenas dentro dos containers de build. As senhas nunca são enviadas ao GitHub.

### Instalação mais simples, em uma máquina nova

1. Abra o menu Iniciar.
2. Procure **PowerShell**.
3. Clique com o botão direito e escolha **Executar como administrador**.
4. Execute:

```powershell
$installer = Join-Path $env:TEMP "install-projeto-pizza.ps1"
Invoke-WebRequest `
  -Uri "https://raw.githubusercontent.com/GuilhermeDuarte14511/ProjetoPizza/main/scripts/install-client.ps1" `
  -OutFile $installer
Unblock-File $installer
powershell -ExecutionPolicy Bypass -File $installer
```

O próprio script baixa o repositório em `C:\ProjetoPizza`. Se os componentes WSL 2 precisarem ser ativados, o instalador solicitará que o Windows seja reiniciado. Depois do reinício, execute os quatro comandos acima novamente; a instalação continuará de forma idempotente.

### Perguntas apresentadas pelo instalador

No primeiro uso, o assistente pergunta nesta ordem:

1. se também deve instalar .NET SDK 10 e Node.js 22+ no Windows;
2. IP fixo ou nome da máquina servidor;
3. porta pública do sistema, padrão `8080`;
4. nome do banco, padrão `projeto_pizza`;
5. login do banco;
6. senha do banco e confirmação;
7. senha inicial de `admin@projetopizza.local` e confirmação;
8. autorização para executar a carga inicial;
9. autorização para iniciar o sistema automaticamente no logon.

A porta local do PostgreSQL não é perguntada normalmente. O instalador tenta usar `5432`; somente quando ela já estiver ocupada solicita uma porta alternativa, sugerindo `55432`. Em uma atualização, a porta já registrada é preservada automaticamente.

A senha do banco aceita entre 12 e 128 caracteres usando letras, números e os símbolos informados pelo assistente. A senha administrativa deve possuir maiúscula, minúscula, número e símbolo.

As perguntas com `[S/n]` assumem **Sim** ao pressionar Enter. Perguntas com `[s/N]` assumem **Não**. As senhas são digitadas de forma oculta e precisam ser confirmadas.

Exemplo de uma primeira instalação:

```text
Instalar também .NET SDK 10 e Node.js 22+ no Windows? [s/N]: N
IP fixo ou nome do servidor na rede [192.168.1.20]:
Porta do sistema [8080]:
Porta local do PostgreSQL: 5432 (disponível)
Nome do banco [projeto_pizza]:
Login do banco PostgreSQL [projeto_pizza]:
Senha do banco: ************
Confirme a senha: ************
Senha inicial de admin@projetopizza.local: ************
Confirme a senha: ************
Aplicar a carga inicial idempotente? [S/n]: S
Iniciar o ProjetoPizza automaticamente no logon? [S/n]: S
Confirmar e iniciar a instalação? [S/n]: S
```

Pressionar Enter nos campos que possuem valor entre colchetes aceita o valor sugerido. Antes de criar a configuração ou iniciar containers, o instalador mostra um resumo final e solicita confirmação.

### O que o instalador executa

Na ordem:

1. solicita elevação administrativa;
2. verifica Windows, virtualização, WSL 2, `winget` e Docker;
3. instala e inicia o Docker Desktop quando necessário;
4. oferece a instalação local do .NET SDK 10 e Node.js 22+ para máquinas de manutenção;
5. coleta e valida os dados sem exibir as senhas;
6. cria a configuração local com ACL restrita;
7. inicia o PostgreSQL com volume persistente;
8. constrói API e frontend;
9. aplica migrations e a carga inicial idempotente;
10. inicia API, frontend e banco com política de reinício;
11. libera somente a porta web no perfil de rede **Privada**;
12. cria atalhos de administração e tablet na área de trabalho pública;
13. registra a inicialização após o logon, quando autorizada;
14. chama o health check e só conclui se o sistema estiver saudável.

O PostgreSQL é publicado somente em `127.0.0.1`; tablets não conseguem acessar o banco diretamente. O Nginx recebe as conexões da rede e encaminha `/backend` para a API dentro da rede privada dos containers.

### Reinício solicitado durante a instalação

Na primeira execução, o Windows pode precisar ativar WSL 2 e `VirtualMachinePlatform`. Nesse caso:

1. o instalador para antes de perguntar ou salvar senhas;
2. uma mensagem solicita a reinicialização;
3. reinicie o Windows;
4. abra novamente o PowerShell como administrador;
5. execute outra vez o mesmo comando de instalação.

O script verifica novamente os componentes já instalados e continua do ponto operacional necessário. Não é preciso remover Docker, WSL ou a pasta do projeto.

Se o Docker Desktop abrir uma tela de termos ou configuração inicial, conclua essa etapa. O instalador espera o mecanismo por até três minutos e exige que ele esteja usando containers Linux.

### O que acontece quando o instalador é executado novamente

O script é idempotente para manutenção e atualização:

- reutiliza o repositório existente;
- detecta o arquivo de instalação anterior;
- pergunta se deve reutilizar as credenciais protegidas;
- preserva o nome, usuário, senha e volume do banco;
- preserva a porta anterior quando ela continua utilizável;
- reaplica migrations incrementalmente;
- reconstrói API e frontend;
- executa o seed idempotente somente quando autorizado;
- atualiza atalhos, firewall e tarefa de inicialização.

O instalador não troca automaticamente a senha de um banco existente. Se o volume existir e as credenciais locais tiverem sido perdidas, ele interrompe para evitar tornar o banco inacessível.

### Onde ficam os dados e as senhas

Os arquivos operacionais ficam fora do repositório:

```text
C:\ProgramData\ProjetoPizza\
  installation.json
  installation-secrets.clixml
  LEIA-ME.txt
  runtime.env
```

- `LEIA-ME.txt` guarda URLs e logins, mas não contém senhas;
- `installation-secrets.clixml` guarda as senhas criptografadas pelo DPAPI do Windows;
- `runtime.env` contém as variáveis necessárias aos containers e possui ACL restrita;
- somente o mesmo usuário do Windows que instalou, `SYSTEM` e administradores locais possuem acesso.

Para consultar os dados sem revelar as senhas:

```powershell
C:\ProjetoPizza\scripts\show-client-configuration.ps1
```

Para revelar as senhas de forma consciente:

```powershell
C:\ProjetoPizza\scripts\show-client-configuration.ps1 -RevealSecrets
```

O comando exige a confirmação `EXIBIR`. A senha administrativa registrada é a senha inicial; se ela for trocada pelo sistema, o arquivo não passa a conhecer a nova senha.

### Endereços após a instalação

Considerando o servidor `192.168.1.20` e a porta `8080`:

```text
Administração: http://192.168.1.20:8080
Tablet:        http://192.168.1.20:8080/mesa
Health check:  http://192.168.1.20:8080/backend/api/v1/health
```

Nos tablets, nunca use `localhost`, pois ele aponta para o próprio tablet.

Ao concluir corretamente, o PowerShell mostra:

```text
INSTALAÇÃO CONCLUÍDA COM SUCESSO
Administração: http://192.168.1.20:8080
Tablet:        http://192.168.1.20:8080/mesa
Login inicial: admin@projetopizza.local
Dados locais:  C:\ProgramData\ProjetoPizza
```

O sucesso só é exibido depois que `GET /backend/api/v1/health` responde HTTP 200. Depois do primeiro login, troque a senha inicial do administrador.

### Instalação a partir de uma cópia já clonada

Abra o PowerShell como administrador na raiz do projeto:

```powershell
Set-Location C:\ProjetoPizza
.\scripts\install-client.ps1
```

Para conferir os pré-requisitos sem instalar ou alterar nada:

```powershell
.\scripts\install-client.ps1 -CheckOnly
```

### Iniciar, parar e diagnosticar

Iniciar novamente:

```powershell
C:\ProjetoPizza\scripts\start-client.ps1
```

Parar os aplicativos sem apagar o banco:

```powershell
$state = "C:\ProgramData\ProjetoPizza"
docker compose `
  --project-name projeto-pizza `
  --env-file "$state\runtime.env" `
  --file "C:\ProjetoPizza\compose.yaml" `
  --profile client `
  stop
```

Ver containers e logs:

```powershell
$state = "C:\ProgramData\ProjetoPizza"
docker compose --project-name projeto-pizza --env-file "$state\runtime.env" --file "C:\ProjetoPizza\compose.yaml" --profile client ps
docker compose --project-name projeto-pizza --env-file "$state\runtime.env" --file "C:\ProjetoPizza\compose.yaml" logs --tail 200
```

Nunca execute `docker compose down -v` na máquina do cliente: a opção `-v` remove o volume do PostgreSQL.

### Atualizar uma instalação

Faça backup antes:

```powershell
C:\ProjetoPizza\scripts\backup-client.ps1
```

O backup é salvo por padrão em `C:\ProgramData\ProjetoPizza\backups`, no formato próprio do `pg_restore`, e o script mostra o SHA-256 para conferência. Copie o arquivo para outro disco ou armazenamento protegido.

Depois:

```powershell
Set-Location C:\ProjetoPizza
git pull --ff-only origin main
.\scripts\install-client.ps1
```

Quando detectar a instalação anterior, o assistente oferece reutilizar as credenciais criptografadas. O banco não é recriado, as migrations são incrementais e os containers são reconstruídos.

Se o volume do banco existir e o arquivo de credenciais tiver sido perdido, o instalador interrompe sem trocar a senha. Essa proteção evita tornar a base existente inacessível.

### Dados iniciais

A carga atual é idempotente e cria o administrador, unidade, catálogo e dados necessários para testar os fluxos. Ela ainda inclui massas demonstrativas. Antes de uma operação comercial definitiva, revise e remova pedidos ou dispositivos demonstrativos que não representem a unidade.

### Problemas comuns

| Mensagem ou situação | Causa provável | Como resolver |
|---|---|---|
| Virtualização parece desativada | Intel VT-x ou AMD-V está desabilitado | Ative a virtualização na BIOS/UEFI e execute novamente |
| Windows precisa ser reiniciado | WSL 2 ou `VirtualMachinePlatform` foi ativado | Reinicie e execute o mesmo instalador novamente |
| `winget` não está disponível | App Installer ausente ou desatualizado | Instale **App Installer** pela Microsoft Store |
| Docker não iniciou em três minutos | Primeiro aceite pendente, reinício necessário ou engine incorreta | Abra Docker Desktop, conclua o assistente e selecione containers Linux |
| Porta `5432` em uso | Outro PostgreSQL ou processo já usa a porta | Informe a porta alternativa sugerida pelo instalador |
| Porta web `8080` em uso | Outro programa está publicado nessa porta | Execute novamente e informe, por exemplo, `8081` |
| Health check falhou | API, banco ou proxy não iniciou corretamente | Consulte `docker compose ... ps` e `docker compose ... logs` conforme os comandos abaixo |
| Credenciais não puderam ser descriptografadas | Outro usuário do Windows executou a reinstalação | Entre com o mesmo usuário que realizou a instalação original |
| Volume existe, mas as credenciais foram perdidas | Arquivo protegido foi removido | Não apague o volume; restaure as credenciais ou faça backup antes de qualquer reinstalação |
| Tablet não abre o sistema | Tablet fora da rede ou endereço `localhost` utilizado | Conecte-o à mesma rede e use o IP fixo do servidor |

Para conferir rapidamente o estado:

```powershell
C:\ProjetoPizza\scripts\install-client.ps1 -CheckOnly
C:\ProjetoPizza\scripts\start-client.ps1
```

Se precisar encaminhar informações ao suporte, envie as mensagens de erro e os logs, mas nunca envie `runtime.env`, `installation-secrets.clixml` ou senhas.

## Instalação manual para desenvolvimento ou contingência

> Esta alternativa exige .NET, Node.js e configuração manual. Para uma máquina Windows 11 da pizzaria, prefira o instalador automatizado acima. O fluxo com IIS permanece documentado para ambientes administrados que não permitem Docker Desktop.

Não execute a instalação manual e a automatizada ao mesmo tempo usando as mesmas portas.

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
