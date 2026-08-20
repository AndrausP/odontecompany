import { useState, type ComponentType } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import {
  BarChart3,
  Building2,
  Calendar,
  LogOut,
  Mail,
  Menu,
  MonitorSmartphone,
  Moon,
  PanelLeftClose,
  PanelLeftOpen,
  Package,
  Receipt,
  Settings,
  Sun,
  Users,
  Wallet,
  X,
} from 'lucide-react'
import { useAuthStore } from '../../lib/auth-store'
import { useMe } from '../../features/auth/useMe'
import { useThemeStore, type Theme } from '../../lib/theme-store'
import { useSidebarStore } from '../../lib/sidebar-store'
import { OrgSwitcher } from '../../features/organizations/OrgSwitcher'
import { cn } from '../../lib/cn'
import type { Role } from '../../types/auth'

interface NavItem {
  to: string
  label: string
  icon: ComponentType<{ size?: number; className?: string }>
  roles?: Role[]
}

interface NavGroup {
  label: string
  items: NavItem[]
}

/**
 * Nav agrupada — spec 024 §7.2. "Clínico" é visível a todos os papéis; "Gestão" some inteiro se
 * nenhum item sobrar visível pro role do usuário (ex.: Dentista não vê o grupo).
 *
 * Comprovante de Pagamento (task 026, rota da task 028): a task 028 define a rota como
 * `/comprovante` (`docs/tasks/028-frontend-comprovante-pagamento.md`, critério 1). Essa rota AINDA
 * não existe em `App.tsx` — até a 028 ser implementada, clicar no item cai no catch-all
 * (`<Route path="*" .../>`) e redireciona pra `/agenda`, sem 404 visível. Oculto pra `Recepcao`
 * (não gera comissão — mesma regra do backend, ver task 023).
 */
const navGroups: NavGroup[] = [
  {
    label: 'Clínico',
    items: [
      { to: '/agenda', label: 'Agenda', icon: Calendar },
      { to: '/pacientes', label: 'Pacientes', icon: Users },
    ],
  },
  {
    label: 'Gestão',
    items: [
      { to: '/financeiro', label: 'Financeiro', icon: Wallet, roles: ['Owner', 'Admin', 'Recepcao'] },
      { to: '/estoque', label: 'Estoque', icon: Package, roles: ['Owner', 'Admin', 'Recepcao'] },
      { to: '/relatorios', label: 'Relatórios', icon: BarChart3, roles: ['Owner', 'Admin'] },
      { to: '/comprovante', label: 'Comprovante de Pagamento', icon: Receipt, roles: ['Owner', 'Admin', 'Dentista'] },
    ],
  },
  {
    label: 'Sistema',
    items: [{ to: '/configuracoes', label: 'Configurações', icon: Settings, roles: ['Owner', 'Admin'] }],
  },
]

const THEME_ORDER: Theme[] = ['light', 'system', 'dark']
const THEME_ICONS: Record<Theme, ComponentType<{ size?: number; className?: string }>> = {
  light: Sun,
  system: MonitorSmartphone,
  dark: Moon,
}
const THEME_LABELS: Record<Theme, string> = {
  light: 'Tema claro',
  system: 'Tema do sistema',
  dark: 'Tema escuro',
}

const focusRing =
  'focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-focus-ring'

/**
 * Substitui o <Outlet/> quando o usuário entrou sem organization (pulou o onboarding — sprint-11,
 * RequireOrganization deixa passar mesmo assim). Nenhuma página real renderiza: todas dependem de
 * organizationId vindo do JWT, que não existe nesse estado, e sairiam batendo 401 à toa. Nav/tema/
 * logout continuam funcionando ao redor — só o conteúdo principal vira este convite pra criar a
 * empresa quando o usuário decidir.
 */
function EmptyOrganizationState() {
  return (
    <div className="flex h-full min-h-[60vh] flex-col items-center justify-center gap-4 text-center">
      <div className="flex size-14 items-center justify-center rounded-full bg-brand-subtle text-brand">
        <Building2 size={28} />
      </div>
      <div className="space-y-1">
        <h1 className="text-lg font-semibold text-ink">Crie sua empresa pra começar</h1>
        <p className="max-w-sm text-sm text-ink-muted">
          Agenda, pacientes, financeiro e estoque só ficam disponíveis depois que sua empresa e sua
          primeira unidade existirem.
        </p>
      </div>
      <NavLink
        to="/onboarding"
        className={cn(
          'rounded-md bg-brand px-4 py-2 text-sm font-medium text-on-brand transition-colors hover:bg-brand-hover',
          focusRing,
        )}
      >
        Criar empresa
      </NavLink>
    </div>
  )
}

