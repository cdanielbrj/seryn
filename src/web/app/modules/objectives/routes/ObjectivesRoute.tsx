import { useEffect, useMemo, useState } from 'react'
import { ExternalLink, Pencil, Target, Trash2 } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Button } from '@/components/primitives/Button'
import { ConfirmDialog } from '@/components/patterns/ConfirmDialog'
import { formatMoney } from '@/formatters/currency'
import { ApiError, deleteObjective, getObjectives } from '../api/objectivesApi'
import { interestLabels, priorityLabels, type Objective, type ObjectiveInterest } from '../model/objective'

const interestRank: Record<ObjectiveInterest, number> = { listado: 0, considerado: 1, desejado: 2 }
function formatMonth(value: string) { const [year, month] = value.split('-').map(Number); return new Intl.DateTimeFormat('pt-BR', { month: 'long', year: 'numeric' }).format(new Date(year, month - 1, 1)) }
function formatDate(value: string) { const [year, month, day] = value.split('-').map(Number); return new Intl.DateTimeFormat('pt-BR').format(new Date(year, month - 1, day)) }

export function ObjectivesRoute() {
  const [objectives, setObjectives] = useState<Objective[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [query, setQuery] = useState('')
  const [interest, setInterest] = useState<ObjectiveInterest | 'todos'>('todos')
  const [sort, setSort] = useState('padrao')
  const [deleting, setDeleting] = useState<Objective | null>(null)
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    let active = true
    void getObjectives().then(value => { if (active) setObjectives(value) })
      .catch(() => { if (active) setError('Não foi possível carregar os objetivos.') })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [])

  const filtered = useMemo(() => objectives.filter(item =>
    (interest === 'todos' || item.interesse === interest) && item.nome.toLocaleLowerCase('pt-BR').includes(query.trim().toLocaleLowerCase('pt-BR')))
    .sort((a, b) => {
      if (sort === 'nome') return a.nome.localeCompare(b.nome, 'pt-BR')
      if (sort === 'valor') return b.valorEstimadoCentavos - a.valorEstimadoCentavos
      if (sort === 'data') return b.dataInclusao.localeCompare(a.dataInclusao)
      return interestRank[b.interesse] - interestRank[a.interesse] || b.prioridade - a.prioridade || b.dataInclusao.localeCompare(a.dataInclusao)
    }), [objectives, interest, query, sort])

  async function confirmDelete() {
    if (!deleting) return
    setBusy(true)
    try { await deleteObjective(deleting.id); setObjectives(items => items.filter(x => x.id !== deleting.id)); setDeleting(null) }
    catch (cause) { setError(cause instanceof ApiError ? cause.message : 'Não foi possível excluir o objetivo.'); setDeleting(null) }
    finally { setBusy(false) }
  }

  return <>
    <header className="page-heading objectives-heading"><div><h1 tabIndex={-1}>Objetivos</h1><p>Organize possibilidades sem alterar seu planejamento financeiro.</p></div><Button asChild><Link to="/objetivos/novo">Novo objetivo</Link></Button></header>
    <section className="panel objective-filters"><label>Buscar<input value={query} onChange={event => setQuery(event.target.value)} placeholder="Nome do objetivo" /></label><label>Interesse<select value={interest} onChange={event => setInterest(event.target.value as ObjectiveInterest | 'todos')}><option value="todos">Todos</option>{Object.entries(interestLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label><label>Ordenar<select value={sort} onChange={event => setSort(event.target.value)}><option value="padrao">Interesse e prioridade</option><option value="nome">Nome</option><option value="valor">Maior valor</option><option value="data">Inclusão mais recente</option></select></label></section>
    {error && <p className="form-error objectives-error" role="alert">{error}</p>}
    {loading ? <section className="panel objectives-state">Carregando objetivos…</section> : filtered.length === 0 ? <section className="panel objectives-state"><Target aria-hidden="true" /><h2>{objectives.length === 0 ? 'Nenhum objetivo cadastrado' : 'Nenhum objetivo encontrado'}</h2><p>{objectives.length === 0 ? 'Liste algo que você queira acompanhar ou considerar.' : 'Revise a busca ou os filtros selecionados.'}</p></section> : <section className="objectives-grid">{filtered.map(item => <article className="panel objective-card" key={item.id}><div className="objective-card-heading"><div><span className={`objective-interest ${item.interesse}`}>{interestLabels[item.interesse]}</span><span className={`objective-priority ${item.prioridade > 0 ? 'positive' : item.prioridade < 0 ? 'negative' : 'neutral'}`}>{item.prioridade > 0 ? '+' : ''}{item.prioridade} · {priorityLabels[item.prioridade]}</span></div><strong>{formatMoney(item.valorEstimadoCentavos)}</strong></div><h2>{item.nome}</h2><p className="objective-dates">Incluído em {formatDate(item.dataInclusao)}{item.dataDesejada ? ` · Desejado para ${formatMonth(item.dataDesejada)}` : ''}</p>{item.observacoes && <p className="objective-notes">{item.observacoes}</p>}<div className="objective-actions">{item.link && <a href={item.link} target="_blank" rel="noreferrer"><ExternalLink />Abrir link</a>}<Button asChild variant="ghost" className="objective-icon-button"><Link to={`/objetivos/${item.id}/editar`} aria-label={`Editar ${item.nome}`}><Pencil /></Link></Button><Button variant="ghost" className="objective-icon-button" aria-label={`Excluir ${item.nome}`} onClick={() => setDeleting(item)}><Trash2 /></Button></div></article>)}</section>}
    {deleting && <ConfirmDialog title="Excluir objetivo?" description={`Excluir “${deleting.nome}” removerá somente este objetivo. Registros e projeções não serão alterados.`} confirmLabel="Excluir objetivo" busy={busy} onCancel={() => setDeleting(null)} onConfirm={() => { void confirmDelete() }} />}
  </>
}
