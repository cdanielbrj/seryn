import { useEffect, useState, type FormEvent } from 'react'
import { Check, KeyRound, Pencil, Trash2 } from 'lucide-react'
import { Button } from '@/components/primitives/Button'
import { ConfirmDialog } from '@/components/patterns/ConfirmDialog'
import { ApiError } from '@/infrastructure/http/client'
import { createOpticalConnection, deleteOpticalConnection, listOpticalConnections, updateOpticalConnection, type OpticalConnection } from '../api/lumenApi'

const emptyForm = { nome: '', provedor: 'openrouter', modelo: '', chave: '', padrao: false }

export function OpticalConnectionsRoute() {
  const [connections, setConnections] = useState<OpticalConnection[]>([])
  const [form, setForm] = useState(emptyForm)
  const [editing, setEditing] = useState<OpticalConnection | null>(null)
  const [removing, setRemoving] = useState<OpticalConnection | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  async function load() { setConnections(await listOpticalConnections()) }
  useEffect(() => { let active = true; listOpticalConnections().then(value => { if (active) setConnections(value) }).catch(() => { if (active) setError('Não foi possível carregar as conexões.') }); return () => { active = false } }, [])

  function edit(connection: OpticalConnection) {
    setEditing(connection); setError('')
    setForm({ nome: connection.nome, provedor: connection.provedor, modelo: connection.modelo, chave: '', padrao: connection.padrao })
  }
  function reset() { setEditing(null); setForm(emptyForm); setError('') }

  async function submit(event: FormEvent) {
    event.preventDefault(); setBusy(true); setError('')
    try {
      if (editing) await updateOpticalConnection(editing.id, form)
      else await createOpticalConnection(form)
      await load(); reset()
    } catch (cause) { setError(cause instanceof ApiError ? Object.values(cause.errors).flat()[0] ?? cause.message : 'Não foi possível salvar a conexão.') }
    finally { setBusy(false) }
  }

  async function makeDefault(connection: OpticalConnection) {
    setBusy(true); setError('')
    try { await updateOpticalConnection(connection.id, { nome: connection.nome, provedor: connection.provedor, modelo: connection.modelo, chave: '', padrao: true }); await load() }
    catch { setError('Não foi possível definir a conexão padrão.') }
    finally { setBusy(false) }
  }

  async function remove() {
    if (!removing) return
    setBusy(true); setError('')
    try { await deleteOpticalConnection(removing.id); setRemoving(null); await load() }
    catch { setError('Não foi possível remover a conexão.') }
    finally { setBusy(false) }
  }

  return <><header className="page-heading"><h1 tabIndex={-1}>Optical Link</h1><p>Conecte provedores de IA à Seryn por meio da arquitetura Lumen.</p></header>
    <div className="tokens-layout">
      <section className="panel token-create"><h2>{editing ? 'Editar conexão' : 'Nova conexão'}</h2><p className="muted">A chave é criptografada e nunca será exibida novamente.</p>
        <form onSubmit={event => { void submit(event) }}>
          <label>Nome<input required maxLength={80} value={form.nome} onChange={event => setForm(current => ({ ...current, nome: event.target.value }))} placeholder="Ex.: OpenRouter pessoal" /></label>
          <label>Provedor<select value={form.provedor} onChange={event => setForm(current => ({ ...current, provedor: event.target.value }))}><option value="openrouter">OpenRouter</option></select></label>
          <label>Modelo padrão<input required maxLength={160} value={form.modelo} onChange={event => setForm(current => ({ ...current, modelo: event.target.value }))} placeholder="Ex.: google/gemma-4-31b-it:free" /></label>
          <label>Chave da API<input type="password" autoComplete="new-password" required={!editing} maxLength={2048} value={form.chave} onChange={event => setForm(current => ({ ...current, chave: event.target.value }))} placeholder={editing ? 'Deixe em branco para manter a atual' : 'Cole a chave fornecida pelo provedor'} /></label>
          <label className="token-scope"><input type="checkbox" checked={form.padrao} disabled={editing?.padrao} onChange={event => setForm(current => ({ ...current, padrao: event.target.checked }))} /><span><strong>Conexão padrão</strong><small>{editing?.padrao ? 'Escolha outra conexão na lista para substituir a padrão atual.' : 'Será usada pelo chat e pelas novas conversas.'}</small></span></label>
          <div className="optical-form-actions"><Button disabled={busy}>{busy ? 'Salvando…' : editing ? 'Salvar alterações' : 'Adicionar conexão'}</Button>{editing && <Button type="button" variant="ghost" onClick={reset}>Cancelar</Button>}</div>
          {error && <p className="form-error" role="alert">{error}</p>}
        </form>
      </section>
      <section className="panel token-list"><h2>Conexões</h2>
        {connections.length === 0 ? <div className="tokens-empty"><KeyRound /><p>Nenhuma conexão cadastrada.</p></div> : <ul>{connections.map(connection => <li key={connection.id}><div><strong>{connection.nome}{connection.padrao && <span className="optical-default"><Check size={13} />Padrão</span>}</strong><code>{connection.chave}</code><small>OpenRouter · modelo padrão: {connection.modelo}</small></div><div className="optical-item-actions">{!connection.padrao && <Button variant="ghost" aria-label={`Usar ${connection.nome} como padrão`} disabled={busy} onClick={() => { void makeDefault(connection) }}><Check size={18} /></Button>}<Button variant="ghost" aria-label={`Editar ${connection.nome}`} disabled={busy} onClick={() => edit(connection)}><Pencil size={18} /></Button><Button variant="ghost" aria-label={`Remover ${connection.nome}`} disabled={busy} onClick={() => setRemoving(connection)}><Trash2 size={18} /></Button></div></li>)}</ul>}
      </section>
    </div>{removing && <ConfirmDialog title="Remover conexão?" description={`A chave de “${removing.nome}” será removida permanentemente. Conversas que dependam dela deixarão de funcionar.`} confirmLabel="Remover conexão" busyLabel="Removendo…" busy={busy} onCancel={() => setRemoving(null)} onConfirm={() => { void remove() }} />}</>
}
