import { Link } from 'react-router-dom'
import type { LucideIcon } from 'lucide-react'
import { Button } from '@/components/primitives/Button'

export function UnavailableState({ title, description, detail, icon: Icon }: { title: string; description: string; detail: string; icon: LucideIcon }) {
  return <><header className="page-heading"><h1 tabIndex={-1}>{title}</h1><p>{description}</p></header><section className="panel unavailable"><div className="large-icon"><Icon aria-hidden="true" /></div><h2>Disponível em uma próxima etapa</h2><p>{detail}</p><Button asChild variant="outline"><Link to="/">Voltar à Visão Geral</Link></Button></section></>
}
