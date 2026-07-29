import type { ClientCartItem } from '../../types/client'

const cartStoragePrefix = 'projeto-pizza.client-cart'

export function getClientCartStorageKey(tableSessionId: string) {
  return `${cartStoragePrefix}.${tableSessionId}`
}

export function loadClientCart(tableSessionId: string): ClientCartItem[] {
  try {
    const value = sessionStorage.getItem(getClientCartStorageKey(tableSessionId))
    return value ? JSON.parse(value) as ClientCartItem[] : []
  } catch {
    return []
  }
}

export function saveClientCart(tableSessionId: string, cart: ClientCartItem[]) {
  sessionStorage.setItem(getClientCartStorageKey(tableSessionId), JSON.stringify(cart))
}

export function clearClientCart(tableSessionId: string) {
  sessionStorage.removeItem(getClientCartStorageKey(tableSessionId))
}
