import { beforeEach, describe, expect, it } from 'vitest'
import {
  clearClientCart,
  getClientCartStorageKey,
  loadClientCart,
  saveClientCart,
} from './clientCartStorage'

describe('client cart storage', () => {
  beforeEach(() => sessionStorage.clear())

  it('isolates carts by table session', () => {
    saveClientCart('session-a', [{ key: 'a', productId: 'product-a', name: 'Pizza', quantity: 1, unitPrice: 50 }])
    saveClientCart('session-b', [{ key: 'b', productId: 'product-b', name: 'Bebida', quantity: 2, unitPrice: 10 }])

    expect(loadClientCart('session-a')).toHaveLength(1)
    expect(loadClientCart('session-a')[0].productId).toBe('product-a')
    expect(loadClientCart('session-b')[0].productId).toBe('product-b')
  })

  it('clears only the completed table session', () => {
    saveClientCart('session-a', [{ key: 'a', productId: 'product-a', name: 'Pizza', quantity: 1, unitPrice: 50 }])
    saveClientCart('session-b', [{ key: 'b', productId: 'product-b', name: 'Bebida', quantity: 1, unitPrice: 10 }])

    clearClientCart('session-a')

    expect(sessionStorage.getItem(getClientCartStorageKey('session-a'))).toBeNull()
    expect(loadClientCart('session-b')).toHaveLength(1)
  })
})
