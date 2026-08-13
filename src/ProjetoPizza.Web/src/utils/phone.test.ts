import { describe, expect, it } from 'vitest'
import { formatPhone, phoneDigits } from './phone'

describe('phone presentation', () => {
  it('formats Brazilian mobile numbers', () => {
    expect(formatPhone('11999998877')).toBe('(11) 99999-8877')
  })

  it('formats Brazilian landline numbers already stored in the database', () => {
    expect(formatPhone('1133334455')).toBe('(11) 3333-4455')
  })

  it('keeps unsupported values unchanged and extracts digits for persistence', () => {
    expect(formatPhone('0800 1234')).toBe('0800 1234')
    expect(phoneDigits('(11) 99999-8877')).toBe('11999998877')
  })
})
