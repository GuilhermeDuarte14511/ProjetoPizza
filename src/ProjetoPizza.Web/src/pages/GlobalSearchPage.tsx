import { ClipboardList, SearchX, TableProperties, UserRound } from 'lucide-react'
import { useMemo, type ReactNode } from 'react'
import { PageHeader } from '../components/ui/PageHeader'
import { ViewTransitionLink } from '../components/ui/ViewTransitionLink'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { queryKeys } from '../lib/queryKeys'
import { adminService } from '../services/adminService'
import { formatPhone } from '../utils/phone'
import { translateEnum } from '../utils/presentation'

function normalize(value: string) {
  return value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLocaleLowerCase('pt-BR').trim()
}

export function GlobalSearchPage() {
  const query = new URLSearchParams(window.location.search).get('q')?.trim() ?? ''
  const term = normalize(query)
  const digits = query.replace(/\D/g, '')
  const { data: orders } = useAdminQuery(queryKeys.orders, adminService.orders)
  const { data: tables } = useAdminQuery(queryKeys.tables, adminService.tables)
  const { data: customers } = useAdminQuery(queryKeys.customers, adminService.customers)

  const results = useMemo(() => ({
    orders: term ? orders.filter((order) => normalize([
      order.number,
      order.customerName,
      order.channel,
      order.status,
      ...order.items.map((item) => item.name),
    ].filter(Boolean).join(' ')).includes(term)).slice(0, 12) : [],
    tables: term ? tables.filter((table) => normalize(`${table.number} ${table.name} ${table.area} ${table.status}`).includes(term.replace(/^mesa\s*/, ''))).slice(0, 12) : [],
    customers: term ? customers.filter((customer) =>
      normalize(customer.name).includes(term) || Boolean(digits && customer.phone.includes(digits))).slice(0, 12) : [],
  }), [customers, digits, orders, tables, term])
  const resultCount = results.orders.length + results.tables.length + results.customers.length

  return (
    <>
      <PageHeader
        title="Busca global"
        description={query ? `${resultCount} resultado(s) para “${query}” em pedidos, mesas e clientes.` : 'Use a busca no topo para localizar itens da operação.'}
      />
      {query && resultCount > 0 ? <div className="global-results">
        <ResultSection icon={<ClipboardList />} title="Pedidos" count={results.orders.length}>
          {results.orders.map((order) => <ViewTransitionLink className="global-result-row" href={`/admin/orders?search=${encodeURIComponent(String(order.number))}`} key={order.id}>
            <span><strong>Pedido #{order.number}</strong><small>{order.customerName ?? 'Cliente não informado'} · {order.items.length} item(ns)</small></span>
            <span><strong>{order.total.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}</strong><small>{translateEnum(order.status)}</small></span>
          </ViewTransitionLink>)}
        </ResultSection>
        <ResultSection icon={<TableProperties />} title="Mesas" count={results.tables.length}>
          {results.tables.map((table) => <ViewTransitionLink className="global-result-row" href={`/admin/tables/${table.id}`} key={table.id}>
            <span><strong>{table.name}</strong><small>{table.area} · {table.capacity} lugares</small></span>
            <span><strong>{table.status}</strong><small>{table.currentTotal.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}</small></span>
          </ViewTransitionLink>)}
        </ResultSection>
        <ResultSection icon={<UserRound />} title="Clientes" count={results.customers.length}>
          {results.customers.map((customer) => <ViewTransitionLink className="global-result-row" href={`/admin/customers?search=${encodeURIComponent(customer.name)}`} key={customer.id}>
            <span><strong>{customer.name}</strong><small>{formatPhone(customer.phone)}</small></span>
            <span><strong>{customer.orderCount} pedido(s)</strong><small>{customer.loyaltyPoints} ponto(s)</small></span>
          </ViewTransitionLink>)}
        </ResultSection>
      </div> : <div className="surface-card global-search-empty"><SearchX /><h2>{query ? 'Nenhum resultado encontrado' : 'Digite o que deseja localizar'}</h2><p>{query ? 'Tente o número do pedido ou da mesa, o nome do cliente ou o telefone.' : 'A busca consulta simultaneamente os dados operacionais disponíveis.'}</p></div>}
    </>
  )
}

function ResultSection({ icon, title, count, children }: { icon: ReactNode; title: string; count: number; children: ReactNode }) {
  if (!count) return null
  return <section className="surface-card global-result-section"><header><span>{icon}</span><h2>{title}</h2><small>{count}</small></header><div>{children}</div></section>
}
