export type Period = 'month' | 'semester' | 'year'
export type Reference = { year: number; month: number }
export const periods: { id: Period; label: string }[] = [
  { id: 'month', label: 'Mês' }, { id: 'semester', label: 'Semestre' }, { id: 'year', label: 'Ano' },
]
const months = ['Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho', 'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro']
export function periodLabel(period: Period, date: Reference) {
  if (period === 'year') return String(date.year)
  if (period === 'semester') return `${date.month <= 6 ? '1º' : '2º'} semestre ${date.year}`
  return `${months[date.month - 1]} ${date.year}`
}
export function movePeriod(period: Period, date: Reference, direction: number): Reference {
  const step = period === 'year' ? 12 : period === 'semester' ? 6 : 1
  const index = date.year * 12 + date.month - 1 + step * direction
  return { year: Math.floor(index / 12), month: ((index % 12) + 12) % 12 + 1 }
}
