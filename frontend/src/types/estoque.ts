/** Espelha Estoque.Contracts.ItemEstoqueDto. */
export interface ItemEstoque {
  id: string
  branchId: string | null
  nome: string
  unidadeMedida: string
  quantidadeAtual: number
  quantidadeMinima: number
  estoqueBaixo: boolean
  ativo: boolean
}
