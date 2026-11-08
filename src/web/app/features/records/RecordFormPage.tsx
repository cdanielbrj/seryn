import { type FormEvent, useEffect, useState } from 'react'
import { ArrowDownLeft, ArrowLeft, ArrowUpRight, CalendarDays, CreditCard, Layers, Save, Tag } from 'lucide-react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ApiError, get, patch, post } from '@/global/api/client'
import { Button } from '@/global/ui/Button'
import { formatMonth, maskMoney, parseMoney, recurrenceLabels, type Category, type Classification, type Direction, type Occurrence, type Recurrence, type RecordItem } from './records'

type FormState = {
  nome: string; movimentacao: Direction; tipoId: string; categoriaId: string; pagamentoId: string
  valor: string; data: string; recorrencia: Recurrence; ocorrencia: Occurrence; parcelas: string
  modoValor: 'por_ocorrencia' | 'total'
}

const emptyForm: FormState = {
  nome: '', movimentacao: 'saida', tipoId: '', categoriaId: '', pagamentoId: '', valor: '', data: '',
  recorrencia: 'unica', ocorrencia: 'indefinida', parcelas: '2', modoValor: 'por_ocorrencia',
}

export function RecordFormPage() {
  const { id } = useParams()
  const editing = Boolean(id)
  const navigate = useNavigate()
  const [form, setForm] = useState<FormState>(emptyForm)
  const [types, setTypes] = useState<Classification[]>([])
  const [payments, setPayments] = useState<Classification[]>([])
  const [categories, setCategories] = useState<Category[]>([])
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({})

  useEffect(() => {
    let active = true
    const requests: [Promise<Classification[]>, Promise<Classification[]>, Promise<Category[]>, Promise<RecordItem | null>] = [
      get('/types'), get('/payment-methods'), get('/categories'), id ? get(`/records/${id}`) : Promise.resolve(null),
    ]
    Promise.all(requests).then(([typeItems, paymentItems, categoryItems, record]) => {
      if (!active) return
      setTypes(typeItems); setPayments(paymentItems); setCategories(categoryItems)
      if (record) setForm({
        nome: record.nome, movimentacao: record.movimentacao, tipoId: record.tipoId,
        categoriaId: record.categoriaId ?? '', pagamentoId: record.pagamentoId,
        valor: (record.valorCentavos / 100).toFixed(2).replace('.', ','), data: record.data,
        recorrencia: record.recorrencia, ocorrencia: record.ocorrencia,
        parcelas: record.parcelas && record.parcelas > 1 ? String(record.parcelas) : '2', modoValor: 'por_ocorrencia',
      })
    }).catch(cause => {
      if (!active) return
      setError(cause instanceof ApiError && cause.status === 404 ? 'Registro não encontrado.' : 'Não foi possível preparar o formulário.')
    }).finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [id])

  function update<K extends keyof FormState>(key: K, value: FormState[K]) {
    setForm(current => ({ ...current, [key]: value }))
    setFieldErrors(current => { const next = { ...current }; delete next[key]; return next })
  }

  function changeRecurrence(value: Recurrence) {
    setForm(current => ({
      ...current,
      recorrencia: value,
      ...(current.recorrencia === 'unica' && value !== 'unica' ? { ocorrencia: 'indefinida' as const } : {}),
      ...(value === 'unica' ? { modoValor: 'por_ocorrencia' as const } : {}),
    }))
    setFieldErrors(current => { const next = { ...current }; delete next.recorrencia; delete next.ocorrencia; return next })
  }

  function changeOccurrence(value: Occurrence) {
    setForm(current => ({ ...current, ocorrencia: value, ...(value === 'indefinida' ? { modoValor: 'por_ocorrencia' as const } : {}) }))
    setFieldErrors(current => { const next = { ...current }; delete next.ocorrencia; delete next.parcelas; delete next.modoValor; return next })
  }

  async function submit(event: FormEvent) {
    event.preventDefault()
    const value = parseMoney(form.valor)
    if (value === null) {
      setFieldErrors({ valorCentavos: ['Informe um valor válido maior que zero, com até duas casas decimais.'] })
      return
    }
    setBusy(true); setError(''); setFieldErrors({})
    const payload = {
      nome: form.nome, movimentacao: form.movimentacao, tipoId: form.tipoId, categoriaId: form.categoriaId || null,
      pagamentoId: form.pagamentoId, valorCentavos: value, data: form.data, recorrencia: form.recorrencia,
      ocorrencia: form.recorrencia === 'unica' ? 'definida' : form.ocorrencia,
      parcelas: form.recorrencia === 'unica' ? 1 : form.ocorrencia === 'definida' ? Number(form.parcelas) : null,
      modoValor: form.recorrencia !== 'unica' && form.ocorrencia === 'definida' ? form.modoValor : 'por_ocorrencia',
    }
    try {
      if (editing) await patch(`/records/${id}`, payload)
      else await post('/records', payload)
      navigate('/registros')
    } catch (cause) {
      if (cause instanceof ApiError) { setError(cause.message); setFieldErrors(cause.errors) }
      else setError('Não foi possível salvar o registro.')
    } finally { setBusy(false) }
  }

  if (loading) return <p className="records-state" role="status">Carregando formulário…</p>
  if (error === 'Registro não encontrado.') return <><header className="page-heading"><h1 tabIndex={-1}>Registro não encontrado</h1><p>Este registro não existe ou já foi excluído.</p></header><Button asChild variant="outline"><Link to="/registros">Voltar aos registros</Link></Button></>

  const missingTypes = types.length === 0
  const missingPayments = payments.length === 0
  if (missingTypes || missingPayments) return <><header className="page-heading"><h1 tabIndex={-1}>{editing ? 'Editar registro' : 'Novo registro'}</h1><p>Prepare as classificações obrigatórias antes de continuar.</p></header><section className="panel record-prerequisites"><Tag aria-hidden="true" /><h2>Falta organizar seu cadastro</h2><p>Todo registro precisa de um Tipo e um Pagamento.</p><div>{missingTypes && <Button asChild><Link to="/tipos">Cadastrar tipo</Link></Button>}{missingPayments && <Button asChild variant={missingTypes ? 'outline' : 'default'}><Link to="/pagamentos">Cadastrar pagamento</Link></Button>}</div></section></>

  const roots = categories.filter(category => category.categoriaPaiId === null)
  const definedRecurring = form.recorrencia !== 'unica' && form.ocorrencia === 'definida'
  const informedCents = parseMoney(form.valor)
  const installmentCount = Number(form.parcelas)
  const calculatedInstallmentCents = form.modoValor === 'total' && informedCents !== null && installmentCount >= 2
    ? Math.max(1, Math.round(informedCents / installmentCount))
    : null
  const valueLabel = definedRecurring ? form.modoValor === 'total' ? 'Valor total' : 'Valor por parcela' : 'Valor'
  const preview = form.recorrencia === 'unica'
    ? `Uma ocorrência em ${form.data ? formatMonth(form.data) : 'um mês a definir'}.`
    : form.ocorrencia === 'indefinida'
      ? `Recorrência ${recurrenceLabels[form.recorrencia].toLocaleLowerCase('pt-BR')} sem ocorrência final.`
      : `${form.parcelas || '—'} ocorrências com frequência ${recurrenceLabels[form.recorrencia].toLocaleLowerCase('pt-BR')}.`

  return <>
    <header className="page-heading record-form-heading"><Link className="back-link" to="/registros"><ArrowLeft size={18} />Registros</Link><h1 tabIndex={-1}>{editing ? 'Editar registro' : 'Novo registro'}</h1><p>Planeje uma entrada ou saída com suas próprias regras de recorrência.</p></header>
    <form className="record-form-layout" onSubmit={event => { void submit(event) }}>
      <div className="record-form-main">
        {error && <p className="form-error" role="alert">{error}</p>}
        <section className="panel record-form-section"><div className="panel-heading"><div className="square-icon"><CalendarDays /></div><div><h2>Dados do registro</h2><p>Identifique e classifique esta previsão.</p></div></div>
          <fieldset className="direction-field"><legend>Movimentação</legend><div className="direction-options"><button type="button" className={form.movimentacao === 'entrada' ? 'selected entrada' : ''} aria-pressed={form.movimentacao === 'entrada'} onClick={() => update('movimentacao', 'entrada')}><ArrowUpRight />Entrada</button><button type="button" className={form.movimentacao === 'saida' ? 'selected saida' : ''} aria-pressed={form.movimentacao === 'saida'} onClick={() => update('movimentacao', 'saida')}><ArrowDownLeft />Saída</button></div>{fieldErrors.movimentacao && <small className="field-error">{fieldErrors.movimentacao[0]}</small>}</fieldset>
          <div className="form-grid"><label className="full">Nome<input value={form.nome} onChange={event => update('nome', event.target.value)} maxLength={120} required autoComplete="off" />{fieldErrors.nome && <small className="field-error">{fieldErrors.nome[0]}</small>}</label>
            <label>Tipo<select value={form.tipoId} onChange={event => update('tipoId', event.target.value)} required><option value="">Selecione</option>{types.map(item => <option key={item.id} value={item.id}>{item.nome}</option>)}</select>{fieldErrors.tipoId && <small className="field-error">{fieldErrors.tipoId[0]}</small>}</label>
            <label><span className="field-label">Categoria <span className="optional">Opcional</span></span><select value={form.categoriaId} onChange={event => update('categoriaId', event.target.value)}><option value="">Sem categoria</option>{roots.map(root => <optgroup key={root.id} label={root.nome}><option value={root.id}>{root.nome}</option>{categories.filter(item => item.categoriaPaiId === root.id).map(child => <option key={child.id} value={child.id}>— {child.nome}</option>)}</optgroup>)}</select>{fieldErrors.categoriaId && <small className="field-error">{fieldErrors.categoriaId[0]}</small>}</label>
            <label>Pagamento<select value={form.pagamentoId} onChange={event => update('pagamentoId', event.target.value)} required><option value="">Selecione</option>{payments.map(item => <option key={item.id} value={item.id}>{item.nome}</option>)}</select>{fieldErrors.pagamentoId && <small className="field-error">{fieldErrors.pagamentoId[0]}</small>}</label>
            <label>{valueLabel}<input value={form.valor} onChange={event => update('valor', maskMoney(event.target.value))} inputMode="numeric" placeholder="0,00" required aria-describedby="value-hint" /><small id="value-hint">Digite somente os números; o formato será aplicado automaticamente.</small>{fieldErrors.valorCentavos && <small className="field-error">{fieldErrors.valorCentavos[0]}</small>}</label>
          </div>
        </section>
        <section className="panel record-form-section"><div className="panel-heading"><div className="square-icon"><CreditCard /></div><div><h2>Recorrência</h2><p>Defina quando e por quanto tempo acontece.</p></div></div>
          <div className="form-grid"><label>Data<input type="month" value={form.data} onChange={event => update('data', event.target.value)} required />{fieldErrors.data && <small className="field-error">{fieldErrors.data[0]}</small>}</label>
            <label>Recorrência<select value={form.recorrencia} onChange={event => changeRecurrence(event.target.value as Recurrence)}>{Object.entries(recurrenceLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select>{fieldErrors.recorrencia && <small className="field-error">{fieldErrors.recorrencia[0]}</small>}</label>
            {form.recorrencia !== 'unica' && <label>Ocorrência<select value={form.ocorrencia} onChange={event => changeOccurrence(event.target.value as Occurrence)}><option value="indefinida">Indefinida</option><option value="definida">Definida</option></select>{fieldErrors.ocorrencia && <small className="field-error">{fieldErrors.ocorrencia[0]}</small>}</label>}
            {definedRecurring && <div className="defined-record-fields"><label className="installments-field">Parcelas<input type="number" min="2" step="1" value={form.parcelas} onChange={event => update('parcelas', event.target.value)} required />{fieldErrors.parcelas && <small className="field-error">{fieldErrors.parcelas[0]}</small>}</label><fieldset className="value-mode-field"><legend>Valor informado</legend><div><button type="button" aria-pressed={form.modoValor === 'por_ocorrencia'} onClick={() => update('modoValor', 'por_ocorrencia')}>Por parcela</button><button type="button" aria-pressed={form.modoValor === 'total'} onClick={() => update('modoValor', 'total')}>Total</button></div>{fieldErrors.modoValor && <small className="field-error">{fieldErrors.modoValor[0]}</small>}</fieldset></div>}
          </div>
        </section>
      </div>
      <aside className="panel record-preview"><div className="square-icon"><Layers /></div><h2>Resumo</h2><p>{preview}</p><dl><div><dt>Movimentação</dt><dd>{form.movimentacao === 'entrada' ? 'Entrada' : 'Saída'}</dd></div><div><dt>{definedRecurring && form.modoValor === 'total' ? 'Valor total informado' : definedRecurring ? 'Valor por parcela' : 'Valor por ocorrência'}</dt><dd>{form.valor ? `R$ ${form.valor}` : '—'}</dd></div>{calculatedInstallmentCents !== null && <div><dt>Valor por parcela</dt><dd>{new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(calculatedInstallmentCents / 100)}</dd></div>}<div><dt>Pagamento</dt><dd>{payments.find(item => item.id === form.pagamentoId)?.nome ?? '—'}</dd></div></dl><div className="record-form-actions"><Button disabled={busy}><Save size={18} />{busy ? 'Salvando…' : 'Salvar registro'}</Button><Button type="button" variant="outline" onClick={() => navigate('/registros')} disabled={busy}>Cancelar</Button></div></aside>
    </form>
  </>
}
