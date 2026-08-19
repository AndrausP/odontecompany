import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuthStore } from '../../lib/auth-store'
import type { Role } from '../../types/auth'

interface ProtectedRouteProps {
  children: ReactNode
  /** Se informado, só papéis nesta lista acessam — senão qualquer usuário autenticado passa. Espelha [Authorize(Roles=...)] do backend, mas é só UX (o backend é quem de fato barra). */
  roles?: Role[]
}

export function ProtectedRoute({ children, roles }: ProtectedRouteProps) {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated())
  const claims = useAuthStore((s) => s.claims)

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />
  }

  // claims.role pode não existir (usuário sem organization ativa ainda, task 018) — trata como
  // "não passa" em vez de deixar `roles.includes(undefined)` decidir por acaso.
  if (roles && (!claims?.role || !roles.includes(claims.role))) {
    return <Navigate to="/agenda" replace />
  }

  return <>{children}</>
}
