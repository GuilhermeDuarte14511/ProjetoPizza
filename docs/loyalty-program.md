# Programa de fidelidade

O programa é configurado por unidade em `/admin/loyalty`. O administrador define taxa de acúmulo, valor de resgate, mínimo, percentual máximo do pedido e validade. A mesma tela cria cupons com período, pedido mínimo, valor máximo e limite total de usos, além de apresentar o razão imutável de pontos.

## Regras de domínio

- Pontos são creditados somente após pagamento no balcão ou conclusão do pedido. Criar ou aceitar um pedido não gera saldo.
- Resgates exigem cliente verificado, saldo suficiente, mínimo configurado e respeito ao percentual máximo do valor elegível.
- O saldo possui validade única por conta: um novo crédito renova a validade do saldo restante. A expiração é registrada quando a conta é consultada ou usada.
- Cancelar um pedido ainda não concluído restaura os pontos resgatados e libera o uso do cupom na mesma transação.
- Cupons e pontos são calculados pela API. Valores estimados pelo Web nunca são aceitos como autoridade.
- Saldos existentes antes da migration recebem validade inicial de 365 dias e são registrados como saldo de abertura no primeiro acesso ao razão.
- Ajustes administrativos aceitam crédito ou débito, nunca permitem saldo negativo e exigem uma justificativa entre 5 e 160 caracteres. O motivo permanece no razão e a ação também é registrada na auditoria.

## Canais

- Admin/balcão: o operador seleciona o cliente e pode combinar desconto manual, um cupom e pontos.
- Central do cliente: em `/admin/customers`, `Detalhes` abre o perfil, o equivalente monetário dos pontos, validade, ticket médio, pedidos recentes, campanhas de cupom e até 100 movimentações. O equivalente é apenas uma estimativa de desconto, não um saldo sacável.
- Delivery: telefone e nascimento verificam o cadastro; uma cotação autoritativa deve ser aplicada antes da confirmação quando houver benefício.
- Tablet: a identificação é opcional. Para usar pontos, telefone e nascimento precisam corresponder a um cliente ativo da unidade.

## Persistência

`customers.loyalty_settings` guarda a política por unidade, `customers.promotion_coupons` guarda campanhas e contador de uso, e `customers.loyalty_transactions` registra créditos, resgates, restaurações, expirações, saldo de abertura e ajustes manuais. O pedido preserva código/identificador do cupom, pontos usados e os descontos manual, promocional e de fidelidade como snapshots.
