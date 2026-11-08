import { Layers, Save } from 'lucide-react'
import { Button } from '@/components/primitives/Button'
import type { Classification } from '../model/record'
import type { RecordFormState } from '../model/recordForm'

type Props = { busy: boolean; calculatedInstallmentCents: number | null; definedRecurring: boolean; form: RecordFormState; payments: Classification[]; preview: string; onCancel: () => void }

export function RecordPreview({ busy, calculatedInstallmentCents, definedRecurring, form, payments, preview, onCancel }: Props) {
  return <aside className="panel record-preview"><div className="square-icon"><Layers /></div><h2>Resumo</h2><p>{preview}</p><dl><div><dt>Movimentação</dt><dd>{form.movimentacao === 'entrada' ? 'Entrada' : 'Saída'}</dd></div><div><dt>{definedRecurring && form.modoValor === 'total' ? 'Valor total informado' : definedRecurring ? 'Valor por parcela' : 'Valor por ocorrência'}</dt><dd>{form.valor ? `R$ ${form.valor}` : '—'}</dd></div>{calculatedInstallmentCents !== null && <div><dt>Valor por parcela</dt><dd>{new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(calculatedInstallmentCents / 100)}</dd></div>}<div><dt>Pagamento</dt><dd>{payments.find(item => item.id === form.pagamentoId)?.nome ?? '—'}</dd></div></dl><div className="record-form-actions"><Button disabled={busy}><Save size={18} />{busy ? 'Salvando…' : 'Salvar registro'}</Button><Button type="button" variant="outline" onClick={onCancel} disabled={busy}>Cancelar</Button></div></aside>
}
