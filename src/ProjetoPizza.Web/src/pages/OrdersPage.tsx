import { CheckCircle2, Clock3, Plus, Printer, Search, Truck } from 'lucide-react'
import { useMemo, useState } from 'react'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { usePdfTableExport } from '../hooks/usePdfTableExport'
import { queryKeys } from '../lib/queryKeys'
import { PageHeader } from '../components/ui/PageHeader'
import { OrderReceiptDialog } from '../components/orders/OrderReceiptDialog'
import { PdfExportButton } from '../components/ui/PdfExportButton'
import { StatusBadge } from '../components/ui/StatusBadge'
import { useToast } from '../components/ui/toast'
import { ViewTransitionLink } from '../components/ui/ViewTransitionLink'
import { adminService } from '../services/adminService'
import { hasPermission } from '../services/authSession'
import type { ManagedOrder, OrderReceipt } from '../types/admin'
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
  const [receipt, setReceipt] = useState<OrderReceipt>()
  const [loadingReceipt, setLoadingReceipt] = useState<string>()
  const [driverNames, setDriverNames] = useState<Record<string, string>>({})
  const toast = useToast()
  const { exportPdf, exporting } = usePdfTableExport()

  const visible = useMemo(() => orders.filter((order) =>
    (channel === 'Todos' || order.channel === channel) &&
    (`${order.number} ${order.customerName ?? ''} ${order.items.map((item) => item.name).join(' ')}`.toLowerCase().includes(search.toLowerCase()))),
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

  async function printReceipt(order: ManagedOrder) {
    setLoadingReceipt(order.id)
    try {
      await adminService.printOrder(order.id)
      toast.success('Impressão enfileirada', `O comprovante do pedido #${order.number} será enviado à impressora configurada.`)
    } catch (error) {
      toast.error('Não foi possível preparar a impressão', getUserErrorMessage(error))
    } finally {
      setLoadingReceipt(undefined)
    }
  }

  async function dispatch(order: ManagedOrder) {
    const driverName = driverNames[order.id]?.trim()
    if (!driverName) { toast.error('Informe o entregador', 'Digite o nome de quem sairá com o pedido.'); return }
    setBusy(order.id)
    try {
      await adminService.dispatchDelivery(order.id, driverName)
      setOrders((current) => current.map((item) => item.id === order.id ? { ...item, deliveryStatus: 'Dispatched', deliveryDriverName: driverName, dispatchedAt: new Date().toISOString() } : item))
      toast.success('Entrega despachada', `${driverName} saiu com o pedido #${order.number}.`)
    } catch (error) { toast.error('Não foi possível despachar', getUserErrorMessage(error)) } finally { setBusy(undefined) }
  }

  async function completeDelivery(order: ManagedOrder) {
    setBusy(order.id)
    try {
      await adminService.completeDelivery(order.id)
      setOrders((current) => current.map((item) => item.id === order.id ? { ...item, status: 'Completed', deliveryStatus: 'Delivered', deliveredAt: new Date().toISOString() } : item))
      toast.success('Entrega concluída', `O pedido #${order.number} foi entregue.`)
    } catch (error) { toast.error('Não foi possível concluir a entrega', getUserErrorMessage(error)) } finally { setBusy(undefined) }
  }

  function exportOrders() {
    const total = visible.reduce((sum, order) => sum + order.total, 0)
    const items = visible.reduce((sum, order) => sum + order.items.reduce((quantity, item) => quantity + item.quantity, 0), 0)
    void exportPdf({
      title: 'Relatório de pedidos',
      subtitle: `Canal: ${translateEnum(channel)}${search ? ` · Busca: ${search}` : ''}`,
      fileName: `pedidos-${new Date().toISOString().slice(0, 10)}.pdf`,
      orientation: 'landscape',
      columns: ['Pedido', 'Data', 'Canal', 'Atendimento', 'Status', 'Itens', 'Descrição', 'Total'],
      rows: visible.map((order) => [
        `#${order.number}`,
        new Date(order.createdAt).toLocaleString('pt-BR'),
        translateEnum(order.channel),
        translateEnum(order.fulfillment),
        translateEnum(order.status),
        String(order.items.reduce((sum, item) => sum + item.quantity, 0)),
        order.items.map((item) => `${item.quantity}x ${item.name}`).join(' · ') || 'Sem itens',
        currency.format(order.total),
      ]),
      metrics: [
        { label: 'Pedidos', value: String(visible.length) },
        { label: 'Itens', value: String(items) },
        { label: 'Valor total', value: currency.format(total) },
        { label: 'Ticket médio', value: currency.format(visible.length ? total / visible.length : 0) },
      ],
      rightAlignedColumns: [5, 7],
    })
  }

  return (
    <>
      <PageHeader title="Gestão de pedidos" description="Acompanhe salão, delivery e retirada em uma única fila." actions={<><PdfExportButton exporting={exporting} onClick={exportOrders} label="Exportar pedidos em PDF" />{hasPermission('admin:write') && <ViewTransitionLink className="primary-button" href="/admin/orders/new"><Plus size={16} /> Novo pedido</ViewTransitionLink>}</>} />
      <div className="toolbar">
        <div className="filter-tabs" role="group" aria-label="Filtrar pedidos por canal">{['Todos', 'DineIn', 'Delivery', 'Pickup'].map((item) => <button key={item} aria-pressed={channel === item} className={channel === item ? 'active' : ''} onClick={() => setChannel(item)}>{translateEnum(item)}</button>)}</div>
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
              {order.customerName && <div className="order-customer-summary"><strong>{order.customerName}</strong>{order.deliveryAddress && <span>{order.deliveryAddress}</span>}{order.deliveryStatus && <span><Truck size={14} /> {translateEnum(order.deliveryStatus)}{order.deliveryDriverName ? ` · ${order.deliveryDriverName}` : ''}</span>}</div>}
              <div className="order-lines">{order.items.map((item) => <div key={item.id}><span>{item.quantity}× {item.name}</span><strong>{currency.format(item.totalPrice)}</strong></div>)}</div>
              <footer><strong>{currency.format(order.total)}</strong><div className="order-card-actions"><button className="secondary-button" disabled={loadingReceipt === order.id} onClick={() => void printReceipt(order)}><Printer size={15} /> {loadingReceipt === order.id ? 'Preparando...' : 'Imprimir'}</button>{order.fulfillment === 'Delivery' && order.deliveryStatus === 'ReadyForDispatch' && hasPermission('operations:write') ? <><input className="driver-name-input" aria-label={`Entregador do pedido ${order.number}`} value={driverNames[order.id] ?? ''} onChange={(event) => setDriverNames((current) => ({ ...current, [order.id]: event.target.value }))} placeholder="Nome do entregador" /><button className="primary-button" disabled={busy === order.id} onClick={() => void dispatch(order)}><Truck size={16} /> Entregador saiu</button></> : order.fulfillment === 'Delivery' && order.deliveryStatus === 'Dispatched' && hasPermission('operations:write') ? <button className="primary-button" disabled={busy === order.id} onClick={() => void completeDelivery(order)}><CheckCircle2 size={16} /> Confirmar entrega</button> : transition && !(order.fulfillment === 'Delivery' && order.status === 'Ready') && hasPermission('operations:write') ? <button className="primary-button" disabled={busy === order.id} onClick={() => void advance(order)}><CheckCircle2 size={16} /> {busy === order.id ? 'Atualizando...' : transition.label}</button> : <span className="completed-label">{order.deliveryStatus === 'Delivered' ? 'Entrega concluída' : transition ? 'Somente leitura' : 'Fluxo concluído'}</span>}</div></footer>
            </article>
          )
        })}
      </section>
      <OrderReceiptDialog receipt={receipt} onClose={() => setReceipt(undefined)} />
    </>
  )
}
