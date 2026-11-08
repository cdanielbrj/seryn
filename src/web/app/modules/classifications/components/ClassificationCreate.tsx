import type { FormEvent } from 'react'
import { Plus } from 'lucide-react'
import { Button } from '@/components/primitives/Button'

export function ClassificationCreate({ id, title, name, busy, onNameChange, onSubmit }: { id: string; title: string; name: string; busy: boolean; onNameChange: (name: string) => void; onSubmit: (event: FormEvent) => void }) {
  return <section className="panel classification-create" aria-labelledby={`${id}-new`}>
    <div className="panel-heading"><div className="square-icon"><Plus aria-hidden="true" /></div><div><h2 id={`${id}-new`}>{title}</h2><p>Use um nome curto e fácil de reconhecer.</p></div></div>
    <form onSubmit={onSubmit}><label htmlFor={`${id}-name`}>Nome</label><div className="classification-create-row"><input id={`${id}-name`} value={name} onChange={event => onNameChange(event.target.value)} maxLength={80} required autoComplete="off" /><Button disabled={busy || !name.trim()}><Plus size={18} aria-hidden="true" />Adicionar</Button></div></form>
  </section>
}
