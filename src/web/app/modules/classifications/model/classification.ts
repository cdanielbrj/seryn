export type Classification = { id: string; nome: string }
export type Category = Classification & { categoriaPaiId: string | null }
