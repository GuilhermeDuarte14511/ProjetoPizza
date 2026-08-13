import type {
  Category,
  Dashboard,
  KitchenTicket,
  PizzaFlavor,
  PizzaRuleSettings,
  PizzaSize,
  Product,
  RestaurantTable,
  ServiceCall,
  TableDetail,
  TableVisualStatus,
} from '../types/admin'

const statuses: TableVisualStatus[] = ['Livre', 'Ocupada', 'Chamando', 'Conta solicitada', 'Pagamento pendente']

export const mockTables: RestaurantTable[] = Array.from({ length: 32 }, (_, index) => {
  const number = index + 1
  const status = number === 3 ? 'Chamando' : number === 12 ? 'Conta solicitada' : statuses[number % 5 === 0 ? 0 : number % 2]
  return {
    id: `40000000-0000-0000-0000-${number.toString().padStart(12, '0')}`,
    number,
    name: `Mesa ${number.toString().padStart(2, '0')}`,
    capacity: number % 4 === 0 ? 6 : 4,
    area: 'Salão Principal',
    status,
    guestCount: status === 'Livre' ? undefined : (number % 5) + 1,
    openedAt: status === 'Livre' ? undefined : new Date(Date.now() - number * 11 * 60_000).toISOString(),
    currentTotal: status === 'Livre' ? 0 : 54.5 + number * 7,
    hasPendingCall: status === 'Chamando',
  }
})

export const mockDashboard: Dashboard = {
  salesToday: 4580,
  ordersToday: 87,
  averageTicket: 52.65,
  occupiedTables: mockTables.filter((table) => table.status !== 'Livre').length,
  totalTables: 32,
  ordersInProduction: 6,
  pendingServiceCalls: 3,
  recentOrders: [
    { number: 1024, channel: 'Salão', status: 'Em preparo', total: 85.9, placedAt: new Date().toISOString() },
    { number: 1023, channel: 'Delivery', status: 'Aceito', total: 142, placedAt: new Date().toISOString() },
    { number: 1022, channel: 'Retirada', status: 'Pronto', total: 150.8, placedAt: new Date().toISOString() },
  ],
  tableStatus: { free: 12, occupied: 16, calling: 2, awaitingPayment: 2 },
  topProducts: [{ name: 'Pizza Calabresa', quantity: 32 }, { name: 'Margherita', quantity: 26 }, { name: 'Coca-Cola 2L', quantity: 20 }],
  paymentMethods: [{ name: 'Cartão de crédito', total: 2420, percentage: 52.84 }, { name: 'Pix', total: 1460, percentage: 31.88 }, { name: 'Dinheiro', total: 700, percentage: 15.28 }],
  stockAlerts: [{ inventoryItemId: 'stock-1', name: 'Mussarela', availableQuantity: 2.5, minimumStock: 5, unitOfMeasure: 'kg' }],
}

export const mockServiceCalls: ServiceCall[] = [
  {
    id: '78000000-0000-0000-0000-000000000003',
    tableSessionId: '73000000-0000-0000-0000-000000000003',
    tableId: mockTables[2].id,
    tableNumber: 3,
    tableName: 'Mesa 03',
    typeCode: 'WAITER',
    typeName: 'Chamar garçom',
    status: 'Pending',
    details: 'Precisamos de mais guardanapos.',
    createdAt: new Date(Date.now() - 2 * 60_000).toISOString(),
  },
]

export const mockCategories: Category[] = [
  { id: 'cat-1', name: 'Pizzas Tradicionais', slug: 'pizzas-tradicionais', isActive: true, isVisibleOnTablet: true },
  { id: 'cat-2', name: 'Pizzas Especiais', slug: 'pizzas-especiais', isActive: true, isVisibleOnTablet: true },
  { id: 'cat-3', name: 'Pizzas Doces', slug: 'pizzas-doces', isActive: true, isVisibleOnTablet: true },
  { id: 'cat-4', name: 'Porções', slug: 'porcoes', isActive: true, isVisibleOnTablet: true },
  { id: 'cat-5', name: 'Bebidas', slug: 'bebidas', isActive: true, isVisibleOnTablet: true },
]

export const mockProducts: Product[] = [
  { id: 'prod-1', categoryId: 'cat-1', sku: 'PIZ-MARG', name: 'Margherita Tradicional', type: 'Pizza', basePrice: 49.9, preparationTimeMinutes: 25, isActive: true, isAvailable: true, isFeatured: true, usesCustomExtras: false, complements: [] },
  { id: 'prod-2', categoryId: 'cat-1', sku: 'PIZ-CALA', name: 'Pizza Calabresa', type: 'Pizza', basePrice: 54.9, preparationTimeMinutes: 25, isActive: true, isAvailable: true, isFeatured: false, usesCustomExtras: false, complements: [] },
  { id: 'prod-3', categoryId: 'cat-5', sku: 'BEB-COCA2', name: 'Coca-Cola 2L', type: 'Beverage', basePrice: 14, preparationTimeMinutes: 0, isActive: true, isAvailable: false, isFeatured: false, usesCustomExtras: false, complements: [] },
  { id: 'prod-4', categoryId: 'cat-4', sku: 'POR-FRIT', name: 'Batata Frita Especial', type: 'Portion', basePrice: 32, preparationTimeMinutes: 15, isActive: true, isAvailable: true, isFeatured: false, usesCustomExtras: false, complements: [] },
]

