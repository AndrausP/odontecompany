import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Modal } from '../../components/ui/Modal'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { Label } from '../../components/ui/Label'
import { Select } from '../../components/ui/Select'
import { createAgendamento, listProfissionais, listSalas } from './api'
import { listPatients } from '../patients/api'
import { getApiErrorMessage } from '../../lib/query-client'

const schema = z
  .object({
    pacienteId: z.string().min(1, 'Selecione um paciente'),
    profissionalId: z.string().min(1, 'Selecione um profissional'),
    salaId: z.string().min(1, 'Selecione uma sala'),
    data: z.string().min(1, 'Data é obrigatória'),
    horaInicio: z.string().min(1, 'Hora de início é obrigatória'),
    horaFim: z.string().min(1, 'Hora de fim é obrigatória'),
  })
  .refine((v) => v.horaFim > v.horaInicio, {
    message: 'Hora de fim deve ser depois da hora de início',
    path: ['horaFim'],
  })

type FormValues = z.infer<typeof schema>

interface NovoAgendamentoModalProps {
  open: boolean
  onClose: () => void
  /** Data pré-selecionada (clique no calendário) — formato yyyy-MM-dd. */
  initialDate?: string
}

export function NovoAgendamentoModal({ open, onClose, initialDate }: NovoAgendamentoModalProps) {
  const queryClient = useQueryClient()

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { data: initialDate ?? '', horaInicio: '09:00', horaFim: '09:30' },
  })

  const { data: profissionais } = useQuery({ queryKey: ['profissionais'], queryFn: listProfissionais, enabled: open })
  const { data: salas } = useQuery({ queryKey: ['salas'], queryFn: listSalas, enabled: open })
  const { data: pacientesPage } = useQuery({
    queryKey: ['patients', 'select'],
    queryFn: () => listPatients({ pageSize: 100 }),
    enabled: open,
  })

  const mutation = useMutation({
    mutationFn: createAgendamento,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['agendamentos'] })
      reset()
      onClose()
    },
  })

  const onSubmit = (values: FormValues) => {
    mutation.mutate({
      pacienteId: values.pacienteId,
      profissionalId: values.profissionalId,
      salaId: values.salaId,
      inicio: `${values.data}T${values.horaInicio}:00`,
      fim: `${values.data}T${values.horaFim}:00`,
    })
  }

  return (
    <Modal open={open} onClose={onClose} title="Novo agendamento">
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-3" noValidate>
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

        <div>
          <Label htmlFor="profissionalId">Profissional</Label>
          <Select id="profissionalId" error={errors.profissionalId?.message} {...register('profissionalId')}>
            <option value="">Selecione…</option>
            {profissionais?.map((p) => (
              <option key={p.id} value={p.id}>
                {p.nome} — {p.especialidade}
              </option>
            ))}
          </Select>
        </div>

        <div>
          <Label htmlFor="salaId">Sala</Label>
          <Select id="salaId" error={errors.salaId?.message} {...register('salaId')}>
            <option value="">Selecione…</option>
            {salas?.map((s) => (
              <option key={s.id} value={s.id}>
                {s.nome}
              </option>
            ))}
          </Select>
        </div>

        <div>
          <Label htmlFor="data">Data</Label>
          <Input id="data" type="date" error={errors.data?.message} {...register('data')} />
        </div>

        <div className="grid grid-cols-2 gap-3">
          <div>
            <Label htmlFor="horaInicio">Início</Label>
            <Input id="horaInicio" type="time" error={errors.horaInicio?.message} {...register('horaInicio')} />
          </div>
          <div>
            <Label htmlFor="horaFim">Fim</Label>
            <Input id="horaFim" type="time" error={errors.horaFim?.message} {...register('horaFim')} />
          </div>
        </div>

        {mutation.isError && <p className="text-sm text-danger">{getApiErrorMessage(mutation.error)}</p>}

        <div className="flex justify-end gap-2 pt-2">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" disabled={mutation.isPending}>
            {mutation.isPending ? 'Criando…' : 'Criar agendamento'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
