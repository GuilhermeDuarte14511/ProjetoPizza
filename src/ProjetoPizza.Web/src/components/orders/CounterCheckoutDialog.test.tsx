import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import type { PaymentMethod } from '../../types/admin'
import { CounterCheckoutDialog } from './CounterCheckoutDialog'

const methods: PaymentMethod[] = [
  { id: 'cash', code: 'CASH', name: 'Dinheiro', requiresExternalReference: false, allowsChange: true, displayOrder: 1, isActive: true },
  { id: 'pix', code: 'PIX', name: 'Pix', requiresExternalReference: true, allowsChange: false, displayOrder: 2, isActive: true },
]

describe('CounterCheckoutDialog', () => {
  it('confirma o valor integral com a forma selecionada', async () => {
    const user = userEvent.setup()
    const onConfirm = vi.fn()
    render(<CounterCheckoutDialog open orderTotal={79.9} itemCount={2} customerName="Ana" methods={methods} saving={false} onClose={() => undefined} onConfirm={onConfirm} />)

    await user.click(screen.getByRole('button', { name: /confirmar pagamento de/i }))

    expect(onConfirm).toHaveBeenCalledWith({ paymentMethodId: 'cash', receivedAmount: 79.9, externalReference: undefined })
  })

  it('exige a referência de um pagamento confirmado externamente', async () => {
    const user = userEvent.setup()
    const onConfirm = vi.fn()
    render(<CounterCheckoutDialog open orderTotal={50} itemCount={1} customerName="Ana" methods={methods} saving={false} onClose={() => undefined} onConfirm={onConfirm} />)

    await user.click(screen.getByRole('radio', { name: /pix/i }))
    await user.click(screen.getByRole('button', { name: /confirmar pagamento/i }))

    expect(screen.getByRole('alert')).toHaveTextContent('Informe a referência da transação confirmada externamente.')
    expect(onConfirm).not.toHaveBeenCalled()
  })
})
