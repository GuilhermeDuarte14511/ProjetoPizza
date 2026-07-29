import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { axe } from 'vitest-axe'
import { BillView } from './ClientViews'

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
