import { type FormEvent, useEffect, useRef, useState } from 'react'
import { Check, CreditCard, Pencil, Plus, Tag, Trash2, X } from 'lucide-react'
import { ApiError, get, patch, post, remove } from '@/global/api/client'
import { Button } from '@/global/ui/Button'

type Classification = { id: string; nome: string }
type Kind = 'types' | 'payment-methods'

const copy = {
  types: { title: 'Tipos', singular: 'tipo', newLabel: 'Novo tipo', emptyLabel: 'Nenhum tipo cadastrado', created: 'Tipo criado com sucesso.', updated: 'Tipo atualizado com sucesso.', deleted: 'Tipo excluído com sucesso.', description: 'Classificações livres para organizar seus registros.', Icon: Tag },
  'payment-methods': { title: 'Pagamentos', singular: 'pagamento', newLabel: 'Novo pagamento', emptyLabel: 'Nenhum pagamento cadastrado', created: 'Pagamento criado com sucesso.', updated: 'Pagamento atualizado com sucesso.', deleted: 'Pagamento excluído com sucesso.', description: 'Defina como suas entradas e saídas são planejadas.', Icon: CreditCard },
} satisfies Record<Kind, { title: string; singular: string; newLabel: string; emptyLabel: string; created: string; updated: string; deleted: string; description: string; Icon: typeof Tag }>

export function ClassificationsPage({ kind }: { kind: Kind }) {
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
  const deleteButton = useRef<HTMLButtonElement>(null)

  useEffect(() => {
    let active = true
    get<Classification[]>(`/${kind}`).then(data => { if (active) setItems(data) })
      .catch(() => { if (active) setError(`Não foi possível carregar ${title.toLocaleLowerCase('pt-BR')}.`) })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [kind, title])

  useEffect(() => {
    if (!deleting) return
    deleteButton.current?.focus()
    const close = (event: KeyboardEvent) => { if (event.key === 'Escape' && !busy) setDeleting(null) }
    document.addEventListener('keydown', close)
    return () => document.removeEventListener('keydown', close)
  }, [deleting, busy])

  function message(cause: unknown) {
    return cause instanceof ApiError ? cause.message : 'Não foi possível concluir. Tente novamente.'
  }

  function resetMessages() { setError(''); setNotice('') }

  async function create(event: FormEvent) {
    event.preventDefault()
    resetMessages()
    setBusy(true)
    try {
      const item = await post<Classification>(`/${kind}`, { nome: name })
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
      const item = await patch<Classification>(`/${kind}/${editing.id}`, { nome: editName })
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
      await remove(`/${kind}/${deleting.id}`)
      setItems(current => current.filter(item => item.id !== deleting.id))
      setNotice(deleted)
      setDeleting(null)
    } catch (cause) { setError(message(cause)); setDeleting(null) }
    finally { setBusy(false) }
  }

  return <>
    <header className="page-heading"><h1 tabIndex={-1}>{title}</h1><p>{description}</p></header>
    <div className="classifications-layout">
      <section className="panel classification-create" aria-labelledby={`${kind}-new`}>
        <div className="panel-heading"><div className="square-icon"><Plus aria-hidden="true" /></div><div><h2 id={`${kind}-new`}>{newLabel}</h2><p>Use um nome curto e fácil de reconhecer.</p></div></div>
        <form onSubmit={event => { void create(event) }}>
          <label htmlFor={`${kind}-name`}>Nome</label>
          <div className="classification-create-row"><input id={`${kind}-name`} value={name} onChange={event => setName(event.target.value)} maxLength={80} required autoComplete="off" /><Button disabled={busy || !name.trim()}><Plus size={18} aria-hidden="true" />Adicionar</Button></div>
        </form>
      </section>
      <section className="panel classification-list" aria-labelledby={`${kind}-list`}>
        <div className="panel-heading"><div className="square-icon"><Icon aria-hidden="true" /></div><div><h2 id={`${kind}-list`}>{title} cadastrados</h2><p>{items.length === 1 ? '1 classificação' : `${items.length} classificações`}</p></div></div>
        {error && <p className="form-error" role="alert">{error}</p>}
        {notice && <p className="form-notice" role="status">{notice}</p>}
        {loading ? <p className="classification-state" role="status">Carregando…</p> : items.length === 0 ? <div className="classification-empty"><Icon aria-hidden="true" /><h3>{emptyLabel}</h3><p>Você poderá usar esta classificação nos seus registros.</p></div> : <ul>{items.map(item => <li key={item.id}>
          {editing?.id === item.id ? <form className="classification-edit" onSubmit={event => { void saveEdit(event) }}><label className="sr-only" htmlFor={`edit-${item.id}`}>Nome do {singular}</label><input id={`edit-${item.id}`} value={editName} onChange={event => setEditName(event.target.value)} maxLength={80} required autoFocus /><Button aria-label={`Salvar ${item.nome}`} disabled={busy || !editName.trim()}><Check size={18} /></Button><Button type="button" variant="ghost" aria-label="Cancelar edição" onClick={() => setEditing(null)} disabled={busy}><X size={18} /></Button></form> : <><span>{item.nome}</span><div className="classification-actions"><Button variant="ghost" aria-label={`Editar ${item.nome}`} onClick={() => { resetMessages(); setEditing(item); setEditName(item.nome) }}><Pencil size={17} /></Button><Button variant="ghost" aria-label={`Excluir ${item.nome}`} onClick={() => { resetMessages(); setDeleting(item) }}><Trash2 size={17} /></Button></div></>}
        </li>)}</ul>}
      </section>
    </div>
    {deleting && <div className="dialog-backdrop" onMouseDown={event => { if (event.target === event.currentTarget && !busy) setDeleting(null) }}><section className="confirm-dialog" role="alertdialog" aria-modal="true" aria-labelledby="delete-title" aria-describedby="delete-description"><div className="danger-icon"><Trash2 aria-hidden="true" /></div><h2 id="delete-title">Excluir {singular}?</h2><p id="delete-description">Excluir “{deleting.nome}” removerá esta classificação definitivamente. Esta ação não pode ser desfeita.</p><div><Button variant="outline" onClick={() => setDeleting(null)} disabled={busy}>Cancelar</Button><Button ref={deleteButton} className="danger-button" onClick={() => { void confirmDelete() }} disabled={busy}>{busy ? 'Excluindo…' : `Excluir ${singular}`}</Button></div></section></div>}
  </>
}
