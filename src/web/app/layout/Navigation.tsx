import { NavLink, useLocation } from 'react-router-dom'
import type { NavigationItem } from './navigation'

export function Navigation({ items, label, className, settingsChildren = false }: { items: NavigationItem[]; label: string; className?: string; settingsChildren?: boolean }) {
  const { pathname } = useLocation()
  return <nav className={className} aria-label={label}>{items.map(item => <NavigationLink key={item.path} item={item} relatedActive={settingsChildren && item.path === '/ajustes' && ['/categorias', '/tipos', '/pagamentos', '/importar-exportar'].includes(pathname)} />)}</nav>
}

function NavigationLink({ item, relatedActive = false }: { item: NavigationItem; relatedActive?: boolean }) {
  return <NavLink to={item.path} end aria-current={relatedActive ? 'page' : undefined} className={({ isActive }) => `nav-link${isActive || relatedActive ? ' active' : ''}`}><item.icon aria-hidden="true" /><span>{item.label}</span></NavLink>
}
