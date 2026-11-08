import { CreditCard } from 'lucide-react'
import { recurrenceLabels, type Occurrence, type Recurrence } from '../model/record'
import type { RecordFieldErrors, RecordFormState, UpdateRecordForm } from '../model/recordForm'

type Props = { errors: RecordFieldErrors; form: RecordFormState; update: UpdateRecordForm; onRecurrenceChange: (value: Recurrence) => void; onOccurrenceChange: (value: Occurrence) => void }

export function RecurrenceSection({ errors, form, update, onRecurrenceChange, onOccurrenceChange }: Props) {
  const defined = form.recorrencia !== 'unica' && form.ocorrencia === 'definida'
  return <section className="panel record-form-section"><div className="panel-heading"><div className="square-icon"><CreditCard /></div><div><h2>Recorrência</h2><p>Defina quando e por quanto tempo acontece.</p></div></div>
    <div className="form-grid"><label>Data<input type="month" value={form.data} onChange={event => update('data', event.target.value)} required />{errors.data && <small className="field-error">{errors.data[0]}</small>}</label>
      <label>Recorrência<select value={form.recorrencia} onChange={event => onRecurrenceChange(event.target.value as Recurrence)}>{Object.entries(recurrenceLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select>{errors.recorrencia && <small className="field-error">{errors.recorrencia[0]}</small>}</label>
      {form.recorrencia !== 'unica' && <label>Ocorrência<select value={form.ocorrencia} onChange={event => onOccurrenceChange(event.target.value as Occurrence)}><option value="indefinida">Indefinida</option><option value="definida">Definida</option></select>{errors.ocorrencia && <small className="field-error">{errors.ocorrencia[0]}</small>}</label>}
      {defined && <div className="defined-record-fields"><label className="installments-field">Parcelas<input type="number" min="2" step="1" value={form.parcelas} onChange={event => update('parcelas', event.target.value)} required />{errors.parcelas && <small className="field-error">{errors.parcelas[0]}</small>}</label><fieldset className="value-mode-field"><legend>Valor informado</legend><div><button type="button" aria-pressed={form.modoValor === 'por_ocorrencia'} onClick={() => update('modoValor', 'por_ocorrencia')}>Por parcela</button><button type="button" aria-pressed={form.modoValor === 'total'} onClick={() => update('modoValor', 'total')}>Total</button></div>{errors.modoValor && <small className="field-error">{errors.modoValor[0]}</small>}</fieldset></div>}
    </div>
  </section>
}
