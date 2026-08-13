import type { ClientCartItem } from '../../types/client'

const cartStoragePrefix = 'projeto-pizza.client-cart'
const orderDraftStoragePrefix = 'projeto-pizza.client-order-draft'

export interface ClientOrderDraft {
  requestId: string
  fingerprint: string
  attemptedAt: string
}

export function getClientCartStorageKey(tableSessionId: string) {
  return `${cartStoragePrefix}.${tableSessionId}`
}

export function loadClientCart(tableSessionId: string): ClientCartItem[] {
  try {
    const key = getClientCartStorageKey(tableSessionId)
    const value = localStorage.getItem(key) ?? sessionStorage.getItem(key)
    return value ? JSON.parse(value) as ClientCartItem[] : []
  } catch {
    return []
  }
}

export function saveClientCart(tableSessionId: string, cart: ClientCartItem[]) {
  const key = getClientCartStorageKey(tableSessionId)
  localStorage.setItem(key, JSON.stringify(cart))
  sessionStorage.removeItem(key)
}

export function clearClientCart(tableSessionId: string) {
  const key = getClientCartStorageKey(tableSessionId)
  localStorage.removeItem(key)
  sessionStorage.removeItem(key)
}

export function getClientOrderDraftStorageKey(tableSessionId: string) {
  return `${orderDraftStoragePrefix}.${tableSessionId}`
}

export function loadClientOrderDraft(tableSessionId: string): ClientOrderDraft | undefined {
  try {
    const value = localStorage.getItem(getClientOrderDraftStorageKey(tableSessionId))
    return value ? JSON.parse(value) as ClientOrderDraft : undefined
  } catch {
    return undefined
  }
}

export function saveClientOrderDraft(tableSessionId: string, draft: ClientOrderDraft) {
  localStorage.setItem(getClientOrderDraftStorageKey(tableSessionId), JSON.stringify(draft))
}

export function clearClientOrderDraft(tableSessionId: string) {
  localStorage.removeItem(getClientOrderDraftStorageKey(tableSessionId))
}
