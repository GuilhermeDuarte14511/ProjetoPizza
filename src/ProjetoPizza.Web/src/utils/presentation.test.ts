import { describe, expect, it } from 'vitest'
import { translateEnum } from './presentation'

describe('translateEnum', () => {
  it.each([
    ['Completed', 'Concluído'],
    ['InProduction', 'Em preparo'],
    ['HighestFlavorPrice', 'Maior valor entre os sabores'],
    ['Withdrawal', 'Sangria'],
    ['ready', 'Pronto'],
    ['start', 'Início do preparo'],
    ['confirm', 'Confirmação'],
    ['KitchenTicket', 'Ticket da cozinha'],
  ])('traduz %s para português', (value, expected) => {
    expect(translateEnum(value)).toBe(expected)
  })

  it('preserva valores desconhecidos para não ocultar dados do servidor', () => {
    expect(translateEnum('NovoStatus')).toBe('NovoStatus')
  })
})
