import { get, patch, post, remove } from '@/infrastructure/http/client'
export { ApiError } from '@/infrastructure/http/client'
import type { Objective, ObjectiveInput } from '../model/objective'
export const getObjectives = () => get<Objective[]>('/objectives')
export const getObjective = (id: string) => get<Objective>(`/objectives/${id}`)
export const createObjective = (input: ObjectiveInput) => post<Objective>('/objectives', input)
export const updateObjective = (id: string, input: ObjectiveInput) => patch<Objective>(`/objectives/${id}`, input)
export const deleteObjective = (id: string) => remove(`/objectives/${id}`)
