import { CalendarDays, ChevronRight, Layers } from 'lucide-react'
import { Link } from 'react-router-dom'
import { formatMoney } from '@/formatters/currency'
import type { DashboardProjection, TopRecordProjection } from '../model/projection'

export function DashboardPanels({ data, loading, empty }: { data: DashboardProjection | null; loading: boolean; empty: boolean }) {
  return <div className="bottom-grid">
    <CategoryPanel data={data} loading={loading} />
    <TopRecordsPanel data={data} loading={loading} empty={empty} />
  </div>
}

function CategoryPanel({ data, loading }: { data: DashboardProjection | null; loading: boolean }) {
  return <section className="panel category-panel desktop-only"><div className="panel-heading"><div className="square-icon"><Layers aria-hidden="true" /></div><div><h2>Por categoria</h2><p>Distribuição das saídas previstas.</p></div></div>{loading ? <div className="category-empty"><p role="status">Carregando categorias…</p></div> : data?.categorias.length ? <CategoryChart categories={data.categorias} total={data.saidasCentavos} /> : <div className="category-empty"><div className="empty-donut" role="img" aria-label="Sem distribuição por categoria"><div><b>—</b><span>Total de saídas</span></div></div><div><h3>Sem categorias no período</h3><p>As saídas previstas serão agrupadas aqui.</p></div></div>}</section>
}

function CategoryChart({ categories, total }: { categories: DashboardProjection['categorias']; total: number }) {
  const colors = ['#117E78', '#456A9B', '#B7783A', '#8C5DA7', '#5B6B70']
  const stops = categories.reduce<{ offset: number; values: string[] }>((state, category, index) => ({ offset: state.offset + category.percentual, values: [...state.values, `${colors[index % colors.length]} ${state.offset}% ${state.offset + category.percentual}%`] }), { offset: 0, values: [] }).values.join(', ')
  return <div className="category-content"><div className="category-donut" style={{ background: `conic-gradient(${stops})` }} role="img" aria-label={`Distribuição de ${formatMoney(total)} em saídas`}><div><b>{formatMoney(total)}</b><span>Total de saídas</span></div></div><div className="category-list">{categories.map((category, index) => <div className="dashboard-category-row" key={category.nome}><div className="dashboard-category-left"><i style={{ background: colors[index % colors.length] }} />{category.nome}</div><span className="value">{formatMoney(category.valorCentavos)}</span><span className="percent">{category.percentual}%</span></div>)}</div></div>
}

function TopRecordsPanel({ data, loading, empty }: { data: DashboardProjection | null; loading: boolean; empty: boolean }) {
  return <section className="panel records-panel"><div className="panel-heading"><div className="square-icon desktop-only"><CalendarDays aria-hidden="true" /></div><div><h2>Maiores movimentações</h2><p className="desktop-only">Registros com maior impacto no período.</p></div><Link className="text-link mobile-only" to="/registros">Ver todos</Link></div>{loading ? <div className="empty-records"><p role="status">Carregando movimentações…</p></div> : empty ? <div className="empty-records"><CalendarDays aria-hidden="true" /><h3>Nenhum registro por aqui</h3><p>Não há registros previstos para este período.</p><Link className="text-link desktop-only" to="/registros">Ir para registros<ChevronRight size={16} aria-hidden="true" /></Link></div> : data && <TopRecords incomes={data.maioresEntradas} expenses={data.maioresSaidas} />}</section>
}

function TopRecords({ incomes, expenses }: { incomes: TopRecordProjection[]; expenses: TopRecordProjection[] }) {
  return <div className="top-records"><TopColumn title="Maiores entradas" direction="entrada" records={incomes} /><TopColumn title="Maiores saídas" direction="saida" records={expenses} /></div>
}

function TopColumn({ title, direction, records }: { title: string; direction: 'entrada' | 'saida'; records: TopRecordProjection[] }) {
  return <div className="top-column"><h3>{title}</h3>{records.length ? records.map(record => <div className="top-record" key={record.id}><div><strong>{record.nome}</strong><span>{record.categoriaNome ?? 'Sem categoria'} · {record.ocorrencias === 1 ? '1 ocorrência' : `${record.ocorrencias} ocorrências`}</span></div><b className={direction}>{direction === 'entrada' ? '+' : '−'} {formatMoney(record.valorCentavos)}</b></div>) : <p>Nenhum registro no período.</p>}</div>
}
