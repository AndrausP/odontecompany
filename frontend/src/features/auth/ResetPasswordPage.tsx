import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation } from '@tanstack/react-query'
import { resetPassword } from './api'
import { getApiErrorMessage } from '../../lib/query-client'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { Label } from '../../components/ui/Label'

const schema = z
  .object({
    novaSenha: z.string().min(8, 'Senha deve ter ao menos 8 caracteres'),
    confirmarSenha: z.string().min(1, 'Confirme a senha'),
  })
  .refine((values) => values.novaSenha === values.confirmarSenha, {
    message: 'As senhas não coincidem',
    path: ['confirmarSenha'],
  })
type FormValues = z.infer<typeof schema>

/**
 * Conclui a redefinição — `token` vem da URL (`?token=...`), o link que `LoggingPasswordResetNotifier`
 * loga hoje (sem provider de email real ainda, mesma dívida do convite). Sucesso manda pro
 * /login já com a senha nova; o backend revoga todas as sessões antigas.
 */
export function ResetPasswordPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token')

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  const mutation = useMutation({
    mutationFn: (values: FormValues) => resetPassword(token ?? '', values.novaSenha),
    onSuccess: () => navigate('/login', { replace: true }),
  })

  if (!token) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-surface px-4">
        <div className="w-full max-w-sm rounded-lg border border-border bg-surface-raised p-8 text-center shadow-sm">
          <h1 className="mb-2 text-lg font-semibold text-ink">Link inválido</h1>
          <p className="mb-4 text-sm text-ink-secondary">
            Este link de redefinição está incompleto. Peça um novo.
          </p>
          <Link to="/esqueci-senha" className="text-sm font-medium text-brand hover:text-brand-hover">
            Pedir novo link
          </Link>
        </div>
      </div>
    )
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-surface px-4">
      <div className="w-full max-w-sm rounded-lg border border-border bg-surface-raised p-8 shadow-sm">
        <h1 className="mb-1 text-xl font-semibold text-ink">OdontoPlatform</h1>
        <p className="mb-6 text-sm text-ink-muted">Escolher senha nova</p>

        <form onSubmit={handleSubmit((values) => mutation.mutate(values))} className="space-y-4" noValidate>
          <div>
            <Label htmlFor="novaSenha">Senha nova</Label>
            <Input
              id="novaSenha"
              type="password"
              autoComplete="new-password"
              error={errors.novaSenha?.message}
              {...register('novaSenha')}
            />
          </div>
          <div>
            <Label htmlFor="confirmarSenha">Confirmar senha</Label>
            <Input
              id="confirmarSenha"
              type="password"
              autoComplete="new-password"
              error={errors.confirmarSenha?.message}
              {...register('confirmarSenha')}
            />
          </div>

          {mutation.isError && <p className="text-sm text-danger">{getApiErrorMessage(mutation.error)}</p>}

          <Button type="submit" className="w-full" disabled={mutation.isPending}>
            {mutation.isPending ? 'Salvando…' : 'Redefinir senha'}
          </Button>
        </form>
      </div>
    </div>
  )
}
