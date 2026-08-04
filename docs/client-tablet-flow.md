# Fluxo do tablet da mesa

## Escopo entregue

A aplicação do cliente está disponível em `/mesa` e consome somente HTTP. Ela cobre:

1. ativação única de tablet provisionado e vinculado a uma mesa;
2. espera animada enquanto a mesa está livre e abertura da comanda pelo cliente com quantidade de pessoas;
3. boas-vindas com identificação da mesa e garçom responsável, quando atribuído;
4. cardápio real por categoria, busca e destaques;
5. montagem de pizza em tamanho, quantidade de sabores, sabores, personalização por sabor, ingredientes adicionais com quantidade, borda inteira ou dividida em duas metades e revisão;
6. carrinho com quantidade, remoção, consumo existente e taxa de serviço;
7. envio idempotente do pedido e criação de tickets por estação;
8. acompanhamento do pedido;
9. chamado da equipe com motivo e detalhes;
10. resumo e solicitação da conta, com preferência de divisão persistida para o caixa;
11. agradecimento animado após o pagamento, QR Code de avaliação e retorno automático à espera em 20 segundos.

Os arquivos em `designs` são somente referência. As variantes `*_atualizada` orientam as etapas equivalentes e permanecem inalteradas.

## Limites e segurança

- O dispositivo precisa ser do tipo `CustomerTablet`, estar desbloqueado e vinculado a uma mesa. A ativação não exige atendimento aberto.
- Em `/admin/devices`, o administrador pode cadastrar um tablet ou gerar um novo vínculo para um já existente. A tela apresenta QR Code e a URL completa para abertura manual em aparelhos sem câmera.
- O link leva uma credencial aleatória de provisionamento, válida por 30 minutos e consumida uma única vez. Somente seu hash SHA-256 é persistido; gerar outro link revoga o anterior e nenhuma senha administrativa é incluída na URL.
- A ativação retorna um token opaco; somente o hash SHA-256 é armazenado.
- O token é enviado em `X-Device-Session` e não concede acesso administrativo.
- Uma nova ativação encerra a credencial anterior do mesmo dispositivo. Bloquear, desvincular, trocar a mesa, reprovisionar ou executar logout também a revoga.
- A credencial opaca do aparelho não expira por tempo: permanece válida até revogação explícita. A associação opcional à `TableSession` muda a cada atendimento.
- Preço, disponibilidade, limites de sabores e regras de composição são validados no servidor.
- A borda possui preço inteiro e preço de meia borda por tamanho. Na divisão, o servidor exige dois recheios diferentes e soma os valores das duas metades; a quantidade de sabores da pizza não altera essa regra.
- O ingrediente é habilitado globalmente em `/admin/catalog/ingredients` e pode ser vinculado como padrão a cada sabor em `/admin/catalog/pizza-flavors`. A aba **Complementos** do modal de produto permite que uma pizza sobrescreva essa lista, inclusive com preço e quantidade máxima próprios; uma lista vazia remove todos os complementos daquela pizza.
- Em pizzas de 2, 3 ou mais sabores, o complemento do produto pode ser aplicado a cada sabor selecionado. Quando o produto não possui configuração própria, prevalecem os complementos permitidos pelo sabor. O preço enviado pelo navegador nunca é aceito como autoridade.
- Cada adicional é persistido em `ordering.order_item_modifiers` com nome, quantidade e preços unitário/total em snapshot, preservando o histórico mesmo após mudanças no catálogo.
- O `RequestId` do pedido é reutilizado como identificador idempotente.
- O tablet abre a comanda quando o cliente informa de 1 a 50 pessoas. Só uma comanda ativa pode existir por mesa. Quando configurado, o turno de caixa continua sendo exigido para enviar pedidos.
- Solicitações duplicadas de atendimento com o mesmo motivo são rejeitadas enquanto estiverem pendentes.
- O carrinho é isolado pelo identificador da sessão da mesa e é limpo quando a operação determina a limpeza após o fechamento.
- Após o bootstrap inicial, a atualização periódica usa `/api/v1/client/state`; o catálogo completo não é transferido a cada ciclo.
- A telemetria usa `POST /api/v1/client/telemetry` com a mesma credencial do aparelho. O envio ocorre a cada minuto, ao recuperar visibilidade e em mudanças de bateria ou conectividade; a ausência da Battery Status API resulta em percentual desconhecido.

## Integração operacional

- Chamados aparecem em `/admin/service-calls`, com mesa, motivo, tempo, responsável e ações separadas para assumir e concluir.
- `TableCallToleranceMinutes` destaca chamados atrasados e `TableCallSoundEnabled` controla o aviso sonoro de novos chamados.
- Todo pedido novo enviado pelo tablet gera um evento SignalR com origem `client`. O painel revalida os dados, mostra um aviso e toca o alerta de dois tons em qualquer rota administrativa; o controle de volume no cabeçalho permite liberar e testar o áudio após a primeira interação exigida pelo navegador.
- Quando a mesa escolhe dividir a conta, `RequestedSplitCount` é gravado em `billing.bills` e abre o modal do caixa no modo dividido com a quantidade solicitada.
- A tela final usa `VITE_GOOGLE_REVIEW_URL` no QR Code e `VITE_INSTAGRAM_URL` no acesso social, preservando `VITE_FEEDBACK_URL` e `VITE_SOCIAL_URL` como aliases legados. Depois de 20 segundos, encerra somente o contexto da comanda e retorna à espera sem apagar o vínculo do aparelho.
- O logout explícito fica disponível na espera e exige confirmação local; depois dele, uma nova ativação administrativa é necessária.

## Desenvolvimento local

Com migration e seed aplicados:

```text
DEV-TABLET-002 -> Mesa 2
DEV-TABLET-003 -> Mesa 3
```

Esses códigos existem apenas para desenvolvimento. Se a tela informar que o caixa está fechado, abra um turno na rota administrativa `/admin/cashier`. Se a conta da mesa já tiver sido solicitada, use outra mesa aberta ou conclua o atendimento pelo fluxo administrativo.
