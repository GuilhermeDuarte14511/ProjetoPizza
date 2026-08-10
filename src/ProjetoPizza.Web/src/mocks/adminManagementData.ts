import type {
  AdminRole,
  AdminUser,
  AuditLog,
  CashRegister,
  CashShift,
  Device,
  FinancialReport,
  ManagedOrder,
  OperationSettings,
  Payment,
  PaymentMethod,
  PizzaCrust,
  SystemSnapshot,
  UnitSettings,
} from '../types/admin'
import { mockCategories, mockDashboard, mockProducts, mockTables } from './adminData'

export const mockOrders: ManagedOrder[] = mockDashboard.recentOrders.map((order, index) => ({
  id: `74000000-0000-0000-0000-${order.number.toString().padStart(12, '0')}`,
  number: order.number,
  channel: order.channel === 'Salão' ? 'DineIn' : order.channel,
  fulfillment: order.channel === 'Delivery' ? 'Delivery' : 'DineIn',
  status: order.status === 'Em preparo' ? 'InProduction' : order.status === 'Pronto' ? 'Ready' : 'Submitted',
  total: order.total,
  createdAt: order.placedAt ?? new Date().toISOString(),
  placedAt: order.placedAt,
  items: [{
    id: `75000000-0000-0000-0000-${(index + 1).toString().padStart(12, '0')}`,
    name: index === 0 ? 'Pizza Grande 3 sabores' : 'Pizza Calabresa',
    quantity: index + 1,
    unitPrice: order.total / (index + 1),
    totalPrice: order.total,
    status: 'Pending',
  }],
}))

export const mockCrusts: PizzaCrust[] = [
  { id: '65000000-0000-0000-0000-000000000001', name: 'Sem borda', description: 'Massa tradicional', isActive: true, isAvailable: true, prices: crustPrices(0, 0) },
  { id: '65000000-0000-0000-0000-000000000002', name: 'Catupiry', description: 'Borda recheada', isActive: true, isAvailable: true, prices: crustPrices(12, 6) },
  { id: '65000000-0000-0000-0000-000000000003', name: 'Cheddar', description: 'Borda recheada', isActive: true, isAvailable: true, prices: crustPrices(12, 6) },
  { id: '65000000-0000-0000-0000-000000000004', name: 'Cream Cheese', description: 'Borda recheada premium', isActive: true, isAvailable: false, prices: crustPrices(14, 7) },
]

function crustPrices(fullPrice: number, halfPrice: number) {
  return [
    { pizzaSizeId: 'size-1', pizzaSizeName: 'Broto', sliceCount: 4, fullPrice, halfPrice },
    { pizzaSizeId: 'size-2', pizzaSizeName: 'Média', sliceCount: 6, fullPrice, halfPrice },
    { pizzaSizeId: 'size-3', pizzaSizeName: 'Grande', sliceCount: 8, fullPrice, halfPrice },
    { pizzaSizeId: 'size-4', pizzaSizeName: 'Família', sliceCount: 12, fullPrice, halfPrice },
  ]
}

export const mockUnitSettings: UnitSettings = {
  id: '10000000-0000-0000-0000-000000000001',
  name: 'Unidade Principal',
  legalName: 'Projeto Pizza Desenvolvimento LTDA',
  tradeName: 'Forno 27',
  cnpj: '00.000.000/0001-00',
  phone: '(11) 99999-0000',
  administrativeEmail: 'dev@projetopizza.local',
  timezone: 'America/Sao_Paulo',
  currencyCode: 'BRL',
}

export const mockOperationSettings: OperationSettings = {
  allowTableWithoutWaiter: false,
  allowOrdersWithoutOpenCashShift: false,
  clearTabletAfterTableClose: true,
  serviceFeePercentage: 10,
  defaultDeliveryFee: 8,
  deliveryOrderSoundEnabled: true,
  tableCallSoundEnabled: true,
  tableCallToleranceMinutes: 5,
}

export const mockCashShift: CashShift = {
  id: '80000000-0000-0000-0000-000000000001',
  register: 'Caixa Principal',
  operator: 'Administrador',
  status: 'Open',
  openedAt: new Date(Date.now() - 7 * 60 * 60_000).toISOString(),
  openingAmount: 200,
  expectedCashAmount: 1245.9,
  movements: [
    { id: 'mov-1', type: 'Sale', amount: 145, description: 'Pagamento conta Mesa 12', reason: 'Venda', createdAt: new Date().toISOString() },
    { id: 'mov-2', type: 'Supply', amount: 100, description: 'Reforço de troco', reason: 'Suprimento', createdAt: new Date(Date.now() - 2 * 60 * 60_000).toISOString() },
  ],
}

export const mockCashRegisters: CashRegister[] = [
  { id: '50000000-0000-0000-0000-000000000001', name: 'Caixa Principal', code: 'CX-01', isActive: true },
]

