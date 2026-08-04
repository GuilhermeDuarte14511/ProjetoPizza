import { describe, expect, it } from 'vitest'
import { isNewClientOrder, type AdminResourceChanged } from './adminEvents'

function event(overrides: Partial<AdminResourceChanged> = {}): AdminResourceChanged {
  return {
    resource: 'orders',
    action: 'POST',
    source: 'client',
    occurredAt: new Date().toISOString(),
    ...overrides,
  }
}

describe('isNewClientOrder', () => {
  it('identifica pedido enviado pelo tablet', () => {
    expect(isNewClientOrder(event())).toBe(true)
  })

  it('ignora transições administrativas de pedidos', () => {
    expect(isNewClientOrder(event({ source: 'admin' }))).toBe(false)
  })

  it('ignora telemetria do tablet', () => {
    expect(isNewClientOrder(event({ resource: 'telemetry' }))).toBe(false)
  })
})
