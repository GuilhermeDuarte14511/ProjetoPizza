import {
  Bell,
  BarChart3,
  BookOpen,
  ChefHat,
  ClipboardList,
  CreditCard,
  Gauge,
  History,
  LogOut,
  Menu,
  MonitorSmartphone,
  ReceiptText,
  Search,
  Settings,
  TableProperties,
  Users,
  WalletCards,
} from 'lucide-react'
import type { FormEvent, ReactNode } from 'react'
import { useEffect, useRef, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { useLocation } from 'wouter'
import { queryKeys } from '../../lib/queryKeys'
import { runViewTransition } from '../../lib/viewTransitions'
import { adminService } from '../../services/adminService'
import { getAuthenticatedUser, hasPermission, logout } from '../../services/authSession'
import { ViewTransitionLink } from '../ui/ViewTransitionLink'

const navigation = [
  {
    label: 'Operação',
    items: [
      { to: '/admin/dashboard', label: 'Visão Geral', icon: Gauge },
      { to: '/admin/tables', label: 'Mesas', icon: TableProperties },
      { to: '/admin/orders', label: 'Pedidos', icon: ClipboardList },
      { to: '/admin/kitchen', label: 'Cozinha', icon: ChefHat },
    ],
  },
  {
    label: 'Financeiro',
    items: [
      { to: '/admin/cashier', label: 'Caixa', icon: WalletCards },
      { to: '/admin/payments', label: 'Pagamentos', icon: CreditCard },
      { to: '/admin/reports', label: 'Relatórios', icon: BarChart3 },
    ],
  },
  {
    label: 'Gestão',
    items: [
      { to: '/admin/catalog/products', label: 'Cardápio', icon: BookOpen },
      { to: '/admin/devices', label: 'Tablets', icon: MonitorSmartphone },
      { to: '/admin/users', label: 'Usuários', icon: Users },
      { to: '/admin/audit', label: 'Auditoria', icon: History },
      { to: '/admin/settings/general', label: 'Configurações', icon: Settings },
    ],
  },
]

interface AdminLayoutProps {
  children: ReactNode
}

export function AdminLayout({ children }: AdminLayoutProps) {
  const [location, navigate] = useLocation()
  const [isMenuOpen, setMenuOpen] = useState(false)
  const [globalSearch, setGlobalSearch] = useState('')
  const user = getAuthenticatedUser()
  const mainRef = useRef<HTMLElement>(null)
  const { data: cashShift, isLoading: isCashShiftLoading } = useQuery({
    queryKey: queryKeys.cashShift,
    queryFn: ({ signal }) => adminService.cashShift(signal),
  })
  const isCashShiftOpen = cashShift?.status === 'Open'

  useEffect(() => {
    const currentLabel = navigation
      .flatMap((group) => group.items)
      .find((item) => location === item.to || location.startsWith(`${item.to}/`))?.label
    document.title = `Forno 27 · ${currentLabel ?? 'Administração'}`
    requestAnimationFrame(() => mainRef.current?.focus({ preventScroll: true }))
  }, [location])

  function isActive(path: string) {
    return location === path || location.startsWith(`${path}/`)
  }

  function search(event: FormEvent) {
    event.preventDefault()
    const query = globalSearch.trim()
    if (!query) return
    const destination = /^mesa\s*/i.test(query) ? '/admin/tables' : '/admin/orders'
    runViewTransition(() => navigate(`${destination}?search=${encodeURIComponent(query.replace(/^mesa\s*/i, ''))}`))
  }

  function signOut() {
    logout()
    runViewTransition(() => navigate('/login'))
  }

  return (
    <div className="admin-shell">
      <a className="skip-link" href="#conteudo-principal">Ir para o conteúdo principal</a>
      <aside className={`sidebar ${isMenuOpen ? 'open' : ''}`}>
        <div className="brand">
          <span className="brand-mark"><ChefHat size={20} /></span>
          <span><strong>Forno 27</strong><small>Unidade Principal</small></span>
        </div>
        {hasPermission('operations:write') && <ViewTransitionLink href="/admin/orders" className="primary-button sidebar-action"><ReceiptText size={17} /> Novo Pedido</ViewTransitionLink>}
        <nav className="sidebar-nav" aria-label="Navegação principal">
          {navigation.map((group) => (
            <section key={group.label}>
              <p className="nav-group">{group.label}</p>
              {group.items.map(({ to, label, icon: Icon }) => (
                <ViewTransitionLink
                  key={to}
                  href={to}
                  title={label}
                  aria-current={isActive(to) ? 'page' : undefined}
                  onClick={() => setMenuOpen(false)}
                  className={`nav-link ${isActive(to) ? 'active' : ''}`}
                >
                  <Icon size={18} /> {label}
                </ViewTransitionLink>
              ))}
            </section>
          ))}
        </nav>
        <ViewTransitionLink href="/login" title="Sair" onClick={() => logout()} className="nav-link sidebar-logout"><LogOut size={18} /> Sair</ViewTransitionLink>
      </aside>
      <button
        className={`sidebar-backdrop ${isMenuOpen ? 'open' : ''}`}
        aria-label="Fechar menu"
        onClick={() => setMenuOpen(false)}
      />

      <div className="app-area">
        <header className="topbar">
          <button
            className="icon-button mobile-menu"
            aria-label="Abrir menu"
            aria-expanded={isMenuOpen}
            onClick={() => setMenuOpen((current) => !current)}
          >
            <Menu size={20} />
          </button>
          <form className="global-search" onSubmit={search}><Search size={18} /><input aria-label="Busca global" value={globalSearch} onChange={(event) => setGlobalSearch(event.target.value)} placeholder="Buscar pedidos, mesas e clientes..." /></form>
          <button
            type="button"
            className={`topbar-status ${isCashShiftLoading ? 'loading' : isCashShiftOpen ? 'open' : 'closed'}`}
            aria-label={isCashShiftLoading ? 'Verificando situação do caixa' : isCashShiftOpen ? 'Caixa aberto. Acessar caixa' : 'Caixa fechado. Acessar caixa'}
            onClick={() => runViewTransition(() => navigate('/admin/cashier'))}
          >
            <span className="status-dot" /> {isCashShiftLoading ? 'Verificando caixa' : isCashShiftOpen ? 'Caixa aberto' : 'Caixa fechado'}
          </button>
          <button className="icon-button" aria-label="Ver auditoria" onClick={() => runViewTransition(() => navigate('/admin/audit'))}><Bell size={19} /><span className="notification-dot" /></button>
          <button className="user-menu" aria-label="Sair" onClick={signOut}><span className="avatar">{user?.displayName.slice(0, 2).toUpperCase() ?? 'AD'}</span><span>{user?.displayName ?? 'Admin'}</span><LogOut size={14} /></button>
        </header>
        <main id="conteudo-principal" ref={mainRef} className="content-canvas" tabIndex={-1}>
          <div key={location} className="page-transition">{children}</div>
        </main>
      </div>
    </div>
  )
}
