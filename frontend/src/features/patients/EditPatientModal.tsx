import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Modal } from '../../components/ui/Modal'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { Label } from '../../components/ui/Label'
import { updatePatient } from './api'
import { getApiErrorMessage } from '../../lib/query-client'
import { formatCpf } from '../../lib/cpf'
import type { Patient } from '../../types/patients'

const schema = z.object({
  nomeCompleto: z.string().min(1, 'Nome é obrigatório'),
  dataNascimento: z.string().min(1, 'Data de nascimento é obrigatória'),
  telefone: z.string().min(1, 'Telefone é obrigatório'),
  email: z.string().email('Email inválido').optional().or(z.literal('')),
  endereco: z.string().optional(),
})

type FormValues = z.infer<typeof schema>

interface EditPatientModalProps {
  patient: Patient | null
  onClose: () => void
}

/** CPF nunca aparece no formulário editável — imutável após o cadastro (regra do backend, ver Patients.Domain.Entities.Patient.AtualizarDadosCadastrais). */
export function EditPatientModal({ patient, onClose }: EditPatientModalProps) {
  const queryClient = useQueryClient()

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  useEffect(() => {
    if (!patient) return
    reset({
      nomeCompleto: patient.nomeCompleto,
      dataNascimento: patient.dataNascimento.slice(0, 10),
      telefone: patient.telefone,
      email: patient.email ?? '',
      endereco: patient.endereco ?? '',
    })
  }, [patient, reset])

  const mutation = useMutation({
    mutationFn: (values: FormValues) =>
      updatePatient(patient!.id, {
        nomeCompleto: values.nomeCompleto,
        dataNascimento: values.dataNascimento,
        telefone: values.telefone,
        email: values.email || undefined,
        endereco: values.endereco || undefined,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['patients'] })
      onClose()
    },
  })

  if (!patient) return null

  return (
    <Modal open={!!patient} onClose={onClose} title="Editar paciente">
      <form onSubmit={handleSubmit((values) => mutation.mutate(values))} className="space-y-3" noValidate>
        <div>
          <Label htmlFor="nomeCompleto">Nome completo</Label>
          <Input id="nomeCompleto" error={errors.nomeCompleto?.message} {...register('nomeCompleto')} />
        </div>

        <div>
          <Label>CPF</Label>
          <p className="rounded-md border border-border bg-surface-sunken px-3 py-2 text-sm text-ink-muted">
            {formatCpf(patient.cpf)} — imutável após o cadastro
          </p>
        </div>

        <div>
          <Label htmlFor="dataNascimento">Data de nascimento</Label>
          <Input id="dataNascimento" type="date" error={errors.dataNascimento?.message} {...register('dataNascimento')} />
        </div>

        <div>
          <Label htmlFor="telefone">Telefone</Label>
          <Input id="telefone" error={errors.telefone?.message} {...register('telefone')} />
        </div>

        <div>
          <Label htmlFor="email">Email (opcional)</Label>
          <Input id="email" type="email" error={errors.email?.message} {...register('email')} />
        </div>

        <div>
          <Label htmlFor="endereco">Endereço (opcional)</Label>
          <Input id="endereco" {...register('endereco')} />
        </div>

        {mutation.isError && <p className="text-sm text-danger">{getApiErrorMessage(mutation.error)}</p>}

        <div className="flex justify-end gap-2 pt-2">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" disabled={mutation.isPending}>
            {mutation.isPending ? 'Salvando…' : 'Salvar alterações'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
