import { get, patch, post, remove } from '@/infrastructure/http/client'
export { ApiError } from '@/infrastructure/http/client'
import type { Category, Classification, Direction, Occurrence, RecordItem, Recurrence } from '../model/record'

export type SaveRecordInput = {
  nome: string
  movimentacao: Direction
  tipoId: string
  categoriaId: string | null
  pagamentoId: string
  valorCentavos: number
  data: string
  recorrencia: Recurrence
  ocorrencia: Occurrence
  parcelas: number | null
  modoValor: 'por_ocorrencia' | 'total'
}

export function getRecords() { return get<RecordItem[]>('/records') }
export function getRecord(id: string) { return get<RecordItem>(`/records/${id}`) }
export function createRecord(input: SaveRecordInput) { return post('/records', input) }
export function updateRecord(id: string, input: SaveRecordInput) { return patch(`/records/${id}`, input) }
export function deleteRecord(id: string) { return remove(`/records/${id}`) }

export function getRecordTypes() { return get<Classification[]>('/types') }
export function getPaymentMethods() { return get<Classification[]>('/payment-methods') }
export function getRecordCategories() { return get<Category[]>('/categories') }
