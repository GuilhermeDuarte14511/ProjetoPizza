import { BellRing, CircleDollarSign, Clock3, RefreshCw, ShoppingCart, TableProperties } from 'lucide-react'
import { MetricCard } from '../components/ui/MetricCard'
import { PageHeader } from '../components/ui/PageHeader'
import { StatusBadge } from '../components/ui/StatusBadge'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { queryKeys } from '../lib/queryKeys'
import { adminService } from '../services/adminService'
import { translateEnum } from '../utils/presentation'

const currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })

export function DashboardPage() {
  const { data: dashboard, refresh, isRefreshing } = useAdminQuery(queryKeys.dashboard, adminService.dashboard)
  const { data: serviceCalls } = useAdminQuery(queryKeys.serviceCalls, adminService.serviceCalls)

  return (
    <>
      <PageHeader
        title="Visão Geral"
        description="Acompanhe a operação da unidade em tempo real."
        actions={
          <button className="secondary-button" disabled={isRefreshing} onClick={() => void refresh()}>
            <RefreshCw className={isRefreshing ? 'spin' : ''} size={16} /> {isRefreshing ? 'Atualizando...' : 'Atualizar'}
          </button>
        }
      />
      <section className="metrics-grid">
        <MetricCard label="Vendas do dia" value={currency.format(dashboard.salesToday)} detail="Pedidos concluídos no dia" icon={CircleDollarSign} />
        <MetricCard label="Pedidos" value={dashboard.ordersToday.toString()} detail="Salão, delivery e retirada" icon={ShoppingCart} />
        <MetricCard label="Ticket médio" value={currency.format(dashboard.averageTicket)} detail="Média dos pedidos concluídos" icon={Clock3} />
        <MetricCard label="Mesas ocupadas" value={`${dashboard.occupiedTables} / ${dashboard.totalTables}`} detail="Ocupação atual do salão" icon={TableProperties} />
        <MetricCard label="Em preparo" value={dashboard.ordersInProduction.toString()} detail="Pedidos em produção agora" icon={Clock3} tone="warning" />
        <MetricCard label="Chamados pendentes" value={dashboard.pendingServiceCalls.toString()} detail="Ação imediata necessária" icon={BellRing} tone="danger" />
      </section>
      <section className="dashboard-grid">
        <article className="surface-card">
          <div className="card-heading"><div><h2>Pedidos em andamento</h2><p>Últimas movimentações da operação.</p></div><a href="/admin/orders">Ver todos</a></div>
          <div className="responsive-table">
            <table>
              <thead><tr><th>Pedido</th><th>Canal</th><th>Status</th><th>Total</th></tr></thead>
              <tbody>{dashboard.recentOrders.map((order) => <tr key={order.number}><td><strong>#{order.number}</strong></td><td>{translateEnum(order.channel)}</td><td><StatusBadge status={order.status} /></td><td>{currency.format(order.total)}</td></tr>)}</tbody>
            </table>
          </div>
        </article>
        <article className="surface-card operational-alerts">
          <div className="card-heading"><div><h2>Chamados das mesas</h2><p>Solicitações reais enviadas pelos tablets.</p></div><a href="/admin/service-calls">Ver fila</a></div>
          {serviceCalls.slice(0, 3).map((call) => (
            <a className={`alert-item ${call.status === 'Pending' ? 'danger' : 'warning'}`} href="/admin/service-calls" key={call.id}>
              <BellRing size={18} />
              <span><strong>{call.tableName} · {call.typeName}</strong><small>{formatCallTime(call.createdAt)} · {translateEnum(call.status)}</small></span>
            </a>
          ))}
          {!serviceCalls.length && <div className="empty-inline">Nenhum chamado ativo no momento.</div>}
        </article>
      </section>
    </>
  )
}

function formatCallTime(value: string) {
  const minutes = Math.max(0, Math.floor((Date.now() - new Date(value).getTime()) / 60_000))
  return minutes < 1 ? 'Agora' : `Há ${minutes} min`
}
