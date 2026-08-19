import { Link } from 'react-router-dom'
import { Calendar, CheckCircle2, Package, ShieldCheck, Users, Wallet } from 'lucide-react'
import { cn } from '../../lib/cn'

// Mesmas classes de Button.tsx (variant="primary"/"secondary") — não dá pra reusar o componente
// Button aqui porque ele renderiza <button>, e o CTA precisa ser navegação real (<Link>, cai bem
// pra SEO/"abrir em nova aba"/etc); <a>/<Link> dentro de <button> é HTML inválido (interativo
// aninhado). Duplicação pequena e deliberada — se Button ganhar suporte a "as" no futuro, troca.
const ctaPrimary =
  'inline-flex items-center justify-center gap-2 rounded-md bg-brand-gradient px-4 py-2 text-sm font-medium text-on-brand transition-colors hover:brightness-90 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-focus-ring'
const ctaSecondary =
  'inline-flex items-center justify-center gap-2 rounded-md border border-border-strong bg-surface-raised px-4 py-2 text-sm font-medium text-ink transition-colors hover:bg-surface-sunken focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-focus-ring'

/**
 * Home pública (sprint-11) — hoje o app caía direto em /login sem nenhuma apresentação. Composição
 * inspirada na home não-logada de players tipo Spotify (nav fixa em cima, hero, seções abaixo,
 * conteúdo mais denso quanto mais desce) — sem copiar marca/conteúdo, só o formato. Pricing é
 * vitrine estática nessa rodada (sprint-11 grill-me): sem checkout/gateway de pagamento, "Assinar"
 * leva pro /signup. Rota pública "/" (App.tsx): autenticado pula pra /agenda automático.
 */

const FEATURES = [
  {
    icon: Calendar,
    title: 'Agenda sem choque de horário',
    description: 'Marca, remarca e confirma consulta em segundos — cada profissional com sua própria grade.',
  },
  {
    icon: Users,
    title: 'Prontuário e cadastro centralizados',
    description: 'Histórico do paciente, CPF, contato e consentimento LGPD num lugar só, sem planilha solta.',
  },
  {
    icon: Wallet,
    title: 'Financeiro sem sustinha',
    description: 'Parcelas, convênios e comissão por profissional calculados automaticamente a cada atendimento.',
  },
  {
    icon: Package,
    title: 'Estoque de insumo sob controle',
    description: 'Saiba o que tá acabando antes de faltar na hora do atendimento.',
  },
] as const

interface PlanFeatureList {
  readonly items: readonly string[]
}

interface Plan extends PlanFeatureList {
  readonly name: string
  readonly price: string
  readonly period: string
  readonly description: string
  readonly highlighted?: boolean
}

const PLANS: readonly Plan[] = [
  {
    name: 'Starter',
    price: 'R$ 129',
    period: '/mês · 1 unidade',
    description: 'Pra clínica única começar a sair da planilha.',
    items: ['Agenda e pacientes ilimitados', '1 unidade', 'Até 3 usuários', 'Suporte por email'],
  },
  {
    name: 'Profissional',
    price: 'R$ 349',
    period: '/mês · até 3 unidades',
    description: 'Pra quem já administra mais de uma unidade.',
    items: [
      'Tudo do Starter',
      'Até 3 unidades na mesma empresa',
      'Financeiro completo (convênios, comissão)',
      'Controle de estoque',
      'Usuários ilimitados',
    ],
    highlighted: true,
  },
  {
    name: 'Rede',
    price: 'Sob consulta',
    period: 'unidades ilimitadas',
    description: 'Pra rede de clínicas com várias unidades sob a mesma empresa.',
    items: ['Tudo do Profissional', 'Unidades ilimitadas', 'Relatórios avançados por unidade', 'Suporte prioritário'],
  },
]

function NavBar() {
  return (
    <header className="sticky top-0 z-30 border-b border-border bg-surface/90 backdrop-blur">
      <div className="mx-auto flex h-16 max-w-6xl items-center justify-between px-4">
        <span className="text-lg font-semibold text-brand">OdontoPlatform</span>
        <nav className="flex items-center gap-2 sm:gap-4">
          <a href="#planos" className="hidden text-sm font-medium text-ink-secondary hover:text-ink sm:inline">
            Planos
          </a>
          <Link to="/login" className="text-sm font-medium text-ink-secondary hover:text-ink">
            Entrar
          </Link>
          <Link to="/signup" className={ctaPrimary}>
            Criar conta
          </Link>
        </nav>
      </div>
    </header>
  )
}

