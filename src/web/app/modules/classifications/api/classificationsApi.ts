import { get, patch, post, remove } from '@/infrastructure/http/client'
export { ApiError } from '@/infrastructure/http/client'
import type { Category, Classification } from '../model/classification'

export type ClassificationKind = 'types' | 'payment-methods'

export function getClassifications(kind: ClassificationKind) { return get<Classification[]>(`/${kind}`) }
export function createClassification(kind: ClassificationKind, nome: string) { return post<Classification>(`/${kind}`, { nome }) }
export function updateClassification(kind: ClassificationKind, id: string, nome: string) { return patch<Classification>(`/${kind}/${id}`, { nome }) }
export function deleteClassification(kind: ClassificationKind, id: string) { return remove(`/${kind}/${id}`) }

export function getCategories() { return get<Category[]>('/categories') }
export function createCategory(nome: string, categoriaPaiId: string | null) { return post<Category>('/categories', { nome, categoriaPaiId }) }
export function updateCategory(id: string, input: { nome: string; categoriaPaiId: string | null }) { return patch<Category>(`/categories/${id}`, input) }
export function deleteCategory(id: string) { return remove(`/categories/${id}`) }
