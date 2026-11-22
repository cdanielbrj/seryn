import { get, post, remove } from '@/infrastructure/http/client'

export type LoginSession = { id: string; criadaEm: string; ultimoUsoEm: string; expiraEm: string; atual: boolean }

export const listSessions = () => get<LoginSession[]>('/auth/sessions')
export const revokeSession = (id: string) => remove(`/auth/sessions/${id}`)
export const revokeOtherSessions = () => post<{ revogadas: number }>('/auth/sessions/revoke-others')
