# Venda no balcão e impressão térmica

## Objetivo

O fluxo de retirada no balcão registra pedido e pagamento em uma única operação. Depois da confirmação, o operador pode enviar dois documentos independentes para a fila de impressão:

1. **Comprovante do cliente**: contém cliente, itens, observações, subtotal, desconto, total, forma de pagamento, valor recebido e troco. É identificado como **documento sem valor fiscal**.
2. **Comanda da cozinha**: contém número do pedido, itens, personalizações e observações em destaque. Não contém preços nem dados de pagamento.

## Fluxo

1. O operador seleciona cliente, retirada, itens, desconto e observações em `/admin/orders/new`.
2. **Revisar e receber pagamento** abre o modal inspirado no design de registro de pagamento.
3. O servidor recalcula os valores e executa atomicamente pedido, conta de balcão, pagamento, movimento de caixa e tickets de produção.
4. O modal de conclusão libera separadamente o comprovante do cliente e a comanda da cozinha.
5. Ao enviar a comanda, tickets novos são confirmados e o pedido passa para o fluxo da produção.

Fechar o modal de pagamento antes da confirmação não cria pedido ou pagamento parcial.

## API

- `POST /api/v1/admin/counter-orders/checkout`: confirma venda de retirada e pagamento integral.
- `POST /api/v1/admin/orders/{id}/print/customer-receipt`: enfileira o comprovante sem valor fiscal.
- `POST /api/v1/admin/orders/{id}/print/kitchen-command`: confirma os tickets novos e enfileira as comandas de produção.

As rotas exigem as permissões administrativas já aplicadas aos pedidos e operações.

## Modelo

`Billing.Bill` pode ser originada por uma sessão de mesa ou diretamente por um pedido de balcão. A associação ao pedido é exclusiva e persistida por `billing.bills.order_id`. As invariantes de pagamento e troco permanecem nos agregados `Bill` e `Payment`.

## Limites

- Não existe autorização de Pix, cartão ou TEF. Quando configurada, a referência externa apenas registra uma confirmação feita fora do sistema.
- O comprovante térmico não é NFC-e, SAT, CF-e ou documento fiscal.
- A impressão física depende de impressora ESC/POS TCP configurada e online.
