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
  tableStatus: { free: number; occupied: number; calling: number; awaitingPayment: number }
  topProducts: Array<{ name: string; quantity: number }>
  paymentMethods: Array<{ name: string; total: number; percentage: number }>
  stockAlerts: Array<{ inventoryItemId: string; name: string; availableQuantity: number; minimumStock: number; unitOfMeasure: string }>
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
  orders: TableOrder[]
  billId?: string
  subtotalAmount: number
  serviceFeePercentage: number
  serviceFeeAmount: number
  totalAmount: number
  remainingAmount: number
  requestedSplitCount?: number
  billItems: Array<{ id: string; name: string; quantity: number; total: number }>
  linkedTables: Array<{ id: string; name: string; isPrimary: boolean }>
  waiters: Array<{ id: string; name: string }>
}

export interface TableOrder {
  id: string
  number: number
  channel: string
  status: string
  subtotal: number
  discount: number
  serviceFee: number
  total: number
  placedAt?: string
  notes?: string
  items: Array<{
    id: string
    name: string
    quantity: number
    unitPrice: number
    totalPrice: number
    notes?: string
    details: string[]
  }>
}

export interface ServiceCall {
  id: string
  tableSessionId: string
  tableId: string
  tableNumber: number
  tableName: string
  typeCode: string
  typeName: string
  status: string
  details?: string
  assignedEmployee?: string
  createdAt: string
  acknowledgedAt?: string
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
  description?: string
  type: string
  basePrice: number
  preparationTimeMinutes: number
  imageUrl?: string
  isActive: boolean
  isAvailable: boolean
  isFeatured: boolean
  usesCustomExtras: boolean
  complements: ProductComplement[]
}

export interface ProductComplement {
  ingredientId?: string
  name: string
  price: number
  maxQuantity: number
}

export type SaveProduct = Omit<Product, 'id' | 'usesCustomExtras' | 'complements'> & {
  id?: string
  usesCustomExtras?: boolean
  complements?: ProductComplement[]
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
  imageUrl?: string
  extras: PizzaFlavorExtra[]
}

export interface PizzaFlavorExtra {
  ingredientId: string
  ingredientName: string
  price: number
  maxQuantity: number
}

export type SavePizzaFlavor = Omit<PizzaFlavor, 'id' | 'extras'> & {
  id?: string
  extras: Array<Omit<PizzaFlavorExtra, 'ingredientName'>>
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
  stationCode: string
  status: string
  createdAt: string
  startedAt?: string
  targetPreparationMinutes: number
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
  customerId?: string
  customerName?: string
  deliveryAddress?: string
  deliveryStatus?: string
  deliveryDriverName?: string
  dispatchedAt?: string
  deliveredAt?: string
  notes?: string
  cancellationReason?: string
  subtotal: number
  discount: number
  total: number
  createdAt: string
  placedAt?: string
  items: OrderLine[]
}

export interface Customer {
  id: string
  name: string
  phone: string
  birthDate: string
  isActive: boolean
  loyaltyPoints: number
  lifetimeSpend: number
  orderCount: number
  lastOrderAt?: string
  createdAt: string
}

export interface LoyaltySettings {
  isEnabled: boolean
  pointsPerCurrencyUnit: number
  redemptionValuePerPoint: number
  minimumRedemptionPoints: number
  maximumRedemptionPercentage: number
  pointsValidityDays: number
}
export interface PromotionCoupon {
  id: string
  code: string
  name: string
  discountType: 'FixedAmount' | 'Percentage'
  value: number
  minimumOrderAmount: number
  maximumDiscountAmount?: number
  startsAt: string
  endsAt: string
  usageLimit?: number
  timesRedeemed: number
  isActive: boolean
}
export interface LoyaltyTransaction {
  id: string
  customerId: string
  customerName: string
  orderId?: string
  type: 'OpeningBalance' | 'Earned' | 'Redeemed' | 'Restored' | 'Expired' | 'ManualAdjustment'
  points: number
  balanceAfter: number
  discount: number
  description: string
  occurredAt: string
}
export interface LoyaltyDashboard {
  settings: LoyaltySettings
  coupons: PromotionCoupon[]
  transactions: LoyaltyTransaction[]
  activeCustomers: number
  pointsInCirculation: number
  grantedDiscount: number
}

export interface CustomerOrderSummary {
  id: string
  number: number
  fulfillment: string
  status: string
  subtotal: number
  discount: number
  total: number
  couponCode?: string
  loyaltyPointsRedeemed: number
  createdAt: string
}

export interface CustomerCoupon {
  id: string
  code: string
  name: string
  discountType: 'FixedAmount' | 'Percentage'
  value: number
  minimumOrderAmount: number
  maximumDiscountAmount?: number
  startsAt: string
  endsAt: string
  availability: 'Available' | 'Scheduled' | 'Expired' | 'Inactive' | 'UsageLimitReached'
  timesUsedByCustomer: number
  lastUsedAt?: string
}

export interface CustomerDetail {
  customer: Customer
  loyaltyPointsExpireAt?: string
  benefitBalance: number
  averageTicket: number
  orders: CustomerOrderSummary[]
  coupons: CustomerCoupon[]
  loyaltyTransactions: LoyaltyTransaction[]
}

export interface Reservation {
  id: string
  customerId?: string
  customerName: string
  phone: string
  partySize: number
  scheduledAt: string
  durationMinutes: number
  notes?: string
  customerBirthDate?: string
  status: string
  createdAt: string
  tableSessionId?: string
  seatedAt?: string
}

