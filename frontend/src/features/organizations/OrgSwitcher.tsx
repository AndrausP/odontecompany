import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { switchOrganization } from '../auth/api'
import { useAuthStore } from '../../lib/auth-store'
import { getApiErrorMessage } from '../../lib/query-client'
import { Select } from '../../components/ui/Select'
import type { OrganizationMembership } from '../../types/auth'

interface OrgSwitcherProps {
  organizations: OrganizationMembership[]
  activeOrganizationId: string | null
}

/**
 * Seletor de organization ativa no AppLayout (task 018). POST /api/auth/switch-organization troca
 * o par de tokens sem exigir login de novo; queryClient.clear() é OBRIGATÓRIO aqui — sem isso a
 * tela continua mostrando dado da organization anterior (cache do React Query é por queryKey, não
 * por organization). É o risco identificado explicitamente na task 018.
 */
export function OrgSwitcher({ organizations, activeOrganizationId }: OrgSwitcherProps) {
  const queryClient = useQueryClient()
  const setSession = useAuthStore((s) => s.setSession)
  const [error, setError] = useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: switchOrganization,
    onSuccess: (result) => {
      setSession({ accessToken: result.accessToken, refreshToken: result.refreshToken })
      // Limpa TUDO — não só ['me']: agenda, pacientes, financeiro etc já podem ter cache da
      // organization anterior. Refetch acontece sozinho pros componentes montados/observando.
      queryClient.clear()
      setError(null)
    },
    onError: (err) => setError(getApiErrorMessage(err)),
  })

  // Usuário com uma organization só não precisa de seletor — só mostra o nome.
  if (organizations.length <= 1) {
    return <p className="truncate text-xs font-medium text-ink-secondary">{organizations[0]?.organizationName}</p>
  }

  const handleChange = (organizationId: string) => {
    if (!organizationId || organizationId === activeOrganizationId) return
    mutation.mutate(organizationId)
  }

  return (
    <div>
      <Select
        aria-label="Organização ativa"
        value={activeOrganizationId ?? ''}
        disabled={mutation.isPending}
        onChange={(e) => handleChange(e.target.value)}
      >
        {organizations.map((org) => (
          <option key={org.organizationId} value={org.organizationId}>
            {org.organizationName}
          </option>
        ))}
      </Select>
      {error && <p className="mt-1 text-xs text-danger">{error}</p>}
    </div>
  )
}
