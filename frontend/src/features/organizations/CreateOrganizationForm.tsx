import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createOrganization } from './api'
import { useAuthStore } from '../../lib/auth-store'
import { getApiErrorMessage } from '../../lib/query-client'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { Label } from '../../components/ui/Label'

const schema = z.object({
  nome: z.string().min(1, 'Nome é obrigatório').max(150, 'Nome não pode ter mais de 150 caracteres'),
  cnpj: z.string().optional(),
  telefone: z.string().optional(),
  endereco: z.string().optional(),
})

type FormValues = z.infer<typeof schema>

interface CreateOrganizationFormProps {
  /** Chamado depois da organization criada + sessão trocada — quem chama decide o próximo passo
   * (sprint-11: OnboardingPage avança pro passo 2, escolher plano, em vez de navegar direto pro app). */
  onCreated: () => void
}

/**
 * Cria a organization e já entra nela — createOrganization devolve token novo escopado, diferente
 * do fluxo de aceitar convite (que precisa de um switch-organization separado). Limpa o cache do
 * React Query antes de avançar por hábito: aqui é sempre a primeira organization do usuário, mas
 * este componente pode ser reusado (multi-org) no futuro e o cache velho seria o mesmo bug do
 * seletor de organization.
 *
 * Cnpj/Telefone/Endereco (sprint-11) são opcionais — "configurar depois": ninguém trava no
 * onboarding só porque não tem o CNPJ em mãos agora.
 */
export function CreateOrganizationForm({ onCreated }: CreateOrganizationFormProps) {
  const queryClient = useQueryClient()
  const setSession = useAuthStore((s) => s.setSession)
  const [serverError, setServerError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  const mutation = useMutation({
    mutationFn: (values: FormValues) => createOrganization(values),
    onSuccess: (result) => {
      setSession({ accessToken: result.accessToken, refreshToken: result.refreshToken })
      queryClient.clear()
      onCreated()
    },
    onError: (error) => setServerError(getApiErrorMessage(error)),
  })

  const onSubmit = (values: FormValues) => {
    setServerError(null)
    mutation.mutate(values)
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
      <div>
        <Label htmlFor="nomeOrganizacao">Nome da empresa</Label>
        <Input
          id="nomeOrganizacao"
          placeholder="Ex.: Clínica Sorriso Feliz"
          error={errors.nome?.message}
          {...register('nome')}
        />
      </div>

      <div>
        <Label htmlFor="cnpjOrganizacao">CNPJ (opcional)</Label>
        <Input id="cnpjOrganizacao" placeholder="00.000.000/0000-00" {...register('cnpj')} />
      </div>

      <div>
        <Label htmlFor="telefoneOrganizacao">Telefone (opcional)</Label>
        <Input id="telefoneOrganizacao" placeholder="11999999999" {...register('telefone')} />
      </div>

      <div>
        <Label htmlFor="enderecoOrganizacao">Endereço (opcional)</Label>
        <Input id="enderecoOrganizacao" placeholder="Rua, número, cidade" {...register('endereco')} />
      </div>

      <p className="text-xs text-ink-muted">Só o nome é obrigatório agora — o resto dá pra configurar depois.</p>

      {serverError && <p className="text-sm text-danger">{serverError}</p>}

      <Button type="submit" className="w-full" disabled={mutation.isPending}>
        {mutation.isPending ? 'Criando…' : 'Criar empresa'}
      </Button>
    </form>
  )
}
