import '../../../setup/vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, expect, it, vi } from 'vitest'
import { RecordsRoute } from '@/modules/records/routes/RecordsRoute'

afterEach(() => vi.unstubAllGlobals())

it('lista e exclui um registro somente após confirmação', async () => {
  const fetch = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input)
    if (url.endsWith('/records') && !init?.method) return new Response(JSON.stringify([{
      id: 'record-1', nome: 'Assinatura API', movimentacao: 'saida', tipoId: 'type-1', tipoNome: 'Assinatura',
      categoriaId: 'category-1', categoriaNome: 'Servidor', pagamentoId: 'payment-1', pagamentoNome: 'Pix',
      valorCentavos: 5990, data: '2027-01', recorrencia: 'mensal', ocorrencia: 'indefinida', parcelas: null,
    }]))
    if (url.endsWith('/auth/csrf')) return new Response(JSON.stringify({ token: 'csrf' }))
    if (url.endsWith('/records/record-1') && init?.method === 'DELETE') return new Response(null, { status: 204 })
    throw new Error(`Unexpected request: ${init?.method ?? 'GET'} ${url}`)
  })
  vi.stubGlobal('fetch', fetch)
  const user = userEvent.setup()
  render(<MemoryRouter><RecordsRoute /></MemoryRouter>)

  expect(await screen.findByText('Assinatura API')).toBeInTheDocument()
  expect(screen.getByText('Assinatura · Servidor · Pix')).toBeInTheDocument()
  expect(screen.getByText((_, element) => element?.classList.contains('record-value') === true && element.textContent?.includes('59,90') === true)).toBeInTheDocument()
  await user.click(screen.getByRole('button', { name: 'Excluir Assinatura API' }))
  expect(screen.getByRole('alertdialog', { name: 'Excluir registro?' })).toBeInTheDocument()
  await user.click(screen.getByRole('button', { name: 'Cancelar' }))
  expect(screen.getByText('Assinatura API')).toBeInTheDocument()
  await user.click(screen.getByRole('button', { name: 'Excluir Assinatura API' }))
  await user.click(screen.getByRole('button', { name: 'Excluir registro' }))
  await waitFor(() => expect(screen.getByText('Nenhum registro cadastrado')).toBeInTheDocument())
})
