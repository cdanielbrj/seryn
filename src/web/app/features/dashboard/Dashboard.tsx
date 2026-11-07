import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { ArrowDownRight, ArrowUpRight, CalendarDays, ChevronLeft, ChevronRight, Equal, Layers, TrendingUp } from 'lucide-react'
import { ApiError, get } from '@/shared/api/client'
import { formatMoney } from '@/features/records/records'
import { chartDescription, monthName, type DashboardProjection, type TopRecordProjection } from './projections'
import { movePeriod, periodLabel, periods, type Period, type Reference } from './period'

export function Dashboard() {
  const [period, setPeriod] = useState<Period>('month')
  const [reference, setReference] = useState<Reference>(() => { const now = new Date(); return { year: now.getFullYear(), month: now.getMonth() + 1 } })
  const [data, setData] = useState<DashboardProjection | null>(null)
  const [error, setError] = useState('')
  const [loadedKey, setLoadedKey] = useState('')
  const [failedKey, setFailedKey] = useState('')
  const label = periodLabel(period, reference)
  const requestKey = `${period}-${reference.year}-${reference.month}`
  const loading = requestKey !== loadedKey && requestKey !== failedKey
  useEffect(() => {
    let active = true
    void get<DashboardProjection>(`/projections?period=${period}&reference=${reference.year}-${String(reference.month).padStart(2, '0')}`)
      .then(value => { if (active) { setData(value); setError(''); setLoadedKey(requestKey); setFailedKey('') } })
      .catch(cause => { if (active) { setData(null); setError(cause instanceof ApiError ? cause.message : 'Não foi possível carregar a projeção.'); setFailedKey(requestKey) } })
    return () => { active = false }
  }, [period, reference, requestKey])
  const noRecords = data !== null && data.entradasCentavos === 0 && data.saidasCentavos === 0
  return <>
    <header className="overview-header">
      <div><h1 tabIndex={-1}>Visão Geral</h1><p className="desktop-subtitle">Panorama e projeção do seu planejamento financeiro.</p><p className="mobile-subtitle">Seu planejamento, em perspectiva.</p></div>
      <div className="period-controls"><div className="segment" role="group" aria-label="Período">{periods.map(item => <button key={item.id} aria-pressed={period === item.id} onClick={() => setPeriod(item.id)}>{item.label}</button>)}</div><div className="date-select"><button aria-label="Período anterior" disabled={movePeriod(period, reference, -1).year < 1} onClick={() => setReference(movePeriod(period, reference, -1))}><ChevronLeft /></button><span aria-live="polite">{label}</span><button aria-label="Próximo período" disabled={movePeriod(period, reference, 1).year > 9999} onClick={() => setReference(movePeriod(period, reference, 1))}><ChevronRight /></button></div></div>
    </header>
    {error && failedKey === requestKey && <p className="form-error dashboard-error" role="alert">{error}</p>}
    <section className="summary-cards" aria-label="Resumo do período"><Stat kind="income" title="Entradas" value={data?.entradasCentavos} loading={loading} icon={<ArrowUpRight aria-hidden="true" />} /><Stat kind="expense" title="Saídas" value={data?.saidasCentavos} loading={loading} icon={<ArrowDownRight aria-hidden="true" />} /><Stat kind="result" title="Resultado" value={data?.resultadoCentavos} loading={loading} icon={<Equal aria-hidden="true" />} /></section>
    <section className="panel projection-panel desktop-only">
      <div className="panel-heading"><div className="square-icon"><TrendingUp aria-hidden="true" /></div><div><h2>Projeção do período</h2><p>{data ? chartDescription(data) : `Resultado previsto acumulado em ${label.toLocaleLowerCase('pt-BR')}.`}</p></div></div>
      {loading ? <ChartState title="Carregando projeção…" /> : noRecords ? <ChartState title="Seu planejamento começa aqui" detail="Ainda não há registros para projetar neste período." /> : data && <ProjectionChart data={data} />}
      <div className="chart-legend"><Legend color="income-dot" label="Entradas (previstas)" value={data?.entradasCentavos} /><Legend color="expense-dot" label="Saídas (previstas)" value={data?.saidasCentavos} /><Legend color="result-dot" label="Resultado acumulado" value={data?.grafico.at(-1)?.resultadoAcumuladoCentavos} /></div>
    </section>
    <div className="bottom-grid">
      <section className="panel category-panel desktop-only"><div className="panel-heading"><div className="square-icon"><Layers aria-hidden="true" /></div><div><h2>Por categoria</h2><p>Distribuição das saídas previstas.</p></div></div>{loading ? <div className="category-empty"><p role="status">Carregando categorias…</p></div> : data?.categorias.length ? <CategoryChart categories={data.categorias} total={data.saidasCentavos} /> : <div className="category-empty"><div className="empty-donut" role="img" aria-label="Sem distribuição por categoria"><div><b>—</b><span>Total de saídas</span></div></div><div><h3>Sem categorias no período</h3><p>As saídas previstas serão agrupadas aqui.</p></div></div>}</section>
      <section className="panel records-panel"><div className="panel-heading"><div className="square-icon desktop-only"><CalendarDays aria-hidden="true" /></div><div><h2>Maiores movimentações</h2><p className="desktop-only">Registros com maior impacto no período.</p></div><Link className="text-link mobile-only" to="/registros">Ver todos</Link></div>{loading ? <div className="empty-records"><p role="status">Carregando movimentações…</p></div> : noRecords ? <div className="empty-records"><CalendarDays aria-hidden="true" /><h3>Nenhum registro por aqui</h3><p>Não há registros previstos para este período.</p><Link className="text-link desktop-only" to="/registros">Ir para registros<ChevronRight size={16} aria-hidden="true" /></Link></div> : data && <TopRecords incomes={data.maioresEntradas} expenses={data.maioresSaidas} />}</section>
    </div>
  </>
}

