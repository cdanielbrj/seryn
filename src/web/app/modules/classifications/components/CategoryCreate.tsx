import { type FormEvent, type RefObject } from 'react'
import { Plus } from 'lucide-react'
import { Button } from '@/components/primitives/Button'
import type { Category } from '../model/classification'

type Props = {
  busy: boolean
  name: string
  nameInput: RefObject<HTMLInputElement | null>
  parentId: string
  roots: Category[]
  onNameChange: (value: string) => void
  onParentChange: (value: string) => void
  onSubmit: (event: FormEvent) => void
}

export function CategoryCreate({ busy, name, nameInput, parentId, roots, onNameChange, onParentChange, onSubmit }: Props) {
  return <section className="panel classification-create" aria-labelledby="categories-new">
    <div className="panel-heading"><div className="square-icon"><Plus aria-hidden="true" /></div><div><h2 id="categories-new">Nova categoria</h2><p>Crie uma categoria principal ou detalhe uma existente.</p></div></div>
    <form onSubmit={onSubmit}>
      <label htmlFor="category-name">Nome</label>
      <input ref={nameInput} id="category-name" value={name} onChange={event => onNameChange(event.target.value)} maxLength={80} required autoComplete="off" />
      <label htmlFor="category-parent">Subcategoria</label>
      <select id="category-parent" value={parentId} onChange={event => onParentChange(event.target.value)}><option value="">—</option>{roots.map(root => <option key={root.id} value={root.id}>{root.nome}</option>)}</select>
      <Button disabled={busy || !name.trim()}><Plus size={18} aria-hidden="true" />Adicionar</Button>
    </form>
  </section>
}