export const mockPizzaSizes: PizzaSize[] = [
  { id: 'size-1', name: 'Broto', shortName: 'B', slices: 4, diameterCm: 20, basePrice: 32, maxFlavors: 1, isActive: true },
  { id: 'size-2', name: 'Média', shortName: 'M', slices: 6, diameterCm: 30, basePrice: 48, maxFlavors: 2, isActive: true },
  { id: 'size-3', name: 'Grande', shortName: 'G', slices: 8, diameterCm: 35, basePrice: 68, maxFlavors: 3, isActive: true },
  { id: 'size-4', name: 'Família', shortName: 'F', slices: 12, diameterCm: 45, basePrice: 84, maxFlavors: 3, isActive: true },
]

export const mockPizzaFlavors: PizzaFlavor[] = [
  { id: 'flavor-1', categoryId: 'cat-1', name: 'Calabresa', description: 'Calabresa, cebola e azeitonas', type: 'Savory', isPremium: false, isVegetarian: false, isActive: true, isAvailable: true, extras: [] },
  { id: 'flavor-2', categoryId: 'cat-2', name: 'Margherita', description: 'Tomate, mussarela e manjericão', type: 'Savory', isPremium: false, isVegetarian: true, isActive: true, isAvailable: true, extras: [] },
  { id: 'flavor-3', categoryId: 'cat-3', name: 'Chocolate', description: 'Chocolate ao leite', type: 'Sweet', isPremium: true, isVegetarian: true, isActive: true, isAvailable: true, extras: [] },
]

export const mockPizzaRules: PizzaRuleSettings = {
  globalMaxFlavors: 3,
  pricingPolicy: 'HighestFlavorPrice',
  allowSweetAndSavoryMix: false,
  allowExtrasPerFlavor: true,
  allowRepeatedFlavors: false,
}

export const mockKitchenTickets: KitchenTicket[] = [
  { id: 'ticket-1', ticketNumber: 1042, orderNumber: 1042, station: 'Pizzaria', stationCode: 'PIZZA', status: 'New', createdAt: new Date().toISOString(), targetPreparationMinutes: 15, itemCount: 3, summary: 'Pizza Grande 3 sabores' },
  { id: 'ticket-2', ticketNumber: 1030, orderNumber: 1030, station: 'Pizzaria', stationCode: 'PIZZA', status: 'Confirmed', createdAt: new Date(Date.now() - 8 * 60_000).toISOString(), targetPreparationMinutes: 15, itemCount: 2, summary: '2 Pizzas Médias' },
  { id: 'ticket-3', ticketNumber: 1035, orderNumber: 1035, station: 'Bar', stationCode: 'BAR', status: 'Preparing', createdAt: new Date(Date.now() - 14 * 60_000).toISOString(), startedAt: new Date(Date.now() - 12 * 60_000).toISOString(), targetPreparationMinutes: 8, itemCount: 2, summary: '2 Coca-Cola 2L' },
]

export function getMockTableDetail(id: string): TableDetail | undefined {
  const table = mockTables.find((item) => item.id === id)
  if (!table) return undefined
  return {
    table,
    sessionId: table.status === 'Livre' ? undefined : `73000000-0000-0000-0000-${table.number.toString().padStart(12, '0')}`,
    sessionNumber: table.status === 'Livre' ? undefined : 1000 + table.number,
    waiter: table.status === 'Livre' ? undefined : 'Carlos Mendes',
    orders: table.status === 'Livre' ? [] : mockDashboard.recentOrders.slice(0, 2).map((order, index) => ({
      ...order,
      id: `mock-table-order-${table.number}-${index}`,
      subtotal: order.total,
      discount: 0,
      serviceFee: 0,
      notes: index === 0 ? 'Massa bem assada.' : undefined,
      items: [{
        id: `mock-table-order-item-${table.number}-${index}`,
        name: index === 0 ? 'Pizza Média · 2 sabores' : 'Refrigerante 2L',
        quantity: 1,
        unitPrice: order.total,
        totalPrice: order.total,
        notes: index === 0 ? 'Sem cebola.' : undefined,
        details: index === 0 ? ['Tamanho: Média', 'Sabores: Calabresa / Mussarela'] : [],
      }],
    })),
    billId: table.status === 'Conta solicitada' || table.status === 'Pagamento pendente'
      ? `79000000-0000-0000-0000-${table.number.toString().padStart(12, '0')}`
      : undefined,
    subtotalAmount: table.currentTotal,
    serviceFeePercentage: 10,
    serviceFeeAmount: table.currentTotal * 0.1,
    totalAmount: table.currentTotal * 1.1,
    remainingAmount: table.currentTotal * 1.1,
    requestedSplitCount: table.status === 'Conta solicitada' ? 3 : undefined,
    billItems: table.status === 'Livre' ? [] : [
      { id: `bill-item-${table.number}-1`, name: 'Pizza grande', quantity: 1, total: table.currentTotal * 0.7 },
      { id: `bill-item-${table.number}-2`, name: 'Bebidas', quantity: 2, total: table.currentTotal * 0.4 },
    ],
    linkedTables: table.status === 'Livre' ? [] : [{ id: table.id, name: table.name, isPrimary: true }],
    waiters: [
      { id: '22000000-0000-0000-0000-000000000001', name: 'Carlos Mendes' },
      { id: '22000000-0000-0000-0000-000000000002', name: 'Ana Souza' },
    ],
  }
}
