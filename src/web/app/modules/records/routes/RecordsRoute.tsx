import { useEffect, useMemo, useState } from 'react'
import { Search } from 'lucide-react'
import { Link } from 'react-router-dom'
import { ApiError, deleteRecord, getRecords } from '../api/recordsApi'
import { Button } from '@/components/primitives/Button'
import { ConfirmDialog } from '@/components/patterns/ConfirmDialog'
import { RecordsList } from '../components/RecordsList'
import type { RecordItem } from '../model/record'

export function RecordsRoute() {
  const [records, setRecords] = useState<RecordItem[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [query, setQuery] = useState('')
  const [deleting, setDeleting] = useState<RecordItem | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    let active = true
    getRecords().then(data => { if (active) setRecords(data) })
      .catch(() => { if (active) setError('Não foi possível carregar os registros.') })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [])

  const filteredRecords = useMemo(() => {
    const normalizedQuery = normalizeSearch(query.trim())
    if (!normalizedQuery) return records

    return records.filter(record => [record.nome, record.tipoNome, record.categoriaNome, record.pagamentoNome]
      .some(value => value !== null && normalizeSearch(value).includes(normalizedQuery)))
  }, [query, records])

  async function confirmDelete() {
    if (!deleting) return
    setBusy(true)
    setError('')
    try {
      await deleteRecord(deleting.id)
      setRecords(current => current.filter(record => record.id !== deleting.id))
      setDeleting(null)
    } catch (cause) {
      setError(cause instanceof ApiError ? cause.message : 'Não foi possível excluir o registro.')
      setDeleting(null)
    } finally { setBusy(false) }
  }

  return <>
    <header className="page-heading records-heading"><div><h1 tabIndex={-1}>Registros</h1><p>Suas entradas e saídas previstas em um só lugar.</p></div><Button asChild><Link to="/registros/novo">Novo registro</Link></Button></header>
    {error && <p className="form-error records-error" role="alert">{error}</p>}
    <section className="panel records-filters" aria-label="Busca de registros"><label htmlFor="records-search">Buscar registros<span className="records-search-input"><Search size={18} aria-hidden="true" /><input id="records-search" type="search" value={query} onChange={event => setQuery(event.target.value)} placeholder="Título, tipo, categoria ou pagamento" /></span></label></section>
    <RecordsList records={filteredRecords} hasSearch={Boolean(query.trim())} loading={loading} onDelete={setDeleting} />
    {deleting && <ConfirmDialog title="Excluir registro?" description={`Excluir “${deleting.nome}” removerá este registro e todas as suas ocorrências previstas das projeções, inclusive de períodos anteriores. Esta ação não pode ser desfeita.`} confirmLabel="Excluir registro" busy={busy} onCancel={() => setDeleting(null)} onConfirm={() => { void confirmDelete() }} />}
  </>
}

function normalizeSearch(value: string) {
  return value.normalize('NFD').replace(/\p{Diacritic}/gu, '').toLocaleLowerCase('pt-BR')
}
