# Experiência e arquitetura do frontend

## Fundação

O painel administrativo mantém o design do Stitch e usa uma camada de experiência compartilhada:

- **TanStack Query** controla cache, cancelamento, retry, revalidação em foco e estados de carregamento. As páginas não inicializam mais com dados fictícios antes da resposta da API.
- **React Hook Form + Zod** validam os modais de cadastro e edição com mensagens em português, foco no primeiro campo inválido e contratos tipados.
- **Radix UI** fornece os diálogos acessíveis; `Dialog` é usado para formulários e `AlertDialog` para ações de risco.
- **Sonner** apresenta sucesso e falhas tratadas, inclusive detalhes seguros fornecidos por Problem Details.
- **SignalR** notifica alterações administrativas com recurso, método e origem. O cliente invalida o cache e busca a versão atual do servidor sem manter regra de negócio no Hub.
- **View Transitions API** anima mudanças de rota quando suportada. O fallback CSS mantém a navegação funcional e respeita `prefers-reduced-motion`.
- **React Number Format** aplica entradas brasileiras consistentes: moeda (`R$ 1.234,56`) em preços, taxas, caixa e pagamentos e celular (`(11) 99999-9999`) nos cadastros de clientes. O telefone segue para a API apenas com dígitos.

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

Os diálogos administrativos compartilham o mesmo sistema de composição: cabeçalho e rodapé fixos, corpo com respiro lateral, grades responsivas, campos com foco visível, switches e checkboxes consistentes e notas contextuais separadas do formulário. A abertura e o fechamento usam transições curtas de opacidade, deslocamento e escala, automaticamente reduzidas quando o sistema solicita menos movimento.

## Testes

```powershell
Set-Location src/ProjetoPizza.Web
npm test
npm run test:coverage
npm run test:e2e
```

O Vitest cobre componentes, apresentações, carrinho isolado por sessão, acessibilidade e schemas. O Playwright administrativo executa os fluxos em Chromium desktop e viewport mobile usando o modo local sem API. Com API e banco iniciados, `npm run test:e2e:client` valida a ativação e o cardápio do tablet real em viewport de iPad.

## Recebimento e auditoria

O modal de pagamento alterna entre recebimento único, divisão por 2 a 50 pessoas e divisão por consumo. A divisão igualitária distribui centavos sem perder ou criar valor; a divisão por consumo atribui cada item da comanda a uma pessoa e calcula sua parte. Ambas coletam forma de pagamento, valor recebido, troco e referência individual e enviam todos os pagamentos em uma única operação.

O histórico administrativo traduz ação, módulo e entidade para português, e a busca reconhece tanto os termos traduzidos quanto os valores técnicos preservados no contrato. A apresentação central também traduz os estados e tipos enumerados do domínio. Para tickets de cozinha, a API projeta o número operacional (`Ticket #1024`) no lugar do GUID, mantendo o identificador técnico no contrato e na exportação CSV.

## Pedido administrativo e impressão térmica

A rota `/admin/orders/new` conduz o atendimento em três blocos: seleção ou cadastro rápido do cliente, escolha entre retirada e entrega e montagem dos itens com o mesmo catálogo/compositor de pizzas do tablet. Em entrega, o endereço é obrigatório e a prévia usa `DefaultDeliveryFee`; preço, disponibilidade, taxa e desconto são confirmados novamente pela API. O identificador da tentativa é mantido durante o envio para impedir pedido duplicado em uma repetição da requisição.

Após a confirmação, a interface mantém a prévia não fiscal de 80 mm. Na listagem de pedidos, o botão `Imprimir` abre o comprovante histórico completo e usa a caixa de impressão do navegador, permitindo conferir o layout e imprimir mesmo sem uma impressora de rede configurada. Nos fluxos com impressora térmica online, comprovante e comanda ainda podem criar trabalhos duráveis e o worker envia ESC/POS pela rede local, com tentativas, erro visível e corte automático quando suportado pelo equipamento.

