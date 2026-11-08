import { get } from '@/infrastructure/http/client'
export { ApiError } from '@/infrastructure/http/client'
import type { Period, Reference } from '../model/period'
import type { DashboardProjection } from '../model/projection'

export function getDashboardProjection(period: Period, reference: Reference) {
  const month = String(reference.month).padStart(2, '0')
  return get<DashboardProjection>(`/projections?period=${period}&reference=${reference.year}-${month}`)
}
