export type ImportResult = { criados: number; existentes: number; excluidos: number }
export type WorkbookSheetPreview = { aba: string; linhas: number; criacoes: number; existentes: number }
export type WorkbookPreview = { abas: WorkbookSheetPreview[]; criacoes: number; existentes: number; dadosAtuais: number; completa: boolean; erros: string[] }
