import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { adminService } from '../services/adminService'
import type { Customer, ManagedOrder, RestaurantTable } from '../types/admin'
import { GlobalSearchPage } from './GlobalSearchPage'

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
  window.history.replaceState({}, '', '/')
})

describe('GlobalSearchPage', () => {
  it('agrupa resultados reais de pedidos e clientes sem diferenciar acentos', async () => {
    window.history.replaceState({}, '', '/admin/search?q=ana')
    vi.spyOn(adminService, 'orders').mockResolvedValue([{
      id: 'order-1', number: 27, channel: 'Administrative', fulfillment: 'Pickup', status: 'Submitted',
      customerName: 'Ana Souza', subtotal: 40, discount: 0, total: 40, createdAt: '2026-08-20T12:00:00Z',
      items: [{ id: 'item-1', name: 'Pizza Margherita', quantity: 1, unitPrice: 40, totalPrice: 40, status: 'Pending' }],
    }] as ManagedOrder[])
    vi.spyOn(adminService, 'tables').mockResolvedValue([{
      id: 'table-1', number: 12, name: 'Mesa 12', capacity: 4, area: 'Salão', status: 'Livre', currentTotal: 0, hasPendingCall: false,
    }] as RestaurantTable[])
    vi.spyOn(adminService, 'customers').mockResolvedValue([{
      id: 'customer-1', name: 'Ána Souza', phone: '11999998877', birthDate: '1990-01-01', isActive: true,
      loyaltyPoints: 40, lifetimeSpend: 40, orderCount: 1, createdAt: '2026-08-01T12:00:00Z',
    }] as Customer[])
    const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })

    render(<QueryClientProvider client={queryClient}><GlobalSearchPage /></QueryClientProvider>)

    expect(await screen.findByText('Pedido #27')).toBeVisible()
    expect(await screen.findByText('Ána Souza')).toBeVisible()
    expect(screen.getByRole('link', { name: /Pedido #27/ })).toHaveAttribute('href', '/admin/orders?search=27')
    expect(screen.queryByText('Mesa 12')).not.toBeInTheDocument()
  })
})
