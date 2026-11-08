export type AnalysisDimension = 'category' | 'payment' | 'type'
export type AnalysisDirection = 'all' | 'income' | 'expense'
export type AnalysisRecord = { id: string; nome: string; movimentacao: 'entrada' | 'saida'; tipo: string; categoria: string; pagamento: string; ocorrencias: number; parcelasTotais: number | null; parcelasPrevistasAteFim: number | null; valorCentavos: number }
export type AnalysisCross = { chave: string; nome: string; ocorrencias: number; registros: number; valorCentavos: number }
export type AnalysisSegment = { chave: string; nome: string; ocorrencias: number; registros: number; valorCentavos: number; percentual: number; cruzamentos: AnalysisCross[]; detalhes: AnalysisRecord[] }
export type AnalysisProjection = {
  periodo: 'month' | 'semester' | 'year'; referencia: string; inicio: string; fim: string; direcao: AnalysisDirection
  dimensaoPrincipal: AnalysisDimension; dimensaoSecundaria: AnalysisDimension | 'none'
  entradasCentavos: number; saidasCentavos: number; resultadoCentavos: number; registros: number; ocorrencias: number
  basePercentualCentavos: number; segmentos: AnalysisSegment[]
}
export const dimensions: { id: AnalysisDimension; label: string }[] = [
  { id: 'category', label: 'Categoria' }, { id: 'payment', label: 'Pagamento' }, { id: 'type', label: 'Tipo' },
]
