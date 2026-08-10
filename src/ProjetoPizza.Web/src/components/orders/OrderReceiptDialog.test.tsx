import { render, screen, within } from '@testing-library/react'
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
  paidAmount: 88,
  changeAmount: 2,
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
  payments: [{ method: 'Dinheiro', amount: 88, receivedAmount: 90, changeAmount: 2, paidAt: '2026-08-05T20:31:00-03:00' }],
}

describe('OrderReceiptDialog', () => {
  it('mostra todos os dados necessários no comprovante sem valor fiscal', () => {
    render(<OrderReceiptDialog receipt={receipt} onClose={() => undefined} />)

    expect(screen.getByText('COMPROVANTE NÃO FISCAL')).toBeInTheDocument()
    expect(screen.getAllByText('2x Pizza grande · 2 sabores')).toHaveLength(2)
    expect(screen.getByText('• Adicional: 1x Bacon (+ R$ 5,00)')).toBeInTheDocument()
    expect(screen.getAllByText(/OBS:/)).not.toHaveLength(0)
    expect(screen.getByText('Rua das Flores, 27 - Centro')).toBeInTheDocument()
    expect(screen.getByText('- R$ 10,00')).toBeInTheDocument()
    expect(screen.getAllByText('R$ 88,00')).toHaveLength(2)
    expect(screen.getByText('R$ 2,00')).toBeInTheDocument()
    const kitchen = screen.getByRole('heading', { name: 'Comanda da cozinha' }).closest('section')!
    expect(within(kitchen).queryByText(/R\$ 5,00/)).not.toBeInTheDocument()
  })

  it('aciona a impressão do navegador', async () => {
    const user = userEvent.setup()
    const print = vi.spyOn(window, 'print').mockImplementation(() => undefined)
    render(<OrderReceiptDialog receipt={receipt} onClose={() => undefined} />)

    await user.click(screen.getByRole('button', { name: /imprimir comprovante/i }))

    expect(print).toHaveBeenCalledOnce()
    print.mockRestore()
  })

  it('envia comprovante e comanda para filas independentes', async () => {
    const user = userEvent.setup()
    const printCustomer = vi.fn().mockResolvedValue(undefined)
    const printKitchen = vi.fn().mockResolvedValue(undefined)
    render(<OrderReceiptDialog receipt={{ ...receipt, fulfillment: 'Pickup' }} onClose={() => undefined} onPrintCustomerReceipt={printCustomer} onPrintKitchenCommand={printKitchen} />)

    await user.click(screen.getByRole('button', { name: /imprimir comprovante/i }))
    await user.click(screen.getByRole('button', { name: /imprimir comanda/i }))

    expect(printCustomer).toHaveBeenCalledOnce()
    expect(printKitchen).toHaveBeenCalledOnce()
    expect(await screen.findAllByText('Enfileirado')).toHaveLength(2)
  })
})
