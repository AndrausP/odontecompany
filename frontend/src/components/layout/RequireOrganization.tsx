import { useEffect, type ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { useMe } from '../../features/auth/useMe'
import { useAuthStore } from '../../lib/auth-store'

interface RequireOrganizationProps {
  children: ReactNode
}

/**
 * Gate pós-login (task 018, +skip sprint-11, +plano sprint-11): só deixa passar pra dentro do
 * AppLayout quem tem organization ATIVA COM PLANO, ou já escolheu "por enquanto não" no
 * onboarding (`user.onboardingSkipped`, persistido no backend — task 018 original não previa esse
 * caso). Duas situações mandam pro /onboarding: zero organization (sem ter pulado), ou
 * organization existe mas `activePlanTier` é null (criou a empresa mas não terminou de escolher
 * plano — "bloqueie quem não tem os planos", regra de negócio da sprint-11). Sem skip pro passo de
 * plano de propósito: só existe "pular" ANTES de criar organization nenhuma; quem já tem
 * organization sempre precisa de plano ativo. Mantém auth-store.organizations/activeOrganizationId
 * sincronizado com o snapshot de /api/me a cada resposta nova — é a fonte usada pelo seletor de
 * organization no AppLayout. Quem entrou sem organization (pulou) ainda passa por aqui pra dentro
 * do AppLayout — é o próprio AppLayout que decide mostrar o dashboard vazio com banner em vez do
 * <Outlet/> normal (ver AppLayout.tsx).
 */
export function RequireOrganization({ children }: RequireOrganizationProps) {
  const { data, isLoading, isError } = useMe()
  const setMe = useAuthStore((s) => s.setMe)

  useEffect(() => {
    if (data) setMe(data)
  }, [data, setMe])

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-surface">
        <p className="text-sm text-ink-muted">Carregando…</p>
      </div>
    )
  }

  // Erro em /me (ex.: token expirado no meio do caminho) — o interceptor 401 do api-client já
  // limpa a sessão nesse caso; ProtectedRoute cuida do redirect pro /login no próximo render.
  if (isError || !data) {
    return null
  }

  if (data.organizations.length === 0 && !data.user.onboardingSkipped) {
    return <Navigate to="/onboarding" replace />
  }

  if (data.organizations.length > 0 && !data.activePlanTier) {
    return <Navigate to="/onboarding" replace />
  }

  return <>{children}</>
}
