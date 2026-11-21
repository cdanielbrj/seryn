import '../../../setup/vitest'
import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ExportRoute } from '@/modules/portability/routes/ExportRoute'
import { ImportRoute } from '@/modules/portability/routes/ImportRoute'

const api = vi.hoisted(() => ({ previewWorkbook: vi.fn(), commitWorkbook: vi.fn(), exportWorkbook: vi.fn() }))
vi.mock('@/modules/portability/api/portabilityApi', () => api)

class Reader {
  result: string | ArrayBuffer | null = null
  error: DOMException | null = null
  onload: null | (() => void) = null
  onerror: null | (() => void) = null
  readAsDataURL() { this.result = 'data:application/octet-stream;base64,eGxzeA=='; this.onload?.() }
}

describe('portability routes', () => {
  afterEach(() => vi.unstubAllGlobals())
  beforeEach(() => {
    vi.clearAllMocks(); vi.stubGlobal('FileReader', Reader)
    api.previewWorkbook.mockResolvedValue({ abas: [{ aba: 'Objetivos', linhas: 1, criacoes: 1, existentes: 0 }], criacoes: 1, existentes: 0, dadosAtuais: 4, completa: true, erros: [] })
    api.commitWorkbook.mockResolvedValue({ criados: 1, existentes: 0, excluidos: 4 })
  })

  it('exports from its own route', () => {
    render(<ExportRoute />)
    fireEvent.click(screen.getByRole('button', { name: /^exportar dados$/i }))
    expect(api.exportWorkbook).toHaveBeenCalledOnce()
  })

  it('previews and explicitly confirms merging workbook data', async () => {
    render(<ImportRoute />)
    fireEvent.change(screen.getByLabelText(/selecionar arquivo/i), { target: { files: [new File(['xlsx'], 'dados.xlsx')] } })
    expect(await screen.findByText('Objetivos')).toBeInTheDocument()
    const merge = screen.getByRole('button', { name: /mesclar dados/i })
    expect(merge).toBeDisabled()
    fireEvent.click(screen.getByLabelText(/confirmo a criação de 1 item/i))
    fireEvent.click(merge)
    await waitFor(() => expect(api.commitWorkbook).toHaveBeenCalledWith('eGxzeA==', true, false, false))
  })

  it('requires a destructive dialog before replacing all user data', async () => {
    render(<ImportRoute />)
    fireEvent.change(screen.getByLabelText(/selecionar arquivo/i), { target: { files: [new File(['xlsx'], 'dados.xlsx')] } })
    fireEvent.click(await screen.findByRole('button', { name: /substituir todos os dados/i }))
    expect(screen.getByRole('alertdialog')).toHaveTextContent(/removerá 4 item/i)
    fireEvent.click(screen.getByRole('button', { name: /^substituir dados$/i }))
    await waitFor(() => expect(api.commitWorkbook).toHaveBeenCalledWith('eGxzeA==', false, true, true))
    expect(await screen.findByText(/4 item.*removido/i)).toBeInTheDocument()
  })
})
