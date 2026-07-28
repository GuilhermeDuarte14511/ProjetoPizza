export const currency = new Intl.NumberFormat('pt-BR', {
  style: 'currency',
  currency: 'BRL',
})

export function splitMoneyEqually(total: number, people: number): number[] {
  const safePeople = Math.max(1, Math.trunc(people))
  const totalCents = Math.round(total * 100)
  const baseCents = Math.floor(totalCents / safePeople)
  const remainder = totalCents % safePeople

  return Array.from(
    { length: safePeople },
    (_, index) => (baseCents + (index < remainder ? 1 : 0)) / 100,
  )
}
