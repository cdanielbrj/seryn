import { useEffect, useState } from 'react'
import { DashboardHeader } from '@/modules/dashboard/components/DashboardHeader'
import { periodLabel, type Period, type Reference } from '@/modules/dashboard/model/period'
import { ApiError, getAnalysis } from '../api/analysesApi'
import { AnalysisSegments } from '../components/AnalysisSegments'
import { AnalysisSummary } from '../components/AnalysisSummary'
import { dimensions, type AnalysisDimension, type AnalysisDirection, type AnalysisProjection } from '../model/analysis'

export function AnalysesRoute() {
  const now = new Date()
  const [period, setPeriod] = useState<Period>('month')
  const [reference, setReference] = useState<Reference>({ year: now.getFullYear(), month: now.getMonth() + 1 })
  const [direction, setDirection] = useState<AnalysisDirection>('expense')
  const [primary, setPrimary] = useState<AnalysisDimension>('category')
  const [secondary, setSecondary] = useState<AnalysisDimension | 'none'>('payment')
  const [data, setData] = useState<AnalysisProjection | null>(null)
  const [error, setError] = useState('')
  const [loadedKey, setLoadedKey] = useState('')
  const [failedKey, setFailedKey] = useState('')
  const availableSecondary = dimensions.filter(item => item.id !== primary)
  const requestKey = `${period}-${reference.year}-${reference.month}-${direction}-${primary}-${secondary}`
  const loading = requestKey !== loadedKey && requestKey !== failedKey

  useEffect(() => {
    let active = true
    void getAnalysis(period, reference, direction, primary, secondary)
      .then(value => { if (active) { setData(value); setError(''); setLoadedKey(requestKey); setFailedKey('') } })
      .catch(cause => { if (active) { setData(null); setError(cause instanceof ApiError ? cause.message : 'Não foi possível carregar a análise.'); setFailedKey(requestKey) } })
    return () => { active = false }
  }, [period, reference, direction, primary, secondary, requestKey])

  function changePrimary(value: AnalysisDimension) {
    setPrimary(value)
    if (secondary === value) setSecondary('none')
  }

  return <>
    <DashboardHeader period={period} reference={reference} label={periodLabel(period, reference)} onPeriodChange={setPeriod} onReferenceChange={setReference} title="Análises" subtitle="Entenda como seus valores planejados se distribuem e se relacionam." mobileSubtitle="Explore a composição do seu planejamento." />
    <section className="panel analysis-filters" aria-label="Filtros da análise">
      <fieldset><legend>Movimentação</legend><div className="segment">{([['all', 'Todas'], ['income', 'Entradas'], ['expense', 'Saídas']] as const).map(([id, label]) => <button key={id} aria-pressed={direction === id} onClick={() => setDirection(id)}>{label}</button>)}</div></fieldset>
      <label>Segmentar por<select value={primary} onChange={event => changePrimary(event.target.value as AnalysisDimension)}>{dimensions.map(item => <option key={item.id} value={item.id}>{item.label}</option>)}</select></label>
      <label>Cruzar com<select value={secondary} onChange={event => setSecondary(event.target.value as AnalysisDimension | 'none')}><option value="none">Sem cruzamento</option>{availableSecondary.map(item => <option key={item.id} value={item.id}>{item.label}</option>)}</select></label>
    </section>
    {error && <p className="form-error analysis-error" role="alert">{error}</p>}
    <AnalysisSummary data={data} loading={loading} />
    <div className="analysis-context"><h2>Composição do período</h2><p>Os percentuais usam o volume planejado pelos filtros atuais. Abra um segmento para ver cruzamentos e registros.</p></div>
    <AnalysisSegments data={data} loading={loading} />
  </>
}
