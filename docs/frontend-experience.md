# Experiência e arquitetura do frontend

## Fundação

O painel administrativo mantém o design do Stitch e usa uma camada de experiência compartilhada:

- **TanStack Query** controla cache, cancelamento, retry, revalidação em foco e estados de carregamento. As páginas não inicializam mais com dados fictícios antes da resposta da API.
- **React Hook Form + Zod** validam os modais de cadastro e edição com mensagens em português, foco no primeiro campo inválido e contratos tipados.
- **Radix UI** fornece os diálogos acessíveis; `Dialog` é usado para formulários e `AlertDialog` para ações de risco.
- **Sonner** apresenta sucesso e falhas tratadas, inclusive detalhes seguros fornecidos por Problem Details.
- **SignalR** notifica alterações administrativas. O cliente invalida o cache e busca a versão atual do servidor sem manter regra de negócio no Hub.
- **View Transitions API** anima mudanças de rota quando suportada. O fallback CSS mantém a navegação funcional e respeita `prefers-reduced-motion`.
- **React Number Format** aplica entrada monetária brasileira consistente (`R$ 1.234,56`) a preços, taxas, caixa e pagamentos sem transformar valores de domínio em texto.

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

O Vitest cobre componentes, apresentações, carrinho isolado por sessão, acessibilidade e schemas. O Playwright administrativo executa os fluxos em Chromium desktop e viewport mobile usando o modo local sem API. Com API e banco iniciados, `npm run test:e2e:client` valida a ativação e o cardápio do tablet real em viewport de iPad.

## Recebimento e auditoria

O modal de pagamento alterna entre recebimento único e divisão por 2 a 50 pessoas. A divisão distribui centavos sem perder ou criar valor, mostra a parcela individual e coleta forma de pagamento, valor recebido, troco e referência de cada pessoa. O envio é único para evitar uma conta parcialmente atualizada.

O histórico administrativo traduz ação, módulo e entidade para português. Para tickets de cozinha, a API projeta o número operacional (`Ticket #1024`) no lugar do GUID, mantendo o identificador técnico no contrato e na exportação CSV.

## Relatórios

A área financeira exporta um arquivo Excel (`.xlsx`) em vez de imprimir a página web. O gerador é carregado somente após a ação do usuário e cria três abas: resumo executivo, pedidos e pagamentos. A planilha preserva valores e datas como tipos nativos, aplica máscaras monetárias, totais, percentuais, cabeçalhos fixos, larguras adequadas e o período selecionado. A geração utiliza `write-excel-file`, escolhida por suportar navegador, múltiplas abas e estilos sem alertas conhecidos no `npm audit`.

As listas operacionais de pedidos, mesas, pagamentos e auditoria são exportadas em PDF tabular, sem capturar a página web. O componente compartilhado inclui identidade da unidade, resumo dos filtros e indicadores, cabeçalho repetido, quebra automática de páginas, rodapé com autoria/data e numeração. A geração utiliza `jsPDF` e `jspdf-autotable`, carregados sob demanda para não aumentar o carregamento inicial das telas.

O indicador de caixa do cabeçalho consome a mesma consulta e a mesma chave de cache da tela de caixa. Assim, ele mostra aberto somente quando existe um turno com status `Open` e permanece sincronizado após fechamento ou atualização em tempo real.

A tela de caixa apresenta a abertura quando não existe turno corrente. O modal acessível seleciona um caixa ativo, recebe o fundo inicial com máscara monetária e mostra sucesso ou erro tratado. A resposta da abertura e o fechamento atualizam imediatamente a chave compartilhada, mantendo página e cabeçalho consistentes sem recarregar o navegador.

## Tablet do cliente

A rota `/mesa` é carregada separadamente do painel e não inicia a conexão SignalR administrativa. A jornada segue as variantes `*_atualizada` do Stitch: ativação, boas-vindas, cardápio, seis etapas de montagem da pizza, carrinho, confirmação, acompanhamento, chamado e solicitação de conta.

O layout preserva os tokens creme e terracota, alvos de toque de pelo menos 52 px, transições com fallback para movimento reduzido, foco contido no montador e mensagens em português. Toasts confirmam mutações; erros de negócio são traduzidos e estados que bloqueiam o pedido, como conta solicitada, aparecem antes da tentativa.

Em tablets com largura de até 900 px, as categorias usam uma barra lateral compacta de ícones. O botão `Categorias` expande a navegação sobre o conteúdo sem alterar a grade, e ela volta ao estado compacto ao selecionar uma categoria, tocar fora ou pressionar `Esc`. Em celulares, a mesma navegação funciona como gaveta lateral, preservando toda a largura do cardápio.

O carrinho fica na sessão do navegador, separado pelo `tableSessionId`, e não atravessa atendimentos. Ao confirmar, a API recalcula preço e disponibilidade. O acompanhamento consulta somente o estado dinâmico a cada ciclo, pausa em aba oculta e trata expiração da sessão; ao concluir o pagamento, limpa o carrinho conforme a configuração e apresenta a tela de agradecimento sem perder abruptamente a leitura final.

Chamados enviados pelo tablet possuem uma fila administrativa dedicada. O cabeçalho monitora novos chamados, respeita a configuração de som e direciona para a tela de aceite/conclusão. A tolerância operacional configurada controla o destaque de atraso.

Uma resposta HTTP 401 dispara o encerramento centralizado da autenticação administrativa, limpa o cache protegido e redireciona imediatamente para o login. O tablet também volta à ativação com mensagem tratada quando seu token expira.

## Tempo real

O endpoint autenticado é:

```text
/hubs/admin
```

O evento `admin:changed` transporta apenas recurso, ação HTTP e horário. Ele não carrega entidades completas nem executa regras; serve para indicar ao cliente que o dado deve ser revalidado.
