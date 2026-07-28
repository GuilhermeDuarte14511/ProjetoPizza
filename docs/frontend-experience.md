# Experiência e arquitetura do frontend

## Fundação

O painel administrativo mantém o design do Stitch e usa uma camada de experiência compartilhada:

- **TanStack Query** controla cache, cancelamento, retry, revalidação em foco e estados de carregamento. As páginas não inicializam mais com dados fictícios antes da resposta da API.
- **React Hook Form + Zod** validam os modais de cadastro e edição com mensagens em português, foco no primeiro campo inválido e contratos tipados.
- **Radix UI** fornece os diálogos acessíveis; `Dialog` é usado para formulários e `AlertDialog` para ações de risco.
- **Sonner** apresenta sucesso e falhas tratadas, inclusive detalhes seguros fornecidos por Problem Details.
- **SignalR** notifica alterações administrativas. O cliente invalida o cache e busca a versão atual do servidor sem manter regra de negócio no Hub.
- **View Transitions API** anima mudanças de rota quando suportada. O fallback CSS mantém a navegação funcional e respeita `prefers-reduced-motion`.

## Estados de interface

Toda rota administrativa possui:

1. skeleton durante a primeira leitura;
2. tela de erro isolada com ação de tentar novamente;
3. revalidação silenciosa ao recuperar foco ou receber evento SignalR;
4. aviso de conexão offline/reconexão;
5. toast para o resultado das mutações.

As ações de escrita são exibidas somente quando a sessão contém `admin:write` ou `operations:write`. A API continua sendo a autoridade final e valida as mesmas permissões.

## Acessibilidade

- link para pular a navegação;
- foco no conteúdo principal e título atualizado ao mudar de rota;
- `aria-current` no menu e semântica de abas/filtros;
- erros associados visualmente aos campos;
- modais com foco gerenciado pelo Radix UI;
- movimento reduzido de acordo com a preferência do sistema;
- teste automatizado básico com axe.

## Testes

```powershell
Set-Location src/ProjetoPizza.Web
npm test
npm run test:coverage
npm run test:e2e
```

O Vitest cobre componentes, apresentações e schemas. O Playwright executa o fluxo de formulário em Chromium desktop e viewport mobile usando o modo local sem API.

## Tempo real

O endpoint autenticado é:

```text
/hubs/admin
```

O evento `admin:changed` transporta apenas recurso, ação HTTP e horário. Ele não carrega entidades completas nem executa regras; serve para indicar ao cliente que o dado deve ser revalidado.
