import { type FormEvent, useEffect, useRef, useState } from 'react'
import { ApiError, createCategory, deleteCategory, getCategories, updateCategory } from '../api/classificationsApi'
import { ConfirmDialog } from '@/components/patterns/ConfirmDialog'
import { CategoryCreate } from '../components/CategoryCreate'
import { CategoryTree } from '../components/CategoryTree'
import type { Category } from '../model/classification'

export function CategoriesRoute() {
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
  const roots = items.filter(item => item.categoriaPaiId === null).sort((a, b) => a.nome.localeCompare(b.nome, 'pt-BR'))

  useEffect(() => {
    let active = true
    getCategories().then(data => { if (active) setItems(data) })
      .catch(() => { if (active) setError('Não foi possível carregar categorias.') })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [])

  function resetMessages() { setError(''); setNotice('') }
  function message(cause: unknown) { return cause instanceof ApiError ? cause.message : 'Não foi possível concluir. Tente novamente.' }

  async function create(event: FormEvent) {
    event.preventDefault()
    resetMessages()
    setBusy(true)
    try {
      const item = await createCategory(name, parentId || null)
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
      const item = await updateCategory(editing.id, {
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
      await deleteCategory(deleting.id)
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

  return <>
    <header className="page-heading"><h1 tabIndex={-1}>Categorias</h1><p>Organize seus registros em categorias e subcategorias.</p></header>
    <div className="classifications-layout">
      <CategoryCreate busy={busy} name={name} nameInput={nameInput} parentId={parentId} roots={roots} onNameChange={setName} onParentChange={setParentId} onSubmit={event => { void create(event) }} />
      <CategoryTree busy={busy} editing={editing} editName={editName} editParentId={editParentId} error={error} items={items} loading={loading} notice={notice} onCancelEdit={() => setEditing(null)} onDelete={item => { resetMessages(); setDeleting(item) }} onEditNameChange={setEditName} onEditParentChange={setEditParentId} onSaveEdit={event => { void saveEdit(event) }} onStartEdit={startEdit} onStartSubcategory={startSubcategory} />
    </div>
    {deleting && <ConfirmDialog busy={busy} title={`Excluir ${deleting.categoriaPaiId ? 'subcategoria' : 'categoria'}?`} description={deleting.categoriaPaiId ? `Excluir “${deleting.nome}” manterá os registros associados na categoria principal.` : `Excluir “${deleting.nome}” manterá os registros associados sem categoria.`} confirmLabel={`Excluir ${deleting.categoriaPaiId ? 'subcategoria' : 'categoria'}`} onCancel={() => setDeleting(null)} onConfirm={() => { void confirmDelete() }} />}
  </>
}
