import { useEffect, useState } from 'react'
import { LogOut, ShieldCheck } from 'lucide-react'
import { Button } from '@/components/primitives/Button'
import { ConfirmDialog } from '@/components/patterns/ConfirmDialog'
import { listSessions, revokeOtherSessions, revokeSession, type LoginSession } from '../api/sessionsApi'

const formatDate = (value: string) => new Intl.DateTimeFormat('pt-BR', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))

export function SessionsRoute() {
  const [sessions, setSessions] = useState<LoginSession[]>([])
  const [revoke, setRevoke] = useState<LoginSession | 'others' | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  async function load() { try { setSessions(await listSessions()) } catch { setError('Não foi possível carregar as sessões.') } }
  useEffect(() => {
    let active = true
    listSessions().then(values => { if (active) setSessions(values) }).catch(() => { if (active) setError('Não foi possível carregar as sessões.') })
    return () => { active = false }
  }, [])

  async function confirmRevoke() {
    if (!revoke) return
    setBusy(true); setError('')
    try {
      if (revoke === 'others') await revokeOtherSessions()
      else await revokeSession(revoke.id)
      setRevoke(null); await load()
    } catch { setError('Não foi possível revogar a sessão.') }
    finally { setBusy(false) }
  }

  const others = sessions.filter(session => !session.atual)
  return <><header className="page-heading"><h1 tabIndex={-1}>Sessões ativas</h1><p>Revise e encerre acessos administrativos que não reconhece.</p></header>
    {error && <p className="form-error portability-message" role="alert">{error}</p>}
    <section className="panel sessions-panel"><div className="panel-heading"><div className="square-icon"><ShieldCheck /></div><div><h2>Acessos administrativos</h2><p>Sessões expiram após oito horas e não são renovadas automaticamente.</p></div></div>
      {others.length > 1 && <Button className="sessions-revoke-others danger-button" onClick={() => setRevoke('others')}><LogOut size={18} />Encerrar todas as outras</Button>}
      <ul>{sessions.map(session => <li key={session.id}><div><strong>{session.atual ? 'Sessão atual' : 'Outra sessão'}</strong><small>Iniciada em {formatDate(session.criadaEm)}</small><small>Último uso em {formatDate(session.ultimoUsoEm)} · expira em {formatDate(session.expiraEm)}</small></div>{!session.atual && <Button variant="ghost" onClick={() => setRevoke(session)}>Encerrar</Button>}</li>)}</ul>
    </section>
    {revoke && <ConfirmDialog title={revoke === 'others' ? 'Encerrar outras sessões?' : 'Encerrar esta sessão?'} description={revoke === 'others' ? `As ${others.length} outras sessões perderão o acesso imediatamente.` : 'Esse acesso administrativo perderá a validade imediatamente.'} confirmLabel="Encerrar sessões" busyLabel="Encerrando…" busy={busy} onCancel={() => setRevoke(null)} onConfirm={() => { void confirmRevoke() }} />}</>
}
