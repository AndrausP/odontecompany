import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios'
import { useAuthStore } from './auth-store'
import type { LoginResult } from '../types/auth'

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
})

// Toda request autenticada leva o access token — organization_id/role/branch_id nunca são enviados
// como header/body manualmente em nenhum lugar do app: o backend só confia no que está dentro
// do JWT assinado (mesma regra reforçada em todo *CommandHandler* do backend).
apiClient.interceptors.request.use((config) => {
  const { accessToken } = useAuthStore.getState()
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`
  }
  return config
})

let refreshInFlight: Promise<string> | null = null

/**
 * 401 → tenta UMA rotação de refresh token (nunca em loop) e refaz a request original com o
 * novo access token. Se o refresh falhar (refresh token também expirado/revogado), limpa a
 * sessão — próxima navegação cai no ProtectedRoute e manda pro login. `refreshInFlight`
 * coalesce múltiplas requests 401 simultâneas numa única chamada de /auth/refresh (evita
 * disparar N refreshes em paralelo se N requests falharem ao mesmo tempo).
 */
apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as (InternalAxiosRequestConfig & { _retry?: boolean }) | undefined

    if (error.response?.status !== 401 || !originalRequest || originalRequest._retry) {
      return Promise.reject(error)
    }

    const { refreshToken, setSession, clearSession } = useAuthStore.getState()
    if (!refreshToken) {
      clearSession()
      return Promise.reject(error)
    }

    originalRequest._retry = true

    try {
      refreshInFlight ??= axios
        .post<LoginResult>(`${import.meta.env.VITE_API_BASE_URL}/api/auth/refresh`, { refreshToken })
        .then((res) => {
          setSession(res.data)
          return res.data.accessToken
        })
        .finally(() => {
          refreshInFlight = null
        })

      const newAccessToken = await refreshInFlight
      originalRequest.headers.Authorization = `Bearer ${newAccessToken}`
      return apiClient(originalRequest)
    } catch (refreshError) {
      clearSession()
      return Promise.reject(refreshError)
    }
  },
)
