import { useEffect, useState } from 'react'
import { ApiError, getAuthStatus, getSession, type Session } from '@/modules/auth/api/authApi'
import { AuthRoute } from '@/modules/auth/routes/AuthRoute'
import { AppLayout } from '@/layout/AppLayout'
import { ConnectionScreen } from './ConnectionScreen'

type State = { kind: 'loading' } | { kind: 'error' } | { kind: 'anonymous'; configured: boolean } | { kind: 'authenticated'; session: Session }

export function App() {
  const [state, setState] = useState<State>({ kind: 'loading' })
  const [attempt, setAttempt] = useState(0)

  useEffect(() => {
    let active = true
    async function load() {
      try {
        const status = await getAuthStatus()
        let session: Session | undefined
        if (status.configured) {
          try { session = await getSession() }
          catch (cause) { if (!(cause instanceof ApiError && cause.status === 401)) throw cause }
        }
        if (active) setState(session ? { kind: 'authenticated', session } : { kind: 'anonymous', configured: status.configured })
      } catch { if (active) setState({ kind: 'error' }) }
    }
    void load()
    return () => { active = false }
  }, [attempt])

  useEffect(() => {
    if (state.kind !== 'authenticated') return
    let active = true
    async function check() {
      try { await getSession() }
      catch (cause) { if (active && cause instanceof ApiError && cause.status === 401) setState({ kind: 'anonymous', configured: true }) }
    }
    const timer = window.setInterval(() => { void check() }, 60_000)
    const visible = () => { if (document.visibilityState === 'visible') void check() }
    document.addEventListener('visibilitychange', visible)
    return () => { active = false; clearInterval(timer); document.removeEventListener('visibilitychange', visible) }
  }, [state.kind])

  const retry = () => { setState({ kind: 'loading' }); setAttempt(current => current + 1) }
  if (state.kind === 'loading' || state.kind === 'error') return <ConnectionScreen state={state.kind} onRetry={retry} />
  if (state.kind === 'anonymous') return <AuthRoute configured={state.configured} onRetry={retry} onLogin={username => setState({ kind: 'authenticated', session: { username } })} />
  return <AppLayout username={state.session.username} onLogout={() => setState({ kind: 'anonymous', configured: true })} />
}
