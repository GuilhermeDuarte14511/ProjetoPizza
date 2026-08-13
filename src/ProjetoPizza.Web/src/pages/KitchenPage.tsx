import { CheckCircle2, ChefHat, Clock3, Expand, RefreshCw, SlidersHorizontal } from 'lucide-react'
import { useEffect, useMemo, useRef, useState } from 'react'
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

type SlaState = 'ok' | 'attention' | 'late'

function elapsedMinutes(ticket: KitchenTicket, now: number) {
  return Math.max(0, Math.floor((now - new Date(ticket.createdAt).getTime()) / 60_000))
}

function slaState(ticket: KitchenTicket, elapsed: number): SlaState {
  if (elapsed > ticket.targetPreparationMinutes) return 'late'
  if (elapsed >= ticket.targetPreparationMinutes * 0.7) return 'attention'
  return 'ok'
}

function slaLabel(ticket: KitchenTicket, elapsed: number) {
  const difference = ticket.targetPreparationMinutes - elapsed
  if (difference < 0) return `${Math.abs(difference)} min atrasado`
  if (difference === 0) return 'Meta atingida'
  return `${difference} min restantes`
}

export function KitchenPage() {
  const { data: tickets, setData: setTickets, refresh: refreshTickets, isRefreshing } = useAdminQuery(queryKeys.kitchenTickets, adminService.kitchenTickets)
  const [busy, setBusy] = useState<string>()
  const [station, setStation] = useState('all')
  const [now, setNow] = useState(() => Date.now())
  const boardRef = useRef<HTMLDivElement>(null)
  const toast = useToast()

  const stations = useMemo(() => Array.from(
    new Map(tickets.map((ticket) => [ticket.stationCode, ticket.station])).entries(),
  ).sort((a, b) => a[1].localeCompare(b[1])), [tickets])

  const visibleTickets = useMemo(
    () => station === 'all' ? tickets : tickets.filter((ticket) => ticket.stationCode === station),
    [station, tickets],
  )

  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 15_000)
    return () => window.clearInterval(timer)
  }, [])

  useEffect(() => {
    const timer = window.setInterval(() => {
      if (document.visibilityState === 'visible') void refreshTickets()
    }, 15_000)
    return () => window.clearInterval(timer)
  }, [refreshTickets])

  async function advance(ticket: KitchenTicket) {
    const transition = ticket.status === 'New' ? 'confirm' : ticket.status === 'Confirmed' ? 'start' : 'ready'
    const nextStatus = ticket.status === 'New' ? 'Confirmed' : ticket.status === 'Confirmed' ? 'Preparing' : 'Ready'
    setBusy(ticket.id)
    try {
      await adminService.transitionKitchenTicket(ticket.id, transition)
      setTickets((current) => current.map((item) => item.id === ticket.id ? {
        ...item,
        status: nextStatus,
        startedAt: nextStatus === 'Preparing' ? new Date().toISOString() : item.startedAt,
      } : item))
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
      setNow(Date.now())
      toast.success('Cozinha atualizada', 'A fila de produção foi sincronizada.')
    } catch (error) {
      toast.error('Não foi possível atualizar a cozinha', getUserErrorMessage(error))
    }
  }

  async function enterFullscreen() {
    try {
      await boardRef.current?.requestFullscreen()
    } catch {
      toast.error('Tela cheia indisponível', 'O navegador não permitiu ampliar o quadro de produção.')
    }
  }

  return (
    <>
      <PageHeader
        title="Cozinha"
        description="Fila ao vivo com tempo de produção, meta por praça e alertas de atraso."
        actions={<>
          <span className="kitchen-mode"><span className="status-dot" /> Ao vivo</span>
          <button className="secondary-button" onClick={() => void enterFullscreen()}><Expand size={16} /> Tela cheia</button>
          <button className="secondary-button" disabled={isRefreshing} onClick={() => void refresh()}><RefreshCw className={isRefreshing ? 'spin' : ''} size={16} /> {isRefreshing ? 'Atualizando...' : 'Atualizar'}</button>
        </>}
      />

      <div className="kitchen-filter" aria-label="Filtrar tickets por estação">
        <span><SlidersHorizontal size={15} /> Praça</span>
        <button className={station === 'all' ? 'active' : ''} onClick={() => setStation('all')}>Todas <b>{tickets.length}</b></button>
        {stations.map(([code, name]) => (
          <button className={station === code ? 'active' : ''} key={code} onClick={() => setStation(code)}>{name} <b>{tickets.filter((ticket) => ticket.stationCode === code).length}</b></button>
        ))}
      </div>

      <div className="kitchen-board" ref={boardRef}>
        {columns.map((column) => {
          const columnTickets = visibleTickets.filter((ticket) => ticket.status === column.key)
          return (
            <section className="kitchen-column" key={column.key}>
              <header><h2>{column.title}</h2><span>{columnTickets.length}</span></header>
              <div className="ticket-stack">
                {columnTickets.map((ticket) => {
                  const elapsed = elapsedMinutes(ticket, now)
                  const state = slaState(ticket, elapsed)
                  return (
                    <article className={`kitchen-ticket sla-${state}`} key={ticket.id}>
                      <div className="ticket-header"><strong>#{ticket.ticketNumber}</strong><span className={`sla-time ${state}`}><Clock3 size={14} /> {elapsed} min</span></div>
                      <small>{ticket.station} · Pedido #{ticket.orderNumber}</small>
                      <h3>{ticket.summary || `${ticket.itemCount} itens`}</h3>
                      <div className="ticket-sla"><span>{slaLabel(ticket, elapsed)}</span><span>Meta {ticket.targetPreparationMinutes} min</span></div>
                      {hasPermission('operations:write') && <button className="ticket-action" disabled={busy === ticket.id} onClick={() => void advance(ticket)}>{column.key === 'Preparing' ? <CheckCircle2 size={16} /> : <ChefHat size={16} />}{busy === ticket.id ? 'Atualizando...' : column.action}</button>}
                    </article>
                  )
                })}
                {columnTickets.length === 0 && <div className="kitchen-empty"><ChefHat size={20} /><span>Nenhum ticket nesta etapa</span></div>}
              </div>
            </section>
          )
        })}
      </div>
    </>
  )
}
