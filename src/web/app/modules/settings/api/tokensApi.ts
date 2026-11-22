import { get, post, remove } from '@/infrastructure/http/client'

export type ApiToken = { id: string; nome: string; prefixo: string; escopos: string[]; criadoEm: string; expiraEm: string | null; ultimoUsoEm: string | null }
export type CreatedApiToken = Omit<ApiToken, 'ultimoUsoEm'> & { token: string }

export const listTokens = () => get<ApiToken[]>('/auth/tokens')
export const createToken = (body: { nome: string; escopos: string[]; expiraEm: string | null }) => post<CreatedApiToken>('/auth/tokens', body)
export const revokeToken = (id: string) => remove(`/auth/tokens/${id}`)