function Hero() {
  return (
    <section className="mx-auto max-w-6xl px-4 py-16 text-center sm:py-24">
      <p className="mb-3 text-sm font-semibold uppercase tracking-wide text-brand">Gestão odontológica</p>
      <h1 className="mx-auto max-w-3xl text-3xl font-semibold tracking-tight text-ink sm:text-5xl">
        Sua clínica organizada do agendamento ao financeiro
      </h1>
      <p className="mx-auto mt-4 max-w-xl text-base text-ink-muted sm:text-lg">
        Agenda, prontuário, financeiro e estoque numa plataforma só — pra você parar de perder tempo
        (e dinheiro) com planilha e caderno.
      </p>
      <div className="mt-8 flex flex-col items-center justify-center gap-3 sm:flex-row">
        <Link to="/signup" className={cn(ctaPrimary, 'w-full px-6 py-3 text-base sm:w-auto')}>
          Começar agora — grátis
        </Link>
        <a href="#planos" className={cn(ctaSecondary, 'w-full px-6 py-3 text-base sm:w-auto')}>
          Ver planos
        </a>
      </div>
      <p className="mt-4 text-xs text-ink-muted">Sem cartão de crédito pra testar.</p>
    </section>
  )
}

function Features() {
  return (
    <section className="border-t border-border bg-surface-raised py-16">
      <div className="mx-auto max-w-6xl px-4">
        <h2 className="text-center text-2xl font-semibold text-ink sm:text-3xl">Tudo que sua clínica precisa</h2>
        <div className="mt-10 grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
          {FEATURES.map(({ icon: Icon, title, description }) => (
            <div key={title} className="rounded-lg border border-border bg-surface p-5">
              <div className="mb-3 flex size-10 items-center justify-center rounded-md bg-brand-subtle text-brand">
                <Icon size={20} />
              </div>
              <h3 className="text-sm font-semibold text-ink">{title}</h3>
              <p className="mt-1 text-sm text-ink-muted">{description}</p>
            </div>
          ))}
        </div>
      </div>
    </section>
  )
}

function PlanCard({ plan }: { plan: Plan }) {
  return (
    <div
      className={cn(
        'flex flex-col rounded-xl border p-6',
        plan.highlighted ? 'border-brand bg-brand-subtle/40 shadow-lg' : 'border-border bg-surface-raised',
      )}
    >
      {plan.highlighted && (
        <span className="mb-3 inline-flex w-fit items-center rounded-full bg-brand px-2.5 py-0.5 text-xs font-semibold text-on-brand">
          Mais popular
        </span>
      )}
      <h3 className="text-lg font-semibold text-ink">{plan.name}</h3>
      <p className="mt-1 text-sm text-ink-muted">{plan.description}</p>
      <div className="mt-4 flex items-baseline gap-1">
        <span className="text-3xl font-semibold text-ink">{plan.price}</span>
        <span className="text-sm text-ink-muted">{plan.period}</span>
      </div>
      <ul className="mt-6 flex-1 space-y-2.5">
        {plan.items.map((item) => (
          <li key={item} className="flex items-start gap-2 text-sm text-ink-secondary">
            <CheckCircle2 size={16} className="mt-0.5 shrink-0 text-brand" />
            <span>{item}</span>
          </li>
        ))}
      </ul>
      <Link to="/signup" className={cn(plan.highlighted ? ctaPrimary : ctaSecondary, 'mt-6 w-full')}>
        Assinar {plan.name}
      </Link>
    </div>
  )
}

function Pricing() {
  return (
    <section id="planos" className="py-16">
      <div className="mx-auto max-w-6xl px-4">
        <h2 className="text-center text-2xl font-semibold text-ink sm:text-3xl">Planos pra cada tamanho de clínica</h2>
        <p className="mx-auto mt-2 max-w-xl text-center text-sm text-ink-muted">
          Comece numa unidade, cresça pra rede sem trocar de plataforma.
        </p>
        <div className="mt-10 grid gap-6 lg:grid-cols-3">
          {PLANS.map((plan) => (
            <PlanCard key={plan.name} plan={plan} />
          ))}
        </div>
      </div>
    </section>
  )
}

function Footer() {
  return (
    <footer className="border-t border-border bg-surface-raised py-8">
      <div className="mx-auto flex max-w-6xl flex-col items-center justify-between gap-3 px-4 text-sm text-ink-muted sm:flex-row">
        <div className="flex items-center gap-2">
          <ShieldCheck size={16} />
          <span>Dados protegidos, consentimento LGPD nativo no cadastro do paciente.</span>
        </div>
        <span>© {new Date().getFullYear()} OdontoPlatform</span>
      </div>
    </footer>
  )
}

export function LandingPage() {
  return (
    <div className="min-h-screen bg-surface">
      <NavBar />
      <Hero />
      <Features />
      <Pricing />
      <Footer />
    </div>
  )
}
