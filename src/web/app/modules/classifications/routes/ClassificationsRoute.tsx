import { type FormEvent, useEffect, useState } from 'react'
import { CreditCard, Tag } from 'lucide-react'
import { ApiError, createClassification, deleteClassification, getClassifications, updateClassification, type ClassificationKind } from '../api/classificationsApi'
import { ConfirmDialog } from '@/components/patterns/ConfirmDialog'
import { ClassificationCreate } from '../components/ClassificationCreate'
import { ClassificationList } from '../components/ClassificationList'
import type { Classification } from '../model/classification'

type Kind = ClassificationKind

const copy = {
  types: { title: 'Tipos', singular: 'tipo', newLabel: 'Novo tipo', emptyLabel: 'Nenhum tipo cadastrado', created: 'Tipo criado com sucesso.', updated: 'Tipo atualizado com sucesso.', deleted: 'Tipo excluído com sucesso.', description: 'Classificações livres para organizar seus registros.', Icon: Tag },
  'payment-methods': { title: 'Pagamentos', singular: 'pagamento', newLabel: 'Novo pagamento', emptyLabel: 'Nenhum pagamento cadastrado', created: 'Pagamento criado com sucesso.', updated: 'Pagamento atualizado com sucesso.', deleted: 'Pagamento excluído com sucesso.', description: 'Defina como suas entradas e saídas são planejadas.', Icon: CreditCard },
} satisfies Record<Kind, { title: string; singular: string; newLabel: string; emptyLabel: string; created: string; updated: string; deleted: string; description: string; Icon: typeof Tag }>

export function ClassificationsRoute({ kind }: { kind: Kind }) {
  const { title, singular, newLabel, emptyLabel, created, updated, deleted, description, Icon } = copy[kind]
  const [items, setItems] = useState<Classification[]>([])
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [name, setName] = useState('')
  const [editing, setEditing] = useState<Classification | null>(null)
  const [editName, setEditName] = useState('')
  const [deleting, setDeleting] = useState<Classification | null>(null)

  useEffect(() => {
    let active = true
    getClassifications(kind).then(data => { if (active) setItems(data) })
      .catch(() => { if (active) setError(`Não foi possível carregar ${title.toLocaleLowerCase('pt-BR')}.`) })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [kind, title])

  function message(cause: unknown) {
    return cause instanceof ApiError ? cause.message : 'Não foi possível concluir. Tente novamente.'
  }

  function resetMessages() { setError(''); setNotice('') }

  async function create(event: FormEvent) {
    event.preventDefault()
    resetMessages()
    setBusy(true)
    try {
      const item = await createClassification(kind, name)
      setItems(current => [...current, item].sort((a, b) => a.nome.localeCompare(b.nome, 'pt-BR')))
      setName('')
      setNotice(created)
    } catch (cause) { setError(message(cause)) }
    finally { setBusy(false) }
  }

  async function saveEdit(event: FormEvent) {
    event.preventDefault()
    if (!editing) return
    resetMessages()
    setBusy(true)
    try {
      const item = await updateClassification(kind, editing.id, editName)
      setItems(current => current.map(existing => existing.id === item.id ? item : existing)
        .sort((a, b) => a.nome.localeCompare(b.nome, 'pt-BR')))
      setEditing(null)
      setNotice(updated)
    } catch (cause) { setError(message(cause)) }
    finally { setBusy(false) }
  }

  async function confirmDelete() {
    if (!deleting) return
    resetMessages()
    setBusy(true)
    try {
      await deleteClassification(kind, deleting.id)
      setItems(current => current.filter(item => item.id !== deleting.id))
      setNotice(deleted)
      setDeleting(null)
    } catch (cause) { setError(message(cause)); setDeleting(null) }
    finally { setBusy(false) }
  }

  return <>
    <header className="page-heading"><h1 tabIndex={-1}>{title}</h1><p>{description}</p></header>
    <div className="classifications-layout">
      <ClassificationCreate id={kind} title={newLabel} name={name} busy={busy} onNameChange={setName} onSubmit={event => { void create(event) }} />
      <ClassificationList id={kind} title={title} singular={singular} emptyLabel={emptyLabel} Icon={Icon} items={items} loading={loading} busy={busy} error={error} notice={notice} editing={editing} editName={editName} onEditNameChange={setEditName} onEdit={item => { resetMessages(); setEditing(item); setEditName(item.nome) }} onCancelEdit={() => setEditing(null)} onDelete={item => { resetMessages(); setDeleting(item) }} onSave={event => { void saveEdit(event) }} />
    </div>
    {deleting && <ConfirmDialog title={`Excluir ${singular}?`} description={`Excluir “${deleting.nome}” removerá esta classificação definitivamente. Esta ação não pode ser desfeita.`} confirmLabel={`Excluir ${singular}`} busy={busy} onCancel={() => setDeleting(null)} onConfirm={() => { void confirmDelete() }} />}
  </>
}
