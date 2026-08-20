import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation } from '@tanstack/react-query'
import { signup } from './api'
import { useAuthStore } from '../../lib/auth-store'
import { getApiErrorMessage } from '../../lib/query-client'
import { Button } from '../../components/ui/Button'
import { Input } from '../../components/ui/Input'
import { Label } from '../../components/ui/Label'

// Espelha SignupCommandValidator do backend (Nome max 150, Password min 8) — validação client-side
// é só UX, o backend valida de novo e é quem de fato barra.
const signupSchema = z
  .object({
    nome: z.string().min(1, 'Nome é obrigatório').max(150, 'Nome não pode ter mais de 150 caracteres'),
    email: z.string().min(1, 'Email é obrigatório').email('Email inválido'),
    password: z.string().min(8, 'Senha deve ter ao menos 8 caracteres'),
    confirmarSenha: z.string().min(1, 'Confirme a senha'),
  })
  .refine((values) => values.password === values.confirmarSenha, {
    message: 'As senhas não coincidem',
    path: ['confirmarSenha'],
  })

type SignupForm = z.infer<typeof signupSchema>

export function SignupPage() {
  const navigate = useNavigate()
  const setSession = useAuthStore((s) => s.setSession)
  const [serverError, setServerError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<SignupForm>({ resolver: zodResolver(signupSchema) })

  const mutation = useMutation({
    mutationFn: signup,
    onSuccess: (result) => {
      // Login automático: usuário recém-criado ainda não tem organization (accessToken sem claim
      // organization_id, refreshToken sempre null — ver SignupResultDto) — ProtectedRoute só exige
      // token válido, não exige organization, então isso já basta pra entrar autenticado. O
      // RequireOrganization no App.tsx redireciona pra /onboarding sozinho.
      setSession({ accessToken: result.accessToken, refreshToken: result.refreshToken })
      navigate('/onboarding', { replace: true })
    },
    onError: (error) => setServerError(getApiErrorMessage(error)),
  })

  const onSubmit = (values: SignupForm) => {
    setServerError(null)
    mutation.mutate({ nome: values.nome, email: values.email, password: values.password })
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-surface px-4">
      <div className="w-full max-w-sm rounded-lg border border-border bg-surface-raised p-8 shadow-sm">
        <h1 className="mb-1 text-xl font-semibold text-ink">OdontoPlatform</h1>
        <p className="mb-6 text-sm text-ink-muted">Criar uma conta</p>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
          <div>
            <Label htmlFor="nome">Nome</Label>
            <Input id="nome" autoComplete="name" error={errors.nome?.message} {...register('nome')} />
          </div>

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
              autoComplete="new-password"
              error={errors.password?.message}
              {...register('password')}
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

          {serverError && <p className="text-sm text-danger">{serverError}</p>}

          <Button type="submit" className="w-full" disabled={mutation.isPending}>
            {mutation.isPending ? 'Criando conta…' : 'Criar conta'}
          </Button>

          {/* Auditoria pré-venda: cadastro não tinha nenhum link de Termos/Privacidade — as
              páginas agora existem (features/marketing/LegalPages.tsx), isso é o link. */}
          <p className="text-center text-xs text-ink-muted">
            Ao criar conta, você concorda com nossos{' '}
            <Link to="/termos" className="text-brand hover:text-brand-hover">
              Termos de Uso
            </Link>{' '}
            e nossa{' '}
            <Link to="/privacidade" className="text-brand hover:text-brand-hover">
              Política de Privacidade
            </Link>
            .
          </p>
        </form>

        <p className="mt-4 text-center text-sm text-ink-muted">
          Já tem uma conta?{' '}
          <Link to="/login" className="font-medium text-brand hover:text-brand-hover">
            Entrar
          </Link>
        </p>
      </div>
    </div>
  )
}
