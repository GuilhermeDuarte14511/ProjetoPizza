import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { adminService } from '../../services/adminService'
import type { CashShift } from '../../types/admin'
import { AdminLayout } from './AdminLayout'

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
})

function renderLayout() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })

  return render(
    <QueryClientProvider client={queryClient}>
      <AdminLayout><p>Conteúdo</p></AdminLayout>
    </QueryClientProvider>,
  )
}

describe('AdminLayout cash status', () => {
  it('exibe caixa fechado quando não existe turno atual', async () => {
    vi.spyOn(adminService, 'cashShift').mockResolvedValue(null)
    renderLayout()

    expect(await screen.findByRole('button', { name: 'Caixa fechado. Acessar caixa' })).toBeVisible()
  })

  it('exibe caixa aberto somente quando o turno atual está aberto', async () => {
    vi.spyOn(adminService, 'cashShift').mockResolvedValue({ status: 'Open' } as CashShift)
    renderLayout()

    expect(await screen.findByRole('button', { name: 'Caixa aberto. Acessar caixa' })).toBeVisible()
  })
})