/** Toggle de tema no rodapé (spec 024 §7.6). Colapsado: botão único cíclico (a spec permite
 * segmented control OU botão cíclico — aqui a escolha muda com o espaço disponível). */
function ThemeToggle({ collapsed }: { collapsed: boolean }) {
  const theme = useThemeStore((s) => s.theme)
  const setTheme = useThemeStore((s) => s.setTheme)

  if (collapsed) {
    const Icon = THEME_ICONS[theme]
    const cycle = () => setTheme(THEME_ORDER[(THEME_ORDER.indexOf(theme) + 1) % THEME_ORDER.length])
    return (
      <button
        type="button"
        onClick={cycle}
        title={`${THEME_LABELS[theme]} — clique para alternar`}
        aria-label="Alternar tema"
        className={cn('flex w-full items-center justify-center rounded-md bg-brand-subtle p-1.5 text-brand', focusRing)}
      >
        <Icon size={16} />
      </button>
    )
  }

  return (
    <div className="flex items-center gap-1 rounded-md border border-border bg-surface-sunken p-1" role="group" aria-label="Tema">
      {THEME_ORDER.map((value) => {
        const Icon = THEME_ICONS[value]
        const active = theme === value
        return (
          <button
            key={value}
            type="button"
            aria-label={THEME_LABELS[value]}
            aria-pressed={active}
            onClick={() => setTheme(value)}
            className={cn(
              'flex flex-1 items-center justify-center rounded p-1.5 transition-colors',
              focusRing,
              active ? 'bg-brand-subtle text-brand' : 'text-ink-secondary hover:text-ink',
            )}
          >
            <Icon size={16} />
          </button>
        )
      })}
    </div>
  )
}

function NavItemLink({ item, collapsed, onNavigate }: { item: NavItem; collapsed: boolean; onNavigate?: () => void }) {
  const Icon = item.icon
  return (
    <NavLink
      to={item.to}
      onClick={onNavigate}
      title={collapsed ? item.label : undefined}
      className={({ isActive }) =>
        cn(
          'flex items-center gap-2.5 rounded-md px-2.5 py-2 text-sm font-medium transition-colors',
          focusRing,
          collapsed && 'justify-center px-0',
          // sólido (não mais bg-brand-subtle) — task 032, pedido do usuário pra bater com a foto
          // de referência (pill de item ativo cheio, não pálido)
          isActive ? 'bg-brand text-on-brand' : 'text-ink-secondary hover:bg-surface-sunken hover:text-ink',
        )
      }
    >
      {({ isActive }) => (
        <>
          {/* Chip do ícone — pedido do usuário pra dar cara de "módulo" ao item de nav, não só
              texto+ícone soltos. Invertido quando ativo (mesma lógica do badge de Convites). */}
          <span
            className={cn(
              'flex size-6 shrink-0 items-center justify-center rounded-md',
              isActive ? 'bg-on-brand/15' : 'bg-surface-sunken',
            )}
          >
            <Icon size={14} className="shrink-0" />
          </span>
          {!collapsed && <span>{item.label}</span>}
        </>
      )}
    </NavLink>
  )
}

function ConvitesNavItem({
  collapsed,
  count,
  onNavigate,
}: {
  collapsed: boolean
  count: number
  onNavigate?: () => void
}) {
  return (
    <NavLink
      to="/convites"
      onClick={onNavigate}
      title={collapsed ? 'Convites' : undefined}
      className={({ isActive }) =>
        cn(
          'relative flex items-center rounded-md px-3 py-2 text-sm font-medium transition-colors',
          focusRing,
          collapsed ? 'justify-center' : 'justify-between',
          isActive ? 'bg-brand text-on-brand' : 'text-ink-secondary hover:bg-surface-sunken hover:text-ink',
        )
      }
    >
      {({ isActive }) => (
        <>
          <span className="flex items-center gap-2">
            <Mail size={16} className="shrink-0" />
            {!collapsed && <span>Convites</span>}
          </span>
          {count > 0 &&
            (collapsed ? (
              <span
                className={cn(
                  'absolute right-1 top-1 h-2 w-2 rounded-full',
                  // invertido quando o item já está ativo (fundo sólido bg-brand) — badge com a
                  // mesma cor do fundo ficaria invisível (task 032: item ativo virou sólido)
                  isActive ? 'bg-on-brand' : 'bg-brand',
                )}
                aria-hidden="true"
              />
            ) : (
              <span
                className={cn(
                  'rounded-full px-2 py-0.5 text-xs font-semibold',
                  isActive ? 'bg-on-brand text-brand' : 'bg-brand text-on-brand',
                )}
              >
                {count}
              </span>
            ))}
        </>
      )}
    </NavLink>
  )
}

