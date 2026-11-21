import { download, post } from '@/infrastructure/http/client'
import type { ImportResult, WorkbookPreview } from '../model/portability'

export const previewWorkbook = (conteudoBase64: string) => post<WorkbookPreview>('/portability/workbook/preview', { conteudoBase64 })
export const commitWorkbook = (conteudoBase64: string, confirmarCriacoes: boolean, substituirDados: boolean, confirmarSubstituicao: boolean) =>
  post<ImportResult>('/portability/workbook/commit', { conteudoBase64, confirmarCriacoes, substituirDados, confirmarSubstituicao })
export async function exportWorkbook() {
  const blob = await download('/portability/export.xlsx')
  const url = URL.createObjectURL(blob)
  const anchor = document.createElement('a')
  anchor.href = url
  anchor.download = `seryn-dados-${new Date().toISOString().slice(0, 10).replaceAll('-', '')}.xlsx`
  anchor.click()
  URL.revokeObjectURL(url)
}
