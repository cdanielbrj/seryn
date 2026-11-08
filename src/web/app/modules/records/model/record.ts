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
