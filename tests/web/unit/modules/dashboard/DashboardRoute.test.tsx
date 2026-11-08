import '../../../setup/vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, expect, it, vi } from 'vitest'
import { DashboardRoute } from '@/modules/dashboard/routes/DashboardRoute'

const projection = {
  periodo: 'month', referencia: '2027-02', inicio: '2027-02', fim: '2027-02', entradasCentavos: 1000000, saidasCentavos: 75000, resultadoCentavos: 925000,
  graficoInicio: '2026-11', graficoFim: '2027-05', grafico: Array.from({ length: 7 }, (_, index) => ({ mes: `2027-0${index + 1}`, entradasCentavos: 0, saidasCentavos: 0, resultadoAcumuladoCentavos: 0, selecionado: index === 1 })),
  categorias: [{ nome: 'Software', valorCentavos: 75000, percentual: 100 }],
  maioresEntradas: [{ id: 'income', nome: 'Salário', categoriaNome: null, ocorrencias: 1, valorCentavos: 800000 }],
  maioresSaidas: [{ id: 'expense', nome: 'API', categoriaNome: 'Software', ocorrencias: 1, valorCentavos: 75000 }],
}

afterEach(() => vi.unstubAllGlobals())

it('shows persisted projection data and updates the selected period', async () => {
  const user = userEvent.setup()
  const fetch = vi.fn().mockResolvedValue(new Response(JSON.stringify(projection)))
  vi.stubGlobal('fetch', fetch)
  render(<MemoryRouter><DashboardRoute /></MemoryRouter>)
  await waitFor(() => expect(screen.getAllByText(/10\.000,00/).length).toBeGreaterThan(0))
  expect(screen.getByRole('heading', { name: 'Projeção do período' })).toBeInTheDocument()
  expect(screen.getByRole('heading', { name: 'Por categoria' })).toBeInTheDocument()
  expect(screen.getByRole('heading', { name: 'Maiores movimentações' })).toBeInTheDocument()
  expect(screen.getByText('Software')).toBeInTheDocument()
  expect(screen.getByText('Maiores entradas')).toBeInTheDocument()
  await user.click(screen.getByRole('button', { name: 'Semestre' }))
  await waitFor(() => expect(fetch).toHaveBeenLastCalledWith(expect.stringContaining('period=semester'), expect.anything()))
  expect(screen.getByRole('button', { name: 'Semestre' })).toHaveAttribute('aria-pressed', 'true')
})
