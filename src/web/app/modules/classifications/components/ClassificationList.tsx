import type { FormEvent } from 'react'
import { Check, Pencil, Trash2, X, type LucideIcon } from 'lucide-react'
import { Button } from '@/components/primitives/Button'
import type { Classification } from '../model/classification'

type ClassificationListProps = {
  id: string
  title: string
  singular: string
  emptyLabel: string
  Icon: LucideIcon
  items: Classification[]
  loading: boolean
  busy: boolean
  error: string
  notice: string
  editing: Classification | null
  editName: string
  onEditNameChange: (name: string) => void
  onEdit: (item: Classification) => void
  onCancelEdit: () => void
  onDelete: (item: Classification) => void
  onSave: (event: FormEvent) => void
}

export function ClassificationList(props: ClassificationListProps) {
  const { id, title, singular, emptyLabel, Icon, items, loading, busy, error, notice, editing, editName, onEditNameChange, onEdit, onCancelEdit, onDelete, onSave } = props
  return <section className="panel classification-list" aria-labelledby={`${id}-list`}>
    <div className="panel-heading"><div className="square-icon"><Icon aria-hidden="true" /></div><div><h2 id={`${id}-list`}>{title} cadastrados</h2><p>{items.length === 1 ? '1 classificação' : `${items.length} classificações`}</p></div></div>
    {error && <p className="form-error" role="alert">{error}</p>}
    {notice && <p className="form-notice" role="status">{notice}</p>}
    {loading ? <p className="classification-state" role="status">Carregando…</p> : items.length === 0 ? <div className="classification-empty"><Icon aria-hidden="true" /><h3>{emptyLabel}</h3><p>Você poderá usar esta classificação nos seus registros.</p></div> : <ul>{items.map(item => <li key={item.id}>
      {editing?.id === item.id ? <form className="classification-edit" onSubmit={onSave}><label className="sr-only" htmlFor={`edit-${item.id}`}>Nome do {singular}</label><input id={`edit-${item.id}`} value={editName} onChange={event => onEditNameChange(event.target.value)} maxLength={80} required autoFocus /><Button aria-label={`Salvar ${item.nome}`} disabled={busy || !editName.trim()}><Check size={18} /></Button><Button type="button" variant="ghost" aria-label="Cancelar edição" onClick={onCancelEdit} disabled={busy}><X size={18} /></Button></form> : <><span>{item.nome}</span><div className="classification-actions"><Button variant="ghost" aria-label={`Editar ${item.nome}`} onClick={() => onEdit(item)}><Pencil size={17} /></Button><Button variant="ghost" aria-label={`Excluir ${item.nome}`} onClick={() => onDelete(item)}><Trash2 size={17} /></Button></div></>}
    </li>)}</ul>}
  </section>
}