function Stat({ kind, title, value, loading, icon }: { kind: 'income' | 'expense' | 'result'; title: string; value?: number; loading: boolean; icon: ReactNode }) {
  const content = loading || value === undefined ? '—' : formatMoney(value)
  const resultTone = kind === 'result' && value !== undefined && value < 0 ? ' negative' : ''
  return <article className={`panel stat ${kind}${resultTone}`}><div className="stat-icon">{icon}</div><div><h2>{kind === 'result' ? <><span className="desktop-only">{title}</span><span className="mobile-only">Resultado previsto</span></> : title}</h2><div className="stat-value" aria-label={loading ? 'Carregando' : content}>{content}</div><p>{kind === 'result' ? 'Projeção do período' : 'Previstas no período'}</p></div></article>
}
function Legend({ color, label, value }: { color: string; label: string; value?: number }) { return <span><i className={`dot ${color}`} />{label}<b>{value === undefined ? '—' : formatMoney(value)}</b></span> }
function ChartState({ title, detail }: { title: string; detail?: string }) { return <div className="empty-chart"><div className="chart-grid" aria-hidden="true" /><div className="empty-chart-message"><TrendingUp aria-hidden="true" /><h3>{title}</h3>{detail && <p>{detail}</p>}<small>O resultado acumulado começa em zero.</small></div></div> }

