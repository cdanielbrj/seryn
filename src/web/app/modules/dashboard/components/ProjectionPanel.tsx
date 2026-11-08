import { useMemo } from 'react'
import { TrendingUp } from 'lucide-react'
import { formatMoney } from '@/formatters/currency'
import { chartDescription, monthName, type DashboardProjection } from '../model/projection'

export function ProjectionPanel({ data, label, loading, empty }: { data: DashboardProjection | null; label: string; loading: boolean; empty: boolean }) {
  return <section className="panel projection-panel desktop-only">
    <div className="panel-heading"><div className="square-icon"><TrendingUp aria-hidden="true" /></div><div><h2>Projeção do período</h2><p>{data ? chartDescription(data) : `Resultado previsto acumulado em ${label.toLocaleLowerCase('pt-BR')}.`}</p></div></div>
    {loading ? <ChartState title="Carregando projeção…" /> : empty ? <ChartState title="Seu planejamento começa aqui" detail="Ainda não há registros para projetar neste período." /> : data && <ProjectionChart data={data} />}
    <div className="chart-legend"><Legend color="income-dot" label="Entradas (previstas)" value={data?.entradasCentavos} /><Legend color="expense-dot" label="Saídas (previstas)" value={data?.saidasCentavos} /><Legend color="result-dot" label="Resultado acumulado" value={data?.grafico.at(-1)?.resultadoAcumuladoCentavos} /></div>
  </section>
}

function Legend({ color, label, value }: { color: string; label: string; value?: number }) {
  return <span><i className={`dot ${color}`} />{label}<b>{value === undefined ? '—' : formatMoney(value)}</b></span>
}

function ChartState({ title, detail }: { title: string; detail?: string }) {
  return <div className="empty-chart"><div className="chart-grid" aria-hidden="true" /><div className="empty-chart-message"><TrendingUp aria-hidden="true" /><h3>{title}</h3>{detail && <p>{detail}</p>}<small>O resultado acumulado começa em zero.</small></div></div>
}

function ProjectionChart({ data }: { data: DashboardProjection }) {
  const buckets = data.grafico
  const dimensions = useMemo(() => {
    const values = buckets.flatMap(item => [item.entradasCentavos, -item.saidasCentavos, item.resultadoAcumuladoCentavos, 0])
    const min = Math.min(...values); const max = Math.max(...values); const span = Math.max(1, max - min)
    return { y: (value: number) => 18 + (max - value) * 174 / span, x: (index: number) => 54 + index * (942 / Math.max(1, buckets.length - 1)), zero: 18 + max * 174 / span }
  }, [buckets])
  const path = buckets.map((item, index) => `${index ? 'L' : 'M'}${dimensions.x(index).toFixed(1)},${dimensions.y(item.resultadoAcumuladoCentavos).toFixed(1)}`).join(' ')
  return <div className="chart-wrap"><svg viewBox="0 0 1050 240" preserveAspectRatio="none" role="img" aria-label="Gráfico de entradas, saídas e resultado acumulado previstos">
    {[0, 1, 2, 3].map(index => <line key={index} x1="48" x2="1028" y1={24 + index * 55} y2={24 + index * 55} className="projection-grid-line" />)}<line x1="48" x2="1028" y1={dimensions.zero} y2={dimensions.zero} className="projection-zero-line" />
    {buckets.map((item, index) => { const x = dimensions.x(index); const incomeY = dimensions.y(item.entradasCentavos); const expenseY = dimensions.y(-item.saidasCentavos); return <g key={item.mes}>{item.selecionado && <rect x={x - 42} y="14" width="84" height="192" rx="8" className="projection-selected" />}<rect x={x - 9} y={Math.min(dimensions.zero, incomeY)} width="7" height={Math.abs(dimensions.zero - incomeY)} rx="3" className="projection-income-bar" /><rect x={x + 2} y={Math.min(dimensions.zero, expenseY)} width="7" height={Math.abs(dimensions.zero - expenseY)} rx="3" className="projection-expense-bar" /><text x={x} y="228" textAnchor="middle" className="projection-label">{monthName(item.mes).slice(0, 3)}</text></g> })}<path d={path} className="projection-result-line" />{buckets.map((item, index) => <circle key={`${item.mes}-point`} cx={dimensions.x(index)} cy={dimensions.y(item.resultadoAcumuladoCentavos)} r={item.selecionado ? 5 : 3} className="projection-result-point" />)}
  </svg></div>
}
