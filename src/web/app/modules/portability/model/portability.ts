export type ImportResult = { criados: number; existentes: number; excluidos: number }
export type WorkbookSheetPreview = { aba: string; linhas: number; criacoes: number; existentes: number }
export type WorkbookPreview = { previewIdMesclagem: string | null; previewIdSubstituicao: string | null; expiraEm: string; abas: WorkbookSheetPreview[]; criacoes: number; existentes: number; dadosAtuais: number; completa: boolean; erros: string[] }
