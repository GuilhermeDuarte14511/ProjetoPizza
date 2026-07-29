# Cobertura das telas administrativas

Todas as referências administrativas inventariadas possuem rota e página concretas no React. Nenhuma rota utiliza página placeholder. A tabela abaixo separa a cobertura da tela das integrações externas que não pertencem ao processo local.

| Tela | Rota | Operações conectadas |
|---|---|---|
| Login | `/login` | autenticação Identity, JWT, bloqueio e rate limit |
| Visão geral | `/admin/dashboard` | indicadores, pedidos recentes e atualização |
| Mapa de mesas | `/admin/tables` | busca, filtros, estado derivado, abertura de mesa e PDF tabular |
| Detalhe da comanda | `/admin/tables/:id` | pedidos, solicitação de conta e pagamento |
| Pedidos | `/admin/orders` | busca, canal, PDF tabular e transições de status |
| Cozinha | `/admin/kitchen` | fila por etapa, atualização e transições de produção |
| Produtos | `/admin/catalog/products` | consulta, busca, cadastro, edição e complementos específicos por pizza |
| Categorias | `/admin/catalog/categories` | consulta, cadastro e edição |
| Sabores | `/admin/catalog/pizza-flavors` | consulta, busca, cadastro, edição, tipo e disponibilidade |
| Bordas | `/admin/catalog/crusts` | consulta, cadastro, edição e preços de borda inteira/meia borda por tamanho |
| Tamanhos e regras | `/admin/catalog/pizza-sizes` e `/admin/settings/pizza-rules` | tamanhos, limites, política de preço e regras de composição |
| Configuração geral | `/admin/settings/general` | identificação e contato da unidade |
| Configuração operacional | `/admin/settings/operation` | taxa, tolerância, sons e permissões operacionais |
| Impressoras | `/admin/settings/printers` | consulta e atualização do dispositivo lógico |
| Backup e sistema | `/admin/settings/backup` | snapshot administrativo exportado em JSON |
| Caixa | `/admin/cashier` | abertura com caixa e fundo inicial, conferência, suprimento/sangria, fechamento, auditoria e indicador global sincronizado |
| Pagamentos | `/admin/payments` e modal da comanda | consulta, PDF tabular e registro de recebimento |
| Relatórios financeiros | `/admin/reports` | período, indicadores, canais, métodos e Excel gerencial com resumo, pedidos e pagamentos |
| Tablets | `/admin/devices` | cadastro, vínculo com mesa, QR Code/URL temporários, status, bateria, rede e bloqueio |
| Usuários e permissões | `/admin/users` e `/admin/roles` | cadastro/edição de usuários, perfis e claims |
| Auditoria | `/admin/audit` | consulta, busca e PDF tabular |

## Limites explícitos

- Impressoras são dispositivos lógicos; teste de spooler e impressão física exigem o driver e o equipamento.
- O snapshot de sistema não substitui um backup físico do PostgreSQL com `pg_dump`.
- Pix, TEF e cartões registram a referência da transação, mas a autorização depende do provedor escolhido.
- Criação completa de um novo pedido, divisão de conta e troca de garçom são fluxos de negócio próprios e não aparecem como ações simuladas nesta entrega.
- A jornada do tablet do cliente está disponível separadamente em `/mesa`; consulte `docs/client-tablet-flow.md`.
