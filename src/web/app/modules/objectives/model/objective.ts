export type ObjectiveInterest = 'listado' | 'considerado' | 'desejado'
export type Objective = { id: string; nome: string; valorEstimadoCentavos: number; interesse: ObjectiveInterest; prioridade: number; link: string | null; observacoes: string | null; dataDesejada: string | null; dataInclusao: string }
export type ObjectiveInput = Omit<Objective, 'id' | 'dataInclusao'>
export const interestLabels: Record<ObjectiveInterest, string> = { listado: 'Listado', considerado: 'Considerado', desejado: 'Desejado' }
export const priorityLabels: Record<number, string> = { [-3]: 'Muito baixa', [-2]: 'Baixa', [-1]: 'Levemente baixa', 0: 'Neutra', 1: 'Levemente alta', 2: 'Alta', 3: 'Muito alta' }
