/** Espelha Identity.Application.DTOs.OrganizationDto — tela de configurações (GET/PUT /api/organizations/{id}). */
export interface OrganizationDetails {
  id: string
  nome: string
  cnpj: string | null
  telefone: string | null
  endereco: string | null
  ativo: boolean
}

/**
 * Espelha Tenancy.Contracts.BranchDto — versão completa (com telefone), diferente do `Branch`
 * reduzido de `types/reports.ts` (usado só no dropdown de filial dos relatórios).
 */
export interface BranchDetails {
  id: string
  organizationId: string
  nome: string
  endereco: string | null
  telefone: string | null
  ativo: boolean
}
