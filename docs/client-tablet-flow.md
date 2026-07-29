# Fluxo do tablet da mesa

## Escopo entregue

A aplicação do cliente está disponível em `/mesa` e consome somente HTTP. Ela cobre:

1. ativação de tablet provisionado e vinculado a uma mesa em atendimento;
2. boas-vindas com identificação da mesa e garçom responsável;
3. cardápio real por categoria, busca e destaques;
4. montagem de pizza em tamanho, quantidade de sabores, sabores, personalização por sabor, ingredientes adicionais com quantidade, borda inteira ou dividida em duas metades e revisão;
5. carrinho com quantidade, remoção, consumo existente e taxa de serviço;
6. envio idempotente do pedido e criação de tickets por estação;
7. acompanhamento do pedido;
8. chamado da equipe com motivo e detalhes;
9. resumo e solicitação da conta, com preferência de divisão persistida para o caixa;
10. agradecimento após o caixa confirmar integralmente o pagamento.

Os arquivos em `designs` são somente referência. As variantes `*_atualizada` orientam as etapas equivalentes e permanecem inalteradas.

## Limites e segurança

- O dispositivo precisa ser do tipo `CustomerTablet`, estar desbloqueado, vinculado a uma mesa e possuir atendimento ativo.
- Em `/admin/devices`, o administrador pode cadastrar um tablet ou gerar um novo vínculo para um já existente. A tela apresenta QR Code e a URL completa para abertura manual em aparelhos sem câmera.
- O link leva uma credencial aleatória de provisionamento, válida por 30 minutos e consumida uma única vez. Somente seu hash SHA-256 é persistido; gerar outro link revoga o anterior e nenhuma senha administrativa é incluída na URL.
- A ativação retorna um token opaco; somente o hash SHA-256 é armazenado.
- O token é enviado em `X-Device-Session` e não concede acesso administrativo.
- Uma nova ativação encerra a sessão anterior do mesmo dispositivo.
- A sessão expira após 12 horas; após o pagamento, o resumo final permanece legível por até duas horas.
- Preço, disponibilidade, limites de sabores e regras de composição são validados no servidor.
- A borda possui preço inteiro e preço de meia borda por tamanho. Na divisão, o servidor exige dois recheios diferentes e soma os valores das duas metades; a quantidade de sabores da pizza não altera essa regra.
- O ingrediente é habilitado globalmente em `/admin/catalog/ingredients` e pode ser vinculado como padrão a cada sabor em `/admin/catalog/pizza-flavors`. A aba **Complementos** do modal de produto permite que uma pizza sobrescreva essa lista, inclusive com preço e quantidade máxima próprios; uma lista vazia remove todos os complementos daquela pizza.
- Em pizzas de 2, 3 ou mais sabores, o complemento do produto pode ser aplicado a cada sabor selecionado. Quando o produto não possui configuração própria, prevalecem os complementos permitidos pelo sabor. O preço enviado pelo navegador nunca é aceito como autoridade.
- Cada adicional é persistido em `ordering.order_item_modifiers` com nome, quantidade e preços unitário/total em snapshot, preservando o histórico mesmo após mudanças no catálogo.
- O `RequestId` do pedido é reutilizado como identificador idempotente.
- A mesa precisa estar aberta. Quando a configuração exigir, também deve existir turno de caixa aberto.
- Solicitações duplicadas de atendimento com o mesmo motivo são rejeitadas enquanto estiverem pendentes.
- O carrinho é isolado pelo identificador da sessão da mesa e é limpo quando a operação determina a limpeza após o fechamento.
- Após o bootstrap inicial, a atualização periódica usa `/api/v1/client/state`; o catálogo completo não é transferido a cada ciclo.

## Integração operacional

- Chamados aparecem em `/admin/service-calls`, com mesa, motivo, tempo, responsável e ações separadas para assumir e concluir.
- `TableCallToleranceMinutes` destaca chamados atrasados e `TableCallSoundEnabled` controla o aviso sonoro de novos chamados.
- Quando a mesa escolhe dividir a conta, `RequestedSplitCount` é gravado em `billing.bills` e abre o modal do caixa no modo dividido com a quantidade solicitada.
- A tela final possui avaliação por link configurável (`VITE_FEEDBACK_URL`), perfil social configurável (`VITE_SOCIAL_URL`), compartilhamento nativo e encerramento explícito da sessão no tablet.

## Desenvolvimento local

Com migration e seed aplicados:

```text
DEV-TABLET-002 -> Mesa 2
DEV-TABLET-003 -> Mesa 3
```

Esses códigos existem apenas para desenvolvimento. Se a tela informar que o caixa está fechado, abra um turno na rota administrativa `/admin/cashier`. Se a conta da mesa já tiver sido solicitada, use outra mesa aberta ou conclua o atendimento pelo fluxo administrativo.
