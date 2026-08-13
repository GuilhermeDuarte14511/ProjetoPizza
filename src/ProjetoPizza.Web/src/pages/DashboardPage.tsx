import { AlertTriangle, BellRing, CircleDollarSign, Clock3, CreditCard, RefreshCw, ShoppingCart, TableProperties, Trophy } from 'lucide-react'
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
      <section className="dashboard-grid dashboard-insights">
        <article className="surface-card">
          <div className="card-heading"><div><h2>Status das mesas</h2><p>Distribuição atual do salão.</p></div><TableProperties size={22} /></div>
          <div className="table-status-grid">
            <StatusMetric label="Livres" value={dashboard.tableStatus.free} tone="success" />
            <StatusMetric label="Ocupadas" value={dashboard.tableStatus.occupied} tone="neutral" />
            <StatusMetric label="Chamando" value={dashboard.tableStatus.calling} tone="danger" />
            <StatusMetric label="Aguardando pagamento" value={dashboard.tableStatus.awaitingPayment} tone="warning" />
          </div>
        </article>
        <article className="surface-card">
          <div className="card-heading"><div><h2>Top 5 mais vendidos</h2><p>Itens dos pedidos realizados hoje.</p></div><Trophy size={22} /></div>
          <ol className="ranking-list">
            {dashboard.topProducts.map((product, index) => <li key={product.name}><span className="ranking-position">{index + 1}</span><strong>{product.name}</strong><span>{product.quantity} un.</span></li>)}
          </ol>
          {!dashboard.topProducts.length && <div className="empty-inline">Nenhuma venda registrada hoje.</div>}
        </article>
        <article className="surface-card">
          <div className="card-heading"><div><h2>Receitas por pagamento</h2><p>Pagamentos confirmados no dia.</p></div><CreditCard size={22} /></div>
          <div className="payment-breakdown">
            {dashboard.paymentMethods.map((method) => <div key={method.name}><span><strong>{method.name}</strong><small>{method.percentage.toLocaleString('pt-BR')}%</small></span><strong>{currency.format(method.total)}</strong><progress max="100" value={method.percentage} aria-label={`${method.name}: ${method.percentage}%`} /></div>)}
          </div>
          {!dashboard.paymentMethods.length && <div className="empty-inline">Nenhum pagamento confirmado hoje.</div>}
        </article>
        <article className="surface-card stock-alert-card">
          <div className="card-heading"><div><h2>Alertas de estoque</h2><p>Itens no mínimo ou abaixo dele.</p></div><AlertTriangle size={22} /></div>
          <div className="stock-alert-list">
            {dashboard.stockAlerts.map((item) => <a href="/admin/inventory" key={item.inventoryItemId}><AlertTriangle size={17} /><span><strong>{item.name}</strong><small>Disponível: {item.availableQuantity.toLocaleString('pt-BR')} {item.unitOfMeasure} · mínimo {item.minimumStock.toLocaleString('pt-BR')}</small></span></a>)}
          </div>
          {!dashboard.stockAlerts.length && <div className="empty-inline">Estoque dentro dos níveis mínimos.</div>}
        </article>
      </section>
    </>
  )
}

function StatusMetric({ label, value, tone }: { label: string; value: number; tone: string }) {
  return <div className={`table-status-item ${tone}`}><strong>{value}</strong><span>{label}</span></div>
}

function formatCallTime(value: string) {
  const minutes = Math.max(0, Math.floor((Date.now() - new Date(value).getTime()) / 60_000))
  return minutes < 1 ? 'Agora' : `Há ${minutes} min`
}
