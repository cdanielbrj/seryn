import { useState, type FormEvent } from 'react'
import { ArrowRight, LockKeyhole, ShieldCheck } from 'lucide-react'
import { post } from '../lib/api'
import { Brand } from './Brand'
import { ThemeControl } from './ThemeControl'
import { Button } from './ui/button'

export function AuthScreen({ configured, onLogin, onRetry }: { configured: boolean; onLogin: (name: string) => void; onRetry: () => void }) {
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = event.currentTarget
    const data = new FormData(form)
    setError('')
    setBusy(true)
    try {
      const credentials = { username: data.get('username'), password: data.get('password') }
      const session = await post<{ username: string }>('/auth/login', credentials)
      form.reset()
      onLogin(session.username)
    } catch (cause) { setError(cause instanceof Error ? cause.message : 'Não foi possível conectar. Tente novamente.') }
    finally { setBusy(false) }
  }
  return <div className="auth-page">
    <header className="auth-header"><Brand /><ThemeControl /></header>
    <main className="auth-card panel">
      <div className="auth-icon">{configured ? <LockKeyhole /> : <ShieldCheck />}</div>
      <p className="eyebrow">Seu planejamento, em perspectiva.</p>
      <h1>{configured ? 'Bem-vindo à Seryn' : 'Aguardando configuração'}</h1>
      <p className="muted">{configured ? 'Entre para acessar seu espaço de planejamento.' : 'O responsável pela instalação precisa configurar o administrador no servidor. Consulte “Primeiro acesso” no README e volte após a configuração.'}</p>
      {configured ? <form onSubmit={submit} className="auth-form">
        <label>Usuário<input name="username" autoComplete="username" required maxLength={64} autoCapitalize="none" spellCheck={false} /></label>
        <label>Senha<input name="password" type="password" autoComplete="current-password" required maxLength={128} /></label>
        {error && <p className="form-error" role="alert">{error}</p>}
        <Button type="submit" disabled={busy}>{busy ? 'Aguarde…' : 'Entrar'}<ArrowRight size={17} aria-hidden="true" /></Button>
      </form> : <Button variant="outline" className="mt-6 w-full" onClick={onRetry}>Verificar novamente</Button>}
      <p className="auth-foot"><ShieldCheck size={15} aria-hidden="true" />Seu espaço pessoal. Seus dados na sua instalação.</p>
    </main>
    <footer className="auth-footer">Seryn · planejamento financeiro</footer>
  </div>
}
