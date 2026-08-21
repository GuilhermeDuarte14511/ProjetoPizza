import type { ClientCatalog, ClientOrderItem } from './client'

export interface DeliveryCatalog {
  catalog: ClientCatalog
  deliveryFee: number
}

export interface DeliveryOrderPlaced {
  id: string
  number: number
  trackingToken: string
  status: string
  total: number
}

export interface DeliveryTracking {
  number: number
  orderStatus: string
  deliveryStatus: string
  customerName: string
  address: string
  driverName?: string
  placedAt: string
  dispatchedAt?: string
  deliveredAt?: string
  total: number
  items: Array<Pick<ClientOrderItem, 'name' | 'quantity' | 'status'>>
}
export interface DeliveryLoyaltyQuote { customerName: string; points: number; expiresAt?: string; couponDiscount: number; loyaltyDiscount: number; totalBenefits: number }