interface SidebarNavProps {
  collapsed: boolean
  visibleGroups: NavGroup[]
  pendingInvitesCount: number
  onNavigate?: () => void
}

/** Itens agrupados + Convites (fora dos grupos, faixa própria — spec 024 §7.2). */
function SidebarNav({ collapsed, visibleGroups, pendingInvitesCount, onNavigate }: SidebarNavProps) {
  return (
    <nav className="flex-1 space-y-1 overflow-y-auto px-2 py-4">
      {visibleGroups.map((group, index) => (
        <div key={group.label} className={cn(collapsed && index > 0 && 'border-t border-border pt-2')}>
          {!collapsed && (
            <p className="px-3 pb-1 pt-3 text-xs font-semibold uppercase tracking-wide text-ink-muted first:pt-0">
              {group.label}
            </p>
          )}
          <div className="space-y-1">
            {group.items.map((item) => (
              <NavItemLink key={item.to} item={item} collapsed={collapsed} onNavigate={onNavigate} />
            ))}
          </div>
        </div>
      ))}

      <div className={cn('border-t border-border pt-2', collapsed ? '' : 'mt-2')}>
        <ConvitesNavItem collapsed={collapsed} count={pendingInvitesCount} onNavigate={onNavigate} />
      </div>
    </nav>
  )
}

interface SidebarFooterProps {
  collapsed: boolean
  role?: Role
  onLogout: () => void
}

function SidebarFooter({ collapsed, role, onLogout }: SidebarFooterProps) {
  return (
    <div className={cn('space-y-3 border-t border-border px-3 py-3', collapsed && 'px-2')}>
      <ThemeToggle collapsed={collapsed} />
      {!collapsed && <p className="truncate text-xs text-ink-muted">{role}</p>}
      <button
        type="button"
        onClick={onLogout}
        title={collapsed ? 'Sair' : undefined}
        className={cn(
          'flex w-full items-center gap-2 rounded-md text-xs font-medium text-ink-muted transition-colors hover:text-ink-secondary',
          focusRing,
          collapsed && 'justify-center',
        )}
      >
        <LogOut size={16} />
        {!collapsed && <span>Sair</span>}
      </button>
    </div>
  )
}

