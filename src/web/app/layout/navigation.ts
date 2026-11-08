import { ArrowLeftRight, CalendarDays, CreditCard, Home, Layers, Settings, Tag, type LucideIcon } from 'lucide-react'

export type NavigationItem = { path: string; label: string; icon: LucideIcon; mobile?: boolean }

export const navigation: NavigationItem[] = [
  { path: '/', label: 'Visão Geral', icon: Home, mobile: true },
  { path: '/registros', label: 'Registros', icon: CalendarDays, mobile: true },
  { path: '/pagamentos', label: 'Pagamentos', icon: CreditCard },
  { path: '/categorias', label: 'Categorias', icon: Layers, mobile: true },
  { path: '/tipos', label: 'Tipos', icon: Tag },
  { path: '/importar-exportar', label: 'Importar / Exportar', icon: ArrowLeftRight },
  { path: '/ajustes', label: 'Ajustes', icon: Settings, mobile: true },
]
