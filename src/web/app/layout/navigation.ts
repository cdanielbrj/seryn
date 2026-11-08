import { ArrowLeftRight, BarChart3, CalendarDays, CreditCard, Home, Layers, Settings, Tag, Target, type LucideIcon } from 'lucide-react'

export type NavigationItem = { path: string; label: string; icon: LucideIcon; mobile?: boolean }

export const navigation: NavigationItem[] = [
  { path: '/', label: 'Visão Geral', icon: Home, mobile: true },
  { path: '/analises', label: 'Análises', icon: BarChart3, mobile: true },
  { path: '/objetivos', label: 'Objetivos', icon: Target, mobile: true },
  { path: '/registros', label: 'Registros', icon: CalendarDays, mobile: true },
  { path: '/pagamentos', label: 'Pagamentos', icon: CreditCard },
  { path: '/categorias', label: 'Categorias', icon: Layers },
  { path: '/tipos', label: 'Tipos', icon: Tag },
  { path: '/importar-exportar', label: 'Importar / Exportar', icon: ArrowLeftRight },
  { path: '/ajustes', label: 'Ajustes', icon: Settings, mobile: true },
]
