import '../../../setup/vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, expect, it, vi } from 'vitest'
import { AnalysesRoute } from '@/modules/analyses/routes/AnalysesRoute'

const analysis = {
  periodo: 'month', referencia: '2027-02', inicio: '2027-02', fim: '2027-02', direcao: 'expense', dimensaoPrincipal: 'category', dimensaoSecundaria: 'payment',
  entradasCentavos: 0, saidasCentavos: 75000, resultadoCentavos: -75000, registros: 1, ocorrencias: 1, basePercentualCentavos: 75000,
  segmentos: [{ chave: 'software', nome: 'Software', ocorrencias: 1, registros: 1, valorCentavos: 75000, percentual: 100,
    cruzamentos: [{ chave: 'pix', nome: 'Pix', ocorrencias: 1, registros: 1, valorCentavos: 75000 }],
    detalhes: [{ id: 'expense', nome: 'API', movimentacao: 'saida', tipo: 'Assinatura', categoria: 'Software', pagamento: 'Pix', ocorrencias: 1, parcelasTotais: 10, parcelasPrevistasAteFim: 4, valorCentavos: 75000 }] }],
}

afterEach(() => vi.unstubAllGlobals())

it('segments projected values, reveals their crossing and changes dimensions', async () => {
  const user = userEvent.setup()
  const fetch = vi.fn().mockResolvedValue(new Response(JSON.stringify(analysis)))
  vi.stubGlobal('fetch', fetch)
  render(<MemoryRouter><AnalysesRoute /></MemoryRouter>)
  await waitFor(() => expect(screen.getByText('Software')).toBeInTheDocument())
  expect(screen.getByText('100% do volume filtrado')).toBeInTheDocument()
  await user.click(screen.getByText('Software'))
  expect(screen.getByText('Pix')).toBeInTheDocument()
  const recordLink = screen.getByRole('link', { name: 'API' })
  expect(recordLink).toHaveAttribute('href', '/registros/expense/editar')
  expect(recordLink).toHaveClass('analysis-record-title')
  expect(recordLink.parentElement).toHaveClass('analysis-record-heading')
  expect(recordLink.parentElement?.parentElement).toHaveClass('analysis-record-info')
  const installmentBadge = screen.getByText('4 de 10 parcelas')
  expect(installmentBadge).toHaveClass('analysis-installment-badge')
  expect(installmentBadge).toHaveAttribute('aria-label', '4 de 10 parcelas previstas até o fim do período')
  expect(screen.getByText('Assinatura · Software · Pix')).toHaveClass('analysis-record-meta')
  await user.selectOptions(screen.getByLabelText('Segmentar por'), 'payment')
  await waitFor(() => expect(fetch).toHaveBeenLastCalledWith(expect.stringContaining('primary=payment'), expect.anything()))
})