function ProjectionChart({ data }: { data: DashboardProjection }) {
  const buckets = data.grafico
  const dimensions = useMemo(() => {
    const values = buckets.flatMap(item => [item.entradasCentavos, -item.saidasCentavos, item.resultadoAcumuladoCentavos, 0]); const min = Math.min(...values); const max = Math.max(...values); const span = Math.max(1, max - min)
    return { y: (value: number) => 18 + (max - value) * 174 / span, x: (index: number) => 54 + index * (942 / Math.max(1, buckets.length - 1)), zero: 18 + max * 174 / span }
  }, [buckets])
  const path = buckets.map((item, index) => `${index ? 'L' : 'M'}${dimensions.x(index).toFixed(1)},${dimensions.y(item.resultadoAcumuladoCentavos).toFixed(1)}`).join(' ')
  return <div className="chart-wrap"><svg viewBox="0 0 1050 240" preserveAspectRatio="none" role="img" aria-label="Gráfico de entradas, saídas e resultado acumulado previstos">
    {[0, 1, 2, 3].map(index => <line key={index} x1="48" x2="1028" y1={24 + index * 55} y2={24 + index * 55} className="projection-grid-line" />)}<line x1="48" x2="1028" y1={dimensions.zero} y2={dimensions.zero} className="projection-zero-line" />
    {buckets.map((item, index) => { const x = dimensions.x(index); const incomeY = dimensions.y(item.entradasCentavos); const expenseY = dimensions.y(-item.saidasCentavos); return <g key={item.mes}>{item.selecionado && <rect x={x - 42} y="14" width="84" height="192" rx="8" className="projection-selected" />}<rect x={x - 9} y={Math.min(dimensions.zero, incomeY)} width="7" height={Math.abs(dimensions.zero - incomeY)} rx="3" className="projection-income-bar" /><rect x={x + 2} y={Math.min(dimensions.zero, expenseY)} width="7" height={Math.abs(dimensions.zero - expenseY)} rx="3" className="projection-expense-bar" /><text x={x} y="228" textAnchor="middle" className="projection-label">{monthName(item.mes).slice(0, 3)}</text></g> })}<path d={path} className="projection-result-line" />{buckets.map((item, index) => <circle key={`${item.mes}-point`} cx={dimensions.x(index)} cy={dimensions.y(item.resultadoAcumuladoCentavos)} r={item.selecionado ? 5 : 3} className="projection-result-point" />)}
  </svg></div>
}

function CategoryChart({ categories, total }: { categories: DashboardProjection['categorias']; total: number }) {
  const colors = ['#117E78', '#456A9B', '#B7783A', '#8C5DA7', '#5B6B70']; const stops = categories.reduce<{ offset: number; values: string[] }>((state, category, index) => ({ offset: state.offset + category.percentual, values: [...state.values, `${colors[index % colors.length]} ${state.offset}% ${state.offset + category.percentual}%`] }), { offset: 0, values: [] }).values.join(', ')
  return <div className="category-content"><div className="category-donut" style={{ background: `conic-gradient(${stops})` }} role="img" aria-label={`Distribuição de ${formatMoney(total)} em saídas`}><div><b>{formatMoney(total)}</b><span>Total de saídas</span></div></div><div className="category-list">{categories.map((category, index) => <div className="category-row" key={category.nome}><div className="category-left"><i style={{ background: colors[index % colors.length] }} />{category.nome}</div><span className="value">{formatMoney(category.valorCentavos)}</span><span className="percent">{category.percentual}%</span></div>)}</div></div>
}
function TopRecords({ incomes, expenses }: { incomes: TopRecordProjection[]; expenses: TopRecordProjection[] }) { return <div className="top-records"><TopColumn title="Maiores entradas" direction="entrada" records={incomes} /><TopColumn title="Maiores saídas" direction="saida" records={expenses} /></div> }
function TopColumn({ title, direction, records }: { title: string; direction: 'entrada' | 'saida'; records: TopRecordProjection[] }) { return <div className="top-column"><h3>{title}</h3>{records.length ? records.map(record => <div className="top-record" key={record.id}><div><strong>{record.nome}</strong><span>{record.categoriaNome ?? 'Sem categoria'} · {record.ocorrencias === 1 ? '1 ocorrência' : `${record.ocorrencias} ocorrências`}</span></div><b className={direction}>{direction === 'entrada' ? '+' : '−'} {formatMoney(record.valorCentavos)}</b></div>) : <p>Nenhum registro no período.</p>}</div> }
