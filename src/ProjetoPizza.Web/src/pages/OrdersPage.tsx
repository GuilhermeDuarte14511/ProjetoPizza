import { CheckCircle2, Clock3, Printer, Search } from 'lucide-react'
import { useMemo, useState } from 'react'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { queryKeys } from '../lib/queryKeys'
import { PageHeader } from '../components/ui/PageHeader'
import { StatusBadge } from '../components/ui/StatusBadge'
import { useToast } from '../components/ui/toast'
import { adminService } from '../services/adminService'
import { hasPermission } from '../services/authSession'
import type { ManagedOrder } from '../types/admin'
import { getUserErrorMessage } from '../utils/errors'
import { translateEnum } from '../utils/presentation'

const currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })
const transitions: Record<string, { action: string; label: string; next: string } | undefined> = {
  Submitted: { action: 'accept', label: 'Aceitar pedido', next: 'Accepted' },
  Accepted: { action: 'start-production', label: 'Iniciar preparo', next: 'InProduction' },
  InProduction: { action: 'ready', label: 'Marcar como pronto', next: 'Ready' },
  Ready: { action: 'complete', label: 'Concluir entrega', next: 'Completed' },
}

export function OrdersPage() {
  const { data: orders, setData: setOrders } = useAdminQuery(queryKeys.orders, adminService.orders)
  const [channel, setChannel] = useState('Todos')
  const [search, setSearch] = useState(() => new URLSearchParams(window.location.search).get('search') ?? '')
  const [busy, setBusy] = useState<string>()
  const toast = useToast()

  const visible = useMemo(() => orders.filter((order) =>
    (channel === 'Todos' || order.channel === channel) &&
    (`${order.number} ${order.items.map((item) => item.name).join(' ')}`.toLowerCase().includes(search.toLowerCase()))),
  [channel, orders, search])

  async function advance(order: ManagedOrder) {
    const transition = transitions[order.status]
    if (!transition) return
    setBusy(order.id)
    try {
      await adminService.transitionOrder(order.id, transition.action)
      setOrders((current) => current.map((item) => item.id === order.id ? { ...item, status: transition.next } : item))
      toast.success('Pedido atualizado', `O pedido #${order.number} agora está como ${translateEnum(transition.next).toLowerCase()}.`)
    } catch (error) {
      toast.error('Não foi possível atualizar o pedido', getUserErrorMessage(error))
    } finally {
      setBusy(undefined)
    }
  }

  return (
    <>
      <PageHeader title="Gestão de pedidos" description="Acompanhe salão, delivery e retirada em uma única fila." actions={<button className="secondary-button" onClick={() => window.print()}><Printer size={16} /> Imprimir lista</button>} />
      <div className="toolbar">
        <div className="filter-tabs" role="group" aria-label="Filtrar pedidos por canal">{['Todos', 'DineIn', 'Delivery', 'Takeaway'].map((item) => <button key={item} aria-pressed={channel === item} className={channel === item ? 'active' : ''} onClick={() => setChannel(item)}>{translateEnum(item)}</button>)}</div>
        <div className="toolbar-search"><Search size={17} /><input aria-label="Buscar pedido ou item" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar pedido ou item..." /></div>
      </div>
      <section className="orders-board">
        {visible.map((order) => {
          const transition = transitions[order.status]
          return (
            <article className="surface-card order-management-card" key={order.id}>
              <header>
                <div><span className="eyebrow">{translateEnum(order.channel)}</span><h2>Pedido #{order.number}</h2></div>
                <StatusBadge status={order.status} />
              </header>
              <div className="order-time"><Clock3 size={15} /> {new Date(order.createdAt).toLocaleString('pt-BR')}</div>
              <div className="order-lines">{order.items.map((item) => <div key={item.id}><span>{item.quantity}× {item.name}</span><strong>{currency.format(item.totalPrice)}</strong></div>)}</div>
              <footer><strong>{currency.format(order.total)}</strong>{transition && hasPermission('operations:write') ? <button className="primary-button" disabled={busy === order.id} onClick={() => void advance(order)}><CheckCircle2 size={16} /> {busy === order.id ? 'Atualizando...' : transition.label}</button> : <span className="completed-label">{transition ? 'Somente leitura' : 'Fluxo concluído'}</span>}</footer>
            </article>
          )
        })}
      </section>
    </>
  )
}
