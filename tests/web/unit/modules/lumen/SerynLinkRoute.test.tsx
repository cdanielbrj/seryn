import '../../../setup/vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, expect, it, vi } from 'vitest'
import { SerynLinkRoute } from '@/modules/lumen/seryn-link/SerynLinkRoute'

afterEach(() => vi.unstubAllGlobals())

it('creates a Seryn Link, reveals its secret once and refreshes the list', async () => {
  const created = { id: 'token-1', nome: 'MCP local', token: 'seryn_pat_secret', prefixo: 'seryn_pat_sec', escopos: ['data:read'], criadoEm: '2026-11-21T20:00:00Z', expiraEm: '2027-02-19T20:00:00Z', ultimoUsoEm: null }
  const fetch = vi.fn()
    .mockResolvedValueOnce(new Response(JSON.stringify([])))
    .mockResolvedValueOnce(new Response(JSON.stringify({ token: 'csrf' })))
    .mockResolvedValueOnce(new Response(JSON.stringify(created), { status: 201 }))
    .mockResolvedValueOnce(new Response(JSON.stringify([created])))
  vi.stubGlobal('fetch', fetch)
  render(<SerynLinkRoute />)
  await screen.findByText('Nenhum Seryn Link ativo.')
  await userEvent.type(screen.getByLabelText('Nome'), 'MCP local')
  await userEvent.click(screen.getByRole('button', { name: 'Criar Seryn Link' }))
  expect(await screen.findByText('seryn_pat_secret')).toBeInTheDocument()
  await waitFor(() => expect(screen.getByText('MCP local')).toBeInTheDocument())
  expect(fetch.mock.calls[2][0]).toBe('/api/v1/lumen/seryn-links')
  expect(fetch.mock.calls[2][1].headers['X-CSRF-TOKEN']).toBe('csrf')
})
