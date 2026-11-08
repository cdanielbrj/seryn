import { type FormEvent } from 'react'
import { Check, ChevronRight, Layers, Pencil, Plus, Trash2, X } from 'lucide-react'
import { Button } from '@/components/primitives/Button'
import type { Category } from '../model/classification'

type Props = {
  busy: boolean
  editing: Category | null
  editName: string
  editParentId: string
  error: string
  items: Category[]
  loading: boolean
  notice: string
  onCancelEdit: () => void
  onDelete: (item: Category) => void
  onEditNameChange: (value: string) => void
  onEditParentChange: (value: string) => void
  onSaveEdit: (event: FormEvent) => void
  onStartEdit: (item: Category) => void
  onStartSubcategory: (item: Category) => void
}

export function CategoryTree(props: Props) {
  const { busy, editing, editName, editParentId, error, items, loading, notice } = props
  const roots = items.filter(item => item.categoriaPaiId === null).sort(compare)
  const children = (id: string) => items.filter(item => item.categoriaPaiId === id).sort(compare)

  function row(item: Category, nested: boolean) {
    const childCount = children(item.id).length
    const cannotDelete = !nested && childCount > 0
    return <div className={`category-row${nested ? ' nested' : ''}`} key={item.id}>
      {nested && <ChevronRight className="category-branch" aria-hidden="true" />}
      {editing?.id === item.id ? <form className="category-edit" onSubmit={props.onSaveEdit}>
        <label className="sr-only" htmlFor={`edit-${item.id}`}>Nome da {nested ? 'subcategoria' : 'categoria'}</label>
        <input id={`edit-${item.id}`} value={editName} onChange={event => props.onEditNameChange(event.target.value)} maxLength={80} required autoFocus />
        {nested && <><label className="sr-only" htmlFor={`parent-${item.id}`}>Subcategoria</label><select id={`parent-${item.id}`} value={editParentId} onChange={event => props.onEditParentChange(event.target.value)}>{roots.map(root => <option key={root.id} value={root.id}>{root.nome}</option>)}</select></>}
        <Button aria-label={`Salvar ${item.nome}`} disabled={busy || !editName.trim()}><Check size={18} /></Button>
        <Button type="button" variant="ghost" aria-label="Cancelar edição" onClick={props.onCancelEdit} disabled={busy}><X size={18} /></Button>
      </form> : <>
        <div className="category-name"><span>{item.nome}</span>{!nested && <small>{childCount === 1 ? '1 subcategoria' : `${childCount} subcategorias`}</small>}</div>
        <div className="classification-actions">
          {!nested && <Button variant="ghost" aria-label={`Adicionar subcategoria em ${item.nome}`} onClick={() => props.onStartSubcategory(item)}><Plus size={17} /></Button>}
          <Button variant="ghost" aria-label={`Editar ${item.nome}`} onClick={() => props.onStartEdit(item)}><Pencil size={17} /></Button>
          <Button variant="ghost" aria-label={cannotDelete ? `${item.nome} possui subcategorias e não pode ser excluída` : `Excluir ${item.nome}`} title={cannotDelete ? 'Mova ou exclua as subcategorias primeiro.' : undefined} disabled={cannotDelete} onClick={() => props.onDelete(item)}><Trash2 size={17} /></Button>
        </div>
      </>}
    </div>
  }

  return <section className="panel classification-list" aria-labelledby="categories-list">
    <div className="panel-heading"><div className="square-icon"><Layers aria-hidden="true" /></div><div><h2 id="categories-list">Categorias cadastradas</h2><p>{items.length === 1 ? '1 classificação' : `${items.length} classificações`}</p></div></div>
    {error && <p className="form-error" role="alert">{error}</p>}
    {notice && <p className="form-notice" role="status">{notice}</p>}
    {loading ? <p className="classification-state" role="status">Carregando…</p> : roots.length === 0 ? <div className="classification-empty"><Layers aria-hidden="true" /><h3>Nenhuma categoria cadastrada</h3><p>Crie uma categoria principal para começar.</p></div> : <div className="category-tree">{roots.map(root => <section key={root.id} className="category-group" aria-label={root.nome}>{row(root, false)}{children(root.id).map(child => row(child, true))}</section>)}</div>}
  </section>
}

function compare(a: Category, b: Category) { return a.nome.localeCompare(b.nome, 'pt-BR') }
