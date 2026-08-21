export interface ClientSession {
  deviceId: string
  tableSessionId?: string
  restaurantName: string
  tableNumber: number
  tableName: string
  guestCount: number
  status: string
  waiterName?: string
  clearTabletAfterTableClose: boolean
}

export interface ClientCategory {
  id: string
  name: string
  slug: string
  icon?: string
  displayOrder: number
}

export interface ClientProduct {
  id: string
  categoryId: string
  name: string
  description?: string
  productType: string
  price: number
  imageUrl?: string
  isFeatured: boolean
  isPopular: boolean
  preparationTimeMinutes: number
  usesCustomExtras: boolean
  complements: ClientPizzaExtra[]
}

export interface ClientPizzaSize {
  id: string
  name: string
  shortName: string
  slices: number
  diameterCm: number
  basePrice: number
  maxFlavors: number
}

export interface ClientPizzaFlavorPrice {
  pizzaSizeId: string
  price: number
  additionalPrice: number
  isAvailable: boolean
}

export interface ClientIngredient {
  id: string
  name: string
  isRemovable: boolean
  isAllergen: boolean
  allergenDescription?: string
}

export interface ClientPizzaFlavor {
  id: string
  categoryId: string
  name: string
  description?: string
  flavorType: string
  isPremium: boolean
  isVegetarian: boolean
  isAvailable: boolean
  soldOutReason?: string
  imageUrl?: string
  prices: ClientPizzaFlavorPrice[]
  ingredients: ClientIngredient[]
  extras: ClientPizzaExtra[]
}

export interface ClientPizzaCrust {
  id: string
  name: string
  description?: string
  isAvailable: boolean
  prices: Array<{ pizzaSizeId: string; fullPrice: number; halfPrice: number }>
}

export interface ClientPizzaExtra {
  id: string
  name: string
  description?: string
  price: number
  maxQuantity: number
  isAllergen: boolean
  allergenDescription?: string
}

export interface ClientPizzaCatalog {
  globalMaxFlavors: number
  pricingPolicy: string
  allowSweetAndSavoryMix: boolean
  allowExtrasPerFlavor: boolean
  allowRepeatedFlavors: boolean
  sizes: ClientPizzaSize[]
  flavors: ClientPizzaFlavor[]
  crusts: ClientPizzaCrust[]
  extras: ClientPizzaExtra[]
}

export interface ClientCatalog {
  categories: ClientCategory[]
  products: ClientProduct[]
  pizza: ClientPizzaCatalog
  serviceFeePercentage: number
}

export interface ClientOrderItem {
  id: string
  productId: string
  name: string
  quantity: number
  unitPrice: number
  totalPrice: number
  status: string
  notes?: string
  pizza?: {
    sizeId: string
    size: string
    flavors: Array<{ id: string; name: string }>
    crustId?: string
    crust?: string
    secondCrustId?: string
    secondCrust?: string
  }
  modifiers: Array<{
    type: string
    name: string
    quantity: number
    unitPrice: number
    totalPrice: number
    pizzaFlavorId?: string
    ingredientId?: string
  }>
}

export interface ClientServiceCall {
  id: string
  serviceCallTypeId: string
  typeName: string
  status: string
  createdAt: string
  acknowledgedAt?: string
  completedAt?: string
}

export interface ClientOrder {
  id: string
  number: number
  status: string
  placedAt?: string
  subtotal: number
  total: number
  items: ClientOrderItem[]
}

export interface ClientBill {
  id?: string
  status: string
  subtotal: number
  serviceFeePercentage: number
  serviceFeeAmount: number
  total: number
  paid: number
  remaining: number
  requestedAt?: string
  requestedSplitCount?: number
}

export interface ClientState {
  session: ClientSession
  serviceCalls: ClientServiceCall[]
  orders: ClientOrder[]
  bill: ClientBill
}

export interface ClientBootstrap {
  session: ClientSession
  catalog: ClientCatalog
  serviceCallTypes: Array<{ id: string; code: string; name: string }>
  serviceCalls: ClientServiceCall[]
  orders: ClientOrder[]
  bill: ClientBill
}

export interface StartClientTableSession {
  guestCount: number
}

export interface ClientActivation {
  token: string
  bootstrap: ClientBootstrap
}

export interface ClientTelemetry {
  batteryPercentage?: number
  isCharging: boolean
  networkStatus: string
  appVersion: string
}

export interface PizzaCartConfiguration {
  sizeId: string
  sizeName: string
  flavorIds: string[]
  flavorNames: string[]
  crustId?: string
  crustName?: string
  secondCrustId?: string
  secondCrustName?: string
  removedIngredientIds: string[]
  extraIngredients: Array<{
    ingredientId: string
    ingredientName: string
    pizzaFlavorId?: string
    pizzaFlavorName?: string
    quantity: number
    unitPrice: number
  }>
}

export interface ClientCartItem {
  key: string
  productId: string
  name: string
  quantity: number
  unitPrice: number
  notes?: string
  imageUrl?: string
  pizza?: PizzaCartConfiguration
}

export interface SubmitClientOrder {
  requestId: string
  notes?: string
  customerPhone?: string
  customerBirthDate?: string
  couponCode?: string
  loyaltyPoints?: number
  items: Array<{
    productId: string
    quantity: number
    notes?: string
    pizza?: {
      sizeId: string
      flavorIds: string[]
      crustId?: string
      secondCrustId?: string
      removedIngredientIds: string[]
      extraIngredients: Array<{
        ingredientId: string
        pizzaFlavorId?: string
        quantity: number
      }>
    }
  }>
}
export interface ClientLoyaltyQuote { customerName: string; points: number; expiresAt?: string; couponDiscount: number; loyaltyDiscount: number; totalBenefits: number }