export const mockPaymentMethods: PaymentMethod[] = [
  { id: '70000000-0000-0000-0000-000000000001', code: 'CASH', name: 'Dinheiro', requiresExternalReference: false, allowsChange: true, displayOrder: 1, isActive: true },
  { id: '70000000-0000-0000-0000-000000000002', code: 'PIX', name: 'Pix', requiresExternalReference: true, allowsChange: false, displayOrder: 2, isActive: true },
  { id: '70000000-0000-0000-0000-000000000003', code: 'CREDIT', name: 'Cartão de Crédito', requiresExternalReference: true, allowsChange: false, displayOrder: 3, isActive: true },
]

export const mockPayments: Payment[] = [
  { id: 'pay-1', billId: 'bill-1', method: 'Pix', status: 'Paid', amount: 145, receivedAmount: 145, changeAmount: 0, externalReference: 'PIX-DEV-001', paidAt: new Date().toISOString() },
  { id: 'pay-2', billId: 'bill-2', method: 'Dinheiro', status: 'Paid', amount: 85.9, receivedAmount: 100, changeAmount: 14.1, paidAt: new Date(Date.now() - 35 * 60_000).toISOString() },
]

export const mockFinancialReport: FinancialReport = {
  from: new Date(Date.now() - 30 * 86400000).toISOString(),
  to: new Date().toISOString(),
  grossSales: 48520.8,
  paidAmount: 47290.4,
  averageTicket: 72.65,
  orderCount: 668,
  channels: [
    { channel: 'DineIn', orders: 410, total: 31280 },
    { channel: 'Delivery', orders: 178, total: 12440.8 },
    { channel: 'Takeaway', orders: 80, total: 4800 },
  ],
  paymentMethods: [
    { method: 'Cartão de Crédito', payments: 285, total: 21490 },
    { method: 'Pix', payments: 190, total: 15120.4 },
    { method: 'Dinheiro', payments: 153, total: 10680 },
  ],
}

export const mockDevices: Device[] = [
  { id: '72000000-0000-0000-0000-000000000001', name: 'Tablet Mesa 02', serialNumber: 'DEV-TABLET-002', type: 'CustomerTablet', platform: 'Android', status: 'Online', batteryPercentage: 82, isCharging: false, networkStatus: 'Wi-Fi', ipAddress: '192.168.10.22', appVersion: '1.0.0', lastSeenAt: new Date().toISOString(), linkedTableId: mockTables[1].id, isLocked: false },
  { id: '72000000-0000-0000-0000-000000000002', name: 'Tablet Mesa 03', serialNumber: 'DEV-TABLET-003', type: 'CustomerTablet', platform: 'Android', status: 'Idle', batteryPercentage: 54, isCharging: true, networkStatus: 'Wi-Fi', ipAddress: '192.168.10.23', appVersion: '1.0.0', lastSeenAt: new Date().toISOString(), linkedTableId: mockTables[2].id, isLocked: false },
  { id: '72000000-0000-0000-0000-000000000004', name: 'Impressora Cozinha', serialNumber: 'DEV-PRINTER-001', type: 'Printer', platform: 'Network', status: 'Online', isCharging: false, networkStatus: 'Ethernet', ipAddress: '192.168.10.31', appVersion: 'Firmware 2.4', lastSeenAt: new Date().toISOString(), isLocked: false },
]

export const mockUsers: AdminUser[] = [
  { id: '20000000-0000-0000-0000-000000000002', email: 'admin@projetopizza.local', displayName: 'Administrador', employeeCode: 'DEV-ADMIN', phone: '11999999999', isActive: true, lastAccessAt: new Date().toISOString(), roles: ['Administrator'] },
]

export const mockRoles: AdminRole[] = [
  { id: '20000000-0000-0000-0000-000000000003', name: 'Administrator', permissions: ['admin:read', 'admin:write', 'operations:read', 'operations:write'], userCount: 1 },
]

export const mockAuditLogs: AuditLog[] = [
  { id: 'audit-1', module: 'Catalog', action: 'Update', entityType: 'Product', entityId: mockProducts[0].id, employee: 'Administrador', occurredAt: new Date().toISOString() },
  { id: 'audit-2', module: 'Dining', action: 'Open', entityType: 'TableSession', entityId: 'session-1', employee: 'Administrador', occurredAt: new Date(Date.now() - 20 * 60_000).toISOString() },
]

export const mockSnapshot: SystemSnapshot = {
  generatedAt: new Date().toISOString(),
  unit: mockUnitSettings,
  categories: mockCategories.length,
  products: mockProducts.length,
  tables: mockTables.length,
  orders: mockOrders.length,
  payments: mockPayments.length,
  devices: mockDevices.length,
}