O detalhe da mesa apresenta cada pedido com data, canal, itens, quantidade, descrição da montagem, observações, preço unitário, subtotal, desconto, serviço e total. Todas as entradas que representam dinheiro no admin — catálogo, estoque, caixa, pedidos, pagamentos, estornos e configurações — usam a máscara brasileira `R$ 1.234,56`; campos de quantidade e percentual continuam numéricos, sem máscara monetária.

## Clientes e navegação administrativa

A rota `/admin/customers` organiza busca, contagem e registros em uma única superfície operacional. Nome, celular formatado, nascimento, fidelidade e situação seguem colunas estáveis no desktop e se reorganizam em blocos legíveis no mobile. A ação `Detalhes` abre uma central responsiva com passaporte do cliente, bilhete de benefícios, validade, equivalente em descontos, valor acumulado, ticket médio e visões alternáveis de movimentações, pedidos e cupons. Ajustes manuais exigem motivo, exibem a projeção do novo saldo e são auditáveis. `/admin/loyalty` reúne as regras globais, edita cupons e apresenta o razão consolidado. Créditos automáticos ocorrem somente em pedidos pagos ou concluídos.

A rota `/admin/reservations` combina agenda e lista de espera. O campo de cliente funciona como combobox acessível, pesquisa por nome ou telefone e preenche telefone e nascimento do cadastro selecionado. Quando a busca não encontra o cliente, a data de nascimento passa a ser obrigatória e a API cria cliente e reserva na mesma transação, evitando registros incompletos. Na acomodação, o operador seleciona uma ou mais mesas livres com capacidade suficiente; a API abre a comanda, vincula as mesas e conclui a recepção atomicamente. Reservas seguem estados pendente, confirmado, recepcionado e concluído, com cancelamento ou ausência; a fila mantém posição, previsão, aviso e acomodação.

A busca do cabeçalho abre `/admin/search`, consulta simultaneamente pedidos, mesas e clientes e agrupa os resultados com destino contextual. O termo aceita número de pedido/mesa, nome, produto, área, estado e telefone. A tela de mesas expõe o acesso direto à gestão estrutural, onde mesas podem ser adicionadas, editadas, desativadas ou excluídas quando ainda não possuem histórico nem tablet vinculado.

A sidebar mantém marca e ação principal no topo, navegação com rolagem independente no centro e saída no rodapé. Essa divisão evita que “Sair” cubra “Configurações” em telas de menor altura e preserva a versão compacta e o drawer mobile.

## Relatórios

A área financeira exporta um arquivo Excel (`.xlsx`) em vez de imprimir a página web. O resumo inclui vendas, recebimento líquido de estornos, CMV calculado pelas baixas automáticas, margem de contribuição e produtividade real das praças. O gerador cria resumo executivo, pedidos e pagamentos, preserva valores e datas nativos e inclui desempenho da produção.

As listas operacionais de pedidos, mesas, pagamentos e auditoria são exportadas em PDF tabular, sem capturar a página web. O componente compartilhado inclui identidade da unidade, resumo dos filtros e indicadores, cabeçalho repetido, quebra automática de páginas, rodapé com autoria/data e numeração. A geração utiliza `jsPDF` e `jspdf-autotable`, carregados sob demanda para não aumentar o carregamento inicial das telas.

O indicador de caixa do cabeçalho consome a mesma consulta e a mesma chave de cache da tela de caixa. Assim, ele mostra aberto somente quando existe um turno com status `Open` e permanece sincronizado após fechamento ou atualização em tempo real. A tela também lista fechamentos anteriores, operador de abertura/fechamento, valores contado/esperado, diferença, observações e movimentos do turno.

O cabeçalho também oferece um teste local do som de notificações. Pedidos criados pelo tablet são distinguidos de transições administrativas pelo campo de origem do evento e disparam um alerta de dois tons em qualquer rota; a primeira interação ou o próprio botão de volume libera o `AudioContext` conforme a política do navegador.

A tela de caixa apresenta a abertura quando não existe turno corrente. O modal acessível seleciona um caixa ativo, recebe o fundo inicial com máscara monetária e mostra sucesso ou erro tratado. A resposta da abertura e o fechamento atualizam imediatamente a chave compartilhada, mantendo página e cabeçalho consistentes sem recarregar o navegador.

