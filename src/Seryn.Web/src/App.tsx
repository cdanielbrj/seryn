import { useEffect, useState } from 'react'
import { Link, NavLink, Route, Routes, useLocation } from 'react-router-dom'
import { ArrowLeftRight, CalendarDays, ChevronRight, CreditCard, Home, Layers, LogOut, Settings, Tag, type LucideIcon } from 'lucide-react'
import { ApiError, get, post } from './lib/api'
import { AuthScreen } from './components/AuthScreen'
import { Brand } from './components/Brand'
import { Dashboard } from './components/Dashboard'
import { ClassificationsPage } from './components/ClassificationsPage'
import { CategoriesPage } from './components/CategoriesPage'
import { ThemeControl } from './components/ThemeControl'
import { Button } from './components/ui/button'

const navigation: { path: string; label: string; icon: LucideIcon; mobile?: boolean }[] = [
  { path: '/', label: 'Visão Geral', icon: Home, mobile: true },
  { path: '/registros', label: 'Registros', icon: CalendarDays, mobile: true },
  { path: '/pagamentos', label: 'Pagamentos', icon: CreditCard },
  { path: '/categorias', label: 'Categorias', icon: Layers, mobile: true },
  { path: '/tipos', label: 'Tipos', icon: Tag },
  { path: '/importar-exportar', label: 'Importar / Exportar', icon: ArrowLeftRight },
  { path: '/ajustes', label: 'Ajustes', icon: Settings, mobile: true },
]
type Session = { username: string }
type State = { kind: 'loading' } | { kind: 'error' } | { kind: 'anonymous'; configured: boolean } | { kind: 'authenticated'; session: Session }

