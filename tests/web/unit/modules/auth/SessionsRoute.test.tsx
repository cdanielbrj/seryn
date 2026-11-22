import '../../../setup/vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, expect, it, vi } from 'vitest'
import { SessionsRoute } from '@/modules/settings/routes/SessionsRoute'

afterEach(() => vi.unstubAllGlobals())

it('lista sessões e revoga outro acesso após confirmação', async () => {
  const current = { id: 'current', criadaEm: '2026-11-21T20:00:00Z', ultimoUsoEm: '2026-11-21T20:10:00Z', expiraEm: '2026-11-22T04:00:00Z', atual: true }
  const other = { ...current, id: 'other', atual: false }
  const fetch = vi.fn()
    .mockResolvedValueOnce(new Response(JSON.stringify([current, other])))
    .mockResolvedValueOnce(new Response(JSON.stringify({ token: 'csrf' })))
    .mockResolvedValueOnce(new Response(null, { status: 204 }))
    .mockResolvedValueOnce(new Response(JSON.stringify([current])))
  vi.stubGlobal('fetch', fetch)
  render(<SessionsRoute />)
  await screen.findByText('Outra sessão')
  await userEvent.click(screen.getByRole('button', { name: 'Encerrar' }))
  await userEvent.click(screen.getByRole('button', { name: 'Encerrar sessões' }))
  await waitFor(() => expect(screen.queryByText('Outra sessão')).not.toBeInTheDocument())
  expect(fetch.mock.calls[2][0]).toBe('/api/v1/auth/sessions/other')
  expect(fetch.mock.calls[2][1].method).toBe('DELETE')
})
