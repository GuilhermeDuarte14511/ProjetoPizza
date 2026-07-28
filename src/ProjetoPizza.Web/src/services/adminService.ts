import { getJson, isApiConfigured, postJson, putJson } from '../api/httpClient'
import {
  getMockTableDetail,
  mockCategories,
  mockDashboard,
  mockKitchenTickets,
  mockPizzaRules,
  mockPizzaFlavors,
  mockPizzaSizes,
  mockProducts,
  mockTables,
} from '../mocks/adminData'
import {
  mockAuditLogs,
  mockCashShift,
  mockCrusts,
  mockDevices,
  mockFinancialReport,
  mockOperationSettings,
  mockOrders,
  mockPaymentMethods,
  mockPayments,
  mockRoles,
  mockSnapshot,
  mockUnitSettings,
  mockUsers,
} from '../mocks/adminManagementData'
import type {
  AdminRole,
  AdminUser,
  AuditLog,
  AuthenticationResult,
  CashShift,
  Category,
  Dashboard,
  Device,
  FinancialReport,
  KitchenTicket,
  ManagedOrder,
  OperationSettings,
  Payment,
  PaymentMethod,
  PizzaCrust,
  PizzaFlavor,
  PizzaRuleSettings,
  PizzaSize,
  Product,
  RestaurantTable,
  SystemSnapshot,
  TableDetail,
  UnitSettings,
} from '../types/admin'

function fromApiOrMock<T>(request: () => Promise<T>, fallback: T): Promise<T> {
  if (!isApiConfigured) return Promise.resolve(fallback)
  return request().catch((error: unknown) => {
    if (error instanceof DOMException && error.name === 'AbortError') {
      return new Promise<T>(() => undefined)
    }
    throw error
  })
}

const demoResult = { id: crypto.randomUUID(), status: 'Saved' }