export function App() {
  const [state, setState] = useState<State>({ kind: 'loading' })
  const [attempt, setAttempt] = useState(0)
  useEffect(() => {
    let active = true
    async function load() {
      try {
        const status = await get<{ configured: boolean }>('/auth/status')
        let session: Session | undefined
        if (status.configured) {
          try { session = await get<Session>('/auth/session') }
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
      try { await get<Session>('/auth/session') }
      catch (cause) { if (active && cause instanceof ApiError && cause.status === 401) setState({ kind: 'anonymous', configured: true }) }
    }
    const timer = window.setInterval(() => { void check() }, 60_000)
    const visible = () => { if (document.visibilityState === 'visible') void check() }
    document.addEventListener('visibilitychange', visible)
    return () => { active = false; clearInterval(timer); document.removeEventListener('visibilitychange', visible) }
  }, [state.kind])

  if (state.kind === 'loading' || state.kind === 'error') return <main className="connection-screen"><Brand /><h1>{state.kind === 'loading' ? 'Preparando seu espaço…' : 'Não foi possível conectar'}</h1><p role="status">{state.kind === 'loading' ? 'Aguarde um instante.' : 'Verifique se a sua instalação está disponível e tente novamente.'}</p>{state.kind === 'error' && <Button onClick={() => { setState({ kind: 'loading' }); setAttempt(attempt + 1) }}>Tentar novamente</Button>}</main>
  if (state.kind === 'anonymous') return <AuthScreen configured={state.configured} onRetry={() => { setState({ kind: 'loading' }); setAttempt(attempt + 1) }} onLogin={username => setState({ kind: 'authenticated', session: { username } })} />
  return <Shell username={state.session.username} onLogout={() => setState({ kind: 'anonymous', configured: true })} />
}

function Shell({ username, onLogout }: { username: string; onLogout: () => void }) {
  const location = useLocation()
  useEffect(() => {
    const current = navigation.find(item => item.path === location.pathname)
    document.title = `Seryn — ${current?.label ?? 'Página não encontrada'}`
    document.querySelector<HTMLElement>('main h1')?.focus()
    window.scrollTo(0, 0)
  }, [location.pathname])
  return <div className="app-shell">
    <a className="skip-link" href="#conteudo">Pular para conteúdo</a>
    <aside className="sidebar"><Brand /><nav aria-label="Navegação principal">{navigation.map(item => <NavigationLink key={item.path} item={item} />)}</nav><div className="sidebar-bottom"><ThemeControl /><p>Seryn · planejamento financeiro</p></div></aside>
    <div className="workspace"><header className="mobile-header"><Brand /><ThemeControl /></header>
      <main id="conteudo" className="main-content"><Routes>
        <Route path="/" element={<Dashboard />} />
        <Route path="/registros" element={<Unavailable title="Registros" icon={CalendarDays} description="Suas entradas e saídas, organizadas em um só lugar." detail="O cadastro de registros estará disponível na etapa de dados e recorrências. Ainda não há registros nesta instalação." />} />
        <Route path="/pagamentos" element={<ClassificationsPage key="payment-methods" kind="payment-methods" />} />
        <Route path="/categorias" element={<CategoriesPage />} />
        <Route path="/tipos" element={<ClassificationsPage key="types" kind="types" />} />
        <Route path="/importar-exportar" element={<Unavailable title="Importar / Exportar" icon={ArrowLeftRight} description="Seus registros, com portabilidade." detail="A importação e exportação de registros originais em CSV e Excel serão disponibilizadas na etapa de portabilidade." />} />
        <Route path="/ajustes" element={<Preferences username={username} onLogout={onLogout} />} />
        <Route path="*" element={<Unavailable title="Página não encontrada" icon={Home} description="Este endereço não existe na Seryn." detail="Use a navegação para voltar ao seu planejamento." />} />
      </Routes></main>
    </div>
    <nav className="bottom-nav" aria-label="Navegação mobile">{navigation.filter(item => item.mobile).map(item => <NavigationLink key={item.path} item={item} />)}</nav>
  </div>
}

function NavigationLink({ item }: { item: typeof navigation[number] }) {
  return <NavLink to={item.path} end className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}><item.icon aria-hidden="true" /><span>{item.label}</span></NavLink>
}

function Unavailable({ title, description, detail, icon: Icon }: { title: string; description: string; detail: string; icon: LucideIcon }) {
  return <><header className="page-heading"><h1 tabIndex={-1}>{title}</h1><p>{description}</p></header><section className="panel unavailable"><div className="large-icon"><Icon aria-hidden="true" /></div><h2>Disponível em uma próxima etapa</h2><p>{detail}</p><Button asChild variant="outline"><Link to="/">Voltar à Visão Geral</Link></Button></section></>
}

function Preferences({ username, onLogout }: { username: string; onLogout: () => void }) {
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  async function logout() {
    setBusy(true)
    setError('')
    try { await post('/auth/logout'); onLogout() }
    catch (cause) {
      if (cause instanceof ApiError && cause.status === 401) onLogout()
      else setError('Não foi possível encerrar a sessão. Tente novamente.')
    } finally { setBusy(false) }
  }
  return <><header className="page-heading"><h1 tabIndex={-1}>Ajustes</h1><p>Seu espaço, do seu jeito.</p></header><div className="settings-grid">
    <section className="panel"><h2>Aparência</h2><p className="muted">Escolha como a Seryn aparece neste dispositivo.</p><ThemeControl expanded /></section>
    <section className="panel"><h2>Seu acesso</h2><p className="muted">Conectado como <strong>{username}</strong>.</p><Button variant="outline" onClick={() => { void logout() }} disabled={busy}><LogOut size={18} aria-hidden="true" />{busy ? 'Saindo…' : 'Sair da Seryn'}</Button>{error && <p className="form-error" role="alert">{error}</p>}</section>
    <section className="panel"><h2>Organização e portabilidade</h2><div className="settings-links"><Link to="/tipos"><Tag aria-hidden="true" />Tipos<ChevronRight aria-hidden="true" /></Link><Link to="/pagamentos"><CreditCard aria-hidden="true" />Pagamentos<ChevronRight aria-hidden="true" /></Link><Link to="/importar-exportar"><ArrowLeftRight aria-hidden="true" />Importar / Exportar<ChevronRight aria-hidden="true" /></Link></div></section>
    <section className="panel"><h2>Sobre esta versão</h2><p className="muted">Fundação da Seryn: acesso seguro, navegação e temas. O cadastro de dados e as projeções chegam na próxima etapa.</p><p className="muted">Todos os valores da Seryn são previsões de planejamento.</p></section>
  </div></>
}
