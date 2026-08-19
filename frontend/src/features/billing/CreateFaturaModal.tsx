import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Modal } from '../../components/ui/Modal'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { Label } from '../../components/ui/Label'
import { Select } from '../../components/ui/Select'
import { createFaturaConvenio, createFaturaParticular, listConvenios } from './api'
import { listPatients } from '../patients/api'
import { listProfissionais } from '../scheduling/api'
import { getApiErrorMessage } from '../../lib/query-client'
import type { FormaPagamento } from '../../types/billing'

const schema = z
  .object({
    pacienteId: z.string().min(1, 'Selecione um paciente'),
    tipo: z.enum(['Particular', 'Convenio']),
    valorTotal: z.number({ error: 'Valor é obrigatório' }).positive('Valor deve ser maior que zero'),
    numeroParcelas: z.number().int().min(1).max(12),
    formaPagamento: z.string().min(1, 'Selecione a forma de pagamento'),
    convenioId: z.string().optional(),
    profissionalId: z.string().optional(),
    // String livre de propósito (não numérico no schema) — vazio vira undefined no submit;
    // validação de faixa (0-100) fica só como atributo HTML min/max, não bloqueia o form.
    comissaoDentistaPercentual: z.string().optional(),
  })
  .refine((v) => v.tipo !== 'Convenio' || !!v.convenioId, {
    message: 'Selecione um convênio',
    path: ['convenioId'],
  })

type FormValues = z.infer<typeof schema>

interface CreateFaturaModalProps {
  open: boolean
  onClose: () => void
}

export function CreateFaturaModal({ open, onClose }: CreateFaturaModalProps) {
  const queryClient = useQueryClient()
  const [tipo, setTipo] = useState<'Particular' | 'Convenio'>('Particular')

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { tipo: 'Particular', numeroParcelas: 1, formaPagamento: 'Pix' },
  })

  const { data: pacientesPage } = useQuery({
    queryKey: ['patients', 'select'],
    queryFn: () => listPatients({ pageSize: 100 }),
    enabled: open,
  })
  const { data: profissionais } = useQuery({ queryKey: ['profissionais'], queryFn: listProfissionais, enabled: open })
  const { data: convenios } = useQuery({
    queryKey: ['convenios'],
    queryFn: () => listConvenios(),
    enabled: open && tipo === 'Convenio',
  })

  const invalidateAndClose = () => {
    queryClient.invalidateQueries({ queryKey: ['faturas'] })
    reset()
    onClose()
  }

  const particularMutation = useMutation({
    mutationFn: createFaturaParticular,
    onSuccess: invalidateAndClose,
  })

  const convenioMutation = useMutation({
    mutationFn: createFaturaConvenio,
    onSuccess: invalidateAndClose,
  })

  const mutation = tipo === 'Particular' ? particularMutation : convenioMutation

  const onSubmit = (values: FormValues) => {
    const comissao = !values.comissaoDentistaPercentual ? undefined : Number(values.comissaoDentistaPercentual)

    if (values.tipo === 'Particular') {
      particularMutation.mutate({
        pacienteId: values.pacienteId,
        profissionalId: values.profissionalId || undefined,
        valorTotal: values.valorTotal,
        numeroParcelas: values.numeroParcelas,
        formaPagamento: values.formaPagamento as FormaPagamento,
        comissaoDentistaPercentual: comissao,
      })
    } else {
      convenioMutation.mutate({
        pacienteId: values.pacienteId,
        convenioId: values.convenioId!,
        profissionalId: values.profissionalId || undefined,
        valorTotal: values.valorTotal,
        comissaoDentistaPercentual: comissao,
      })
    }
  }

  return (
    <Modal open={open} onClose={onClose} title="Nova fatura">
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-3" noValidate>
        <div>
          <Label>Tipo</Label>
          <div className="flex gap-4 text-sm">
            <label className="flex items-center gap-1.5">
              <input
                type="radio"
                value="Particular"
                checked={tipo === 'Particular'}
                onChange={() => {
                  setTipo('Particular')
                  setValue('tipo', 'Particular')
                }}
              />
              Particular
            </label>
            <label className="flex items-center gap-1.5">
              <input
                type="radio"
                value="Convenio"
                checked={tipo === 'Convenio'}
                onChange={() => {
                  setTipo('Convenio')
                  setValue('tipo', 'Convenio')
                }}
              />
              Convênio
            </label>
          </div>
        </div>

        <div>
          <Label htmlFor="pacienteId">Paciente</Label>
          <Select id="pacienteId" error={errors.pacienteId?.message} {...register('pacienteId')}>
            <option value="">Selecione…</option>
            {pacientesPage?.items.map((p) => (
              <option key={p.id} value={p.id}>
                {p.nomeCompleto}
              </option>
            ))}
          </Select>
        </div>

        {tipo === 'Convenio' && (
          <div>
            <Label htmlFor="convenioId">Convênio</Label>
            <Select id="convenioId" error={errors.convenioId?.message} {...register('convenioId')}>
              <option value="">Selecione…</option>
              {convenios?.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.nome}
                </option>
              ))}
            </Select>
          </div>
        )}

        <div>
          <Label htmlFor="profissionalId">Profissional (opcional — pra cálculo de comissão)</Label>
          <Select id="profissionalId" {...register('profissionalId')}>
            <option value="">Nenhum</option>
            {profissionais?.map((p) => (
              <option key={p.id} value={p.id}>
                {p.nome}
              </option>
            ))}
          </Select>
        </div>

        <div>
          <Label htmlFor="valorTotal">Valor total (R$)</Label>
          <Input
            id="valorTotal"
            type="number"
            min="0.01"
            step="0.01"
            error={errors.valorTotal?.message}
            {...register('valorTotal', { valueAsNumber: true })}
          />
        </div>

        {tipo === 'Particular' && (
          <div className="grid grid-cols-2 gap-3">
            <div>
              <Label htmlFor="numeroParcelas">Parcelas</Label>
              <Input
                id="numeroParcelas"
                type="number"
                min="1"
                max="12"
                error={errors.numeroParcelas?.message}
                {...register('numeroParcelas', { valueAsNumber: true })}
              />
            </div>
            <div>
              <Label htmlFor="formaPagamento">Forma de pagamento</Label>
              <Select id="formaPagamento" error={errors.formaPagamento?.message} {...register('formaPagamento')}>
                <option value="Pix">Pix</option>
                <option value="Cartao">Cartão</option>
                <option value="Boleto">Boleto</option>
                <option value="Dinheiro">Dinheiro</option>
              </Select>
            </div>
          </div>
        )}

        <div>
          <Label htmlFor="comissaoDentistaPercentual">Comissão do dentista % (opcional)</Label>
          <Input
            id="comissaoDentistaPercentual"
            type="number"
            min="0"
            max="100"
            step="0.01"
            error={errors.comissaoDentistaPercentual?.message}
            {...register('comissaoDentistaPercentual')}
          />
        </div>

        {mutation.isError && <p className="text-sm text-danger">{getApiErrorMessage(mutation.error)}</p>}

        <div className="flex justify-end gap-2 pt-2">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" disabled={mutation.isPending}>
            {mutation.isPending ? 'Criando…' : 'Criar fatura'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
