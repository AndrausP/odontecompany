import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Modal } from '../../components/ui/Modal'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { Label } from '../../components/ui/Label'
import { Checkbox } from '../../components/ui/Checkbox'
import { createPatient } from './api'
import { getApiErrorMessage } from '../../lib/query-client'
import { formatCpf, isValidCpf, onlyDigits } from '../../lib/cpf'

const schema = z.object({
  nomeCompleto: z.string().min(1, 'Nome é obrigatório'),
  cpf: z.string().refine(isValidCpf, 'CPF inválido'),
  dataNascimento: z.string().min(1, 'Data de nascimento é obrigatória'),
  telefone: z.string().min(1, 'Telefone é obrigatório'),
  email: z.string().email('Email inválido').optional().or(z.literal('')),
  endereco: z.string().optional(),
  consentimentoLgpd: z.literal(true, { message: 'Consentimento LGPD é obrigatório pra cadastrar' }),
})

type FormValues = z.infer<typeof schema>

interface CreatePatientModalProps {
  open: boolean
  onClose: () => void
}

export function CreatePatientModal({ open, onClose }: CreatePatientModalProps) {
  const queryClient = useQueryClient()

  const {
    register,
    handleSubmit,
    reset,
    watch,
    setValue,
    formState: { errors },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  const mutation = useMutation({
    mutationFn: (values: FormValues) =>
      createPatient({
        nomeCompleto: values.nomeCompleto,
        cpf: onlyDigits(values.cpf),
        dataNascimento: values.dataNascimento,
        telefone: values.telefone,
        email: values.email || undefined,
        endereco: values.endereco || undefined,
        consentimentoLgpd: true,
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['patients'] })
      reset()
      onClose()
    },
  })

  const cpfValue = watch('cpf')

  return (
    <Modal open={open} onClose={onClose} title="Novo paciente">
      <form onSubmit={handleSubmit((values) => mutation.mutate(values))} className="space-y-3" noValidate>
        <div>
          <Label htmlFor="nomeCompleto">Nome completo</Label>
          <Input id="nomeCompleto" error={errors.nomeCompleto?.message} {...register('nomeCompleto')} />
        </div>

        <div>
          <Label htmlFor="cpf">CPF</Label>
          <Input
            id="cpf"
            inputMode="numeric"
            placeholder="000.000.000-00"
            value={cpfValue ? formatCpf(cpfValue) : ''}
            onChange={(e) => setValue('cpf', onlyDigits(e.target.value), { shouldValidate: true })}
            error={errors.cpf?.message}
          />
        </div>

        <div>
          <Label htmlFor="dataNascimento">Data de nascimento</Label>
          <Input id="dataNascimento" type="date" error={errors.dataNascimento?.message} {...register('dataNascimento')} />
        </div>

        <div>
          <Label htmlFor="telefone">Telefone</Label>
          <Input id="telefone" placeholder="11999999999" error={errors.telefone?.message} {...register('telefone')} />
        </div>

        <div>
          <Label htmlFor="email">Email (opcional)</Label>
          <Input id="email" type="email" error={errors.email?.message} {...register('email')} />
        </div>

        <div>
          <Label htmlFor="endereco">Endereço (opcional)</Label>
          <Input id="endereco" {...register('endereco')} />
        </div>

        <Checkbox
          label="Paciente consentiu com o tratamento de dados pessoais (LGPD)"
          error={errors.consentimentoLgpd?.message}
          {...register('consentimentoLgpd')}
        />

        {mutation.isError && <p className="text-sm text-danger">{getApiErrorMessage(mutation.error)}</p>}

        <div className="flex justify-end gap-2 pt-2">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" disabled={mutation.isPending}>
            {mutation.isPending ? 'Salvando…' : 'Cadastrar paciente'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
