/** Papéis do backend (Identity.Domain.Enums.Role) — nunca digitado à mão em outro lugar. */
export type Role = 'Admin' | 'Dentista' | 'Recepcao' | 'Owner'

/** Espelha Identity.Domain.Enums.MembershipStatus. */
export type MembershipStatus = 'Ativo' | 'Inativo'

/**
 * Espelha Identity.Application.DTOs.LoginResultDto — devolvido por login/refresh/switch-organization.
 * RefreshToken é `null` quando o AccessToken foi emitido SEM organization ativa (usuário com zero
 * memberships — ver JwtTokenService.GenerateAccessToken no backend).
 */
export interface LoginResult {
  accessToken: string
  refreshToken: string | null
  expiresIn: number
}

/**
 * Claims decodificadas do access token (JWT). `organization_id`/`role` NÃO existem no token de um
 * usuário recém-criado sem organization nenhuma (task 018) — o backend omite a claim inteira nesse
 * caso (não manda vazio/null), então aqui são opcionais de verdade. Nunca assumir que existem sem
 * checar antes. `branch_id` só quando o usuário está restrito a uma unidade (Fase 5).
 */
export interface AccessTokenClaims {
  sub: string
  organization_id?: string
  role?: Role
  branch_id?: string
  exp: number
}

/** Espelha Identity.Application.DTOs.SignupResultDto (task 018). RefreshToken sempre null (usuário sem organization ainda). */
export interface SignupResult {
  userId: string
  nome: string
  email: string
  accessToken: string
  refreshToken: string | null
  expiresIn: number
}

/** Espelha Identity.Application.DTOs.CreateOrganizationResultDto (task 018) — token novo já escopado à organization criada. */
export interface CreateOrganizationResult {
  organizationId: string
  organizationName: string
  accessToken: string
  refreshToken: string
  expiresIn: number
}

/** Espelha Identity.Application.DTOs.UserDto (task 018, +onboardingSkipped sprint-11). */
export interface UserProfile {
  id: string
  nome: string
  email: string
  ativo: boolean
  onboardingSkipped: boolean
}

/** Espelha Identity.Application.DTOs.OrganizationMembershipDto (task 018) — uma organization a que o usuário pertence, com o papel dessa afiliação. */
export interface OrganizationMembership {
  organizationId: string
  organizationName: string
  role: Role
  status: MembershipStatus
  branchId?: string
}

/** Espelha Identity.Application.DTOs.PendingInviteDto (task 018) — convite pendente do ponto de vista do convidado. */
export interface PendingInvite {
  inviteId: string
  organizationId: string
  organizationName: string
  role: Role
  expiresAt: string
}

/** Espelha Identity.Application.DTOs.MeResultDto (task 018). */
export interface MeResult {
  user: UserProfile
  organizations: OrganizationMembership[]
  activeOrganizationId: string | null
  pendingInvites: PendingInvite[]
}

/** Espelha Identity.Application.DTOs.AcceptInviteResultDto (task 018) — não vem token novo junto, ver comentário no backend. */
export interface AcceptInviteResult {
  organizationId: string
  role: Role
}
