export class ApiError extends Error {
  constructor(message: string, public status: number) { super(message) }
}

async function parse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const problem = await response.json().catch(() => ({})) as { title?: string }
    throw new ApiError(problem.title ?? 'Não foi possível concluir. Tente novamente.', response.status)
  }
  return response.status === 204 ? undefined as T : response.json() as Promise<T>
}

export async function get<T>(path: string): Promise<T> {
  return parse<T>(await fetch(`/api/v1${path}`, { credentials: 'same-origin', cache: 'no-store' }))
}

export async function post<T>(path: string, body?: unknown): Promise<T> {
  // Fetch a token for the current identity: tokens issued before login cannot be reused after it.
  const { token } = await get<{ token: string }>('/auth/csrf')
  return parse<T>(await fetch(`/api/v1${path}`, {
    method: 'POST', credentials: 'same-origin', cache: 'no-store',
    headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': token },
    body: JSON.stringify(body ?? {}),
  }))
}
