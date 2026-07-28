import { describe, expect, it } from 'vitest'
import { categorySchema, paymentSchema, pizzaSizeSchema, productSchema } from './formSchemas'

describe('schemas dos formulários administrativos', () => {
  it('rejeita produto sem nome, categoria e SKU', () => {
    const result = productSchema.safeParse({
      name: '',
      categoryId: '',
      sku: '',
      type: 'Standard',
      basePrice: -1,
      isActive: true,
      isAvailable: true,
      isFeatured: false,
    })
    expect(result.success).toBe(false)
  })

  it('rejeita slug fora do formato usado nas URLs', () => {
    const result = categorySchema.safeParse({
      name: 'Pizzas',
      slug: 'Pizzas Especiais',
      isActive: true,
      isVisibleOnTablet: true,
    })
    expect(result.success).toBe(false)
  })

  it('mantém o limite de três sabores por tamanho', () => {
    const result = pizzaSizeSchema.safeParse({
      name: 'Família',
      shortName: 'F',
      slices: 12,
      diameterCm: 45,
      basePrice: 80,
      maxFlavors: 4,
      isActive: true,
    })
    expect(result.success).toBe(false)
  })

  it('aceita um pagamento válido', () => {
    const result = paymentSchema.safeParse({
      paymentMethodId: crypto.randomUUID(),
      amount: 50,
      receivedAmount: 50,
      externalReference: 'PIX-123',
    })
    expect(result.success).toBe(true)
  })
})
