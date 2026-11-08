import type { ReactNode } from 'react'
import { ArrowDownRight, ArrowUpRight, Equal } from 'lucide-react'
import { formatMoney } from '@/formatters/currency'
import type { DashboardProjection } from '../model/projection'

export function SummaryCards({ data, loading }: { data: DashboardProjection | null; loading: boolean }) {
  return <section className="summary-cards" aria-label="Resumo do período">
    <SummaryCard kind="income" title="Entradas" value={data?.entradasCentavos} loading={loading} icon={<ArrowUpRight aria-hidden="true" />} />
    <SummaryCard kind="expense" title="Saídas" value={data?.saidasCentavos} loading={loading} icon={<ArrowDownRight aria-hidden="true" />} />
    <SummaryCard kind="result" title="Resultado" value={data?.resultadoCentavos} loading={loading} icon={<Equal aria-hidden="true" />} />
  </section>
}

function SummaryCard({ kind, title, value, loading, icon }: { kind: 'income' | 'expense' | 'result'; title: string; value?: number; loading: boolean; icon: ReactNode }) {
  const content = loading || value === undefined ? '—' : formatMoney(value)
  const resultTone = kind === 'result' && value !== undefined && value < 0 ? ' negative' : ''
  return <article className={`panel stat ${kind}${resultTone}`}><div className="stat-icon">{icon}</div><div><h2>{kind === 'result' ? <><span className="desktop-only">{title}</span><span className="mobile-only">Resultado previsto</span></> : title}</h2><div className="stat-value" aria-label={loading ? 'Carregando' : content}>{content}</div><p>{kind === 'result' ? 'Projeção do período' : 'Previstas no período'}</p></div></article>
}
