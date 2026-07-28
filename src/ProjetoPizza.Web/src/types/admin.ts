export type TableVisualStatus = 'Livre' | 'Ocupada' | 'Chamando' | 'Conta solicitada' | 'Pagamento pendente'

export interface Dashboard {
  salesToday: number
  ordersToday: number
  averageTicket: number
  occupiedTables: number
  totalTables: number
  ordersInProduction: number
  pendingServiceCalls: number
  recentOrders: DashboardOrder[]
}

export interface DashboardOrder {
  number: number
  channel: string
  status: string
  total: number
  placedAt?: string
}

export interface RestaurantTable {
  id: string
  number: number
  name: string
  capacity: number
  area: string
  status: TableVisualStatus
  guestCount?: number
  openedAt?: string
  currentTotal: number
  hasPendingCall: boolean
}

export interface TableDetail {
  table: RestaurantTable
  sessionId?: string
  sessionNumber?: number
  waiter?: string
  orders: DashboardOrder[]
  billId?: string
  remainingAmount: number
}

export interface Category {
  id: string
  name: string
  slug: string
  description?: string
  isActive: boolean
  isVisibleOnTablet: boolean
}

export interface Product {
  id: string
  categoryId: string
  sku: string
  name: string
  type: string
  basePrice: number
  isActive: boolean
  isAvailable: boolean
  isFeatured: boolean
}

export interface PizzaSize {
  id: string
  name: string
  shortName: string
  slices: number
  diameterCm: number
  basePrice: number
  maxFlavors: number
  isActive: boolean
}

export interface PizzaFlavor {
  id: string
  categoryId: string
  name: string
  description?: string
  type: 'Savory' | 'Sweet' | string
  isPremium: boolean
  isVegetarian: boolean
  isActive: boolean
  isAvailable: boolean
  soldOutReason?: string
}

export interface PizzaRuleSettings {
  globalMaxFlavors: number
  pricingPolicy: string
  allowSweetAndSavoryMix: boolean
  allowExtrasPerFlavor: boolean
  allowRepeatedFlavors: boolean
}

export interface KitchenTicket {
  id: string
  ticketNumber: number
  orderNumber: number
  station: string
  status: string
  createdAt: string
  itemCount: number
  summary?: string
}

export interface OrderLine {
  id: string
  name: string
  quantity: number
  unitPrice: number
  totalPrice: number
  status: string
}

export interface ManagedOrder {
  id: string
  number: number
  channel: string
  fulfillment: string
  status: string
  total: number
  createdAt: string
  placedAt?: string
  items: OrderLine[]
}

export interface PizzaCrust {
  id: string
  name: string
  description?: string
  isActive: boolean
  isAvailable: boolean
}

export interface UnitSettings {
  id: string
  name: string
  legalName: string
  tradeName: string
  cnpj: string
  phone?: string
  administrativeEmail?: string
  timezone: string
  currencyCode: string
}

export interface OperationSettings {
  allowTableWithoutWaiter: boolean
  allowOrdersWithoutOpenCashShift: boolean
  clearTabletAfterTableClose: boolean
  serviceFeePercentage: number
  defaultDeliveryFee: number
  deliveryOrderSoundEnabled: boolean
  tableCallSoundEnabled: boolean
  tableCallToleranceMinutes: number
}

export interface CashMovement {
  id: string
  type: string
  amount: number
  description: string
  reason: string
  createdAt: string
}

export interface CashShift {
  id: string
  register: string
  operator: string
  status: string
  openedAt: string
  openingAmount: number
  expectedCashAmount: number
  countedCashAmount?: number
  differenceAmount?: number
  movements: CashMovement[]
}

export interface PaymentMethod {
  id: string
  code: string
  name: string
  requiresExternalReference: boolean
  allowsChange: boolean
  isActive: boolean
}

export interface Payment {
  id: string
  billId: string
  payer?: string
  method: string
  status: string
  amount: number
  receivedAmount: number
  changeAmount: number
  externalReference?: string
  paidAt?: string
}

export interface FinancialReport {
  from: string
  to: string
  grossSales: number
  paidAmount: number
  averageTicket: number
  orderCount: number
  channels: Array<{ channel: string; orders: number; total: number }>
  paymentMethods: Array<{ method: string; payments: number; total: number }>
}

export interface Device {
  id: string
  name: string
  serialNumber: string
  type: string
  platform: string
  status: string
  batteryPercentage?: number
  isCharging: boolean
  networkStatus?: string
  ipAddress?: string
  appVersion?: string
  lastSeenAt?: string
  linkedTableId?: string
  isLocked: boolean
}

export interface AuditLog {
  id: string
  module: string
  action: string
  entityType: string
  entityId: string
  entityDescription?: string
  employee?: string
  occurredAt: string
}

export interface SystemSnapshot {
  generatedAt: string
  unit: UnitSettings
  categories: number
  products: number
  tables: number
  orders: number
  payments: number
  devices: number
}

export interface AdminUser {
  id: string
  email: string
  displayName: string
  employeeCode: string
  isActive: boolean
  lastAccessAt?: string
  roles: string[]
}

export interface AdminRole {
  id: string
  name: string
  permissions: string[]
  userCount: number
}

export interface AuthenticatedUser {
  id: string
  email: string
  displayName: string
  roles: string[]
  permissions: string[]
}

export interface AuthenticationResult {
  accessToken: string
  expiresAt: string
  user: AuthenticatedUser
}
