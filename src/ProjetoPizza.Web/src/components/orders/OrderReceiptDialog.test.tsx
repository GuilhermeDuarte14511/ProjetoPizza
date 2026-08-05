import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { OrderReceipt } from '../../types/admin'
import { OrderReceiptDialog } from './OrderReceiptDialog'

const receipt: OrderReceipt = {
  id: '10000000-0000-0000-0000-000000000001',
  number: 27,
  customerName: 'Ana Souza',
  customerPhone: '11999998877',
  fulfillment: 'Delivery',
  deliveryAddress: 'Rua das Flores, 27 - Centro',
  placedAt: '2026-08-05T20:30:00-03:00',
  subtotal: 90,
  deliveryFee: 8,
  discount: 10,
  total: 88,
  notes: 'Entregar na portaria.',
  items: [{
    id: '20000000-0000-0000-0000-000000000001',
    name: 'Pizza grande · 2 sabores',
    quantity: 2,
    unitPrice: 45,
    totalPrice: 90,
    notes: 'Bem assada.',
    details: ['Sabores: Calabresa / Marguerita', 'Adicional: 1x Bacon (+ R$ 5,00)'],
  }],
}

describe('OrderReceiptDialog', () => {
  it('mostra todos os dados necessários na comanda não fiscal', () => {
    render(<OrderReceiptDialog receipt={receipt} onClose={() => undefined} />)

    expect(screen.getByText('COMANDA NÃO FISCAL')).toBeInTheDocument()
    expect(screen.getByText('2x Pizza grande · 2 sabores')).toBeInTheDocument()
    expect(screen.getByText('• Adicional: 1x Bacon (+ R$ 5,00)')).toBeInTheDocument()
    expect(screen.getByText('OBS:')).toBeInTheDocument()
    expect(screen.getByText('Rua das Flores, 27 - Centro')).toBeInTheDocument()
    expect(screen.getByText('- R$ 10,00')).toBeInTheDocument()
    expect(screen.getByText('R$ 88,00')).toBeInTheDocument()
  })

  it('aciona a impressão do navegador', async () => {
    const user = userEvent.setup()
    const print = vi.spyOn(window, 'print').mockImplementation(() => undefined)
    render(<OrderReceiptDialog receipt={receipt} onClose={() => undefined} />)

    await user.click(screen.getByRole('button', { name: /imprimir comanda/i }))

    expect(print).toHaveBeenCalledOnce()
    print.mockRestore()
  })
})
