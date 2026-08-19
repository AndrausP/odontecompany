export type StatusFatura = 'Pendente' | 'ParcialmentePaga' | 'Paga' | 'Vencida' | 'Cancelada'
export type StatusParcela = 'Pendente' | 'Paga' | 'Vencida'
export type FormaPagamento = 'Dinheiro' | 'Cartao' | 'Boleto' | 'Pix'
export type TipoFatura = 'Particular' | 'Convenio'

/** Espelha Billing.Contracts.ParcelaDto. */
export interface Parcela {
  id: string
  numeroParcela: number
  valorParcela: number
  dataVencimento: string
  dataPagamento: string | null
  status: StatusParcela
}

/** Espelha Billing.Contracts.FaturaDto. */
export interface Fatura {
  id: string
  pacienteId: string
  agendamentoId: string | null
  profissionalId: string | null
  tipoFatura: TipoFatura
  convenioId: string | null
  valorTotal: number
  formaPagamento: FormaPagamento
  status: StatusFatura
  comissaoDentistaPercentual: number | null
  valorComissao: number
  protocoloConvenio: string | null
  parcelas: Parcela[]
  createdAt: string
}

/** Espelha Billing.Contracts.ConvenioDto. */
export interface Convenio {
  id: string
  nome: string
  codigoExterno: string | null
  ativo: boolean
}
