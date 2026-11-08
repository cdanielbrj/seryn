import { ArrowDownRight, ArrowUpRight, ListChecks, Scale } from 'lucide-react'
import { formatMoney } from '@/formatters/currency'
import type { AnalysisProjection } from '../model/analysis'

export function AnalysisSummary({ data, loading }: { data: AnalysisProjection | null; loading: boolean }) {
  const cards = [
    { label: 'Entradas planejadas', value: data?.entradasCentavos, Icon: ArrowUpRight, kind: 'income' },
    { label: 'Saídas planejadas', value: data?.saidasCentavos, Icon: ArrowDownRight, kind: 'expense' },
    { label: 'Resultado projetado', value: data?.resultadoCentavos, Icon: Scale, kind: 'result' },
    { label: 'Registros analisados', value: data?.registros, Icon: ListChecks, kind: 'count' },
  ]
  return <section className="analysis-summary" aria-label="Resumo da análise">{cards.map(({ label, value, Icon, kind }) => <article className={`panel analysis-stat ${kind}`} key={label}><Icon aria-hidden="true" /><div><span>{label}</span><strong>{loading || value === undefined ? '—' : kind === 'count' ? value : formatMoney(value)}</strong>{kind === 'count' && data && <small>{data.ocorrencias} ocorrências no período</small>}</div></article>)}</section>
}
