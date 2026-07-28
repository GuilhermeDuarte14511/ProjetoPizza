# ProjetoPizza

- Arquitetura: monólito modular em Clean Architecture (`Domain <- Application <- Infrastructure <- Api`; o `Web` consome somente HTTP).
- Módulos: Core, Identity, Catalog, Inventory, Dining, Ordering, Production, Billing, Cashier, Devices, Notifications e Audit.
- Antes de alterar uma tela, analise `C:\teste\designs`; variantes `*_atualizada` têm precedência. Nunca altere os arquivos originais do Stitch.
- Código e nomes de domínio em inglês; documentação e textos da interface em português. Preserve invariantes no Domain e configure persistência apenas na Infrastructure.
- Não adicione Repository genérico, Unit of Work, MediatR, AutoMapper, service locator ou dependências circulares.
- Build e testes: `dotnet restore`, `dotnet build`, `dotnet test`.
- Frontend: `npm install`, `npm run lint`, `npm run build` em `src/ProjetoPizza.Web`.
- Banco: `docker compose up -d`; migration/seed: `dotnet run --project src/ProjetoPizza.Api -- --seed`.
- Atualize a documentação quando o modelo, API, banco ou fluxo visual mudar.
- Execute as validações proporcionais à mudança antes de concluir.
- Não implemente funcionalidades fora do escopo solicitado e nunca registre credenciais reais.
