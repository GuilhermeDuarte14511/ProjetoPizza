import { QueryClientProvider } from '@tanstack/react-query'
import { lazy, Suspense, useEffect, useState, type ReactNode } from 'react'
import { Redirect, Route, Switch } from 'wouter'
import { AdminLayout } from './components/layout/AdminLayout'
import { RealtimeProvider } from './components/realtime/RealtimeProvider'
import { AppErrorBoundary } from './components/ui/AppErrorBoundary'
import { PageSkeleton } from './components/ui/PageSkeleton'
import { ToastProvider } from './components/ui/ToastProvider'
import { getAccessToken, isApiConfigured, unauthorizedEventName } from './api/httpClient'
import { queryClient } from './lib/queryClient'
import { logout } from './services/authSession'

const AuditPage = lazy(() => import('./pages/AuditPage').then((module) => ({ default: module.AuditPage })))
const CashierPage = lazy(() => import('./pages/CashierPage').then((module) => ({ default: module.CashierPage })))
const ClientTabletPage = lazy(() => import('./pages/ClientTabletPage').then((module) => ({ default: module.ClientTabletPage })))
const CategoriesPage = lazy(() => import('./pages/CategoriesPage').then((module) => ({ default: module.CategoriesPage })))
const CrustsPage = lazy(() => import('./pages/CrustsPage').then((module) => ({ default: module.CrustsPage })))
const CustomersPage = lazy(() => import('./pages/CustomersPage').then((module) => ({ default: module.CustomersPage })))
const IngredientsPage = lazy(() => import('./pages/IngredientsPage').then((module) => ({ default: module.IngredientsPage })))
const DashboardPage = lazy(() => import('./pages/DashboardPage').then((module) => ({ default: module.DashboardPage })))
const DevicesPage = lazy(() => import('./pages/DevicesPage').then((module) => ({ default: module.DevicesPage })))
const FinancialReportsPage = lazy(() => import('./pages/FinancialReportsPage').then((module) => ({ default: module.FinancialReportsPage })))
const KitchenPage = lazy(() => import('./pages/KitchenPage').then((module) => ({ default: module.KitchenPage })))
const LoginPage = lazy(() => import('./pages/LoginPage').then((module) => ({ default: module.LoginPage })))
const NewOrderPage = lazy(() => import('./pages/NewOrderPage').then((module) => ({ default: module.NewOrderPage })))
const OrdersPage = lazy(() => import('./pages/OrdersPage').then((module) => ({ default: module.OrdersPage })))
const PaymentsPage = lazy(() => import('./pages/PaymentsPage').then((module) => ({ default: module.PaymentsPage })))
const PizzaSettingsPage = lazy(() => import('./pages/PizzaSettingsPage').then((module) => ({ default: module.PizzaSettingsPage })))
const PizzaFlavorsPage = lazy(() => import('./pages/PizzaFlavorsPage').then((module) => ({ default: module.PizzaFlavorsPage })))
const ProductsPage = lazy(() => import('./pages/ProductsPage').then((module) => ({ default: module.ProductsPage })))
const SettingsPage = lazy(() => import('./pages/SettingsPage').then((module) => ({ default: module.SettingsPage })))
const ServiceCallsPage = lazy(() => import('./pages/ServiceCallsPage').then((module) => ({ default: module.ServiceCallsPage })))
const TableDetailPage = lazy(() => import('./pages/TableDetailPage').then((module) => ({ default: module.TableDetailPage })))
const TablesPage = lazy(() => import('./pages/TablesPage').then((module) => ({ default: module.TablesPage })))
const UsersPage = lazy(() => import('./pages/UsersPage').then((module) => ({ default: module.UsersPage })))

