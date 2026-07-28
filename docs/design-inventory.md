# Inventário dos designs

Fonte analisada: `C:\teste\designs`. Foram lidos 40 arquivos HTML, 39 arquivos `screen.png` e os guias `forno_27_professional_admin/DESIGN.md` e `forno_27_guest_interface/DESIGN.md`. Os arquivos originais do Stitch permanecem inalterados.

## Sistema visual consolidado

- Administrativo: Inter; sidebar grafite `#252B33` com 260 px; canvas `#F5F6F8`; cards brancos; ação terracota entre `#A83300` e `#CC4916`; bordas `#E4E7EC`; raios de 8 a 16 px; topbar de 64 px.
- Tablet: fundo creme próximo de `#FFF8F3`; terracota como ação; cards brancos e alvos de toque mínimos de 52 px; composição fixa originalmente pensada para 1280 × 800.
- Estados: verde para sucesso/livre, âmbar para atenção/conta, vermelho para chamado/erro e azul para informação/pagamento.
- Componentes recorrentes: menu lateral, topbar, busca, cards métricos, tabelas, abas, badges, toggles, modais e formulários.

## Telas administrativas

| Tela | Arquivo | Finalidade | Componentes e ações | Entidades relacionadas | Observações |
|---|---|---|---|---|---|
| Login | `designs/login_forno_27/code.html` | Autenticar equipe | formulário, usuário, senha, manter conectado, recuperar senha, status do servidor | IdentityUser, Employee | Duplicada pelo HTML raiz `forno_27_pizza_management_system`; esta pasta possui captura válida. |
| Visão geral | `designs/vis_o_geral_forno_27/code.html` | Dashboard operacional | métricas, seletor de período, pedidos, status de mesas, alertas, formas de pagamento | Order, TableSession, ServiceCall, Payment | Exibe 22 mesas, enquanto o mapa e o requisito consolidado usam 32. |
| Mapa de mesas | `designs/mapa_de_mesas_forno_27/code.html` | Operar salão | filtros, busca, cards de mesa, abrir mesa, painel lateral de detalhes | RestaurantTable, TableSession, Order, ServiceCall, Bill | Estados visuais devem ser calculados, não persistidos. |
| Detalhe da comanda | `designs/detalhe_da_comanda_forno_27/code.html` | Consultar atendimento | cabeçalho da comanda, pedidos, histórico, resumo, registrar pagamento | TableSession, Order, OrderItem, Bill, Payment | Mistura termos “mesa” e “comanda”; o domínio usa `TableSession`. |
| Cozinha | `designs/cozinha_forno_27/code.html` | Kanban de produção | colunas novos/confirmados/em preparo, cards, cronômetro, confirmar/iniciar/finalizar | KitchenTicket, KitchenTicketItem, ProductionStation, OrderItem | Captura priorizada para a página administrativa inicial. |
| Pedidos delivery/retirada | `designs/gest_o_de_pedidos_delivery_retirada_forno_27/code.html` | Operar canais externos | abas, filtros, cards, imprimir, avançar status | Order, OrderItem, SalesChannel | Valores divergem de outras telas e não foram copiados para o seed. |
| Cardápio | `designs/card_pio_forno_27/code.html` | Gerenciar catálogo | abas, filtros, tabela, status, adicionar/editar/excluir | Category, Product, ProductVariant | “Excluir” visual não implica exclusão física de referências transacionais. |
| Configurações gerais | `designs/configura_es_gerais_forno_27/code.html` | Dados da unidade | formulário, upload de logo, descartar/salvar | RestaurantUnit | CNPJ e contatos aparecem como dados de exemplo. |
| Configurações de operação | `designs/configura_es_opera_o_forno_27/code.html` | Regras do atendimento | toggles, taxas, sons, tolerância, salvar | OperationSettings | Corresponde diretamente às configurações de domínio. |
| Regras de pizzas | `designs/configura_es_regras_de_pizzas_forno_27/code.html` | Configurar composição | política de preço, limite global, toggles, tabela de tamanhos | PizzaSettings, PizzaSize | Algumas telas do tablet citam “massa”; o modelo solicitado usa borda nesta etapa. |
| Impressoras | `designs/configura_es_impressoras_forno_27/code.html` | Gerenciar impressão | cards de impressoras, conexão, estação, teste | ProductionStation, Device | Implementada como gerenciamento de dispositivo lógico; impressão física depende do spooler. |
| Backup e sistema | `designs/configura_es_backup_sistema_forno_27/code.html` | Operação técnica | backup automático/manual, histórico, informações do sistema | AuditLog | A tela exporta snapshot administrativo; backup físico exige `pg_dump` e armazenamento. |
| Fechamento de caixa | `designs/fechamento_de_caixa_forno_27/code.html` | Fechar turno | conferência, valores, diferença, observações | CashShift, CashMovement, Payment | `screen.png` está corrompido pelo Stitch; análise baseada no HTML. |
| Registrar pagamento | `designs/registrar_pagamento_modal_forno_27/code.html` | Receber conta | modal, métodos, valor recebido, troco, atalhos, confirmar | Payment, PaymentMethod, Bill, CashShift | `screen.png` está corrompido; o HTML define o modal. |
| Relatórios financeiros | `designs/relat_rios_financeiros_forno_27/code.html` | Analisar resultado | período, métricas, gráficos, exportar | Order, Payment, CashMovement | Exportação implementada como Excel gerencial com resumo executivo e abas detalhadas. |
| Tablets | `designs/gerenciamento_de_tablets_forno_27/code.html` | Monitorar dispositivos | indicadores, busca, tabela, bateria/rede, vincular | Device, DeviceSession, RestaurantTable | Tela conectada a status, vínculo e bloqueio de `Device`. |
| Usuários e permissões | `designs/usu_rios_e_permiss_es_forno_27/code.html` | Gerenciar acesso | métricas, perfis, matriz, usuários, convites | IdentityUser, IdentityRole, Employee | Autenticação, usuários, perfis e claims administrativas estão conectados ao Identity. |
| Auditoria | `designs/auditoria_e_hist_rico_de_logs_forno_27/code.html` | Rastrear ações | filtros, tabela, gravidade, exportar/imprimir | AuditLog, Employee | Logs são imutáveis por intenção. |

