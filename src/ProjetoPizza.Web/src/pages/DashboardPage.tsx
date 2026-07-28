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
        <MetricCard label="Vendas do dia" value={currency.format(dashboard.salesToday)} detail="+12% em relação a ontem" icon={CircleDollarSign} />
        <MetricCard label="Pedidos" value={dashboard.ordersToday.toString()} detail="Salão, delivery e retirada" icon={ShoppingCart} />
        <MetricCard label="Ticket médio" value={currency.format(dashboard.averageTicket)} detail="+ R$ 2,10 no período" icon={Clock3} />
        <MetricCard label="Mesas ocupadas" value={`${dashboard.occupiedTables} / ${dashboard.totalTables}`} detail="Ocupação atual do salão" icon={TableProperties} />
        <MetricCard label="Em preparo" value={dashboard.ordersInProduction.toString()} detail="Tempo médio de 18 min" icon={Clock3} tone="warning" />
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
          <div className="card-heading"><div><h2>Alertas operacionais</h2><p>Itens que pedem atenção.</p></div></div>
          <div className="alert-item danger"><BellRing size={18} /><span><strong>Mesa 03 solicita atendimento</strong><small>Há 2 minutos</small></span></div>
          <div className="alert-item warning"><Clock3 size={18} /><span><strong>Pedido #1019 acima do tempo</strong><small>Pizzaria · 28 minutos</small></span></div>
          <div className="alert-item"><ShoppingCart size={18} /><span><strong>Estoque baixo de mussarela</strong><small>1,8 kg disponíveis</small></span></div>
        </article>
      </section>
    </>
  )
}
