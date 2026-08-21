import { cleanup, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { adminService } from '../../services/adminService'
import type { CustomerDetail } from '../../types/admin'
import { CustomerDetailModal } from './CustomerDetailModal'

const detail: CustomerDetail = {
  customer: {
    id: 'customer-1', name: 'Ana Souza', phone: '11999998877', birthDate: '1992-05-18', isActive: true,
    loyaltyPoints: 100, lifetimeSpend: 240, orderCount: 3, lastOrderAt: '2026-08-19T20:00:00Z', createdAt: '2025-01-10T12:00:00Z',
  },
  loyaltyPointsExpireAt: '2027-08-20T12:00:00Z',
  benefitBalance: 5,
  averageTicket: 80,
  loyaltyTransactions: [{
    id: 'transaction-1', customerId: 'customer-1', customerName: 'Ana Souza', type: 'Earned', points: 100,
    balanceAfter: 100, discount: 0, description: 'Pontos ganhos no pedido concluído', occurredAt: '2026-08-19T20:00:00Z',
  }],
  orders: [{
    id: 'order-1', number: 1047, fulfillment: 'Delivery', status: 'Completed', subtotal: 90, discount: 10,
    total: 80, couponCode: 'VOLTE10', loyaltyPointsRedeemed: 0, createdAt: '2026-08-19T20:00:00Z',
  }],
  coupons: [{
    id: 'coupon-1', code: 'VOLTE10', name: 'Volte esta semana', discountType: 'Percentage', value: 10,
    minimumOrderAmount: 50, startsAt: '2026-08-01T00:00:00Z', endsAt: '2026-08-31T23:59:59Z',
    availability: 'Available', timesUsedByCustomer: 1, lastUsedAt: '2026-08-19T20:00:00Z',
  }],
}

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
})

describe('CustomerDetailModal', () => {
  it('apresenta relacionamento e alterna entre pedidos e cupons', async () => {
    const user = userEvent.setup()
    vi.spyOn(adminService, 'customerDetail').mockResolvedValue(detail)

    render(<CustomerDetailModal customerId="customer-1" canWrite onClose={() => undefined} onEdit={() => undefined} onCustomerChanged={() => undefined} />)

    expect(await screen.findByText('Ana Souza')).toBeVisible()
    expect(screen.getByText('100')).toBeVisible()
    expect(screen.getByText(/Equivale a até/)).toHaveTextContent('R$ 5,00')

    await user.click(screen.getByRole('button', { name: /Pedidos1/ }))
    expect(screen.getByText('#1047')).toBeVisible()
    expect(screen.getByText('Entrega')).toBeVisible()

    await user.click(screen.getByRole('button', { name: /Cupons1/ }))
    expect(screen.getByText('VOLTE10')).toBeVisible()
    expect(screen.getByText('Disponível')).toBeVisible()
  })

  it('envia ajuste justificado e atualiza o cliente', async () => {
    const user = userEvent.setup()
    const updated = { ...detail, customer: { ...detail.customer, loyaltyPoints: 125 }, benefitBalance: 6.25 }
    vi.spyOn(adminService, 'customerDetail').mockResolvedValue(detail)
    const adjust = vi.spyOn(adminService, 'adjustCustomerLoyaltyPoints').mockResolvedValue(updated)
    const onCustomerChanged = vi.fn()

    render(<CustomerDetailModal customerId="customer-1" canWrite onClose={() => undefined} onEdit={() => undefined} onCustomerChanged={onCustomerChanged} />)
    await screen.findByText('Ana Souza')
    await user.click(screen.getByRole('button', { name: 'Ajustar pontos' }))
    const adjustmentDialog = await screen.findByRole('dialog', { name: 'Ajustar pontos' })
    await user.type(within(adjustmentDialog).getByLabelText(/Quantidade de pontos/), '25')
    await user.type(within(adjustmentDialog).getByLabelText(/Motivo do ajuste/), 'Correção do pedido 1047')
    await user.click(within(adjustmentDialog).getByRole('button', { name: 'Confirmar ajuste' }))

    await waitFor(() => expect(adjust).toHaveBeenCalledWith('customer-1', { points: 25, reason: 'Correção do pedido 1047' }))
    expect(onCustomerChanged).toHaveBeenCalledWith(updated.customer)
  })
})
