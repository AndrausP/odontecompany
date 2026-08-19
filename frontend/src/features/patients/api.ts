import { apiClient } from '../../lib/api-client'
import type { PagedResult } from '../../types/common'
import type { Patient } from '../../types/patients'

export interface ListPatientsParams {
  nome?: string
  cpf?: string
  includeInactive?: boolean
  page?: number
  pageSize?: number
}

export async function listPatients(params: ListPatientsParams = {}): Promise<PagedResult<Patient>> {
  const { data } = await apiClient.get<PagedResult<Patient>>('/api/patients', {
    params: { pageSize: 50, ...params },
  })
  return data
}

export interface CreatePatientPayload {
  nomeCompleto: string
  cpf: string
  dataNascimento: string
  telefone: string
  email?: string
  endereco?: string
  consentimentoLgpd: boolean
}

export async function createPatient(payload: CreatePatientPayload): Promise<Patient> {
  const { data } = await apiClient.post<Patient>('/api/patients', payload)
  return data
}

export interface UpdatePatientPayload {
  nomeCompleto: string
  dataNascimento: string
  telefone: string
  email?: string
  endereco?: string
}

export async function updatePatient(id: string, payload: UpdatePatientPayload): Promise<Patient> {
  const { data } = await apiClient.put<Patient>(`/api/patients/${id}`, payload)
  return data
}

export async function deactivatePatient(id: string): Promise<void> {
  await apiClient.delete(`/api/patients/${id}`)
}
