import { Link } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation } from '@tanstack/react-query'
import { forgotPassword } from './api'
import { getApiErrorMessage } from '../../lib/query-client'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { Label } from '../../components/ui/Label'

const schema = z.object({
  email: z.string().min(1, 'Email é obrigatório').email('Email inválido'),
})
type FormValues = z.infer<typeof schema>

/**
 * Pedido de redefinição de senha (auditoria pré-venda) — antes disso não existia NENHUM jeito de
 * recuperar acesso: quem esquecia a senha ficava travado pra sempre. Resposta do backend é sempre
 * a mesma (anti-enumeração) — a UI mostra sucesso genérico independente do email existir.
 */
export function ForgotPasswordPage() {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({ resolver: zodResolver(schema) })

  const mutation = useMutation({ mutationFn: (values: FormValues) => forgotPassword(values.email) })

  return (
    <div className="flex min-h-screen items-center justify-center bg-surface px-4">
      <div className="w-full max-w-sm rounded-lg border border-border bg-surface-raised p-8 shadow-sm">
        <h1 className="mb-1 text-xl font-semibold text-ink">OdontoPlatform</h1>
        <p className="mb-6 text-sm text-ink-muted">Recuperar senha</p>

        {mutation.isSuccess ? (
          <p className="text-sm text-ink-secondary">
            Se <strong className="text-ink">{mutation.variables?.email}</strong> tiver uma conta, enviamos as
            instruções de redefinição pra esse email.
          </p>
        ) : (
          <form onSubmit={handleSubmit((values) => mutation.mutate(values))} className="space-y-4" noValidate>
            <p className="text-sm text-ink-secondary">
              Informe o email da sua conta — enviamos um link pra você escolher uma senha nova.
            </p>
            <div>
              <Label htmlFor="email">Email</Label>
              <Input id="email" type="email" autoComplete="username" error={errors.email?.message} {...register('email')} />
            </div>

            {mutation.isError && <p className="text-sm text-danger">{getApiErrorMessage(mutation.error)}</p>}

            <Button type="submit" className="w-full" disabled={mutation.isPending}>
              {mutation.isPending ? 'Enviando…' : 'Enviar instruções'}
            </Button>
          </form>
        )}

        <p className="mt-4 text-center text-sm text-ink-muted">
          <Link to="/login" className="font-medium text-brand hover:text-brand-hover">
            Voltar pro login
          </Link>
        </p>
      </div>
    </div>
  )
}
