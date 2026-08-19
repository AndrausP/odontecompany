import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation } from '@tanstack/react-query'
import { createBranch } from './api'
import { getApiErrorMessage } from '../../lib/query-client'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { Label } from '../../components/ui/Label'

const schema = z.object({
  nome: z.string().min(1, 'Nome é obrigatório').max(150, 'Nome não pode ter mais de 150 caracteres'),
  endereco: z.string().optional(),
  telefone: z.string().optional(),
})

type FormValues = z.infer<typeof schema>

interface CreateBranchFormProps {
  onCreated: () => void
}

/**
 * Passo 3 do onboarding guiado (sprint-11, depois de escolher o plano) — a primeira filial/
 * unidade da organization (endereço/telefone opcionais — sede ou não não importa pro backend
 * hoje, é só mais uma Branch: BranchesController/CreateBranchCommand não distingue sede de filial
 * secundária). Pode 400 com `Branch.LimiteDoPlanoAtingido` se o plano escolhido não permitir mais
 * unidades — não deveria acontecer aqui (organization recém-criada, 0 branches), mas o erro do
 * backend já vem com mensagem pronta pro usuário. Organization pode ter N branches depois,
 * criadas fora do onboarding (fora de escopo aqui).
 */
export function CreateBranchForm({ onCreated }: CreateBranchFormProps) {
  const [serverError, setServerError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  const mutation = useMutation({
    mutationFn: (values: FormValues) => createBranch(values),
    onSuccess: () => onCreated(),
    onError: (error) => setServerError(getApiErrorMessage(error)),
  })

  const onSubmit = (values: FormValues) => {
    setServerError(null)
    mutation.mutate(values)
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
      <div>
        <Label htmlFor="nomeFilial">Nome da unidade</Label>
        <Input
          id="nomeFilial"
          placeholder="Ex.: Unidade Centro"
          error={errors.nome?.message}
          {...register('nome')}
        />
      </div>

      <div>
        <Label htmlFor="enderecoFilial">Endereço (opcional)</Label>
        <Input id="enderecoFilial" placeholder="Rua, número, cidade" {...register('endereco')} />
      </div>

      <div>
        <Label htmlFor="telefoneFilial">Telefone (opcional)</Label>
        <Input id="telefoneFilial" placeholder="11999999999" {...register('telefone')} />
      </div>

      {serverError && <p className="text-sm text-danger">{serverError}</p>}

      <Button type="submit" className="w-full" disabled={mutation.isPending}>
        {mutation.isPending ? 'Criando…' : 'Criar unidade'}
      </Button>
    </form>
  )
}
