import userEvent from '@testing-library/user-event'
import { render, screen } from '@testing-library/react'
import { useState } from 'react'
import { describe, expect, it } from 'vitest'
import { CurrencyInput } from './CurrencyInput'

function CurrencyInputHarness() {
  const [value, setValue] = useState(0)
  return (
    <CurrencyInput
      aria-label="Valor"
      value={value}
      onCurrencyValueChange={setValue}
    />
  )
}

describe('CurrencyInput', () => {
  it('substitui o zero inicial quando o usuário começa a digitar', async () => {
    const user = userEvent.setup()
    render(<CurrencyInputHarness />)
    const input = screen.getByLabelText('Valor')

    await user.click(input)
    await user.keyboard('250')

    expect(input).toHaveValue('R$ 250,00')
  })
})
