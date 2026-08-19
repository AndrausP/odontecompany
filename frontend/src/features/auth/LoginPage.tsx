import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation } from '@tanstack/react-query'
import { login } from './api'
import { useAuthStore } from '../../lib/auth-store'
import { getApiErrorMessage } from '../../lib/query-client'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { Label } from '../../components/ui/Label'

const loginSchema = z.object({
  email: z.string().min(1, 'Email é obrigatório').email('Email inválido'),
  password: z.string().min(1, 'Senha é obrigatória'),
})

type LoginForm = z.infer<typeof loginSchema>

export function LoginPage() {
  const navigate = useNavigate()
  const setSession = useAuthStore((s) => s.setSession)
  const [serverError, setServerError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginForm>({ resolver: zodResolver(loginSchema) })

  const mutation = useMutation({
    mutationFn: login,
    onSuccess: (result) => {
      setSession(result)
      navigate('/agenda', { replace: true })
    },
    onError: (error) => setServerError(getApiErrorMessage(error)),
  })

  const onSubmit = (values: LoginForm) => {
    setServerError(null)
    mutation.mutate(values)
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-surface px-4">
      <div className="w-full max-w-sm rounded-lg border border-border bg-surface-raised p-8 shadow-sm">
        <h1 className="mb-1 text-xl font-semibold text-ink">OdontoPlatform</h1>
        <p className="mb-6 text-sm text-ink-muted">Entrar na sua conta</p>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <div>
            <Label htmlFor="email">Email</Label>
            <Input
              id="email"
              type="email"
              autoComplete="username"
              error={errors.email?.message}
              {...register('email')}
            />
          </div>

          <div>
            <Label htmlFor="password">Senha</Label>
            <Input
              id="password"
              type="password"
              autoComplete="current-password"
              error={errors.password?.message}
              {...register('password')}
            />
          </div>

          {serverError && <p className="text-sm text-danger">{serverError}</p>}

          <Button type="submit" className="w-full" disabled={mutation.isPending}>
            {mutation.isPending ? 'Entrando…' : 'Entrar'}
          </Button>
        </form>

        <p className="mt-4 text-center text-sm text-ink-muted">
          Ainda não tem uma conta?{' '}
          <Link to="/signup" className="font-medium text-brand hover:text-brand-hover">
            Criar conta
          </Link>
        </p>
      </div>
    </div>
  )
}
