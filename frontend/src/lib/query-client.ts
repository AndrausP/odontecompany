import { QueryClient } from '@tanstack/react-query'
import { isAxiosError } from 'axios'
import type { ApiErrorBody } from '../types/common'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      staleTime: 30_000,
    },
  },
})

/** Extrai a mensagem de erro do corpo `{ error: string }` que todo controller devolve em falha — cai pra mensagem genérica se a resposta não tiver esse formato (erro de rede, 500 sem corpo, etc). */
export function getApiErrorMessage(error: unknown): string {
  if (isAxiosError<ApiErrorBody>(error) && error.response?.data?.error) {
    return error.response.data.error
  }
  return 'Erro inesperado. Tente novamente.'
}
