import { get } from '@/infrastructure/http/client'
export { ApiError } from '@/infrastructure/http/client'
import type { Period, Reference } from '@/modules/dashboard/model/period'
import type { AnalysisDimension, AnalysisDirection, AnalysisProjection } from '../model/analysis'

export function getAnalysis(period: Period, reference: Reference, direction: AnalysisDirection, primary: AnalysisDimension, secondary: AnalysisDimension | 'none') {
  const month = String(reference.month).padStart(2, '0')
  const query = new URLSearchParams({ period, reference: `${reference.year}-${month}`, direction, primary, secondary })
  return get<AnalysisProjection>(`/analyses?${query}`)
}