export const adminService = {
  login: async (email: string, password: string): Promise<AuthenticationResult> => {
    if (!isApiConfigured) {
      return {
        accessToken: 'development-mock-token',
        expiresAt: new Date(Date.now() + 30 * 60_000).toISOString(),
        user: { id: mockUsers[0].id, email, displayName: 'Administrador', roles: ['Administrator'], permissions: mockRoles[0].permissions },
      }
    }
    return postJson('/api/v1/auth/login', { email, password })
  },
  dashboard: (signal?: AbortSignal) => fromApiOrMock(() => getJson<Dashboard>('/api/v1/admin/dashboard', signal), mockDashboard),
  tables: (signal?: AbortSignal) => fromApiOrMock(() => getJson<RestaurantTable[]>('/api/v1/admin/tables', signal), mockTables),
  table: (id: string, signal?: AbortSignal): Promise<TableDetail> =>
    fromApiOrMock(() => getJson<TableDetail>(`/api/v1/admin/tables/${id}`, signal), getMockTableDetail(id)!),
  categories: (signal?: AbortSignal) => fromApiOrMock(() => getJson<Category[]>('/api/v1/admin/categories', signal), mockCategories),
  products: (signal?: AbortSignal) => fromApiOrMock(() => getJson<Product[]>('/api/v1/admin/products', signal), mockProducts),
  pizzaSizes: (signal?: AbortSignal) => fromApiOrMock(() => getJson<PizzaSize[]>('/api/v1/admin/pizza-sizes', signal), mockPizzaSizes),
  pizzaFlavors: (signal?: AbortSignal) => fromApiOrMock(() => getJson<PizzaFlavor[]>('/api/v1/admin/pizza-flavors', signal), mockPizzaFlavors),
  pizzaRules: (signal?: AbortSignal) => fromApiOrMock(() => getJson<PizzaRuleSettings>('/api/v1/admin/settings/pizza-rules', signal), mockPizzaRules),
  kitchenTickets: (signal?: AbortSignal) => fromApiOrMock(() => getJson<KitchenTicket[]>('/api/v1/admin/kitchen/tickets', signal), mockKitchenTickets),
  orders: (signal?: AbortSignal) => fromApiOrMock(() => getJson<ManagedOrder[]>('/api/v1/admin/orders', signal), mockOrders),
  crusts: (signal?: AbortSignal) => fromApiOrMock(() => getJson<PizzaCrust[]>('/api/v1/admin/pizza-crusts', signal), mockCrusts),
  unitSettings: (signal?: AbortSignal) => fromApiOrMock(() => getJson<UnitSettings>('/api/v1/admin/settings/unit', signal), mockUnitSettings),
  operationSettings: (signal?: AbortSignal) => fromApiOrMock(() => getJson<OperationSettings>('/api/v1/admin/settings/operation', signal), mockOperationSettings),
  cashShift: (signal?: AbortSignal) => fromApiOrMock(() => getJson<CashShift | undefined>('/api/v1/admin/cashier/current', signal), mockCashShift),
  paymentMethods: (signal?: AbortSignal) => fromApiOrMock(() => getJson<PaymentMethod[]>('/api/v1/admin/payment-methods', signal), mockPaymentMethods),
  payments: (signal?: AbortSignal) => fromApiOrMock(() => getJson<Payment[]>('/api/v1/admin/payments', signal), mockPayments),
  financialReport: (from?: string, to?: string, signal?: AbortSignal) => {
    const query = new URLSearchParams()
    if (from) query.set('from', from)
    if (to) query.set('to', to)
    return fromApiOrMock(() => getJson<FinancialReport>(`/api/v1/admin/reports/financial?${query}`, signal), mockFinancialReport)
  },
  devices: (signal?: AbortSignal) => fromApiOrMock(() => getJson<Device[]>('/api/v1/admin/devices', signal), mockDevices),
  users: (signal?: AbortSignal) => fromApiOrMock(() => getJson<AdminUser[]>('/api/v1/admin/users', signal), mockUsers),
  roles: (signal?: AbortSignal) => fromApiOrMock(() => getJson<AdminRole[]>('/api/v1/admin/roles', signal), mockRoles),
  audit: (signal?: AbortSignal) => fromApiOrMock(() => getJson<AuditLog[]>('/api/v1/admin/audit', signal), mockAuditLogs),
  systemSnapshot: (signal?: AbortSignal) => fromApiOrMock(() => getJson<SystemSnapshot>('/api/v1/admin/system/snapshot', signal), mockSnapshot),

  saveUnit: (command: Omit<UnitSettings, 'id' | 'timezone' | 'currencyCode'>) =>
    isApiConfigured ? putJson<void, typeof command>('/api/v1/admin/settings/unit', command) : Promise.resolve(),
  saveOperationSettings: (command: OperationSettings) =>
    isApiConfigured ? putJson<void, OperationSettings>('/api/v1/admin/settings/operation', command) : Promise.resolve(),
  savePizzaRules: (command: PizzaRuleSettings) =>
    isApiConfigured ? putJson<void, PizzaRuleSettings>('/api/v1/admin/settings/pizza-rules', command) : Promise.resolve(),
  saveCategory: (command: Partial<Category> & Pick<Category, 'name' | 'slug' | 'isActive' | 'isVisibleOnTablet'>) =>
    isApiConfigured
      ? command.id
        ? putJson(`/api/v1/admin/categories/${command.id}`, command)
        : postJson('/api/v1/admin/categories', command)
      : Promise.resolve(demoResult),
  saveProduct: (command: Partial<Product> & Pick<Product, 'categoryId' | 'sku' | 'name' | 'type' | 'basePrice' | 'isActive' | 'isAvailable' | 'isFeatured'>) =>
    isApiConfigured
      ? command.id
        ? putJson(`/api/v1/admin/products/${command.id}`, { ...command, preparationTimeMinutes: 15 })
        : postJson('/api/v1/admin/products', { ...command, preparationTimeMinutes: 15 })
      : Promise.resolve(demoResult),
  savePizzaSize: (command: PizzaSize | Omit<PizzaSize, 'id'>) =>
    isApiConfigured && 'id' in command
      ? putJson(`/api/v1/admin/pizza-sizes/${command.id}`, command)
      : isApiConfigured ? postJson('/api/v1/admin/pizza-sizes', command) : Promise.resolve(demoResult),
  saveCrust: (command: PizzaCrust | Omit<PizzaCrust, 'id'>) =>
    isApiConfigured && 'id' in command
      ? putJson(`/api/v1/admin/pizza-crusts/${command.id}`, command)
      : isApiConfigured ? postJson('/api/v1/admin/pizza-crusts', command) : Promise.resolve(demoResult),
  savePizzaFlavor: (command: PizzaFlavor | Omit<PizzaFlavor, 'id'>) =>
    isApiConfigured && 'id' in command
      ? putJson(`/api/v1/admin/pizza-flavors/${command.id}`, command)
      : isApiConfigured ? postJson('/api/v1/admin/pizza-flavors', command) : Promise.resolve(demoResult),
  openTable: (tableId: string, guestCount: number) =>
    isApiConfigured ? postJson(`/api/v1/admin/tables/${tableId}/open`, { tableId, guestCount }) : Promise.resolve(demoResult),
  requestBill: (tableSessionId: string) =>
    isApiConfigured ? postJson(`/api/v1/admin/table-sessions/${tableSessionId}/request-bill`, {}) : Promise.resolve({ ...demoResult, status: 'Requested' }),
  transitionOrder: (id: string, transition: string) =>
    isApiConfigured ? postJson(`/api/v1/admin/orders/${id}/transitions/${transition}`, {}) : Promise.resolve({ ...demoResult, status: transition }),
  transitionKitchenTicket: (id: string, transition: string) =>
    isApiConfigured ? postJson(`/api/v1/admin/kitchen/tickets/${id}/transitions/${transition}`, {}) : Promise.resolve({ ...demoResult, status: transition }),
  recordPayment: (command: { billId: string; paymentMethodId: string; amount: number; receivedAmount: number; externalReference?: string }) =>
    isApiConfigured ? postJson('/api/v1/admin/payments', command) : Promise.resolve({ ...demoResult, status: 'Paid' }),
  registerCashMovement: (command: { type: string; amount: number; description: string; reason: string }) =>
    isApiConfigured ? postJson('/api/v1/admin/cashier/movements', command) : Promise.resolve(demoResult),
  closeCashShift: (countedCashAmount: number, notes?: string) =>
    isApiConfigured ? postJson('/api/v1/admin/cashier/close', { countedCashAmount, notes }) : Promise.resolve({ ...demoResult, status: 'Closed' }),
  updateDevice: (device: Device) =>
    isApiConfigured ? putJson(`/api/v1/admin/devices/${device.id}`, device) : Promise.resolve({ ...demoResult, status: device.status }),
  saveUser: (command: Partial<AdminUser> & { password?: string; phone?: string }): Promise<string> =>
    isApiConfigured
      ? command.id ? putJson<string, typeof command>(`/api/v1/admin/users/${command.id}`, command) : postJson<string, typeof command>('/api/v1/admin/users', command)
      : Promise.resolve(demoResult.id),
  saveRole: (command: Partial<AdminRole>): Promise<string> =>
    isApiConfigured
      ? command.id ? putJson<string, typeof command>(`/api/v1/admin/roles/${command.id}`, command) : postJson<string, typeof command>('/api/v1/admin/roles', command)
      : Promise.resolve(demoResult.id),
}
