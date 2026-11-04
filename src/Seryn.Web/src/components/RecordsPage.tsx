import { useEffect, useRef, useState } from 'react'
import { ArrowDownLeft, ArrowUpRight, CalendarDays, Pencil, Plus, Trash2 } from 'lucide-react'
import { Link } from 'react-router-dom'
import { ApiError, get, remove } from '../lib/api'
import { formatMoney, formatMonth, recurrenceLabels, type RecordItem } from '../lib/records'
import { Button } from './ui/button'

export function RecordsPage() {
  const [records, setRecords] = useState<RecordItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [deleting, setDeleting] = useState<RecordItem | null>(null)
  const [busy, setBusy] = useState(false)
  const deleteButton = useRef<HTMLButtonElement>(null)

  useEffect(() => {
    let active = true
    get<RecordItem[]>('/records').then(data => { if (active) setRecords(data) })
      .catch(() => { if (active) setError('Não foi possível carregar os registros.') })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [])

  useEffect(() => {
    if (!deleting) return
    deleteButton.current?.focus()
    const close = (event: KeyboardEvent) => { if (event.key === 'Escape' && !busy) setDeleting(null) }
    document.addEventListener('keydown', close)
    return () => document.removeEventListener('keydown', close)
  }, [deleting, busy])

  async function confirmDelete() {
    if (!deleting) return
    setBusy(true)
    setError('')
    try {
      await remove(`/records/${deleting.id}`)
      setRecords(current => current.filter(record => record.id !== deleting.id))
      setDeleting(null)
    } catch (cause) {
      setError(cause instanceof ApiError ? cause.message : 'Não foi possível excluir o registro.')
      setDeleting(null)
    } finally { setBusy(false) }
  }

  return <>
    <header className="page-heading records-heading"><div><h1 tabIndex={-1}>Registros</h1><p>Suas entradas e saídas previstas em um só lugar.</p></div><Button asChild><Link to="/registros/novo"><Plus size={18} aria-hidden="true" />Novo registro</Link></Button></header>
    {error && <p className="form-error records-error" role="alert">{error}</p>}
    <section className="panel records-list" aria-label="Registros cadastrados">
      {loading ? <p className="records-state" role="status">Carregando…</p> : records.length === 0 ? <div className="records-empty"><CalendarDays aria-hidden="true" /><h2>Nenhum registro cadastrado</h2><p>Cadastre uma entrada ou saída para começar seu planejamento.</p><Button asChild><Link to="/registros/novo"><Plus size={18} />Criar primeiro registro</Link></Button></div> : <div className="record-rows">{records.map(record => <article className="record-row" key={record.id}>
        <div className={`record-direction ${record.movimentacao}`} aria-label={record.movimentacao === 'entrada' ? 'Entrada' : 'Saída'}>{record.movimentacao === 'entrada' ? <ArrowUpRight /> : <ArrowDownLeft />}</div>
        <div className="record-main"><strong>{record.nome}</strong><span>{record.tipoNome}{record.categoriaNome ? ` · ${record.categoriaNome}` : ''} · {record.pagamentoNome}</span></div>
        <div className="record-schedule"><span>{recurrenceLabels[record.recorrencia]}</span><small>{formatMonth(record.data)}{record.parcelas && record.parcelas > 1 ? ` · ${record.parcelas} parcelas` : ''}</small></div>
        <strong className={`record-value ${record.movimentacao}`}>{record.movimentacao === 'saida' ? '−' : '+'}{formatMoney(record.valorCentavos)}</strong>
        <div className="classification-actions"><Button asChild variant="ghost" aria-label={`Editar ${record.nome}`}><Link to={`/registros/${record.id}/editar`}><Pencil size={17} /></Link></Button><Button variant="ghost" aria-label={`Excluir ${record.nome}`} onClick={() => setDeleting(record)}><Trash2 size={17} /></Button></div>
      </article>)}</div>}
    </section>
    {deleting && <div className="dialog-backdrop" onMouseDown={event => { if (event.target === event.currentTarget && !busy) setDeleting(null) }}><section className="confirm-dialog" role="alertdialog" aria-modal="true" aria-labelledby="delete-record-title" aria-describedby="delete-record-description"><div className="danger-icon"><Trash2 aria-hidden="true" /></div><h2 id="delete-record-title">Excluir registro?</h2><p id="delete-record-description">Excluir “{deleting.nome}” removerá este registro e todas as suas ocorrências previstas das projeções, inclusive de períodos anteriores. Esta ação não pode ser desfeita.</p><div><Button variant="outline" onClick={() => setDeleting(null)} disabled={busy}>Cancelar</Button><Button ref={deleteButton} className="danger-button" onClick={() => { void confirmDelete() }} disabled={busy}>{busy ? 'Excluindo…' : 'Excluir registro'}</Button></div></section></div>}
  </>
}
