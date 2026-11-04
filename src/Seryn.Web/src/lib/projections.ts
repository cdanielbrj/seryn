export type ProjectionBucket = { mes: string; entradasCentavos: number; saidasCentavos: number; resultadoAcumuladoCentavos: number; selecionado: boolean }
export type CategoryProjection = { nome: string; valorCentavos: number; percentual: number }
export type TopRecordProjection = { id: string; nome: string; categoriaNome: string | null; ocorrencias: number; valorCentavos: number }
export type DashboardProjection = {
  periodo: 'month' | 'semester' | 'year'; referencia: string; inicio: string; fim: string
  entradasCentavos: number; saidasCentavos: number; resultadoCentavos: number
  graficoInicio: string; graficoFim: string; grafico: ProjectionBucket[]
  categorias: CategoryProjection[]; maioresEntradas: TopRecordProjection[]; maioresSaidas: TopRecordProjection[]
}

const formatter = new Intl.DateTimeFormat('pt-BR', { month: 'long', year: 'numeric' })
export function monthName(value: string) {
  const [year, month] = value.split('-').map(Number)
  return formatter.format(new Date(year, month - 1, 1))
}

export function chartDescription(data: DashboardProjection) {
  if (data.periodo === 'month') return `Resultado previsto acumulado entre ${monthName(data.graficoInicio)} e ${monthName(data.graficoFim)}.`
  return `Resultado previsto acumulado em ${data.periodo === 'semester' ? 'todo o semestre selecionado' : 'todo o ano selecionado'}.`
}
