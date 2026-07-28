import { CheckCircle2, ChefHat, Clock3, RefreshCw } from 'lucide-react'
import { useState } from 'react'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { queryKeys } from '../lib/queryKeys'
import { PageHeader } from '../components/ui/PageHeader'
import { useToast } from '../components/ui/toast'
import { adminService } from '../services/adminService'
import { hasPermission } from '../services/authSession'
import type { KitchenTicket } from '../types/admin'
import { getUserErrorMessage } from '../utils/errors'
import { translateEnum } from '../utils/presentation'

const columns = [
  { key: 'New', title: 'Novos', action: 'Confirmar pedido' },
  { key: 'Confirmed', title: 'Confirmados', action: 'Iniciar preparo' },
  { key: 'Preparing', title: 'Em preparo', action: 'Marcar como pronto' },
] as const

export function KitchenPage() {
  const { data: tickets, setData: setTickets, refresh: refreshTickets, isRefreshing } = useAdminQuery(queryKeys.kitchenTickets, adminService.kitchenTickets)
  const [busy, setBusy] = useState<string>()
  const toast = useToast()

  async function advance(ticket: KitchenTicket) {
    const transition = ticket.status === 'New' ? 'confirm' : ticket.status === 'Confirmed' ? 'start' : 'ready'
    const nextStatus = ticket.status === 'New' ? 'Confirmed' : ticket.status === 'Confirmed' ? 'Preparing' : 'Ready'
    setBusy(ticket.id)
    try {
      await adminService.transitionKitchenTicket(ticket.id, transition)
      setTickets((current) => current.map((item) => item.id === ticket.id ? { ...item, status: nextStatus } : item))
      toast.success('Produção atualizada', `O ticket #${ticket.ticketNumber} agora está como ${translateEnum(nextStatus).toLowerCase()}.`)
    } catch (error) {
      toast.error('Não foi possível atualizar o ticket', getUserErrorMessage(error))
    } finally {
      setBusy(undefined)
    }
  }

  async function refresh() {
    try {
      await refreshTickets()
      toast.success('Cozinha atualizada', 'A fila de produção foi sincronizada.')
    } catch (error) {
      toast.error('Não foi possível atualizar a cozinha', getUserErrorMessage(error))
    }
  }

  return (
    <>
      <PageHeader title="Cozinha" description="Controle visual da produção por etapa e estação." actions={<><span className="kitchen-mode"><span className="status-dot" /> Modo operação</span><button className="secondary-button" disabled={isRefreshing} onClick={() => void refresh()}><RefreshCw className={isRefreshing ? 'spin' : ''} size={16} /> {isRefreshing ? 'Atualizando...' : 'Atualizar'}</button></>} />
      <div className="kitchen-board">
        {columns.map((column) => (
          <section className="kitchen-column" key={column.key}>
            <header><h2>{column.title}</h2><span>{tickets.filter((ticket) => ticket.status === column.key).length}</span></header>
            <div className="ticket-stack">
              {tickets.filter((ticket) => ticket.status === column.key).map((ticket) => (
                <article className="kitchen-ticket" key={ticket.id}>
                  <div className="ticket-header"><strong>#{ticket.ticketNumber}</strong><span><Clock3 size={14} /> 8 min</span></div>
                  <small>{ticket.station} · Pedido #{ticket.orderNumber}</small>
                  <h3>{ticket.summary ?? `${ticket.itemCount} itens`}</h3>
                  <p>{ticket.itemCount} itens para produção</p>
                  {hasPermission('operations:write') && <button className="ticket-action" disabled={busy === ticket.id} onClick={() => void advance(ticket)}>{column.key === 'Preparing' ? <CheckCircle2 size={16} /> : <ChefHat size={16} />}{busy === ticket.id ? 'Atualizando...' : column.action}</button>}
                </article>
              ))}
            </div>
          </section>
        ))}
      </div>
    </>
  )
}
