export type Direction = 'entrada' | 'saida'
export type Recurrence = 'unica' | 'mensal' | 'trimestral' | 'semestral' | 'anual'
export type Occurrence = 'definida' | 'indefinida'

export type RecordItem = {
  id: string
  nome: string
  movimentacao: Direction
  tipoId: string
  tipoNome: string
  categoriaId: string | null
  categoriaNome: string | null
  pagamentoId: string
  pagamentoNome: string
  valorCentavos: number
  data: string
  recorrencia: Recurrence
  ocorrencia: Occurrence
  parcelas: number | null
}

export type Classification = { id: string; nome: string }
export type Category = Classification & { categoriaPaiId: string | null }

export const recurrenceLabels: Record<Recurrence, string> = {
  unica: 'Única', mensal: 'Mensal', trimestral: 'Trimestral', semestral: 'Semestral', anual: 'Anual',
}

export function formatMoney(cents: number) {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(cents / 100)
}

export function formatMonth(value: string) {
  const [year, month] = value.split('-').map(Number)
  return new Intl.DateTimeFormat('pt-BR', { month: 'short', year: 'numeric' })
    .format(new Date(year, month - 1, 1)).replace('.', '')
}

export function parseMoney(value: string) {
  let normalized = value.trim().replace(/\s/g, '')
  if (normalized.includes(',')) normalized = normalized.replace(/\./g, '').replace(',', '.')
  if (!/^\d+(\.\d{1,2})?$/.test(normalized)) return null
  const cents = Math.round(Number(normalized) * 100)
  return Number.isSafeInteger(cents) && cents > 0 ? cents : null
}

/** Formats typed or pasted digits as a Brazilian currency amount without a currency prefix. */
export function maskMoney(value: string) {
  const digits = value.replace(/\D/g, '').replace(/^0+(?=\d)/, '')
  if (!digits) return ''
  const padded = digits.padStart(3, '0')
  const integer = padded.slice(0, -2).replace(/\B(?=(\d{3})+(?!\d))/g, '.')
  return `${integer},${padded.slice(-2)}`
}
