import { useState } from 'react'
import { FileSpreadsheet, Upload } from 'lucide-react'
import { ConfirmDialog } from '@/components/patterns/ConfirmDialog'
import { Button } from '@/components/primitives/Button'
import { ApiError } from '@/infrastructure/http/client'
import { commitWorkbook, previewWorkbook } from '../api/portabilityApi'
import type { WorkbookPreview } from '../model/portability'

export function ImportRoute() {
  const [content, setContent] = useState('')
  const [preview, setPreview] = useState<WorkbookPreview | null>(null)
  const [busy, setBusy] = useState(false)
  const [notice, setNotice] = useState('')
  const [error, setError] = useState('')
  const [confirmCreates, setConfirmCreates] = useState(false)
  const [confirmReplace, setConfirmReplace] = useState(false)

  function resetMessages() { setError(''); setNotice('') }

  async function chooseWorkbook(file?: File) {
    resetMessages(); setPreview(null); setConfirmCreates(false); setConfirmReplace(false)
    if (!file) return
    if (file.size > 10 * 1024 * 1024) { setError('A planilha excede o limite de 10 MB.'); return }
    setBusy(true)
    try { const base64 = await fileBase64(file); setContent(base64); setPreview(await previewWorkbook(base64)) }
    catch (cause) { setError(message(cause, 'Não foi possível analisar a planilha.')) }
    finally { setBusy(false) }
  }

  async function importWorkbook(replace: boolean) {
    setBusy(true); resetMessages()
    try {
      const result = await commitWorkbook(content, confirmCreates, replace, replace)
      setNotice(replace
        ? `${result.excluidos} item(ns) anterior(es) removido(s) e ${result.criados} restaurado(s).`
        : `${result.criados} item(ns) criado(s); ${result.existentes} já existia(m).`)
      setPreview(null); setContent(''); setConfirmCreates(false)
    } catch (cause) { setError(message(cause, 'Não foi possível importar a planilha.')) }
    finally { setBusy(false); setConfirmReplace(false) }
  }

  return <>
    <header className="page-heading"><h1 tabIndex={-1}>Importar dados</h1><p>Mescle informações ou restaure integralmente os dados do usuário.</p></header>
    {error && <p className="form-error portability-message" role="alert">{error}</p>}
    {notice && <p className="form-notice portability-message" role="status">{notice}</p>}
    <section className="panel portability-panel">
      <div className="panel-heading"><div className="square-icon"><FileSpreadsheet aria-hidden="true" /></div><div><h2>Selecionar planilha</h2><p>Aceita XLSX e XLSM no padrão Seryn; macros e conteúdo externo são ignorados.</p></div></div>
      <input id="workbook-file" className="sr-only" type="file" accept=".xlsx,.xlsm,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet,application/vnd.ms-excel.sheet.macroEnabled.12" disabled={busy} onChange={event => void chooseWorkbook(event.target.files?.[0])} />
      <Button className="portability-file-action" asChild variant="outline"><label htmlFor="workbook-file"><Upload aria-hidden="true" />{busy ? 'Processando…' : 'Selecionar arquivo'}</label></Button>
      {preview && <WorkbookPreviewPanel preview={preview} busy={busy} confirmed={confirmCreates} onConfirm={setConfirmCreates} onMerge={() => void importWorkbook(false)} onReplace={() => setConfirmReplace(true)} />}
    </section>
    {confirmReplace && preview && <ConfirmDialog title="Substituir todos os dados?" description={`Esta ação removerá ${preview.dadosAtuais} item(ns) atual(is) e manterá somente os dados desta planilha. Usuário, acesso e configurações do host não serão alterados.`} confirmLabel="Substituir dados" busyLabel="Substituindo…" busy={busy} onCancel={() => setConfirmReplace(false)} onConfirm={() => void importWorkbook(true)} />}
  </>
}

function WorkbookPreviewPanel({ preview, busy, confirmed, onConfirm, onMerge, onReplace }: { preview: WorkbookPreview; busy: boolean; confirmed: boolean; onConfirm: (value: boolean) => void; onMerge: () => void; onReplace: () => void }) {
  if (preview.erros.length > 0) return <p className="form-error portability-preview-error" role="alert">{preview.erros.join(' ')}</p>
  return <div className="portability-preview">
    <PreviewTable label="Prévia da planilha" headers={['Aba', 'Linhas', 'Novos', 'Já existentes']} rows={preview.abas.map(item => [item.aba, item.linhas, item.criacoes, item.existentes])} />
    <div className="portability-preview-controls">
      {!!preview.criacoes && <label className="portability-confirm"><input type="checkbox" checked={confirmed} onChange={event => onConfirm(event.target.checked)} />Confirmo a criação de {preview.criacoes} item(ns) ao mesclar.</label>}
      <div className="portability-actions">
        <Button disabled={busy || (!!preview.criacoes && !confirmed)} onClick={onMerge}><Upload aria-hidden="true" />{busy ? 'Importando…' : 'Mesclar dados'}</Button>
        <Button className="danger-button" disabled={busy || !preview.completa} onClick={onReplace}>Substituir todos os dados</Button>
      </div>
      {!preview.completa && <p className="portability-replace-help">A substituição exige as cinco abas da planilha completa da Seryn.</p>}
    </div>
  </div>
}

function PreviewTable({ label, headers, rows }: { label: string; headers: string[]; rows: (string | number)[][] }) {
  return <div className="portability-table" role="region" aria-label={label} tabIndex={0}><table><thead><tr>{headers.map(header => <th key={header}>{header}</th>)}</tr></thead><tbody>{rows.map((row, index) => <tr key={`${row[0]}-${index}`}>{row.map((cell, cellIndex) => <td key={`${cellIndex}-${cell}`}>{cell}</td>)}</tr>)}</tbody></table></div>
}

function message(cause: unknown, fallback: string) {
  return cause instanceof ApiError ? Object.values(cause.errors).flat().join(' ') || cause.message : fallback
}

function fileBase64(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onerror = () => reject(reader.error)
    reader.onload = () => resolve(String(reader.result).split(',', 2)[1] ?? '')
    reader.readAsDataURL(file)
  })
}
