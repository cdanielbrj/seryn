import '../../../setup/vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, expect, it, vi } from 'vitest'
import { ObjectivesRoute } from '@/modules/objectives/routes/ObjectivesRoute'

const values = [
  { id: '1', nome: 'Notebook', valorEstimadoCentavos: 600000, interesse: 'desejado', prioridade: 2, link: 'https://example.com', observacoes: 'Comparar modelos', dataDesejada: '2027-03', dataInclusao: '2026-11-08' },
  { id: '2', nome: 'Cadeira', valorEstimadoCentavos: 250000, interesse: 'listado', prioridade: 0, link: null, observacoes: null, dataDesejada: null, dataInclusao: '2026-11-07' },
]
afterEach(() => vi.unstubAllGlobals())
it('lists, filters and exposes objective actions', async () => {
  const user = userEvent.setup(); vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify(values))))
  render(<MemoryRouter><ObjectivesRoute /></MemoryRouter>)
  await waitFor(() => expect(screen.getByText('Notebook')).toBeInTheDocument())
  expect(screen.getByText('+2 · Alta')).toHaveClass('positive')
  expect(screen.getByRole('link', { name: 'Editar Notebook' })).toHaveAttribute('href', '/objetivos/1/editar')
  await user.selectOptions(screen.getByLabelText('Interesse'), 'listado')
  expect(screen.queryByText('Notebook')).not.toBeInTheDocument()
  expect(screen.getByText('Cadeira')).toBeInTheDocument()
})