## Telas do tablet/cliente

| Tela | Arquivo | Finalidade | Componentes e ações | Entidades relacionadas | Observações |
|---|---|---|---|---|---|
| Boas-vindas da mesa | `designs/boas_vindas_mesa_12_forno_27/code.html` | Iniciar jornada | foto, identificação da mesa, ver cardápio, ajuda | DeviceSession, TableSession, RestaurantTable | Interface do cliente não foi implementada nesta entrega. |
| Início do cardápio | `designs/card_pio_in_cio_forno_27/code.html` | Explorar catálogo | categorias, busca, carrinho, destaques, montar pizza | Category, Product, Order | Base para futura aplicação do tablet. |
| Lista de pizzas | `designs/listagem_de_pizzas_forno_27/code.html` | Escolher produto | filtros, ordenação, cards, indisponível, escolher opções | Product, PizzaFlavor, PizzaFlavorPrice | Preços “a partir de” são apenas referência visual. |
| Quantidade de sabores | `designs/montar_pizza_quantidade_de_sabores_forno_27/code.html` | Definir composição | progresso, opções 1/2/3, voltar/continuar | PizzaSize, OrderItemPizza | Duplicada; variante atualizada tem cabeçalho e progresso mais consistentes. |
| Quantidade de sabores atualizada | `designs/montar_pizza_quantidade_de_sabores_forno_27_atualizada/code.html` | Versão preferencial da etapa | cabeçalho compacto, cards de seleção, progresso | PizzaSize, OrderItemPizza | Referência principal para essa etapa. |
| Escolher tamanho | `designs/montar_pizza_escolher_tamanho_forno_27/code.html` | Selecionar tamanho | etapas, cards Broto/Média/Grande/Família | PizzaSize | Duplicada pela variante atualizada. |
| Escolher tamanho atualizada | `designs/montar_pizza_escolher_tamanho_forno_27_atualizada/code.html` | Versão preferencial | topbar, progresso, quatro cards, continuar | PizzaSize | Confirma limites 1, 2, 3 e 3 sabores. |
| Escolher sabores | `designs/montar_pizza_escolher_sabores_forno_27/code.html` | Compor pizza | diagrama de partes, filtros, cards, indisponível, avançar | PizzaFlavor, PizzaFlavorPrice, OrderItemPizzaFlavor | Duplicada pela atualizada. |
| Escolher sabores atualizada | `designs/montar_pizza_escolher_sabores_forno_27_atualizada/code.html` | Versão preferencial | layout em duas áreas, preço adicional, premium, esgotado | PizzaFlavor, PizzaFlavorPrice, OrderItemPizzaFlavor | Referência principal; exige snapshots e composição normalizada. |
| Personalizar sabores | `designs/montar_pizza_personalizar_sabores_forno_27/code.html` | Remover/adicionar ingredientes | abas por metade, toggles, imagem, pular/confirmar | Ingredient, PizzaFlavorIngredient, OrderItemModifier | Visualmente quase idêntica à atualizada. |
| Personalizar sabores atualizada | `designs/montar_pizza_personalizar_sabores_forno_27_atualizada/code.html` | Versão preferencial | seleção por sabor, toggles, progresso | Ingredient, OrderItemModifier | Referência principal. |
| Escolher borda | `designs/montar_pizza_escolher_borda_forno_27/code.html` | Escolher recheio | cards com imagem/preço, subtotal, revisar | PizzaCrust, PizzaCrustPrice | Quase idêntica à atualizada. |
| Escolher borda atualizada | `designs/montar_pizza_escolher_borda_forno_27_atualizada/code.html` | Versão preferencial | seleção, preço adicional, subtotal | PizzaCrust, PizzaCrustPrice | Referência principal. |
| Revisão da pizza | `designs/montar_pizza_revis_o_forno_27/code.html` | Conferir configuração | resumo, editar, observações, quantidade, adicionar | OrderItemPizza, OrderItemPizzaFlavor, OrderItemModifier | Duplicada pela atualizada. |
| Revisão da pizza atualizada | `designs/montar_pizza_revis_o_forno_27_atualizada/code.html` | Versão preferencial | imagem, resumo lateral, etapas e total | OrderItem, OrderItemPizza | Referência principal. |
| Carrinho | `designs/carrinho_de_pedidos_forno_27/code.html` | Revisar pedido | itens, quantidade, remover, resumo, confirmar | Order, OrderItem | Duplicada pela atualizada. |
| Carrinho atualizado | `designs/carrinho_de_pedidos_forno_27_atualizada/code.html` | Versão preferencial | resumo fixo, taxa, total e confirmação | Order, OrderItem | Referência principal. |
| Meus pedidos | `designs/meus_pedidos_forno_27/code.html` | Acompanhar produção | cards por pedido, timeline, total consumido | Order, OrderItem, KitchenTicket | Status do cliente é projeção do fluxo produtivo. |
| Chamar garçom | `designs/chamar_gar_om_forno_27/code.html` | Solicitar atendimento | grade de motivos, detalhes, enviar | ServiceCallType, ServiceCall | Motivos orientaram o seed. |
| Solicitar conta atualizada | `designs/solicitar_a_conta_forno_27_atualizada/code.html` | Encerrar consumo | itens, subtotal, taxa, pagar junto/dividir, solicitar | Bill, BillItem, BillSplit, TableSession | “Requested” aparece como estado de Bill; sessão usa `BillRequested`. |
| Agradecimento | `designs/agradecimento_forno_27/code.html` | Finalizar jornada | mensagem, nota fiscal, QR code, avaliação | Bill, Payment, DeviceSession | Pós-pagamento fora do administrativo inicial. |

