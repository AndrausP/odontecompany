import { Navigate, Route, Routes } from 'react-router-dom'
import { AppLayout } from './components/layout/AppLayout'
import { ProtectedRoute } from './components/layout/ProtectedRoute'
import { RequireOrganization } from './components/layout/RequireOrganization'
import { LoginPage } from './features/auth/LoginPage'
import { SignupPage } from './features/auth/SignupPage'
import { OnboardingPage } from './features/organizations/OnboardingPage'
import { InvitesPage } from './features/invites/InvitesPage'
import { AgendaPage } from './features/scheduling/AgendaPage'
import { PatientsPage } from './features/patients/PatientsPage'
import { FinanceiroPage } from './features/billing/FinanceiroPage'
import { EstoquePage } from './features/estoque/EstoquePage'
import { ReportsPage } from './features/reports/ReportsPage'
import { ComprovantePage } from './features/reports/ComprovantePage'

export function App() {
  return (
    <Routes>
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
        <Route index element={<Navigate to="/agenda" replace />} />
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

      <Route path="*" element={<Navigate to="/agenda" replace />} />
    </Routes>
  )
}
