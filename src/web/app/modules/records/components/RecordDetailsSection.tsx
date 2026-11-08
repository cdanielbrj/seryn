import { ArrowDownLeft, ArrowUpRight, CalendarDays } from 'lucide-react'
import { maskMoney } from '../lib/formatRecord'
import type { Category, Classification } from '../model/record'
import type { RecordFieldErrors, RecordFormState, UpdateRecordForm } from '../model/recordForm'

type Props = {
  categories: Category[]
  errors: RecordFieldErrors
  form: RecordFormState
  payments: Classification[]
  types: Classification[]
  update: UpdateRecordForm
  valueLabel: string
}

export function RecordDetailsSection({ categories, errors, form, payments, types, update, valueLabel }: Props) {
  const roots = categories.filter(category => category.categoriaPaiId === null)
  return <section className="panel record-form-section"><div className="panel-heading"><div className="square-icon"><CalendarDays /></div><div><h2>Dados do registro</h2><p>Identifique e classifique esta previsão.</p></div></div>
    <fieldset className="direction-field"><legend>Movimentação</legend><div className="direction-options"><button type="button" className={form.movimentacao === 'entrada' ? 'selected entrada' : ''} aria-pressed={form.movimentacao === 'entrada'} onClick={() => update('movimentacao', 'entrada')}><ArrowUpRight />Entrada</button><button type="button" className={form.movimentacao === 'saida' ? 'selected saida' : ''} aria-pressed={form.movimentacao === 'saida'} onClick={() => update('movimentacao', 'saida')}><ArrowDownLeft />Saída</button></div>{errors.movimentacao && <small className="field-error">{errors.movimentacao[0]}</small>}</fieldset>
    <div className="form-grid"><label className="full">Nome<input value={form.nome} onChange={event => update('nome', event.target.value)} maxLength={120} required autoComplete="off" />{errors.nome && <small className="field-error">{errors.nome[0]}</small>}</label>
      <label>Tipo<select value={form.tipoId} onChange={event => update('tipoId', event.target.value)} required><option value="">Selecione</option>{types.map(item => <option key={item.id} value={item.id}>{item.nome}</option>)}</select>{errors.tipoId && <small className="field-error">{errors.tipoId[0]}</small>}</label>
      <label><span className="field-label">Categoria <span className="optional">Opcional</span></span><select value={form.categoriaId} onChange={event => update('categoriaId', event.target.value)}><option value="">Sem categoria</option>{roots.map(root => <optgroup key={root.id} label={root.nome}><option value={root.id}>{root.nome}</option>{categories.filter(item => item.categoriaPaiId === root.id).map(child => <option key={child.id} value={child.id}>— {child.nome}</option>)}</optgroup>)}</select>{errors.categoriaId && <small className="field-error">{errors.categoriaId[0]}</small>}</label>
      <label>Pagamento<select value={form.pagamentoId} onChange={event => update('pagamentoId', event.target.value)} required><option value="">Selecione</option>{payments.map(item => <option key={item.id} value={item.id}>{item.nome}</option>)}</select>{errors.pagamentoId && <small className="field-error">{errors.pagamentoId[0]}</small>}</label>
      <label>{valueLabel}<input value={form.valor} onChange={event => update('valor', maskMoney(event.target.value))} inputMode="numeric" placeholder="0,00" required aria-describedby="value-hint" /><small id="value-hint">Digite somente os números; o formato será aplicado automaticamente.</small>{errors.valorCentavos && <small className="field-error">{errors.valorCentavos[0]}</small>}</label>
    </div>
  </section>
}
