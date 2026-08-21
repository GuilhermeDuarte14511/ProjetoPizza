const enumLabels: Record<string, string> = {
  Draft: 'Rascunho', New: 'Novo', Submitted: 'Enviado', Accepted: 'Aceito', Confirmed: 'Confirmado',
  Seated: 'Recepcionado', NoShow: 'Não compareceu', Waiting: 'Aguardando', Notified: 'Avisado',
  InProduction: 'Em preparo', Preparing: 'Em preparo', Ready: 'Pronto', Completed: 'Concluído',
  Complete: 'Conclusão', Cancelled: 'Cancelado', Pending: 'Pendente', Acknowledged: 'Assumido',
  InProgress: 'Em atendimento', Authorized: 'Autorizado', Paid: 'Pago', Failed: 'Falhou',
  Refunded: 'Estornado', PartiallyRefunded: 'Parcialmente estornado', PartiallyPaid: 'Parcialmente pago',
  Open: 'Aberto', Opening: 'Abertura', Closing: 'Em fechamento', Closed: 'Fechado', Requested: 'Solicitada',
  PaymentInProgress: 'Pagamento em andamento', BillRequested: 'Conta solicitada', PaymentPending: 'Pagamento pendente',
  Online: 'Conectado', Offline: 'Desconectado', Idle: 'Ocioso', Blocked: 'Bloqueado', Maintenance: 'Em manutenção',
  DineIn: 'Salão', Delivery: 'Entrega', Pickup: 'Retirada', Takeaway: 'Retirada', Website: 'Site',
  Application: 'Aplicativo', Administrative: 'Administrativo', AwaitingPreparation: 'Aguardando preparo',
  ReadyForDispatch: 'Pronto para despacho', Dispatched: 'Saiu para entrega', Delivered: 'Entregue',
  Standard: 'Padrão', Pizza: 'Pizza', PizzaFlavor: 'Sabor de pizza', Beverage: 'Bebida', Portion: 'Porção',
  Dessert: 'Sobremesa', Combo: 'Combo', Additional: 'Adicional', Savory: 'Salgado', Sweet: 'Doce',
  CustomerTablet: 'Tablet do cliente', KitchenDisplay: 'Monitor da cozinha', PointOfSale: 'Ponto de venda',
  Printer: 'Impressora', TestPage: 'Página de teste', CustomerReceipt: 'Comprovante do cliente',
  CashClosing: 'Fechamento de caixa', FiscalDocument: 'Documento fiscal', Processing: 'Processando',
  Entry: 'Entrada', Consumption: 'Consumo', Adjustment: 'Ajuste', Loss: 'Perda', Return: 'Devolução',
  ReservationRelease: 'Liberação de reserva', Reserved: 'Reservado', Consumed: 'Consumido', Released: 'Liberado',
  SentToProduction: 'Enviado à produção', Add: 'Adicionar', Remove: 'Remover', Extra: 'Adicional',
  None: 'Nenhuma', Whole: 'Inteira', Split: 'Dividida', Administrator: 'Administrador',
  HighestFlavorPrice: 'Maior valor entre os sabores', AverageFlavorPrice: 'Média dos sabores',
  ProportionalFlavorPrice: 'Valor proporcional',

  Create: 'Criação', Created: 'Criado', Update: 'Atualização', Updated: 'Atualizado', Delete: 'Exclusão', Deleted: 'Excluído',
  confirm: 'Confirmação', accept: 'Aceite', 'start-production': 'Início da produção', start: 'Início do preparo',
  ready: 'Pronto', complete: 'Conclusão', Adjust: 'Ajuste', Acknowledge: 'Atendimento assumido',
  Close: 'Fechamento', Provision: 'Provisionamento', Pay: 'Pagamento', Request: 'Solicitação',
  SplitPayment: 'Pagamento dividido', OpenTable: 'Abertura de mesa', OpenFromTablet: 'Abertura pelo tablet',
  OpenFromSeating: 'Abertura pela recepção', SeatReservation: 'Recepção de reserva',
  SeatWaitlist: 'Recepção da lista de espera', CreateReservation: 'Criação de reserva',
  JoinWaitlist: 'Entrada na lista de espera', CreateFromReservation: 'Cadastro pela reserva',
  CreateAdministrative: 'Criação administrativa', CreateExternalDelivery: 'Criação pelo delivery',
  SubmitFromTablet: 'Envio pelo tablet', CallFromTablet: 'Chamado pelo tablet',
  RequestFromTablet: 'Conta solicitada pelo tablet', AssignWaiter: 'Atribuição de garçom',
  LinkTable: 'Vinculação de mesa', TransferTable: 'Transferência de mesa',
  CancelApproved: 'Cancelamento aprovado', DiscountApproved: 'Desconto aprovado',
  DispatchDelivery: 'Saída para entrega', CompleteDelivery: 'Conclusão da entrega', FailDelivery: 'Falha na entrega',
  RefundApproved: 'Estorno aprovado', CounterCheckout: 'Fechamento no balcão', UpdateImage: 'Atualização de imagem',
  UpdateLoyaltySettings: 'Atualização das regras de fidelidade', CreateCoupon: 'Criação de cupom', UpdateCoupon: 'Atualização de cupom',
  QueuePrinterTest: 'Teste de impressão enfileirado', QueueOrderReceipt: 'Comprovante enfileirado',
  QueueKitchenCommand: 'Comanda da cozinha enfileirada', CashIn: 'Suprimento', CashOut: 'Sangria',
  Supply: 'Suprimento', Withdrawal: 'Sangria', Sale: 'Venda', Refund: 'Estorno',
  OpeningBalance: 'Saldo inicial', Earned: 'Pontos ganhos', Redeemed: 'Pontos usados', Restored: 'Pontos devolvidos',
  Expired: 'Expirado', ManualAdjustment: 'Ajuste manual', AdjustLoyaltyPoints: 'Ajuste manual de pontos',
  Available: 'Disponível', Scheduled: 'Agendado', Inactive: 'Inativo', UsageLimitReached: 'Limite atingido',
  FixedAmount: 'Valor fixo', Percentage: 'Percentual',

  Catalog: 'Cardápio', Dining: 'Salão', Production: 'Produção', Billing: 'Financeiro', Cashier: 'Caixa',
  Inventory: 'Estoque', Ordering: 'Pedidos', Customers: 'Clientes', Devices: 'Dispositivos', Core: 'Configurações',
  KitchenTicket: 'Ticket da cozinha', Bill: 'Conta', Payment: 'Pagamento', Product: 'Produto', Category: 'Categoria',
  Ingredient: 'Ingrediente', PizzaCrust: 'Borda de pizza', PizzaSize: 'Tamanho de pizza',
  TableSession: 'Atendimento de mesa', RestaurantTable: 'Mesa', DiningArea: 'Área do salão', Reservation: 'Reserva',
  WaitlistEntry: 'Entrada da lista de espera', ServiceCall: 'Chamado de mesa', ServiceCallType: 'Tipo de chamado',
  Order: 'Pedido', Customer: 'Cliente', RestaurantUnit: 'Unidade', OperationSettings: 'Configurações da operação',
  LoyaltySettings: 'Regras de fidelidade', PromotionCoupon: 'Cupom promocional',
  PizzaSettings: 'Regras de pizza', CashRegister: 'Caixa', CashShift: 'Turno de caixa',
  CashMovement: 'Movimento de caixa', PaymentMethod: 'Forma de pagamento', ProductionStation: 'Estação de produção',
  InventoryItem: 'Item de estoque', Recipe: 'Ficha técnica', Device: 'Dispositivo', PrintJob: 'Trabalho de impressão',
  Network: 'Rede',
  Manual: 'Manual', Automatic: 'Automático',
}

export function translateEnum(value?: string | null) {
  if (!value) return 'Não informado'
  return enumLabels[value] ?? value
}

export function enumCssToken(value: string) {
  return value
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/([a-z])([A-Z])/g, '$1-$2')
    .replace(/\s+/g, '-')
    .toLowerCase()
}