## Arquivo agregado

| Tela | Arquivo | Finalidade | Componentes e ações | Entidades relacionadas | Observações |
|---|---|---|---|---|---|
| Login agregado | `designs/forno_27_pizza_management_system/code.html` | Exportação única do sistema | mesmos componentes do login | IdentityUser, Employee | Conteúdo equivalente a `login_forno_27`; não possui `screen.png` próprio. |

## Inconsistências consolidadas

1. Quantidade de mesas: dashboard mostra 22; mapa, requisitos e seed usam 32.
2. Etapas do montador: algumas telas exibem quatro etapas, outras cinco e outras incluem “Adicionais”; o domínio separa tamanho, sabores, personalização, borda e revisão sem codificar a UI como regra.
3. Terminologia: “massa”, “borda”, “opcionais” e “adicionais” variam entre telas.
4. Preços: o mesmo produto aparece com valores diferentes; o seed possui uma fonte única e claramente marcada como desenvolvimento.
5. Estados: textos alternam português e inglês; API usa enums em inglês e o Web apresenta rótulos localizados.
6. Capturas quebradas: `fechamento_de_caixa_forno_27/screen.png` e `registrar_pagamento_modal_forno_27/screen.png` contêm somente `<FIFE Image failed to fetch>`.
7. Menus laterais apresentam pequenas diferenças de ícones, ordem e presença de “Chamados”; o layout integrado usa um único mapa de navegação.
