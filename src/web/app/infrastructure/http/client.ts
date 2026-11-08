export class ApiError extends Error {
  constructor(message: string, public status: number, public errors: Record<string, string[]> = {}) { super(message) }
}

async function parse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const problem = await response.json().catch(() => ({})) as { title?: string; errors?: Record<string, string[]> }
    throw new ApiError(problem.title ?? 'Não foi possível concluir. Tente novamente.', response.status, problem.errors)
  }
  return response.status === 204 ? undefined as T : response.json() as Promise<T>
}

async function mutate<T>(method: 'POST' | 'PATCH' | 'DELETE', path: string, body?: unknown): Promise<T> {
  const { token } = await get<{ token: string }>('/auth/csrf')
  return parse<T>(await fetch(`/api/v1${path}`, {
    method, credentials: 'same-origin', cache: 'no-store',
    headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': token },
    body: body === undefined ? undefined : JSON.stringify(body),
  }))
}

export async function get<T>(path: string): Promise<T> {
  return parse<T>(await fetch(`/api/v1${path}`, { credentials: 'same-origin', cache: 'no-store' }))
}

export async function post<T>(path: string, body?: unknown): Promise<T> {
  return mutate<T>('POST', path, body ?? {})
}

export async function patch<T>(path: string, body: unknown): Promise<T> { return mutate<T>('PATCH', path, body) }
export async function remove(path: string): Promise<void> { return mutate<void>('DELETE', path) }
