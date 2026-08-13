import { beforeEach, describe, expect, it } from 'vitest'
import {
  clearClientCart,
  clearClientOrderDraft,
  getClientCartStorageKey,
  getClientOrderDraftStorageKey,
  loadClientCart,
  loadClientOrderDraft,
  saveClientCart,
  saveClientOrderDraft,
} from './clientCartStorage'

describe('client cart storage', () => {
  beforeEach(() => {
    localStorage.clear()
    sessionStorage.clear()
  })

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

    expect(localStorage.getItem(getClientCartStorageKey('session-a'))).toBeNull()
    expect(loadClientCart('session-b')).toHaveLength(1)
  })

  it('keeps the idempotency key until the order is confirmed', () => {
    saveClientOrderDraft('session-a', {
      requestId: 'request-a',
      fingerprint: 'cart-a',
      attemptedAt: '2026-08-12T10:00:00.000Z',
    })

    expect(loadClientOrderDraft('session-a')?.requestId).toBe('request-a')
    clearClientOrderDraft('session-a')
    expect(localStorage.getItem(getClientOrderDraftStorageKey('session-a'))).toBeNull()
  })
})
