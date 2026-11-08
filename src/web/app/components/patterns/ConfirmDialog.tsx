import { useEffect, useRef } from 'react'
import { Trash2 } from 'lucide-react'
import { Button } from '@/components/primitives/Button'

type ConfirmDialogProps = {
  title: string
  description: string
  confirmLabel: string
  busy: boolean
  onCancel: () => void
  onConfirm: () => void
}

export function ConfirmDialog({ title, description, confirmLabel, busy, onCancel, onConfirm }: ConfirmDialogProps) {
  const confirmButton = useRef<HTMLButtonElement>(null)

  useEffect(() => {
    confirmButton.current?.focus()
    const close = (event: KeyboardEvent) => { if (event.key === 'Escape' && !busy) onCancel() }
    document.addEventListener('keydown', close)
    return () => document.removeEventListener('keydown', close)
  }, [busy, onCancel])

  return <div className="dialog-backdrop" onMouseDown={event => { if (event.target === event.currentTarget && !busy) onCancel() }}>
    <section className="confirm-dialog" role="alertdialog" aria-modal="true" aria-labelledby="confirm-title" aria-describedby="confirm-description">
      <div className="danger-icon"><Trash2 aria-hidden="true" /></div><h2 id="confirm-title">{title}</h2><p id="confirm-description">{description}</p>
      <div><Button variant="outline" onClick={onCancel} disabled={busy}>Cancelar</Button><Button ref={confirmButton} className="danger-button" onClick={onConfirm} disabled={busy}>{busy ? 'Excluindo…' : confirmLabel}</Button></div>
    </section>
  </div>
}
