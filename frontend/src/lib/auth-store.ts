import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import { jwtDecode } from 'jwt-decode'
import type { AccessTokenClaims, MeResult, OrganizationMembership } from '../types/auth'

/** Qualquer resultado de auth que emite um par de tokens novo (login/signup/switch-organization/create-organization). */
interface TokenPair {
  accessToken: string
  refreshToken: string | null
}

interface AuthState {
  accessToken: string | null
  refreshToken: string | null
  claims: AccessTokenClaims | null
  /** Espelho do `/api/me` (task 018) — não confiar só no JWT decodificado pra listar organizations/trocar org, o token só carrega a organization ATIVA. */
  activeOrganizationId: string | null
  organizations: OrganizationMembership[]
  setSession: (result: TokenPair) => void
  setMe: (me: MeResult) => void
  clearSession: () => void
  isAuthenticated: () => boolean
}

/**
 * Sessão do usuário — persistida em localStorage (chave `odonto-auth`) pra sobreviver a reload
 * de página. Claims (organization_id/role/branch_id) são decodificadas do próprio JWT no client, sem
 * endpoint /me — o token já carrega tudo que a UI precisa pra decidir o que mostrar (RBAC é
 * sempre reforçado de verdade no backend; aqui é só pra render condicional, nunca a fonte da
 * verdade de autorização).
 *
 * IMPORTANTE (task 018): `claims.organization_id`/`claims.role` PODEM não existir — usuário
 * recém-criado via signup não tem organization nenhuma ainda, e o backend omite a claim inteira
 * nesse caso (não manda vazio). Todo consumidor deste store precisa tratar como opcional.
 */
export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      accessToken: null,
      refreshToken: null,
      claims: null,
      activeOrganizationId: null,
      organizations: [],

      setSession: (result) => {
        const claims = jwtDecode<AccessTokenClaims>(result.accessToken)
        set({ accessToken: result.accessToken, refreshToken: result.refreshToken, claims })
      },

      setMe: (me) => set({ activeOrganizationId: me.activeOrganizationId, organizations: me.organizations }),

      clearSession: () =>
        set({ accessToken: null, refreshToken: null, claims: null, activeOrganizationId: null, organizations: [] }),

      isAuthenticated: () => {
        const { accessToken, claims } = get()
        if (!accessToken || !claims) return false
        // exp é em segundos desde epoch (padrão JWT); Date.now() é em ms.
        return claims.exp * 1000 > Date.now()
      },
    }),
    { name: 'odonto-auth' },
  ),
)
