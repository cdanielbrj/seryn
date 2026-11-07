import '../../../setup/vitest'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { afterEach, expect, it, vi } from 'vitest'
import { RecordFormPage } from '@/features/records/RecordFormPage'

afterEach(() => vi.unstubAllGlobals())

it('exige classificações obrigatórias antes de mostrar o formulário', async () => {
  vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL) => {
    const url = String(input)
    if (url.endsWith('/types') || url.endsWith('/payment-methods') || url.endsWith('/categories'))
      return new Response(JSON.stringify([]))
    throw new Error(`Unexpected request: ${url}`)
  }))
  render(<MemoryRouter><RecordFormPage /></MemoryRouter>)
  expect(await screen.findByText('Falta organizar seu cadastro')).toBeInTheDocument()
  expect(screen.getByRole('link', { name: 'Cadastrar tipo' })).toHaveAttribute('href', '/tipos')
  expect(screen.getByRole('link', { name: 'Cadastrar pagamento' })).toHaveAttribute('href', '/pagamentos')
})

it('mostra campos condicionais e envia um registro definido em centavos', async () => {
  let submitted: Record<string, unknown> | undefined
  const fetch = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input)
    if (url.endsWith('/types')) return new Response(JSON.stringify([{ id: 'type-1', nome: 'Assinatura' }]))
    if (url.endsWith('/payment-methods')) return new Response(JSON.stringify([{ id: 'payment-1', nome: 'Pix' }]))
    if (url.endsWith('/categories')) return new Response(JSON.stringify([{ id: 'category-1', nome: 'Software', categoriaPaiId: null }]))
    if (url.endsWith('/auth/csrf')) return new Response(JSON.stringify({ token: 'csrf' }))
    if (url.endsWith('/records') && init?.method === 'POST') {
      submitted = JSON.parse(String(init.body)) as Record<string, unknown>
      return new Response(JSON.stringify({ id: 'record-1' }), { status: 201 })
    }
    throw new Error(`Unexpected request: ${init?.method ?? 'GET'} ${url}`)
  })
  vi.stubGlobal('fetch', fetch)
  const user = userEvent.setup()
  render(<MemoryRouter initialEntries={['/registros/novo']}><Routes><Route path="/registros/novo" element={<RecordFormPage />} /><Route path="/registros" element={<p>Lista de registros</p>} /></Routes></MemoryRouter>)

  expect(await screen.findByRole('heading', { name: 'Novo registro' })).toBeInTheDocument()
  expect(screen.queryByLabelText('Ocorrência')).not.toBeInTheDocument()
  expect(screen.getByLabelText(/Valor/)).toHaveValue('')
  await user.type(screen.getByLabelText('Nome'), 'API')
  await user.selectOptions(screen.getByLabelText('Tipo'), 'type-1')
  await user.selectOptions(screen.getByLabelText(/Categoria/), 'category-1')
  await user.selectOptions(screen.getByLabelText('Pagamento'), 'payment-1')
  fireEvent.change(screen.getByLabelText(/Valor/), { target: { value: '1.234,56' } })
  expect(screen.getByLabelText(/Valor/)).toHaveValue('1.234,56')
  await user.type(screen.getByLabelText('Data'), '2027-01')
  await user.selectOptions(screen.getByLabelText('Recorrência'), 'trimestral')
  expect(screen.getByLabelText('Ocorrência')).toBeInTheDocument()
  expect(screen.getByLabelText('Ocorrência')).toHaveValue('indefinida')
  await user.selectOptions(screen.getByLabelText('Ocorrência'), 'definida')
  expect(screen.getByLabelText('Parcelas')).toBeInTheDocument()
  await user.clear(screen.getByLabelText('Parcelas'))
  await user.type(screen.getByLabelText('Parcelas'), '4')
  await user.click(screen.getByRole('button', { name: 'Total' }))
  expect(screen.getByRole('button', { name: 'Total' })).toHaveAttribute('aria-pressed', 'true')
  expect(screen.getByText((_, element) => element?.tagName === 'DD' && element.textContent?.includes('308,64') === true)).toBeInTheDocument()
  await user.click(screen.getByRole('button', { name: 'Entrada' }))
  await user.click(screen.getByRole('button', { name: 'Salvar registro' }))

  await waitFor(() => expect(screen.getByText('Lista de registros')).toBeInTheDocument())
  expect(submitted).toMatchObject({
    nome: 'API', movimentacao: 'entrada', tipoId: 'type-1', categoriaId: 'category-1', pagamentoId: 'payment-1',
    valorCentavos: 123456, data: '2027-01', recorrencia: 'trimestral', ocorrencia: 'definida', parcelas: 4, modoValor: 'total',
  })
})
