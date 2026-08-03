import { act, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { axe } from 'vitest-axe'
import { BillView, StandbyView, ThankYouView } from './ClientViews'

const bill = {
  status: 'Open',
  subtotal: 100,
  serviceFeePercentage: 10,
  serviceFeeAmount: 10,
  total: 110,
  paid: 0,
  remaining: 110,
}

describe('BillView', () => {
  it('envia a quantidade de pessoas escolhida para o backend', async () => {
    const user = userEvent.setup()
    const onRequest = vi.fn()
    render(<BillView bill={bill} guestCount={3} isSubmitting={false} onRequest={onRequest} />)

    await user.click(screen.getByRole('radio', { name: /dividir a conta/i }))
    await user.click(screen.getByRole('button', { name: /aumentar pessoas/i }))
    await user.click(screen.getByRole('button', { name: /solicitar conta/i }))

    expect(onRequest).toHaveBeenCalledWith(4)
  })

  it('não possui violações básicas de acessibilidade', async () => {
    render(<main><BillView bill={bill} guestCount={2} isSubmitting={false} onRequest={() => undefined} /></main>)

    expect((await axe(document.body)).violations).toEqual([])
  })
})

describe('StandbyView', () => {
  it('inicia uma nova comanda com a quantidade de pessoas escolhida', async () => {
    const user = userEvent.setup()
    const onStart = vi.fn()
    render(
      <StandbyView
        session={{
          deviceId: 'device-1',
          restaurantName: 'Forno 27',
          tableNumber: 12,
          tableName: 'Mesa 12',
          guestCount: 0,
          status: 'Idle',
          clearTabletAfterTableClose: true,
        }}
        isSubmitting={false}
        onStart={onStart}
        onLogout={() => undefined}
      />,
    )

    await user.click(screen.getByRole('button', { name: /toque para iniciar seu pedido/i }))
    await user.click(screen.getByRole('button', { name: /aumentar quantidade/i }))
    await user.click(screen.getByRole('button', { name: /confirmar e ver cardápio/i }))

    expect(onStart).toHaveBeenCalledWith(3)
  })
})

describe('ThankYouView', () => {
  it('retorna automaticamente para a espera depois de vinte segundos', () => {
    vi.useFakeTimers()
    const onFinish = vi.fn()
    render(<ThankYouView onFinish={onFinish} />)

    act(() => vi.advanceTimersByTime(20_000))

    expect(onFinish).toHaveBeenCalledTimes(1)
    vi.useRealTimers()
  })
})
