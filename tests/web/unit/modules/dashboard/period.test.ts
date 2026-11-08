import '../../../setup/vitest'
import { describe, expect, it } from 'vitest'
import { movePeriod, periodLabel } from '@/modules/dashboard/model/period'

describe('navegação civil do período', () => {
  it('atravessa anos em ambas as direções', () => {
    expect(movePeriod('month', { year: 2027, month: 1 }, -1)).toEqual({ year: 2026, month: 12 })
    expect(movePeriod('month', { year: 2026, month: 12 }, 1)).toEqual({ year: 2027, month: 1 })
    expect(movePeriod('year', { year: 2027, month: 2 }, -1)).toEqual({ year: 2026, month: 2 })
  })
  it('nomeia semestres de calendário e avança seis meses', () => {
    expect(periodLabel('semester', { year: 2027, month: 6 })).toBe('1º semestre 2027')
    expect(periodLabel('semester', { year: 2027, month: 7 })).toBe('2º semestre 2027')
    expect(movePeriod('semester', { year: 2027, month: 11 }, 1)).toEqual({ year: 2028, month: 5 })
    expect(periodLabel('year', { year: 2027, month: 3 })).toBe('2027')
  })
})
