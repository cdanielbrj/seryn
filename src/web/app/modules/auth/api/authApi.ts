import { get, post } from '@/infrastructure/http/client'
export { ApiError } from '@/infrastructure/http/client'

export type Session = { username: string }

export function getAuthStatus() { return get<{ configured: boolean }>('/auth/status') }
export function getSession() { return get<Session>('/auth/session') }
export function login(credentials: { username: string; password: string }) {
  return post<Session>('/auth/login', credentials)
}
export function logout() { return post<void>('/auth/logout') }
