/** Espelha Contracts.Abstractions.Pagination.PagedResult<T> do backend. */
export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

/** Corpo de erro padrão devolvido pelos controllers ({ error: string }) em toda falha 4xx. */
export interface ApiErrorBody {
  error: string
}
