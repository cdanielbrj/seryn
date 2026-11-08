import { useEffect } from 'react'
import { useLocation } from 'react-router-dom'
import { Brand } from '@/components/patterns/Brand'
import { ThemeControl } from '@/components/patterns/ThemeControl'
import { AppRouter } from '@/bootstrap/router'
import { Navigation } from './Navigation'
import { navigation } from './navigation'

export function AppLayout({ username, onLogout }: { username: string; onLogout: () => void }) {
  const location = useLocation()
  useEffect(() => {
    const current = navigation.find(item => item.path === location.pathname) ??
      (location.pathname.startsWith('/registros/') ? navigation.find(item => item.path === '/registros') : undefined) ??
      (location.pathname.startsWith('/objetivos/') ? navigation.find(item => item.path === '/objetivos') : undefined)
    document.title = `Seryn — ${current?.label ?? 'Página não encontrada'}`
    document.querySelector<HTMLElement>('main h1')?.focus()
    window.scrollTo(0, 0)
  }, [location.pathname])

  return <div className="app-shell">
    <a className="skip-link" href="#conteudo">Pular para conteúdo</a>
    <aside className="sidebar"><Brand /><Navigation items={navigation} label="Navegação principal" /><div className="sidebar-bottom"><ThemeControl /><p>Seryn · planejamento financeiro</p></div></aside>
    <div className="workspace"><header className="mobile-header"><Brand /><ThemeControl /></header><main id="conteudo" className="main-content"><AppRouter username={username} onLogout={onLogout} /></main></div>
    <Navigation className="bottom-nav" items={navigation.filter(item => item.mobile)} label="Navegação mobile" />
  </div>
}
