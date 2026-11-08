import type { Direction, Occurrence, Recurrence } from './record'

export type RecordFormState = {
  nome: string
  movimentacao: Direction
  tipoId: string
  categoriaId: string
  pagamentoId: string
  valor: string
  data: string
  recorrencia: Recurrence
  ocorrencia: Occurrence
  parcelas: string
  modoValor: 'por_ocorrencia' | 'total'
}

export type RecordFieldErrors = Record<string, string[]>
export type UpdateRecordForm = <K extends keyof RecordFormState>(key: K, value: RecordFormState[K]) => void

export const emptyRecordForm: RecordFormState = {
  nome: '', movimentacao: 'saida', tipoId: '', categoriaId: '', pagamentoId: '', valor: '', data: '',
  recorrencia: 'unica', ocorrencia: 'indefinida', parcelas: '2', modoValor: 'por_ocorrencia',
}
