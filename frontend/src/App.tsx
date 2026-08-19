import { lazy, Suspense } from 'react'
import { Navigate, Route, Routes } from 'react-router-dom'
import { AppLayout } from './components/layout/AppLayout'
import { ProtectedRoute } from './components/layout/ProtectedRoute'
import { RequireOrganization } from './components/layout/RequireOrganization'
import { useAuthStore } from './lib/auth-store'

// Cada rota de página vira chunk próprio (`vite build` alertava >500 kB num bundle só,
// sprint-11/033) — o shell (AppLayout/ProtectedRoute/RequireOrganization) continua eager,
// só a página muda por chunk. AppLayout renderiza a nav antes da página carregar.
const LandingPage = lazy(() =>
  import('./features/marketing/LandingPage').then((m) => ({ default: m.LandingPage })),
)
const LoginPage = lazy(() => import('./features/auth/LoginPage').then((m) => ({ default: m.LoginPage })))
const SignupPage = lazy(() => import('./features/auth/SignupPage').then((m) => ({ default: m.SignupPage })))
const OnboardingPage = lazy(() =>
  import('./features/organizations/OnboardingPage').then((m) => ({ default: m.OnboardingPage })),
)
const InvitesPage = lazy(() => import('./features/invites/InvitesPage').then((m) => ({ default: m.InvitesPage })))
const AgendaPage = lazy(() => import('./features/scheduling/AgendaPage').then((m) => ({ default: m.AgendaPage })))
const PatientsPage = lazy(() => import('./features/patients/PatientsPage').then((m) => ({ default: m.PatientsPage })))
const FinanceiroPage = lazy(() =>
  import('./features/billing/FinanceiroPage').then((m) => ({ default: m.FinanceiroPage })),
)
const EstoquePage = lazy(() => import('./features/estoque/EstoquePage').then((m) => ({ default: m.EstoquePage })))
const ReportsPage = lazy(() => import('./features/reports/ReportsPage').then((m) => ({ default: m.ReportsPage })))
const ComprovantePage = lazy(() =>
  import('./features/reports/ComprovantePage').then((m) => ({ default: m.ComprovantePage })),
)

function RouteFallback() {
  // Mesmo idioma de loading já usado dentro das páginas (InvitesPage, ComprovantePage) —
  // aqui é o fallback de troca de rota, antes do chunk/JS da página terminar de baixar.
  return (
    <div className="flex h-full items-center justify-center p-8">
      <p className="text-sm text-ink-muted">Carregando…</p>
    </div>
  )
}

/**
 * "/" pública (sprint-11) — antes o app caía direto em /login sem apresentação nenhuma. Quem já
 * tem sessão válida pula direto pro app (não faz sentido logado ver a landing de novo); sem
 * sessão, é a vitrine (nav + hero + planos, LandingPage).
 */
function LandingGate() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated())
  return isAuthenticated ? <Navigate to="/agenda" replace /> : <LandingPage />
}

export function App() {
  return (
    <Suspense fallback={<RouteFallback />}>
      <Routes>
        <Route path="/" element={<LandingGate />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/signup" element={<SignupPage />} />

        {/* Sem RequireOrganization de propósito — é exatamente a rota que precisa funcionar pra
            quem ainda NÃO tem organization nenhuma (signup recém-feito). */}
        <Route
          path="/onboarding"
          element={
            <ProtectedRoute>
              <OnboardingPage />
            </ProtectedRoute>
          }
        />

        <Route
          element={
            <ProtectedRoute>
              <RequireOrganization>
                <AppLayout />
              </RequireOrganization>
            </ProtectedRoute>
          }
        >
          <Route path="/agenda" element={<AgendaPage />} />
          <Route path="/pacientes" element={<PatientsPage />} />
          <Route path="/convites" element={<InvitesPage />} />
          <Route
            path="/financeiro"
            element={
              <ProtectedRoute roles={['Owner', 'Admin', 'Recepcao']}>
                <FinanceiroPage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/estoque"
            element={
              <ProtectedRoute roles={['Owner', 'Admin', 'Recepcao']}>
                <EstoquePage />
              </ProtectedRoute>
            }
          />
          <Route
            path="/relatorios"
            element={
              <ProtectedRoute roles={['Owner', 'Admin']}>
                <ReportsPage />
              </ProtectedRoute>
            }
          />
          {/* Sem `roles` no ProtectedRoute de propósito (task 028, critério 4): pra Recepcao,
              `roles` faria um redirect silencioso pra /agenda — a página precisa mostrar uma
              mensagem clara de "sem permissão" em vez disso, então o check de role é feito dentro
              do próprio ComprovantePage. */}
          <Route path="/comprovante" element={<ComprovantePage />} />
        </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </Suspense>
  )
}
