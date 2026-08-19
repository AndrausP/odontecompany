/** Espelha Patients.Contracts.PatientDto. */
export interface Patient {
  id: string
  nomeCompleto: string
  cpf: string
  dataNascimento: string
  telefone: string
  email: string | null
  endereco: string | null
  consentimentoLgpd: boolean
  dataConsentimentoLgpd: string | null
  ativo: boolean
  createdAt: string
  updatedAt: string | null
}