O dashboard segue os blocos gerenciais do Stitch: situação detalhada das mesas, Top 5 do dia, receitas por forma de pagamento e alertas vindos dos saldos reais de estoque. Os alertas têm contenção própria e textos quebráveis, sem ultrapassar a superfície em larguras menores. A rota `/admin/inventory` usa uma tabela operacional com rolagem horizontal contida, separa nome, saldo atual e reservado e mantém as ações legíveis; também cadastra custo e saldo, registra ajustes e mantém fichas técnicas por produto ou sabor/tamanho. Ao enviar um pedido, a Application reserva atomicamente os ingredientes; o primeiro início de produção consome a reserva e o cancelamento anterior ao preparo a libera, sempre associado ao item do pedido.

## Tablet do cliente

A rota `/mesa` é carregada separadamente do painel e não inicia a conexão SignalR administrativa. A jornada segue as variantes `*_atualizada` do Stitch: ativação única, espera animada, abertura da comanda com quantidade de pessoas, boas-vindas, cardápio, seis etapas de montagem da pizza, carrinho, confirmação, acompanhamento, chamado, solicitação de conta e agradecimento.

O layout preserva os tokens creme e terracota, alvos de toque de pelo menos 52 px, transições com fallback para movimento reduzido, foco contido no montador e mensagens em português. Toasts confirmam mutações; erros de negócio são traduzidos e estados que bloqueiam o pedido, como conta solicitada, aparecem antes da tentativa.

Em tablets com largura de até 900 px, as categorias usam uma barra lateral compacta de ícones. O botão `Categorias` expande a navegação sobre o conteúdo sem alterar a grade, e ela volta ao estado compacto ao selecionar uma categoria, tocar fora ou pressionar `Esc`. Em celulares, a mesma navegação funciona como gaveta lateral, preservando toda a largura do cardápio.

O carrinho fica em armazenamento local, separado pelo `tableSessionId`, e sobrevive ao fechamento do navegador. Uma tentativa ambígua mantém o mesmo `RequestId`, bloqueia alterações e permite repetir exatamente o mesmo envio sem duplicar o pedido; a limpeza ocorre somente após confirmação. O cliente também permite editar pizzas, repetir consumo anterior por identificadores reais, filtrar sabores, ver prazo estimado, sugestões contextuais e estado do chamado de mesa.

## Delivery externo

`/delivery` oferece cardápio, montagem de pizza, checkout sem adquirência, endereço, taxa calculada pelo servidor, cotação de pontos/cupons e rastreio. Telefone e nascimento protegem a consulta do saldo. O admin despacha somente pedidos prontos, informa o entregador e confirma a entrega. O token público é aleatório/idempotente no cliente e somente seu hash é gravado no banco.

Enquanto autenticado, o tablet publica telemetria a cada 30 segundos e em mudanças relevantes. A leitura de bateria usa a Battery Status API quando disponível; em navegadores ou contextos que não a expõem, a API mantém o percentual como desconhecido e ainda registra presença, conectividade, versão e último contato. O painel administrativo exibe o último heartbeat e só apresenta o dispositivo como online enquanto esse contato tiver até dois minutos.

Chamados enviados pelo tablet possuem uma fila administrativa dedicada. O cabeçalho monitora novos chamados, respeita a configuração de som e direciona para a tela de aceite/conclusão. A tolerância operacional configurada controla o destaque de atraso.

Uma resposta HTTP 401 dispara o encerramento centralizado da autenticação administrativa, limpa o cache protegido e redireciona imediatamente para o login. O tablet volta à ativação com mensagem tratada quando sua credencial é revogada ou encerrada.

## Tempo real

O endpoint autenticado é:

```text
/hubs/admin
```

O evento `admin:changed` transporta apenas recurso, ação HTTP e horário. Ele não carrega entidades completas nem executa regras; serve para indicar ao cliente que o dado deve ser revalidado.