function AdminPage({ children }: { children: ReactNode }) {
  const [isAuthenticated, setAuthenticated] = useState(() => !isApiConfigured || Boolean(getAccessToken()))

  useEffect(() => {
    const handleUnauthorized = () => {
      logout()
      queryClient.clear()
      setAuthenticated(false)
    }
    window.addEventListener(unauthorizedEventName, handleUnauthorized)
    return () => window.removeEventListener(unauthorizedEventName, handleUnauthorized)
  }, [])

  if (!isAuthenticated || (isApiConfigured && !getAccessToken())) return <Redirect to="/login" />
  return (
    <RealtimeProvider>
      <AdminLayout>
        <AppErrorBoundary>
          <Suspense fallback={<PageSkeleton />}>{children}</Suspense>
        </AppErrorBoundary>
      </AdminLayout>
    </RealtimeProvider>
  )
}

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <Suspense fallback={<PageSkeleton />}>
          <Switch>
            <Route path="/mesa"><ClientTabletPage /></Route>
            <Route path="/login"><LoginPage /></Route>
            <Route path="/admin"><Redirect to="/admin/dashboard" /></Route>
            <Route path="/admin/dashboard"><AdminPage><DashboardPage /></AdminPage></Route>
            <Route path="/admin/tables"><AdminPage><TablesPage /></AdminPage></Route>
            <Route path="/admin/tables/:id"><AdminPage><TableDetailPage /></AdminPage></Route>
            <Route path="/admin/orders/new"><AdminPage><NewOrderPage /></AdminPage></Route>
            <Route path="/admin/orders"><AdminPage><OrdersPage /></AdminPage></Route>
            <Route path="/admin/kitchen"><AdminPage><KitchenPage /></AdminPage></Route>
            <Route path="/admin/service-calls"><AdminPage><ServiceCallsPage /></AdminPage></Route>
            <Route path="/admin/catalog"><Redirect to="/admin/catalog/products" /></Route>
            <Route path="/admin/catalog/products"><AdminPage><ProductsPage /></AdminPage></Route>
            <Route path="/admin/catalog/categories"><AdminPage><CategoriesPage /></AdminPage></Route>
            <Route path="/admin/catalog/crusts"><AdminPage><CrustsPage /></AdminPage></Route>
            <Route path="/admin/catalog/ingredients"><AdminPage><IngredientsPage /></AdminPage></Route>
            <Route path="/admin/catalog/pizza-sizes"><AdminPage><PizzaSettingsPage initialTab="sizes" /></AdminPage></Route>
            <Route path="/admin/catalog/pizza-flavors"><AdminPage><PizzaFlavorsPage /></AdminPage></Route>
            <Route path="/admin/cashier"><AdminPage><CashierPage /></AdminPage></Route>
            <Route path="/admin/payments"><AdminPage><PaymentsPage /></AdminPage></Route>
            <Route path="/admin/reports"><AdminPage><FinancialReportsPage /></AdminPage></Route>
            <Route path="/admin/devices"><AdminPage><DevicesPage /></AdminPage></Route>
            <Route path="/admin/customers"><AdminPage><CustomersPage /></AdminPage></Route>
            <Route path="/admin/users"><AdminPage><UsersPage tab="users" /></AdminPage></Route>
            <Route path="/admin/roles"><AdminPage><UsersPage tab="roles" /></AdminPage></Route>
            <Route path="/admin/audit"><AdminPage><AuditPage /></AdminPage></Route>
            <Route path="/admin/settings"><Redirect to="/admin/settings/general" /></Route>
            <Route path="/admin/settings/general"><AdminPage><SettingsPage section="general" /></AdminPage></Route>
            <Route path="/admin/settings/operation"><AdminPage><SettingsPage section="operation" /></AdminPage></Route>
            <Route path="/admin/settings/pizza-rules"><AdminPage><PizzaSettingsPage initialTab="rules" /></AdminPage></Route>
            <Route path="/admin/settings/printers"><AdminPage><SettingsPage section="printers" /></AdminPage></Route>
            <Route path="/admin/settings/backup"><AdminPage><SettingsPage section="backup" /></AdminPage></Route>
            <Route><Redirect to="/login" /></Route>
          </Switch>
        </Suspense>
      </ToastProvider>
    </QueryClientProvider>
  )
}
