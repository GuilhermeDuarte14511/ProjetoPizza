import { getJson, isApiConfigured, postJson, putJson } from '../api/httpClient'
import { createUuid } from '../lib/uuid'
import {
  getMockTableDetail,
  mockCategories,
  mockDashboard,
  mockKitchenTickets,
  mockPizzaRules,
  mockPizzaFlavors,
  mockPizzaSizes,
  mockProducts,
  mockServiceCalls,
  mockTables,
} from '../mocks/adminData'
import {
  mockAuditLogs,
  mockCashRegisters,
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
import { mockAdministrativeOrderCatalog, mockCustomers } from '../mocks/adminOrderData'
import type {
  AdministrativeOrderCatalog,
  AdminRole,
  AdminUser,
  AuditLog,
  AuthenticationResult,
  CashRegister,
  CashShift,
  Category,
  CreateAdministrativeOrder,
  CreatedOrder,
  Customer,
  Dashboard,
  Device,
  DeviceProvisioning,
  FinancialReport,
  KitchenTicket,
  Ingredient,
  ManagedOrder,
  OperationSettings,
  OrderReceipt,
  Payment,
  PaymentMethod,
  PizzaCrust,
  PizzaFlavor,
  PizzaRuleSettings,
  PizzaSize,
  Product,
  RestaurantTable,
  SavePizzaFlavor,
  SaveProduct,
  ServiceCall,
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

const demoResult = { id: createUuid(), status: 'Saved' }
const mockCustomerStore = [...mockCustomers]
const mockReceiptStore = new Map<string, OrderReceipt>()

function saveMockCustomer(command: Omit<Customer, 'id' | 'createdAt'> & { id?: string }): Customer {
  const customer: Customer = {
    ...command,
    id: command.id || createUuid(),
    phone: command.phone.replace(/\D/g, ''),
    createdAt: mockCustomerStore.find((item) => item.id === command.id)?.createdAt ?? new Date().toISOString(),
  }
  const index = mockCustomerStore.findIndex((item) => item.id === customer.id)
  if (index >= 0) mockCustomerStore[index] = customer
  else mockCustomerStore.push(customer)
  return customer
}

function createMockAdministrativeOrder(command: CreateAdministrativeOrder): CreatedOrder {
  const existingReceipt = mockReceiptStore.get(command.requestId)
  if (existingReceipt) {
    return { id: existingReceipt.id, number: existingReceipt.number, status: 'Submitted', total: existingReceipt.total, receipt: existingReceipt }
  }

  const number = Math.max(0, ...mockOrders.map((order) => order.number)) + 1
  const placedAt = new Date().toISOString()
  const customer = mockCustomerStore.find((item) => item.id === command.customerId)
  const receiptItems = command.items.map((requestedItem) => {
    const product = mockAdministrativeOrderCatalog.catalog.products.find((item) => item.id === requestedItem.productId)
    const details: string[] = []
    let name = product?.name ?? 'Produto'
    let unitPrice = product?.price ?? 0
    if (requestedItem.pizza) {
      const size = mockAdministrativeOrderCatalog.catalog.pizza.sizes.find((item) => item.id === requestedItem.pizza?.sizeId)
      const flavors = requestedItem.pizza.flavorIds
        .map((id) => mockAdministrativeOrderCatalog.catalog.pizza.flavors.find((item) => item.id === id))
        .filter((item) => item !== undefined)
      const flavorPrices = flavors.map((flavor) => flavor.prices.find((price) => price.pizzaSizeId === size?.id)?.price ?? 0)
      unitPrice = flavorPrices.length ? Math.max(...flavorPrices) : (size?.basePrice ?? unitPrice)
      name = `Pizza ${size?.name ?? ''} · ${flavors.length} sabor(es)`.trim()
      details.push(`Tamanho: ${size?.name ?? '-'}`, `Sabores: ${flavors.map((flavor) => flavor.name).join(' / ')}`)
      const firstCrust = mockAdministrativeOrderCatalog.catalog.pizza.crusts.find((item) => item.id === requestedItem.pizza?.crustId)
      const secondCrust = mockAdministrativeOrderCatalog.catalog.pizza.crusts.find((item) => item.id === requestedItem.pizza?.secondCrustId)
      if (firstCrust && size) {
        const firstPrice = firstCrust.prices.find((price) => price.pizzaSizeId === size.id)
        if (secondCrust) {
          const secondPrice = secondCrust.prices.find((price) => price.pizzaSizeId === size.id)
          unitPrice += (firstPrice?.halfPrice ?? 0) + (secondPrice?.halfPrice ?? 0)
          details.push(`Borda: 1/2 ${firstCrust.name} + 1/2 ${secondCrust.name}`)
        } else {
          unitPrice += firstPrice?.fullPrice ?? 0
          details.push(`Borda: ${firstCrust.name}`)
        }
      }
      for (const extra of requestedItem.pizza.extraIngredients ?? []) {
        const flavor = flavors.find((item) => item.id === extra.pizzaFlavorId)
        const catalogExtra = flavor?.extras.find((item) => item.id === extra.ingredientId)
          ?? mockAdministrativeOrderCatalog.catalog.pizza.extras.find((item) => item.id === extra.ingredientId)
          ?? product?.complements.find((item) => item.id === extra.ingredientId)
        const extraTotal = (catalogExtra?.price ?? 0) * extra.quantity
        unitPrice += extraTotal
        details.push(`Adicional: ${extra.quantity}x ${catalogExtra?.name ?? 'Item'} (+ ${extraTotal.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })})`)
      }
    }
    return {
      id: createUuid(),
      name,
      quantity: requestedItem.quantity,
      unitPrice,
      totalPrice: unitPrice * requestedItem.quantity,
      notes: requestedItem.notes,
      details,
    }
  })
  const subtotal = receiptItems.reduce((sum, item) => sum + item.totalPrice, 0)
  const deliveryFee = command.fulfillment === 'Delivery' ? mockAdministrativeOrderCatalog.defaultDeliveryFee : 0
  const total = subtotal + deliveryFee - command.discountAmount
  const receipt: OrderReceipt = {
    id: command.requestId,
    number,
    customerName: customer?.name ?? 'Consumidor',
    customerPhone: customer?.phone ?? '',
    fulfillment: command.fulfillment,
    deliveryAddress: command.deliveryAddress,
    placedAt,
    subtotal,
    deliveryFee,
    discount: command.discountAmount,
    total,
    notes: command.notes,
    items: receiptItems,
  }
  mockReceiptStore.set(command.requestId, receipt)
  mockOrders.unshift({
    id: command.requestId,
    number,
    channel: command.fulfillment,
    fulfillment: command.fulfillment,
    status: 'Submitted',
    customerId: customer?.id,
    customerName: customer?.name,
    deliveryAddress: command.deliveryAddress,
    notes: command.notes,
    total,
    createdAt: placedAt,
    placedAt,
    items: receiptItems.map((item) => ({ ...item, status: 'Pending' })),
  })
  return { id: command.requestId, number, status: 'Submitted', total, receipt }
}

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
  serviceCalls: (signal?: AbortSignal) => fromApiOrMock(() => getJson<ServiceCall[]>('/api/v1/admin/service-calls', signal), mockServiceCalls),
  pizzaRules: (signal?: AbortSignal) => fromApiOrMock(() => getJson<PizzaRuleSettings>('/api/v1/admin/settings/pizza-rules', signal), mockPizzaRules),
  kitchenTickets: (signal?: AbortSignal) => fromApiOrMock(() => getJson<KitchenTicket[]>('/api/v1/admin/kitchen/tickets', signal), mockKitchenTickets),
  orders: (signal?: AbortSignal) => fromApiOrMock(() => getJson<ManagedOrder[]>('/api/v1/admin/orders', signal), mockOrders),
  orderCatalog: (signal?: AbortSignal) => fromApiOrMock(() => getJson<AdministrativeOrderCatalog>('/api/v1/admin/orders/catalog', signal), mockAdministrativeOrderCatalog),
  customers: (signal?: AbortSignal) => fromApiOrMock(() => getJson<Customer[]>('/api/v1/admin/customers', signal), mockCustomerStore),
  crusts: (signal?: AbortSignal) => fromApiOrMock(() => getJson<PizzaCrust[]>('/api/v1/admin/pizza-crusts', signal), mockCrusts),
  ingredients: (signal?: AbortSignal) => fromApiOrMock(() => getJson<Ingredient[]>('/api/v1/admin/ingredients', signal), []),
  unitSettings: (signal?: AbortSignal) => fromApiOrMock(() => getJson<UnitSettings>('/api/v1/admin/settings/unit', signal), mockUnitSettings),
  operationSettings: (signal?: AbortSignal) => fromApiOrMock(() => getJson<OperationSettings>('/api/v1/admin/settings/operation', signal), mockOperationSettings),
  cashRegisters: (signal?: AbortSignal) =>
    fromApiOrMock(() => getJson<CashRegister[]>('/api/v1/admin/cashier/registers', signal), mockCashRegisters),
  cashShift: (signal?: AbortSignal): Promise<CashShift | null> =>
    fromApiOrMock(() => getJson<CashShift | null>('/api/v1/admin/cashier/current', signal), mockCashShift),
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
  saveProduct: (command: SaveProduct) =>
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
  saveIngredient: (command: Ingredient | Omit<Ingredient, 'id'>) =>
    isApiConfigured && 'id' in command
      ? putJson(`/api/v1/admin/ingredients/${command.id}`, command)
      : isApiConfigured ? postJson('/api/v1/admin/ingredients', command) : Promise.resolve(demoResult),
  savePizzaFlavor: (command: SavePizzaFlavor) =>
    isApiConfigured && 'id' in command
      ? putJson(`/api/v1/admin/pizza-flavors/${command.id}`, command)
      : isApiConfigured ? postJson('/api/v1/admin/pizza-flavors', command) : Promise.resolve(demoResult),
  saveCustomer: (command: Omit<Customer, 'id' | 'createdAt'> & { id?: string }) =>
    isApiConfigured
      ? command.id
        ? putJson<Customer, typeof command>(`/api/v1/admin/customers/${command.id}`, command)
        : postJson<Customer, typeof command>('/api/v1/admin/customers', command)
      : Promise.resolve(saveMockCustomer(command)),
  createOrder: (command: CreateAdministrativeOrder): Promise<CreatedOrder> =>
    isApiConfigured
      ? postJson<CreatedOrder, CreateAdministrativeOrder>('/api/v1/admin/orders', command)
      : Promise.resolve(createMockAdministrativeOrder(command)),
  orderReceipt: (id: string, signal?: AbortSignal) =>
    fromApiOrMock(
      () => getJson<OrderReceipt>(`/api/v1/admin/orders/${id}/receipt`, signal),
      (mockReceiptStore.get(id) ?? {
        id,
        number: 0,
        customerName: 'Consumidor',
        customerPhone: '',
        fulfillment: 'Pickup',
        placedAt: new Date().toISOString(),
        subtotal: 0,
        deliveryFee: 0,
        discount: 0,
        total: 0,
        items: [],
      }),
    ),
  openTable: (tableId: string, guestCount: number) =>
    isApiConfigured ? postJson(`/api/v1/admin/tables/${tableId}/open`, { tableId, guestCount }) : Promise.resolve(demoResult),
  requestBill: (tableSessionId: string) =>
    isApiConfigured ? postJson(`/api/v1/admin/table-sessions/${tableSessionId}/request-bill`, {}) : Promise.resolve({ ...demoResult, status: 'Requested' }),
  transitionOrder: (id: string, transition: string) =>
    isApiConfigured ? postJson(`/api/v1/admin/orders/${id}/transitions/${transition}`, {}) : Promise.resolve({ ...demoResult, status: transition }),
  transitionKitchenTicket: (id: string, transition: string) =>
    isApiConfigured ? postJson(`/api/v1/admin/kitchen/tickets/${id}/transitions/${transition}`, {}) : Promise.resolve({ ...demoResult, status: transition }),
  acknowledgeServiceCall: (id: string) =>
    isApiConfigured ? postJson(`/api/v1/admin/service-calls/${id}/acknowledge`, {}) : Promise.resolve({ ...demoResult, id, status: 'Acknowledged' }),
  completeServiceCall: (id: string) =>
    isApiConfigured ? postJson(`/api/v1/admin/service-calls/${id}/complete`, {}) : Promise.resolve({ ...demoResult, id, status: 'Completed' }),
  recordPayment: (command: { billId: string; paymentMethodId: string; amount: number; receivedAmount: number; externalReference?: string }) =>
    isApiConfigured ? postJson('/api/v1/admin/payments', command) : Promise.resolve({ ...demoResult, status: 'Paid' }),
  recordSplitPayment: (command: { billId: string; payments: Array<{ payer: string; paymentMethodId: string; amount: number; receivedAmount: number; externalReference?: string }> }) =>
    isApiConfigured ? postJson('/api/v1/admin/payments/split', command) : Promise.resolve({ ...demoResult, status: 'Paid' }),
  openCashShift: (command: { cashRegisterId: string; openingAmount: number }): Promise<CashShift> =>
    isApiConfigured
      ? postJson('/api/v1/admin/cashier/open', command)
      : Promise.resolve({
          id: createUuid(),
          register: mockCashRegisters.find((register) => register.id === command.cashRegisterId)?.name ?? 'Caixa',
          operator: 'Administrador',
          status: 'Open',
          openedAt: new Date().toISOString(),
          openingAmount: command.openingAmount,
          expectedCashAmount: command.openingAmount,
          movements: [],
        }),
  registerCashMovement: (command: { type: string; amount: number; description: string; reason: string }) =>
    isApiConfigured ? postJson('/api/v1/admin/cashier/movements', command) : Promise.resolve(demoResult),
  closeCashShift: (countedCashAmount: number, notes?: string) =>
    isApiConfigured ? postJson('/api/v1/admin/cashier/close', { countedCashAmount, notes }) : Promise.resolve({ ...demoResult, status: 'Closed' }),
  updateDevice: (device: Device) =>
    isApiConfigured ? putJson(`/api/v1/admin/devices/${device.id}`, device) : Promise.resolve({ ...demoResult, status: device.status }),
  createCustomerTablet: (command: { name: string; platform: string; linkedTableId: string }): Promise<DeviceProvisioning> =>
    isApiConfigured
      ? postJson('/api/v1/admin/devices/tablets', command)
      : Promise.resolve({
          device: {
            id: createUuid(),
            name: command.name,
            serialNumber: `TAB-${createUuid().slice(0, 12).toUpperCase()}`,
            type: 'CustomerTablet',
            platform: command.platform,
            status: 'Offline',
            isCharging: false,
            linkedTableId: command.linkedTableId,
            isLocked: false,
          },
          activationToken: createUuid().replaceAll('-', ''),
          expiresAt: new Date(Date.now() + 30 * 60_000).toISOString(),
        }),
  provisionCustomerTablet: (id: string, linkedTableId: string): Promise<DeviceProvisioning> =>
    isApiConfigured
      ? postJson(`/api/v1/admin/devices/${id}/provision`, { linkedTableId })
      : Promise.resolve({
          device: { ...mockDevices.find((device) => device.id === id)!, linkedTableId },
          activationToken: createUuid().replaceAll('-', ''),
          expiresAt: new Date(Date.now() + 30 * 60_000).toISOString(),
        }),
  saveUser: (command: Partial<AdminUser> & { password?: string; phone?: string }): Promise<string> =>
    isApiConfigured
      ? command.id ? putJson<string, typeof command>(`/api/v1/admin/users/${command.id}`, command) : postJson<string, typeof command>('/api/v1/admin/users', command)
      : Promise.resolve(demoResult.id),
  saveRole: (command: Partial<AdminRole>): Promise<string> =>
    isApiConfigured
      ? command.id ? putJson<string, typeof command>(`/api/v1/admin/roles/${command.id}`, command) : postJson<string, typeof command>('/api/v1/admin/roles', command)
      : Promise.resolve(demoResult.id),
}
