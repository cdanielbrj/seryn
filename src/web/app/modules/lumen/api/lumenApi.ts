import { get, post, remove, put } from '@/infrastructure/http/client'

export type ChatConnection = { disponivel: boolean; provedor: string; modelo: string | null }
export type ChatMessage = { papel: 'usuario' | 'seryn'; conteudo: string }
export type ChatQuery = { ferramenta: string; argumentos: unknown; resultado: unknown; fontes: { nome: string; caminho: string }[] }
export type ChatResponse = {
  resposta: string; modelo: string; consultas: ChatQuery[]
  uso: { entradaTokens: number; saidaTokens: number; custoUsd: number | null }
}
export type OpticalConnection = { id: string; nome: string; provedor: string; modelo: string; chave: string; padrao: boolean; criadoEm: string; atualizadoEm: string }
export type OpticalConnectionInput = { nome: string; provedor: string; modelo: string; chave: string; padrao: boolean }

export function getChatConnection(signal?: AbortSignal) { return get<ChatConnection>('/lumen/chat/status', signal) }
export function sendChat(mensagens: ChatMessage[], referencia: string, modelo: string, sessao: string, signal: AbortSignal) {
  return post<ChatResponse>('/lumen/chat', { mensagens, referencia, modelo, sessao }, signal)
}
export const listOpticalConnections = () => get<OpticalConnection[]>('/lumen/optical-links')
export const createOpticalConnection = (body: OpticalConnectionInput) => post<OpticalConnection>('/lumen/optical-links', body)
export const updateOpticalConnection = (id: string, body: OpticalConnectionInput) => put<OpticalConnection>(`/lumen/optical-links/${id}`, body)
export const deleteOpticalConnection = (id: string) => remove(`/lumen/optical-links/${id}`)