export interface WaitlistEntry {
  id: string
  customerId?: string
  customerName: string
  phone: string
  partySize: number
  estimatedWaitMinutes: number
  notes?: string
  status: string
  enteredAt: string
  notifiedAt?: string
  tableSessionId?: string
  seatedAt?: string
}

export interface AdministrativeOrderCatalog {
  catalog: ClientCatalog
  defaultDeliveryFee: number
}

export interface CreateAdministrativeOrder extends SubmitClientOrder {
  customerId: string
  fulfillment: 'Pickup' | 'Delivery'
  deliveryAddress?: string
  discountAmount: number
  couponCode?: string
  loyaltyPoints?: number
}

export interface CreatedOrder {
  id: string
  number: number
  status: string
  total: number
  receipt: OrderReceipt
}

export interface CounterPaymentDraft {
  paymentMethodId: string
  receivedAmount: number
  externalReference?: string
}

export interface CheckoutCounterOrder {
  order: CreateAdministrativeOrder
  payment: CounterPaymentDraft
}

export interface OrderReceipt {
  id: string
  number: number
  customerName: string
  customerPhone: string
  fulfillment: string
  deliveryAddress?: string
  placedAt: string
  subtotal: number
  deliveryFee: number
  discount: number
  total: number
  paidAmount: number
  changeAmount: number
  notes?: string
  items: OrderReceiptItem[]
  payments: OrderReceiptPayment[]
}

export interface OrderReceiptPayment {
  method: string
  amount: number
  receivedAmount: number
  changeAmount: number
  paidAt: string
}

export interface OrderReceiptItem {
  id: string
  name: string
  quantity: number
  unitPrice: number
  totalPrice: number
  notes?: string
  details: string[]
}

export interface PizzaCrust {
  id: string
  name: string
  description?: string
  isActive: boolean
  isAvailable: boolean
  prices: PizzaCrustPrice[]
}

export interface PizzaCrustPrice {
  pizzaSizeId: string
  pizzaSizeName: string
  sliceCount: number
  fullPrice: number
  halfPrice: number
}

export interface Ingredient {
  id: string
  name: string
  description?: string
  isActive: boolean
  isAllergen: boolean
  allergenDescription?: string
  isAvailableAsExtra: boolean
  extraPrice: number
  maxExtraQuantity: number
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

export interface CashRegister {
  id: string
  name: string
  code: string
  isActive: boolean
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

export interface CashShiftHistory extends CashShift {
  closedBy?: string
  closedAt?: string
  closingNotes?: string
}

export interface PaymentMethod {
  id: string
  code: string
  name: string
  requiresExternalReference: boolean
  allowsChange: boolean
  displayOrder: number
  isActive: boolean
}

export interface DiningAreaSetting { id: string; name: string; displayOrder: number; isActive: boolean }
export interface RestaurantTableSetting { id: string; diningAreaId: string; areaName: string; number: number; name: string; capacity: number; displayOrder: number; isActive: boolean }
export interface ProductionStationSetting { id: string; name: string; code: string; targetPreparationMinutes: number; displayOrder: number; isActive: boolean }
export interface ServiceCallTypeSetting { id: string; code: string; name: string; isActive: boolean }
export interface InventoryItem {
  id: string
  name: string
  sku: string
  unitOfMeasure: string
  minimumStock: number
  unitCost: number
  currentQuantity: number
  reservedQuantity: number
  availableQuantity: number
  isLowStock: boolean
  isActive: boolean
}
export interface InventoryRecipeItem { inventoryItemId: string; inventoryItemName: string; quantity: number; unitOfMeasure: string }
export interface InventoryRecipe {
  id: string
  productId?: string
  productName?: string
  pizzaFlavorId?: string
  pizzaFlavorName?: string
  pizzaSizeId?: string
  pizzaSizeName?: string
  yieldQuantity: number
  items: InventoryRecipeItem[]
}
export interface SaveInventoryRecipe {
  id?: string
  productId?: string
  pizzaFlavorId?: string
  pizzaSizeId?: string
  yieldQuantity: number
  items: Array<{ inventoryItemId: string; quantity: number; unitOfMeasure: string }>
}
export interface DatabaseBackup { fileName: string; createdAt: string; sizeBytes: number; type: string }

export interface Payment {
  id: string
  billId: string
  payer?: string
  method: string
  status: string
  amount: number
  receivedAmount: number
  changeAmount: number
  refundedAmount?: number
  externalReference?: string
  paidAt?: string
  refundedAt?: string
  refundReason?: string
}

export interface FinancialReport {
  from: string
  to: string
  grossSales: number
  paidAmount: number
  foodCost: number
  contributionMargin: number
  contributionMarginPercentage: number
  averageTicket: number
  orderCount: number
  completedTickets: number
  averagePreparationMinutes: number
  onTimeRate: number
  channels: Array<{ channel: string; orders: number; total: number }>
  paymentMethods: Array<{ method: string; payments: number; total: number }>
  productionStations: Array<{ station: string; tickets: number; averagePreparationMinutes: number; onTimeRate: number }>
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
  printerPort?: number
  paperWidthMm?: number
  autoPrintKitchenTickets?: boolean
  autoPrintCustomerReceipts?: boolean
  autoPrintFiscalDocuments?: boolean
}

export interface PrintJob {
  id: string
  printerId: string
  printerName: string
  documentType: string
  status: string
  attempts: number
  lastError?: string
  createdAt: string
  completedAt?: string
}

export interface DeviceProvisioning {
  device: Device
  activationToken: string
  expiresAt: string
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
  phone?: string
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
import type { ClientCatalog, SubmitClientOrder } from './client'
