import { get, post, remove } from '@/infrastructure/http/client'

export type SerynLink = { id: string; nome: string; prefixo: string; escopos: string[]; criadoEm: string; expiraEm: string | null; ultimoUsoEm: string | null }
export type CreatedSerynLink = Omit<SerynLink, 'ultimoUsoEm'> & { token: string }

export const listSerynLinks = () => get<SerynLink[]>('/lumen/seryn-links')
export const createSerynLink = (body: { nome: string; escopos: string[]; expiraEm: string | null }) => post<CreatedSerynLink>('/lumen/seryn-links', body)
export const revokeSerynLink = (id: string) => remove(`/lumen/seryn-links/${id}`)
