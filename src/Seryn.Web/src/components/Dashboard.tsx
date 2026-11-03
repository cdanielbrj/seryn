import { useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowDownRight, ArrowUpRight, CalendarDays, ChevronLeft, ChevronRight, Equal, Layers, TrendingUp } from 'lucide-react'
import { movePeriod, periodLabel, periods, type Period, type Reference } from '../lib/period'

export function Dashboard() {
  const [period, setPeriod] = useState<Period>('month')
  const [reference, setReference] = useState<Reference>(() => { const now = new Date(); return { year: now.getFullYear(), month: now.getMonth() + 1 } })
  const label = periodLabel(period, reference)
  return <>
    <header className="overview-header">
      <div><h1 tabIndex={-1}>Visão Geral</h1><p className="desktop-subtitle">Panorama e projeção do seu planejamento financeiro.</p><p className="mobile-subtitle">Seu planejamento, em perspectiva.</p></div>
      <div className="period-controls">
        <div className="segment" role="group" aria-label="Período">{periods.map(item => <button key={item.id} aria-pressed={period === item.id} onClick={() => setPeriod(item.id)}>{item.label}</button>)}</div>
        <div className="date-select"><button aria-label="Período anterior" disabled={movePeriod(period, reference, -1).year < 1} onClick={() => setReference(movePeriod(period, reference, -1))}><ChevronLeft /></button><span aria-live="polite">{label}</span><button aria-label="Próximo período" disabled={movePeriod(period, reference, 1).year > 9999} onClick={() => setReference(movePeriod(period, reference, 1))}><ChevronRight /></button></div>
      </div>
    </header>
    <section className="summary-cards" aria-label="Resumo do período">
      <article className="panel stat income"><div className="stat-icon"><ArrowUpRight aria-hidden="true" /></div><div><h2>Entradas</h2><div className="stat-value" aria-label="Sem dados">—</div><p>Previstas no período</p></div></article>
      <article className="panel stat expense"><div className="stat-icon"><ArrowDownRight aria-hidden="true" /></div><div><h2>Saídas</h2><div className="stat-value" aria-label="Sem dados">—</div><p>Previstas no período</p></div></article>
      <article className="panel stat result"><div className="stat-icon"><Equal aria-hidden="true" /></div><div><h2><span className="desktop-only">Resultado</span><span className="mobile-only">Resultado previsto</span></h2><div className="stat-value" aria-label="Sem dados">—</div><p><span className="desktop-only">Projeção do período</span><span className="mobile-only">Diferença entre entradas e saídas</span></p></div></article>
    </section>
    <section className="panel projection-panel desktop-only">
      <div className="panel-heading"><div className="square-icon"><TrendingUp aria-hidden="true" /></div><div><h2>Projeção do período</h2><p>Resultado previsto acumulado em {label.toLocaleLowerCase('pt-BR')}.</p></div></div>
      <div className="empty-chart"><div className="chart-grid" aria-hidden="true" /><div className="empty-chart-message"><TrendingUp aria-hidden="true" /><h3>Seu planejamento começa aqui</h3><p>Ainda não há registros para projetar neste período.</p><small>O resultado acumulado começa em zero.</small></div></div>
      <div className="chart-legend"><span><i className="dot income-dot" />Entradas (previstas)<b>—</b></span><span><i className="dot expense-dot" />Saídas (previstas)<b>—</b></span><span><i className="dot result-dot" />Resultado acumulado<b>—</b></span></div>
    </section>
    <div className="bottom-grid">
      <section className="panel category-panel desktop-only">
        <div className="panel-heading"><div className="square-icon"><Layers aria-hidden="true" /></div><div><h2>Por categoria</h2><p>Distribuição das saídas previstas.</p></div></div>
        <div className="category-empty"><div className="empty-donut" role="img" aria-label="Sem distribuição por categoria"><div><b>—</b><span>Total de saídas</span></div></div><div><h3>Sem categorias no período</h3><p>As saídas previstas serão agrupadas aqui.</p></div></div>
      </section>
      <section className="panel records-panel">
        <div className="panel-heading"><div className="square-icon desktop-only"><CalendarDays aria-hidden="true" /></div><div><h2>Registros previstos</h2><p className="desktop-only">Ocorrências pertencentes ao período.</p></div><Link className="text-link mobile-only" to="/registros">Ver todos</Link></div>
        <div className="empty-records"><CalendarDays aria-hidden="true" /><h3>Nenhum registro por aqui</h3><p>Não há registros previstos para este período.</p><Link className="text-link desktop-only" to="/registros">Ir para registros<ChevronRight size={16} aria-hidden="true" /></Link></div>
      </section>
    </div>
  </>
}
