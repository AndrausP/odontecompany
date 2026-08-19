import { useEffect, type ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { useMe } from '../../features/auth/useMe'
import { useAuthStore } from '../../lib/auth-store'

interface RequireOrganizationProps {
  children: ReactNode
}

/**
 * Gate pós-login (task 018, +skip sprint-11): só deixa passar pra dentro do AppLayout quem já
 * tem organization ativa OU já escolheu "por enquanto não" no onboarding
 * (`user.onboardingSkipped`, persistido no backend — task 018 original não previa esse caso).
 * Usuário recém-criado via signup, sem organization e sem ter pulado ainda, cai em /onboarding —
 * criar organização ou aceitar convite pendente. Mantém auth-store.organizations/
 * activeOrganizationId sincronizado com o snapshot de /api/me a cada resposta nova — é a fonte
 * usada pelo seletor de organization no AppLayout. Quem entrou sem organization (pulou) ainda
 * passa por aqui pra dentro do AppLayout — é o próprio AppLayout que decide mostrar o dashboard
 * vazio com banner em vez do <Outlet/> normal (ver AppLayout.tsx).
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

  return <>{children}</>
}
