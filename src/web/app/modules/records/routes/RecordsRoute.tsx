import { useEffect, useState } from 'react'
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
  const [deleting, setDeleting] = useState<RecordItem | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    let active = true
    getRecords().then(data => { if (active) setRecords(data) })
      .catch(() => { if (active) setError('Não foi possível carregar os registros.') })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [])

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
    <RecordsList records={records} loading={loading} onDelete={setDeleting} />
    {deleting && <ConfirmDialog title="Excluir registro?" description={`Excluir “${deleting.nome}” removerá este registro e todas as suas ocorrências previstas das projeções, inclusive de períodos anteriores. Esta ação não pode ser desfeita.`} confirmLabel="Excluir registro" busy={busy} onCancel={() => setDeleting(null)} onConfirm={() => { void confirmDelete() }} />}
  </>
}
