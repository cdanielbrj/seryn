import { useEffect, useState, type FormEvent } from 'react'
import { Copy, KeyRound, Trash2 } from 'lucide-react'
import { Button } from '@/components/primitives/Button'
import { ConfirmDialog } from '@/components/patterns/ConfirmDialog'
import { ApiError } from '@/infrastructure/http/client'
import { createToken, listTokens, revokeToken, type ApiToken, type CreatedApiToken } from '../api/tokensApi'

const scopeOptions = [
  ['data:read', 'Leitura', 'Consulta registros, objetivos, classificações, projeções e análises.'],
  ['data:write', 'Escrita', 'Cria e edita dados individuais.'],
  ['data:delete', 'Exclusão', 'Exclui dados individuais, inclusive classificações, respeitando as regras existentes.'],
] as const

const formatDate = (value: string | null) => value ? new Intl.DateTimeFormat('pt-BR', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)) : 'Sem expiração'

export function ApiTokensRoute() {
  const [tokens, setTokens] = useState<ApiToken[]>([])
  const [name, setName] = useState('')
  const [scopes, setScopes] = useState<string[]>(['data:read'])
  const [expires, setExpires] = useState(true)
  const [created, setCreated] = useState<CreatedApiToken | null>(null)
  const [revoking, setRevoking] = useState<ApiToken | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')

  async function load() { try { setTokens(await listTokens()) } catch { setError('Não foi possível carregar os tokens.') } }
  useEffect(() => {
    let active = true
    listTokens().then(values => { if (active) setTokens(values) })
      .catch(() => { if (active) setError('Não foi possível carregar os tokens.') })
    return () => { active = false }
  }, [])

  function toggle(scope: string) {
    setScopes(current => {
      if (current.includes(scope))
        return scope === 'data:write' ? current.filter(value => value !== 'data:write' && value !== 'data:delete') : current.filter(value => value !== scope)
      if (scope === 'data:delete') return [...new Set([...current, 'data:write', 'data:delete'])]
      return [...current, scope]
    })
  }

  async function submit(event: FormEvent) {
    event.preventDefault(); setBusy(true); setError(''); setCreated(null)
    try {
      const expiry = expires ? new Date(Date.now() + 90 * 24 * 60 * 60 * 1000).toISOString().replace(/\.\d{3}Z$/, 'Z') : null
      const token = await createToken({ nome: name, escopos: scopes, expiraEm: expiry })
      setCreated(token); setName(''); setScopes(['data:read']); await load()
    } catch (cause) { setError(cause instanceof ApiError ? Object.values(cause.errors).flat()[0] ?? cause.message : 'Não foi possível criar o token.') }
    finally { setBusy(false) }
  }

  async function revoke() {
    if (!revoking) return
    setBusy(true); setError('')
    try { await revokeToken(revoking.id); setRevoking(null); await load() }
    catch { setError('Não foi possível revogar o token.') }
    finally { setBusy(false) }
  }

  return <><header className="page-heading"><h1 tabIndex={-1}>Tokens de integração</h1><p>Conceda acesso controlado a integrações locais ou externas.</p></header>
    <div className="tokens-layout">
      <section className="panel token-create"><h2>Novo token</h2><p className="muted">O segredo será exibido apenas uma vez. Portabilidade, backup e administração nunca ficam disponíveis por token.</p>
        <form onSubmit={event => { void submit(event) }}><label>Nome<input value={name} maxLength={80} onChange={event => setName(event.target.value)} placeholder="Ex.: MCP no MacBook" required /></label>
          <fieldset><legend>Permissões</legend>{scopeOptions.map(([value, label, description]) => <label key={value} className="token-scope"><input type="checkbox" checked={scopes.includes(value)} onChange={() => toggle(value)} /><span><strong>{label}</strong><small>{description}</small></span></label>)}</fieldset>
          <label className="token-scope"><input type="checkbox" checked={expires} onChange={event => setExpires(event.target.checked)} /><span><strong>Expirar em 90 dias</strong><small>Tokens sem expiração exigem revogação manual.</small></span></label>
          <Button disabled={busy || scopes.length === 0}>{busy ? 'Criando…' : 'Criar token'}</Button>{error && <p className="form-error" role="alert">{error}</p>}</form>
      </section>
      <section className="panel token-list"><h2>Tokens ativos</h2>{created && <div className="token-secret" role="status"><strong>Copie agora; este segredo não será mostrado novamente.</strong><code>{created.token}</code><Button variant="outline" onClick={() => { void navigator.clipboard.writeText(created.token) }}><Copy size={17} />Copiar token</Button></div>}
        {tokens.length === 0 ? <div className="tokens-empty"><KeyRound /><p>Nenhum token ativo.</p></div> : <ul>{tokens.map(token => <li key={token.id}><div><strong>{token.nome}</strong><code>{token.prefixo}…</code><small>{token.escopos.join(' · ')}</small><small>Criado em {formatDate(token.criadoEm)} · {token.expiraEm ? `expira em ${formatDate(token.expiraEm)}` : 'sem expiração'}</small></div><Button variant="ghost" aria-label={`Revogar ${token.nome}`} onClick={() => setRevoking(token)}><Trash2 size={18} /></Button></li>)}</ul>}
      </section>
    </div>{revoking && <ConfirmDialog title="Revogar token?" description={`A integração “${revoking.nome}” perderá o acesso imediatamente.`} confirmLabel="Revogar token" busyLabel="Revogando…" busy={busy} onCancel={() => setRevoking(null)} onConfirm={() => { void revoke() }} />}</>
}
