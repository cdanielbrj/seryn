import { ArrowDownLeft, ArrowUpRight, CalendarDays, Pencil, Plus, Trash2 } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Button } from '@/components/primitives/Button'
import { formatMoney } from '@/formatters/currency'
import { formatMonth } from '../lib/formatRecord'
import { recurrenceLabels, type RecordItem } from '../model/record'

export function RecordsList({ records, loading, onDelete }: { records: RecordItem[]; loading: boolean; onDelete: (record: RecordItem) => void }) {
  return <section className="panel records-list" aria-label="Registros cadastrados">
    {loading ? <p className="records-state" role="status">Carregando…</p> : records.length === 0 ? <RecordsEmpty /> : <div className="record-rows">{records.map(record => <RecordRow key={record.id} record={record} onDelete={onDelete} />)}</div>}
  </section>
}

function RecordsEmpty() {
  return <div className="records-empty"><CalendarDays aria-hidden="true" /><h2>Nenhum registro cadastrado</h2><p>Cadastre uma entrada ou saída para começar seu planejamento.</p><Button asChild><Link to="/registros/novo"><Plus size={18} />Criar primeiro registro</Link></Button></div>
}

function RecordRow({ record, onDelete }: { record: RecordItem; onDelete: (record: RecordItem) => void }) {
  return <article className="record-row">
    <div className={`record-direction ${record.movimentacao}`} aria-label={record.movimentacao === 'entrada' ? 'Entrada' : 'Saída'}>{record.movimentacao === 'entrada' ? <ArrowUpRight /> : <ArrowDownLeft />}</div>
    <div className="record-main"><strong>{record.nome}</strong><span>{record.tipoNome}{record.categoriaNome ? ` · ${record.categoriaNome}` : ''} · {record.pagamentoNome}</span></div>
    <div className="record-schedule"><span>{recurrenceLabels[record.recorrencia]}</span><small>{formatMonth(record.data)}{record.parcelas && record.parcelas > 1 ? ` · ${record.parcelas} parcelas` : ''}</small></div>
    <strong className={`record-value ${record.movimentacao}`}>{record.movimentacao === 'saida' ? '−' : '+'}{formatMoney(record.valorCentavos)}</strong>
    <div className="classification-actions"><Button asChild variant="ghost" aria-label={`Editar ${record.nome}`}><Link to={`/registros/${record.id}/editar`}><Pencil size={17} /></Link></Button><Button variant="ghost" aria-label={`Excluir ${record.nome}`} onClick={() => onDelete(record)}><Trash2 size={17} /></Button></div>
  </article>
}
