# Cobertura das telas administrativas

Todas as referências administrativas inventariadas possuem rota e página concretas no React. Nenhuma rota utiliza página placeholder. A tabela abaixo separa a cobertura da tela das integrações externas que não pertencem ao processo local.

| Tela | Rota | Operações conectadas |
|---|---|---|
| Login | `/login` | autenticação Identity, JWT, bloqueio e rate limit |
| Visão geral | `/admin/dashboard` | indicadores no fuso da unidade, status detalhado das mesas, pedidos recentes, Top 5, receitas por pagamento, estoque baixo e atualização |
| Mapa de mesas | `/admin/tables` | busca, filtros, estado derivado, abertura de mesa, acesso à gestão estrutural e PDF tabular |
| Detalhe da comanda | `/admin/tables/:id` | pedidos com itens, montagem, observações, valores unitários e totais, garçom responsável, união e transferência de mesa, solicitação de conta, pagamento único e divisão por pessoas ou consumo |
| Reservas e espera | `/admin/reservations` | busca de cliente por nome, criação atômica de cliente, seleção de uma ou mais mesas livres e abertura da comanda na recepção, agenda, confirmação, conclusão, cancelamento, fila, previsão e aviso |
| Busca global | `/admin/search` | consulta conjunta de pedidos, mesas e clientes por número, nome, produto, área, estado ou telefone, com destinos contextuais |
| Pedidos | `/admin/orders` | busca, canal, PDF, transições, despacho de delivery, entregador, confirmação de entrega, checkout de balcão, prévia e impressão pelo navegador do comprovante histórico sem valor fiscal e filas térmicas independentes |
| Novo pedido | `/admin/orders/new` | atendimento presencial ou por telefone, retirada/entrega, cardápio, montagem de pizza, adicionais, observações, desconto, taxa e impressão térmica |
| Clientes | `/admin/customers` | busca, cadastro e edição; central detalhada com perfil, validade e equivalente dos pontos, ticket médio, pedidos, cupons, razão e ajuste manual justificado/auditável |
| Fidelidade | `/admin/loyalty` | regras por unidade, indicadores, cupons com período/limites e razão imutável de pontos |
| Cozinha | `/admin/kitchen` | fila por etapa e praça, tempo vivo, meta de preparo, alerta de SLA, tela cheia, atualização automática e transições de produção |
| Produtos | `/admin/catalog/products` | consulta, busca, cadastro, descrição, tempo de preparo, upload JPEG/PNG/WebP e complementos específicos por pizza |
| Categorias | `/admin/catalog/categories` | consulta, cadastro e edição |
| Sabores | `/admin/catalog/pizza-flavors` | consulta, busca, cadastro, edição, imagem, tipo e disponibilidade |
| Bordas | `/admin/catalog/crusts` | consulta, cadastro, edição e preços de borda inteira/meia borda por tamanho |
| Tamanhos e regras | `/admin/catalog/pizza-sizes` e `/admin/settings/pizza-rules` | tamanhos, limites, política de preço e regras de composição |
| Configuração geral | `/admin/settings/general` | identificação e contato da unidade |
| Configuração operacional | `/admin/settings/operation` | taxa, tolerância, sons e permissões operacionais |
| Estrutura operacional | `/admin/settings/structure` | áreas, mesas, caixas, formas de pagamento, estações de produção e tipos de chamado; mesas permitem inclusão, edição, desativação e exclusão segura com auditoria |
| Impressoras | `/admin/settings/printers` | cadastro ESC/POS TCP, host/porta, papel 58/80 mm, teste físico, fila durável, tentativas e estado do equipamento |
| Backup e sistema | `/admin/settings/backup` | `pg_dump` físico manual, rotina automática, retenção, histórico, download e snapshot JSON auxiliar |
| Caixa | `/admin/cashier` | abertura com caixa e fundo inicial, conferência, suprimento/sangria, fechamento, histórico por turno, operadores, diferenças, auditoria e indicador global sincronizado |
| Estoque | `/admin/inventory` | tabela responsiva de itens, custo unitário, saldo e reservado, alertas, ajustes e fichas técnicas; reserva multicanal no envio, consumo ao iniciar produção e liberação no cancelamento anterior |
| Pagamentos | `/admin/payments` e modal da comanda | consulta, PDF tabular, recebimento, divisão por pessoas/consumo e estorno total ou parcial com motivo |
| Relatórios financeiros | `/admin/reports` | período, vendas, CMV, margem de contribuição, produtividade por praça, canais, métodos e Excel gerencial |
| Tablets | `/admin/devices` | cadastro, vínculo com mesa, QR Code/URL temporários, status, bateria, rede e bloqueio |
| Usuários e permissões | `/admin/users` e `/admin/roles` | cadastro/edição de usuários com telefone preservado, perfis, claims e auditoria de alterações de acesso |
| Auditoria | `/admin/audit` | consulta e busca em português, tradução centralizada de módulo/ação/entidade e PDF tabular |

## Limites explícitos

- A fila física envia ESC/POS por TCP e executa corte automático em equipamentos compatíveis; USB/spooler Windows ainda dependem do modelo escolhido.
- NFC-e permanece desabilitada até a configuração fiscal, credenciamento, certificado, CSC e homologação da UF; consulte `docs/fiscal-readiness.md`.
- O backup físico fica no volume/diretório configurado no servidor; a política operacional ainda deve copiar arquivos para outro disco ou armazenamento protegido e testar restaurações.
- Pix, TEF e cartões registram a referência da transação, mas a autorização depende do provedor escolhido.
- A fidelidade credita apenas pedidos pagos/concluídos, permite resgate configurável e cupons promocionais em todos os canais; níveis e campanhas automáticas de aniversário continuam como evolução futura.
- Reservas e lista de espera organizam o fluxo interno do salão; confirmações por WhatsApp/SMS dependem do provedor de mensageria escolhido.
- A jornada do tablet do cliente está disponível separadamente em `/mesa`; consulte `docs/client-tablet-flow.md`.
