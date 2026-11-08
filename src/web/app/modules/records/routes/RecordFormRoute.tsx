import { type FormEvent, useEffect, useState } from 'react'
import { ArrowLeft, Tag } from 'lucide-react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { ApiError, createRecord, getPaymentMethods, getRecord, getRecordCategories, getRecordTypes, updateRecord, type SaveRecordInput } from '../api/recordsApi'
import { Button } from '@/components/primitives/Button'
import { formatMonth, parseMoney } from '../lib/formatRecord'
import { recurrenceLabels, type Category, type Classification, type Occurrence, type Recurrence, type RecordItem } from '../model/record'
import { RecordDetailsSection } from '../components/RecordDetailsSection'
import { RecordPreview } from '../components/RecordPreview'
import { RecurrenceSection } from '../components/RecurrenceSection'
import { emptyRecordForm, type RecordFormState } from '../model/recordForm'

export function RecordFormRoute() {
  const { id } = useParams()
  const editing = Boolean(id)
  const navigate = useNavigate()
  const [form, setForm] = useState<RecordFormState>(emptyRecordForm)
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
      getRecordTypes(), getPaymentMethods(), getRecordCategories(), id ? getRecord(id) : Promise.resolve(null),
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

  function update<K extends keyof RecordFormState>(key: K, value: RecordFormState[K]) {
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
    const payload: SaveRecordInput = {
      nome: form.nome, movimentacao: form.movimentacao, tipoId: form.tipoId, categoriaId: form.categoriaId || null,
      pagamentoId: form.pagamentoId, valorCentavos: value, data: form.data, recorrencia: form.recorrencia,
      ocorrencia: form.recorrencia === 'unica' ? 'definida' : form.ocorrencia,
      parcelas: form.recorrencia === 'unica' ? 1 : form.ocorrencia === 'definida' ? Number(form.parcelas) : null,
      modoValor: form.recorrencia !== 'unica' && form.ocorrencia === 'definida' ? form.modoValor : 'por_ocorrencia',
    }
    try {
      if (editing && id) await updateRecord(id, payload)
      else await createRecord(payload)
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
        <RecordDetailsSection categories={categories} errors={fieldErrors} form={form} payments={payments} types={types} update={update} valueLabel={valueLabel} />
        <RecurrenceSection errors={fieldErrors} form={form} update={update} onRecurrenceChange={changeRecurrence} onOccurrenceChange={changeOccurrence} />
      </div>
      <RecordPreview busy={busy} calculatedInstallmentCents={calculatedInstallmentCents} definedRecurring={definedRecurring} form={form} payments={payments} preview={preview} onCancel={() => navigate('/registros')} />
    </form>
  </>
}
