import { ChevronDown } from 'lucide-react'
import { Link } from 'react-router-dom'
import { formatMoney } from '@/formatters/currency'
import type { AnalysisProjection } from '../model/analysis'

export function AnalysisSegments({ data, loading }: { data: AnalysisProjection | null; loading: boolean }) {
  if (loading) return <section className="panel analysis-state">Calculando a composição do período…</section>
  if (!data || data.segmentos.length === 0) return <section className="panel analysis-state"><h2>Nenhum valor planejado</h2><p>Não há ocorrências para os filtros e o período selecionados.</p></section>
  return <section className="analysis-segments" aria-label="Composição segmentada">{data.segmentos.map(segment => <details className="panel analysis-segment" key={segment.chave}>
    <summary><div className="analysis-segment-name"><ChevronDown aria-hidden="true" /><div><strong>{segment.nome}</strong><span>{segment.registros} {segment.registros === 1 ? 'registro' : 'registros'} · {segment.ocorrencias} {segment.ocorrencias === 1 ? 'ocorrência' : 'ocorrências'}</span></div></div><div className="analysis-segment-total"><strong>{formatMoney(segment.valorCentavos)}</strong><span>{segment.percentual}% do volume filtrado</span></div></summary>
    <div className="analysis-segment-body">
      {segment.cruzamentos.length > 0 && <div className="analysis-cross"><h3>Cruzamento</h3>{segment.cruzamentos.map(item => <div key={item.chave}><span>{item.nome}<small>{item.registros} {item.registros === 1 ? 'registro' : 'registros'}</small></span><strong>{formatMoney(item.valorCentavos)}</strong></div>)}</div>}
      <div className="analysis-records"><h3>Registros que compõem o total</h3>{segment.detalhes.map(record => <div className="analysis-record" key={record.id}><div className="analysis-record-info"><div className="analysis-record-heading"><Link className="analysis-record-title" to={`/registros/${record.id}/editar`}>{record.nome}</Link>{record.parcelasTotais !== null && record.parcelasPrevistasAteFim !== null && <span className="analysis-installment-badge" aria-label={`${record.parcelasPrevistasAteFim} de ${record.parcelasTotais} parcelas previstas até o fim do período`} title={`${record.parcelasPrevistasAteFim} de ${record.parcelasTotais} parcelas previstas até o fim do período`}>{record.parcelasPrevistasAteFim} de {record.parcelasTotais} parcelas</span>}</div><span className="analysis-record-meta">{record.tipo} · {record.categoria} · {record.pagamento}</span></div><span>{record.ocorrencias}×</span><strong className={record.movimentacao}>{formatMoney(record.valorCentavos)}</strong></div>)}</div>
    </div>
  </details>)}</section>
}
