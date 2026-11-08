import { type FormEvent, useEffect, useState } from 'react'
import { ArrowLeft, Save } from 'lucide-react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { Button } from '@/components/primitives/Button'
import { ApiError, createObjective, getObjective, updateObjective } from '../api/objectivesApi'
import { maskObjectiveMoney, parseObjectiveMoney } from '../lib/objectiveMoney'
import { interestLabels, priorityLabels, type ObjectiveInput, type ObjectiveInterest } from '../model/objective'

type FormState = { nome: string; valor: string; interesse: ObjectiveInterest; prioridade: number; link: string; observacoes: string; dataDesejada: string }
const emptyForm: FormState = { nome: '', valor: '', interesse: 'listado', prioridade: 0, link: '', observacoes: '', dataDesejada: '' }

export function ObjectiveFormRoute() {
  const { id } = useParams()
  const editing = Boolean(id)
  const navigate = useNavigate()
  const [form, setForm] = useState<FormState>(emptyForm)
  const [included, setIncluded] = useState<string | null>(null)
  const [loading, setLoading] = useState(editing)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})

  useEffect(() => {
    if (!id) return
    let active = true
    void getObjective(id).then(value => { if (active) { setForm({ nome: value.nome, valor: (value.valorEstimadoCentavos / 100).toFixed(2).replace('.', ','), interesse: value.interesse, prioridade: value.prioridade, link: value.link ?? '', observacoes: value.observacoes ?? '', dataDesejada: value.dataDesejada ?? '' }); setIncluded(value.dataInclusao) } })
      .catch(cause => { if (active) setError(cause instanceof ApiError && cause.status === 404 ? 'Objetivo não encontrado.' : 'Não foi possível preparar o formulário.') })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [id])

  function update<K extends keyof FormState>(key: K, value: FormState[K]) { setForm(current => ({ ...current, [key]: value })); setFieldErrors(current => { const next = { ...current }; delete next[key]; return next }) }
  async function submit(event: FormEvent) {
    event.preventDefault()
    const value = parseObjectiveMoney(form.valor)
    if (value === null) { setFieldErrors({ valorEstimadoCentavos: ['Informe um valor válido maior que zero, com até duas casas decimais.'] }); return }
    const payload: ObjectiveInput = { nome: form.nome, valorEstimadoCentavos: value, interesse: form.interesse, prioridade: form.prioridade, link: form.link || null, observacoes: form.observacoes || null, dataDesejada: form.dataDesejada || null }
    setBusy(true); setError(''); setFieldErrors({})
    try { if (editing && id) await updateObjective(id, payload); else await createObjective(payload); navigate('/objetivos') }
    catch (cause) { if (cause instanceof ApiError) { setError(cause.message); setFieldErrors(cause.errors) } else setError('Não foi possível salvar o objetivo.') }
    finally { setBusy(false) }
  }

  if (loading) return <p className="objectives-state" role="status">Carregando formulário…</p>
  if (error === 'Objetivo não encontrado.') return <><header className="page-heading"><h1 tabIndex={-1}>Objetivo não encontrado</h1><p>Este objetivo não existe ou já foi excluído.</p></header><Button asChild variant="outline"><Link to="/objetivos">Voltar aos objetivos</Link></Button></>
  return <>
    <header className="page-heading objective-form-heading"><Link className="back-link" to="/objetivos"><ArrowLeft size={18} />Objetivos</Link><h1 tabIndex={-1}>{editing ? 'Editar objetivo' : 'Novo objetivo'}</h1><p>Registre uma possibilidade sem adicioná-la às projeções financeiras.</p></header>
    <form className="panel objective-form" onSubmit={event => { void submit(event) }}>
      {error && <p className="form-error" role="alert">{error}</p>}
      <div className="objective-form-grid">
        <label className="full">Nome<input value={form.nome} onChange={event => update('nome', event.target.value)} maxLength={120} required />{fieldErrors.nome && <small className="field-error">{fieldErrors.nome[0]}</small>}</label>
        <label>Valor estimado<input inputMode="numeric" value={form.valor} onChange={event => update('valor', maskObjectiveMoney(event.target.value))} placeholder="0,00" required />{fieldErrors.valorEstimadoCentavos && <small className="field-error">{fieldErrors.valorEstimadoCentavos[0]}</small>}</label>
        <label><span className="field-label">Data desejada <span className="optional">opcional</span></span><input type="month" value={form.dataDesejada} onChange={event => update('dataDesejada', event.target.value)} />{fieldErrors.dataDesejada && <small className="field-error">{fieldErrors.dataDesejada[0]}</small>}</label>
      </div>
      <fieldset className="objective-interest-field"><legend>Interesse</legend><div>{(Object.entries(interestLabels) as [ObjectiveInterest, string][]).map(([value, label]) => <button type="button" key={value} aria-pressed={form.interesse === value} onClick={() => update('interesse', value)}>{label}</button>)}</div>{fieldErrors.interesse && <small className="field-error">{fieldErrors.interesse[0]}</small>}</fieldset>
      <fieldset className="objective-priority-field"><legend>Prioridade <span>{form.prioridade > 0 ? '+' : ''}{form.prioridade} · {priorityLabels[form.prioridade]}</span></legend><div>{[-3, -2, -1, 0, 1, 2, 3].map(value => <button type="button" key={value} aria-label={`Prioridade ${value}: ${priorityLabels[value]}`} aria-pressed={form.prioridade === value} onClick={() => update('prioridade', value)}>{value > 0 ? '+' : ''}{value}</button>)}</div><small>Menor prioridade <span>Maior prioridade</span></small>{fieldErrors.prioridade && <small className="field-error">{fieldErrors.prioridade[0]}</small>}</fieldset>
      <div className="objective-form-grid">
        <label className="full"><span className="field-label">Link <span className="optional">opcional</span></span><input type="url" value={form.link} onChange={event => update('link', event.target.value)} maxLength={2048} placeholder="https://" />{fieldErrors.link && <small className="field-error">{fieldErrors.link[0]}</small>}</label>
        <label className="full"><span className="field-label">Observações <span className="optional">opcional</span></span><textarea value={form.observacoes} onChange={event => update('observacoes', event.target.value)} maxLength={2000} rows={5} />{fieldErrors.observacoes && <small className="field-error">{fieldErrors.observacoes[0]}</small>}</label>
      </div>
      {included && <p className="objective-included">Data de inclusão preservada: {included.split('-').reverse().join('/')}</p>}
      <div className="objective-form-actions"><Button disabled={busy}><Save />{busy ? 'Salvando…' : 'Salvar objetivo'}</Button><Button type="button" variant="outline" disabled={busy} onClick={() => navigate('/objetivos')}>Cancelar</Button></div>
    </form>
  </>
}
