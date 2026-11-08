import { NavLink } from 'react-router-dom'
import type { NavigationItem } from './navigation'

export function Navigation({ items, label, className }: { items: NavigationItem[]; label: string; className?: string }) {
  return <nav className={className} aria-label={label}>{items.map(item => <NavigationLink key={item.path} item={item} />)}</nav>
}

function NavigationLink({ item }: { item: NavigationItem }) {
  return <NavLink to={item.path} end className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}><item.icon aria-hidden="true" /><span>{item.label}</span></NavLink>
}
