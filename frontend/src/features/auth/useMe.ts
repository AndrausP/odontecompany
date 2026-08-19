import { useQuery } from '@tanstack/react-query'
import { getMe } from './api'
import { useAuthStore } from '../../lib/auth-store'

/**
 * Espelho reativo de GET /api/me (task 018) — fonte de verdade pra organizations[]/pendingInvites/
 * activeOrganizationId. Só habilitado com sessão válida (token presente e não expirado); usado por
 * RequireOrganization, OnboardingPage, AppLayout (seletor de organization) e InvitesPage. Mesma
 * queryKey em todo lugar ('me') — cache compartilhado entre esses componentes, sem refetch duplicado.
 */
export function useMe() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated())

  return useQuery({
    queryKey: ['me'],
    queryFn: getMe,
    enabled: isAuthenticated,
  })
}
