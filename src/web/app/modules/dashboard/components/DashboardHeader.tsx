import { ChevronLeft, ChevronRight } from 'lucide-react'
import { movePeriod, periods, type Period, type Reference } from '../model/period'

type DashboardHeaderProps = {
  period: Period
  reference: Reference
  label: string
  onPeriodChange: (period: Period) => void
  onReferenceChange: (reference: Reference) => void
}

export function DashboardHeader({ period, reference, label, onPeriodChange, onReferenceChange }: DashboardHeaderProps) {
  const previous = movePeriod(period, reference, -1)
  const next = movePeriod(period, reference, 1)
  return <header className="overview-header">
    <div><h1 tabIndex={-1}>Visão Geral</h1><p className="desktop-subtitle">Panorama e projeção do seu planejamento financeiro.</p><p className="mobile-subtitle">Seu planejamento, em perspectiva.</p></div>
    <div className="period-controls">
      <div className="segment" role="group" aria-label="Período">{periods.map(item => <button key={item.id} aria-pressed={period === item.id} onClick={() => onPeriodChange(item.id)}>{item.label}</button>)}</div>
      <div className="date-select"><button aria-label="Período anterior" disabled={previous.year < 1} onClick={() => onReferenceChange(previous)}><ChevronLeft /></button><span aria-live="polite">{label}</span><button aria-label="Próximo período" disabled={next.year > 9999} onClick={() => onReferenceChange(next)}><ChevronRight /></button></div>
    </div>
  </header>
}
