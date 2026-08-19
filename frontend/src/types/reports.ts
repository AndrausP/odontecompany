/** Espelha Reporting.Contracts.DashboardResumoDto. */
export interface DashboardResumo {
  organizationId: string
  dataInicio: string
  dataFim: string
  pacientesAtivos: number
  totalAgendamentos: number
  agendamentosConcluidos: number
  agendamentosCancelados: number
  valorTotalFaturado: number
  valorTotalRecebido: number
  valorTotalPendente: number
  taxaConclusao: number
}

/** Espelha Billing.Contracts.FaturamentoResumoDto. */
export interface FaturamentoResumo {
  valorTotalFaturado: number
  valorTotalRecebido: number
  valorTotalPendente: number
  quantidadeFaturas: number
}

/** Espelha Scheduling.Contracts.AgendaResumoDto. */
export interface AgendaResumo {
  totalAgendamentos: number
  agendados: number
  confirmados: number
  concluidos: number
  cancelados: number
}

/** Espelha Tenancy.Contracts.BranchDto — usado só pelo dropdown de filial do dashboard de comissões. */
export interface Branch {
  id: string
  organizationId: string
  nome: string
  endereco: string | null
  ativo: boolean
}

/**
 * Espelha Reporting.Contracts.ComissaoLinhaDto (task 023). `profissionalId`/`branchId`/`classe`
 * podem vir `null` (bucket "Não atribuído" / profissional sem membership correspondente).
 */
export interface ComissaoLinha {
  profissionalId: string | null
  profissionalNome: string
  branchId: string | null
  branchNome: string | null
  classe: string | null
  valorPago: number
  valorComissao: number
  quantidadeFaturas: number
  mediaDiariaComissao: number
}

/** Espelha Reporting.Contracts.ComissaoResumoDto (task 023). */
export interface ComissaoResumo {
  organizationId: string
  dataInicio: string
  dataFim: string
  diasNoPeriodo: number
  valorComissaoTotal: number
  valorPagoTotal: number
  quantidadeFaturas: number
  mediaDiariaComissao: number
  mediaDiariaValorPago: number
  linhas: ComissaoLinha[]
}
