import { type FormEvent, useEffect, useRef, useState } from 'react'
import { Check, ChevronRight, Layers, Pencil, Plus, Trash2, X } from 'lucide-react'
import { ApiError, get, patch, post, remove } from '@/shared/api/client'
import { Button } from '@/shared/ui/Button'

type Category = { id: string; nome: string; categoriaPaiId: string | null }

export function CategoriesPage() {
  const [items, setItems] = useState<Category[]>([])
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [name, setName] = useState('')
  const [parentId, setParentId] = useState('')
  const [editing, setEditing] = useState<Category | null>(null)
  const [editName, setEditName] = useState('')
  const [editParentId, setEditParentId] = useState('')
  const [deleting, setDeleting] = useState<Category | null>(null)
  const nameInput = useRef<HTMLInputElement>(null)
  const deleteButton = useRef<HTMLButtonElement>(null)
  const roots = items.filter(item => item.categoriaPaiId === null).sort(compare)
  const children = (id: string) => items.filter(item => item.categoriaPaiId === id).sort(compare)

  useEffect(() => {
    let active = true
    get<Category[]>('/categories').then(data => { if (active) setItems(data) })
      .catch(() => { if (active) setError('Não foi possível carregar categorias.') })
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

  function resetMessages() { setError(''); setNotice('') }
  function message(cause: unknown) { return cause instanceof ApiError ? cause.message : 'Não foi possível concluir. Tente novamente.' }

  async function create(event: FormEvent) {
    event.preventDefault()
    resetMessages()
    setBusy(true)
    try {
      const item = await post<Category>('/categories', { nome: name, categoriaPaiId: parentId || null })
      setItems(current => [...current, item])
      setName('')
      setNotice(item.categoriaPaiId ? 'Subcategoria criada com sucesso.' : 'Categoria criada com sucesso.')
    } catch (cause) { setError(message(cause)) }
    finally { setBusy(false) }
  }

  async function saveEdit(event: FormEvent) {
    event.preventDefault()
    if (!editing) return
    resetMessages()
    setBusy(true)
    try {
      const item = await patch<Category>(`/categories/${editing.id}`, {
        nome: editName,
        categoriaPaiId: editing.categoriaPaiId === null ? null : editParentId,
      })
      setItems(current => current.map(existing => existing.id === item.id ? item : existing))
      setEditing(null)
      setNotice(item.categoriaPaiId ? 'Subcategoria atualizada com sucesso.' : 'Categoria atualizada com sucesso.')
    } catch (cause) { setError(message(cause)) }
    finally { setBusy(false) }
  }

  async function confirmDelete() {
    if (!deleting) return
    resetMessages()
    setBusy(true)
    try {
      await remove(`/categories/${deleting.id}`)
      setItems(current => current.filter(item => item.id !== deleting.id))
      setNotice(deleting.categoriaPaiId ? 'Subcategoria excluída com sucesso.' : 'Categoria excluída com sucesso.')
      setDeleting(null)
    } catch (cause) { setError(message(cause)); setDeleting(null) }
    finally { setBusy(false) }
  }

  function startSubcategory(root: Category) {
    resetMessages()
    setParentId(root.id)
    nameInput.current?.focus()
  }

  function startEdit(item: Category) {
    resetMessages()
    setEditing(item)
    setEditName(item.nome)
    setEditParentId(item.categoriaPaiId ?? '')
  }

  function row(item: Category, nested: boolean) {
    const childCount = children(item.id).length
    const cannotDelete = !nested && childCount > 0
    return <div className={`category-row${nested ? ' nested' : ''}`} key={item.id}>
      {nested && <ChevronRight className="category-branch" aria-hidden="true" />}
      {editing?.id === item.id ? <form className="category-edit" onSubmit={event => { void saveEdit(event) }}>
        <label className="sr-only" htmlFor={`edit-${item.id}`}>Nome da {nested ? 'subcategoria' : 'categoria'}</label>
        <input id={`edit-${item.id}`} value={editName} onChange={event => setEditName(event.target.value)} maxLength={80} required autoFocus />
        {nested && <><label className="sr-only" htmlFor={`parent-${item.id}`}>Subcategoria</label><select id={`parent-${item.id}`} value={editParentId} onChange={event => setEditParentId(event.target.value)}>{roots.map(root => <option key={root.id} value={root.id}>{root.nome}</option>)}</select></>}
        <Button aria-label={`Salvar ${item.nome}`} disabled={busy || !editName.trim()}><Check size={18} /></Button>
        <Button type="button" variant="ghost" aria-label="Cancelar edição" onClick={() => setEditing(null)} disabled={busy}><X size={18} /></Button>
      </form> : <>
        <div className="category-name"><span>{item.nome}</span>{!nested && <small>{childCount === 1 ? '1 subcategoria' : `${childCount} subcategorias`}</small>}</div>
        <div className="classification-actions">
          {!nested && <Button variant="ghost" aria-label={`Adicionar subcategoria em ${item.nome}`} onClick={() => startSubcategory(item)}><Plus size={17} /></Button>}
          <Button variant="ghost" aria-label={`Editar ${item.nome}`} onClick={() => startEdit(item)}><Pencil size={17} /></Button>
          <Button variant="ghost" aria-label={cannotDelete ? `${item.nome} possui subcategorias e não pode ser excluída` : `Excluir ${item.nome}`} title={cannotDelete ? 'Mova ou exclua as subcategorias primeiro.' : undefined} disabled={cannotDelete} onClick={() => { resetMessages(); setDeleting(item) }}><Trash2 size={17} /></Button>
        </div>
      </>}
    </div>
  }

  return <>
    <header className="page-heading"><h1 tabIndex={-1}>Categorias</h1><p>Organize seus registros em categorias e subcategorias.</p></header>
    <div className="classifications-layout">
      <section className="panel classification-create" aria-labelledby="categories-new">
        <div className="panel-heading"><div className="square-icon"><Plus aria-hidden="true" /></div><div><h2 id="categories-new">Nova categoria</h2><p>Crie uma categoria principal ou detalhe uma existente.</p></div></div>
        <form onSubmit={event => { void create(event) }}>
          <label htmlFor="category-name">Nome</label>
          <input ref={nameInput} id="category-name" value={name} onChange={event => setName(event.target.value)} maxLength={80} required autoComplete="off" />
          <label htmlFor="category-parent">Subcategoria</label>
          <select id="category-parent" value={parentId} onChange={event => setParentId(event.target.value)}><option value="">—</option>{roots.map(root => <option key={root.id} value={root.id}>{root.nome}</option>)}</select>
          <Button disabled={busy || !name.trim()}><Plus size={18} aria-hidden="true" />Adicionar</Button>
        </form>
      </section>
      <section className="panel classification-list" aria-labelledby="categories-list">
        <div className="panel-heading"><div className="square-icon"><Layers aria-hidden="true" /></div><div><h2 id="categories-list">Categorias cadastradas</h2><p>{items.length === 1 ? '1 classificação' : `${items.length} classificações`}</p></div></div>
        {error && <p className="form-error" role="alert">{error}</p>}
        {notice && <p className="form-notice" role="status">{notice}</p>}
        {loading ? <p className="classification-state" role="status">Carregando…</p> : roots.length === 0 ? <div className="classification-empty"><Layers aria-hidden="true" /><h3>Nenhuma categoria cadastrada</h3><p>Crie uma categoria principal para começar.</p></div> : <div className="category-tree">{roots.map(root => <section key={root.id} className="category-group" aria-label={root.nome}>{row(root, false)}{children(root.id).map(child => row(child, true))}</section>)}</div>}
      </section>
    </div>
    {deleting && <div className="dialog-backdrop" onMouseDown={event => { if (event.target === event.currentTarget && !busy) setDeleting(null) }}><section className="confirm-dialog" role="alertdialog" aria-modal="true" aria-labelledby="delete-title" aria-describedby="delete-description"><div className="danger-icon"><Trash2 aria-hidden="true" /></div><h2 id="delete-title">Excluir {deleting.categoriaPaiId ? 'subcategoria' : 'categoria'}?</h2><p id="delete-description">{deleting.categoriaPaiId ? `Excluir “${deleting.nome}” manterá os registros associados na categoria principal.` : `Excluir “${deleting.nome}” manterá os registros associados sem categoria.`}</p><div><Button variant="outline" onClick={() => setDeleting(null)} disabled={busy}>Cancelar</Button><Button ref={deleteButton} className="danger-button" onClick={() => { void confirmDelete() }} disabled={busy}>{busy ? 'Excluindo…' : `Excluir ${deleting.categoriaPaiId ? 'subcategoria' : 'categoria'}`}</Button></div></section></div>}
  </>
}

function compare(a: Category, b: Category) { return a.nome.localeCompare(b.nome, 'pt-BR') }
