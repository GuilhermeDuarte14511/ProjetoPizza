import { describe, expect, it } from 'vitest'
import { splitMoneyEqually } from './money'

describe('splitMoneyEqually', () => {
  it('distributes cent remainders without changing the total', () => {
    const result = splitMoneyEqually(100, 3)

    expect(result).toEqual([33.34, 33.33, 33.33])
    expect(result.reduce((total, value) => total + value, 0)).toBeCloseTo(100)
  })
})
