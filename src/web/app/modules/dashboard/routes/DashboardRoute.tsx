import { useEffect, useState } from 'react'
import { ApiError, getDashboardProjection } from '../api/dashboardApi'
import { DashboardHeader } from '../components/DashboardHeader'
import { DashboardPanels } from '../components/DashboardPanels'
import { ProjectionPanel } from '../components/ProjectionPanel'
import { SummaryCards } from '../components/SummaryCards'
import { periodLabel, type Period, type Reference } from '../model/period'
import type { DashboardProjection } from '../model/projection'

export function DashboardRoute() {
  const [period, setPeriod] = useState<Period>('month')
  const [reference, setReference] = useState<Reference>(() => {
    const now = new Date()
    return { year: now.getFullYear(), month: now.getMonth() + 1 }
  })
  const [data, setData] = useState<DashboardProjection | null>(null)
  const [error, setError] = useState('')
  const [loadedKey, setLoadedKey] = useState('')
  const [failedKey, setFailedKey] = useState('')
  const label = periodLabel(period, reference)
  const requestKey = `${period}-${reference.year}-${reference.month}`
  const loading = requestKey !== loadedKey && requestKey !== failedKey

  useEffect(() => {
    let active = true
    void getDashboardProjection(period, reference)
      .then(value => {
        if (active) { setData(value); setError(''); setLoadedKey(requestKey); setFailedKey('') }
      })
      .catch(cause => {
        if (active) { setData(null); setError(cause instanceof ApiError ? cause.message : 'Não foi possível carregar a projeção.'); setFailedKey(requestKey) }
      })
    return () => { active = false }
  }, [period, reference, requestKey])

  const noRecords = data !== null && data.entradasCentavos === 0 && data.saidasCentavos === 0
  return <>
    <DashboardHeader period={period} reference={reference} label={label} onPeriodChange={setPeriod} onReferenceChange={setReference} />
    {error && failedKey === requestKey && <p className="form-error dashboard-error" role="alert">{error}</p>}
    <SummaryCards data={data} loading={loading} />
    <ProjectionPanel data={data} label={label} loading={loading} empty={noRecords} />
    <DashboardPanels data={data} loading={loading} empty={noRecords} />
  </>
}
