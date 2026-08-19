import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { Modal } from '../../components/ui/Modal'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { Label } from '../../components/ui/Label'
import { createItemEstoque } from './api'
import { getApiErrorMessage } from '../../lib/query-client'

const schema = z.object({
  nome: z.string().min(1, 'Nome é obrigatório'),
  unidadeMedida: z.string().min(1, 'Unidade de medida é obrigatória'),
  quantidadeMinima: z.number().min(0, 'Quantidade mínima não pode ser negativa'),
})

type FormValues = z.infer<typeof schema>

interface CreateItemEstoqueModalProps {
  open: boolean
  onClose: () => void
}

export function CreateItemEstoqueModal({ open, onClose }: CreateItemEstoqueModalProps) {
  const queryClient = useQueryClient()

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FormValues>({ resolver: zodResolver(schema), defaultValues: { quantidadeMinima: 0 } })

  const mutation = useMutation({
    mutationFn: createItemEstoque,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['estoque'] })
      reset()
      onClose()
    },
  })

  return (
    <Modal open={open} onClose={onClose} title="Novo item de estoque">
      <form onSubmit={handleSubmit((values) => mutation.mutate(values))} className="space-y-3" noValidate>
        <div>
          <Label htmlFor="nome">Nome do material/insumo</Label>
          <Input id="nome" placeholder="Luva de procedimento" error={errors.nome?.message} {...register('nome')} />
        </div>

        <div>
          <Label htmlFor="unidadeMedida">Unidade de medida</Label>
          <Input id="unidadeMedida" placeholder="caixa, unidade, ml…" error={errors.unidadeMedida?.message} {...register('unidadeMedida')} />
        </div>

        <div>
          <Label htmlFor="quantidadeMinima">Quantidade mínima (alerta de estoque baixo)</Label>
          <Input
            id="quantidadeMinima"
            type="number"
            min="0"
            step="0.01"
            error={errors.quantidadeMinima?.message}
            {...register('quantidadeMinima', { valueAsNumber: true })}
          />
        </div>

        {mutation.isError && <p className="text-sm text-danger">{getApiErrorMessage(mutation.error)}</p>}

        <div className="flex justify-end gap-2 pt-2">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" disabled={mutation.isPending}>
            {mutation.isPending ? 'Criando…' : 'Cadastrar item'}
          </Button>
        </div>
      </form>
    </Modal>
  )
}