export function AppLayout() {
  const navigate = useNavigate()
  const claims = useAuthStore((s) => s.claims)
  const clearSession = useAuthStore((s) => s.clearSession)
  // Mesma queryKey ['me'] usada pelo RequireOrganization — cache já quente, sem request extra.
  const { data: me } = useMe()
  const collapsed = useSidebarStore((s) => s.collapsed)
  const toggleCollapsed = useSidebarStore((s) => s.toggleCollapsed)
  const [mobileOpen, setMobileOpen] = useState(false)

  // claims.role pode não existir (usuário sem organization ativa) — nunca assumir presente.
  const isItemVisible = (item: NavItem) => !item.roles || (claims?.role && item.roles.includes(claims.role))
  const visibleGroups = navGroups
    .map((group) => ({ ...group, items: group.items.filter(isItemVisible) }))
    .filter((group) => group.items.length > 0)
  const pendingInvitesCount = me?.pendingInvites.length ?? 0
  // Pulou o onboarding (sprint-11) — sem organization, nenhuma página de gestão tem o que buscar
  // (organizationId vem do JWT, que não existe nesse estado). `me` só fica undefined durante o
  // primeiro load (RequireOrganization já bloqueia até ter dado — nunca renderiza aqui vazio).
  const hasNoOrganization = me ? me.organizations.length === 0 : false

  const handleLogout = () => {
    clearSession()
    navigate('/login', { replace: true })
  }

  return (
    <div className="flex min-h-screen flex-col bg-surface lg:flex-row">
      {/* Barra superior mobile (<lg) — spec 024 §7.5. Hambúrguer + logo; OrgSwitcher e toggle de
          tema ficam dentro do drawer, não aqui, pra não competir por espaço em tela estreita. */}
      <header className="flex h-14 items-center gap-3 border-b border-border bg-surface-raised px-4 lg:hidden">
        <button
          type="button"
          onClick={() => setMobileOpen(true)}
          aria-label="Abrir menu"
          className={cn('rounded-md p-1.5 text-ink-secondary hover:bg-surface-sunken hover:text-ink', focusRing)}
        >
          <Menu size={20} />
        </button>
        <span className="text-sm font-semibold text-brand">OdontoPlatform</span>
      </header>

      {/* Drawer mobile — não é bottom bar (spec 024 §7.5: com 2 grupos + convites, bottom bar
          apertaria ou esconderia itens atrás de "mais"). */}
      {mobileOpen && (
        <div className="fixed inset-0 z-40 lg:hidden">
          <div className="fixed inset-0 bg-ink/40" onClick={() => setMobileOpen(false)} aria-hidden="true" />
          <div className="fixed inset-y-0 left-0 z-50 flex w-64 flex-col bg-surface-raised shadow-xl">
            <div className="flex items-center justify-between border-b border-border px-4 py-4">
              <span className="text-sm font-semibold text-brand">OdontoPlatform</span>
              <button
                type="button"
                onClick={() => setMobileOpen(false)}
                aria-label="Fechar menu"
                className={cn('rounded-md p-1.5 text-ink-secondary hover:bg-surface-sunken hover:text-ink', focusRing)}
              >
                <X size={20} />
              </button>
            </div>
            <div className="space-y-2 border-b border-border px-4 py-4">
              {me && <OrgSwitcher organizations={me.organizations} activeOrganizationId={me.activeOrganizationId} />}
            </div>
            <SidebarNav
              collapsed={false}
              visibleGroups={visibleGroups}
              pendingInvitesCount={pendingInvitesCount}
              onNavigate={() => setMobileOpen(false)}
            />
            <SidebarFooter collapsed={false} role={claims?.role} onLogout={handleLogout} />
          </div>
        </div>
      )}

      {/* Sidebar desktop (≥lg) — spec 024 §7.2/§7.4. `relative overflow-hidden`: task 032, contém
          o flourish decorativo abaixo (blob borrado, só visível no dark). */}
      <aside
        className={cn(
          'relative hidden shrink-0 flex-col overflow-hidden border-r border-border bg-surface-raised transition-[width] duration-150 lg:flex',
          collapsed ? 'w-16' : 'w-56',
        )}
      >
        <div aria-hidden="true" className="sidebar-flourish pointer-events-none absolute inset-x-0 bottom-0 -z-10 h-72" />
        <div className={cn('space-y-2 border-b border-border px-3 py-4', collapsed && 'px-2')}>
          <div className={cn('flex items-center', collapsed ? 'flex-col gap-2' : 'justify-between')}>
            {!collapsed && <span className="text-sm font-semibold text-brand">OdontoPlatform</span>}
            <button
              type="button"
              onClick={toggleCollapsed}
              aria-label={collapsed ? 'Expandir menu' : 'Recolher menu'}
              className={cn('rounded-md p-1.5 text-ink-secondary hover:bg-surface-sunken hover:text-ink', focusRing)}
            >
              {collapsed ? <PanelLeftOpen size={16} /> : <PanelLeftClose size={16} />}
            </button>
          </div>
          {!collapsed && me && (
            <OrgSwitcher organizations={me.organizations} activeOrganizationId={me.activeOrganizationId} />
          )}
        </div>

        <SidebarNav collapsed={collapsed} visibleGroups={visibleGroups} pendingInvitesCount={pendingInvitesCount} />
        <SidebarFooter collapsed={collapsed} role={claims?.role} onLogout={handleLogout} />
      </aside>

      <main className="flex-1 overflow-auto p-6">{hasNoOrganization ? <EmptyOrganizationState /> : <Outlet />}</main>
    </div>
  )
}
