import {
  Cake,
  CalendarClock,
  ChevronRight,
  CircleDollarSign,
  History,
  Pencil,
  Phone,
  ReceiptText,
  RefreshCw,
  ShoppingBag,
  Sparkles,
  Star,
  TicketPercent,
  UserRound,
} from 'lucide-react'
import { useCallback, useEffect, useMemo, useState } from 'react'
import { Modal } from '../../components/ui/Modal'
import { StatusBadge } from '../../components/ui/StatusBadge'
import { useToast } from '../../components/ui/toast'
import { adminService } from '../../services/adminService'
import type { Customer, CustomerCoupon, CustomerDetail } from '../../types/admin'
import { getUserErrorMessage } from '../../utils/errors'
import { formatCurrency } from '../../utils/money'
import { formatPhone } from '../../utils/phone'
import { translateEnum } from '../../utils/presentation'

type DetailState =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'success'; detail: CustomerDetail }

type CustomerView = 'movements' | 'orders' | 'coupons'

interface CustomerDetailModalProps {
  customerId: string
  onClose: () => void
  onEdit: (customer: Customer) => void
  onCustomerChanged: (customer: Customer) => void
  canWrite: boolean
}

export function CustomerDetailModal({ customerId, onClose, onEdit, onCustomerChanged, canWrite }: CustomerDetailModalProps) {
  const [state, setState] = useState<DetailState>({ status: 'loading' })
  const [view, setView] = useState<CustomerView>('movements')
  const [adjusting, setAdjusting] = useState(false)

  const requestDetail = useCallback((signal?: AbortSignal) => adminService.customerDetail(customerId, signal), [customerId])

  async function retry() {
    setState({ status: 'loading' })
    try {
      const detail = await requestDetail()
      setState({ status: 'success', detail })
    } catch (error) {
      setState({ status: 'error', message: getUserErrorMessage(error) })
    }
  }

  useEffect(() => {
    const controller = new AbortController()
    requestDetail(controller.signal)
      .then((detail) => setState({ status: 'success', detail }))
      .catch((error: unknown) => {
        if (error instanceof DOMException && error.name === 'AbortError') return
        setState({ status: 'error', message: getUserErrorMessage(error) })
      })
    return () => controller.abort()
  }, [requestDetail])

  const detail = state.status === 'success' ? state.detail : undefined

  return (
    <>
      <Modal
        open
        size="large"
        title="Central do cliente"
        description="Perfil, relacionamento, benefícios e histórico em uma única visão."
        onClose={onClose}
      >
        <div className="customer-center" aria-live="polite">
          {state.status === 'loading' && <CustomerDetailSkeleton />}
          {state.status === 'error' && (
            <div className="customer-center-state" role="alert">
              <span><UserRound /></span>
              <strong>Não foi possível carregar este cliente</strong>
              <p>{state.message}</p>
              <button type="button" className="secondary-button" onClick={() => void retry()}><RefreshCw size={15} /> Tentar novamente</button>
            </div>
          )}
          {detail && (
            <CustomerCenterContent
              detail={detail}
              view={view}
              canWrite={canWrite}
              onViewChange={setView}
              onAdjust={() => setAdjusting(true)}
              onEdit={() => onEdit(detail.customer)}
            />
          )}
        </div>
      </Modal>

      {adjusting && detail && (
        <LoyaltyAdjustmentModal
          detail={detail}
          onClose={() => setAdjusting(false)}
          onSaved={(updated) => {
            setState({ status: 'success', detail: updated })
            onCustomerChanged(updated.customer)
            setAdjusting(false)
          }}
        />
      )}
    </>
  )
}

interface CustomerCenterContentProps {
  detail: CustomerDetail
  view: CustomerView
  canWrite: boolean
  onViewChange: (view: CustomerView) => void
  onAdjust: () => void
  onEdit: () => void
}

function CustomerCenterContent({ detail, view, canWrite, onViewChange, onAdjust, onEdit }: CustomerCenterContentProps) {
  const { customer } = detail
  const availableCoupons = detail.coupons.filter((coupon) => coupon.availability === 'Available').length
  const initials = customer.name.split(/\s+/).slice(0, 2).map((part) => part[0]).join('').toLocaleUpperCase('pt-BR')

  return (
    <>
      <section className="customer-passport" aria-labelledby="customer-passport-name">
        <div className="customer-passport-profile">
          <span className="customer-passport-avatar" aria-hidden="true">{initials || <UserRound />}</span>
          <div>
            <span className="customer-passport-kicker">Passaporte do cliente</span>
            <h2 id="customer-passport-name">{customer.name}</h2>
            <div className="customer-passport-contacts">
              <span><Phone size={14} /> {formatPhone(customer.phone)}</span>
              <span><Cake size={14} /> {formatBirthDate(customer.birthDate)}</span>
              <span><CalendarClock size={14} /> Desde {formatMonthYear(customer.createdAt)}</span>
            </div>
          </div>
        </div>
        <div className="customer-passport-actions">
          <StatusBadge status={customer.isActive ? 'Ativo' : 'Inativo'} />
          {canWrite && <button type="button" className="secondary-button" onClick={onEdit}><Pencil size={15} /> Editar cadastro</button>}
        </div>
      </section>

      <section className="customer-benefit-stage" aria-label="Resumo de benefícios e relacionamento">
        <article className="customer-benefit-ticket">
          <div className="customer-benefit-ticket-heading"><Star size={19} /><span>Saldo de benefícios</span></div>
          <strong>{customer.loyaltyPoints.toLocaleString('pt-BR')}<small> pontos</small></strong>
          <p>Equivale a até <b>{formatCurrency(detail.benefitBalance)}</b> em descontos. Não é saldo sacável.</p>
          <footer>
            <span><CalendarClock size={14} /> {detail.loyaltyPointsExpireAt ? `Expira em ${formatDate(detail.loyaltyPointsExpireAt)}` : 'Sem pontos a expirar'}</span>
            {canWrite && customer.isActive && <button type="button" onClick={onAdjust}><Sparkles size={15} /> Ajustar pontos</button>}
          </footer>
        </article>
        <div className="customer-relationship-stats">
          <article><ShoppingBag /><span><small>Pedidos concluídos</small><strong>{customer.orderCount}</strong></span></article>
          <article><CircleDollarSign /><span><small>Valor acumulado</small><strong>{formatCurrency(customer.lifetimeSpend)}</strong></span></article>
          <article><ReceiptText /><span><small>Ticket médio</small><strong>{formatCurrency(detail.averageTicket)}</strong></span></article>
          <article><TicketPercent /><span><small>Cupons disponíveis</small><strong>{availableCoupons}</strong></span></article>
        </div>
      </section>

      <nav className="customer-center-switcher" aria-label="Informações do cliente">
        <ViewButton active={view === 'movements'} count={detail.loyaltyTransactions.length} icon={<History />} onClick={() => onViewChange('movements')}>Movimentações</ViewButton>
        <ViewButton active={view === 'orders'} count={detail.orders.length} icon={<ReceiptText />} onClick={() => onViewChange('orders')}>Pedidos</ViewButton>
        <ViewButton active={view === 'coupons'} count={detail.coupons.length} icon={<TicketPercent />} onClick={() => onViewChange('coupons')}>Cupons</ViewButton>
      </nav>

      <section className="customer-center-panel">
        {view === 'movements' && <LoyaltyMovements detail={detail} />}
        {view === 'orders' && <CustomerOrders detail={detail} />}
        {view === 'coupons' && <CustomerCoupons coupons={detail.coupons} />}
      </section>
    </>
  )
}

function ViewButton({ active, count, icon, onClick, children }: { active: boolean; count: number; icon: React.ReactNode; onClick: () => void; children: React.ReactNode }) {
  return <button type="button" aria-pressed={active} onClick={onClick}>{icon}<span>{children}</span><small>{count}</small></button>
}

function LoyaltyMovements({ detail }: { detail: CustomerDetail }) {
  if (!detail.loyaltyTransactions.length) return <CustomerPanelEmpty icon={<History />} title="Nenhuma movimentação de pontos" text="As entradas, resgates, estornos e ajustes aparecerão aqui." />
  return (
    <div className="customer-receipt-ledger" aria-label="Histórico de pontos">
      {detail.loyaltyTransactions.map((transaction) => (
        <article key={transaction.id}>
          <span className={`customer-ledger-mark ${transaction.points > 0 ? 'positive' : 'negative'}`} aria-hidden="true" />
          <div><strong>{translateEnum(transaction.type)}</strong><p>{transaction.description}</p><small>{formatDateTime(transaction.occurredAt)}{transaction.orderId ? ` · Pedido vinculado` : ''}</small></div>
          <span className={`customer-ledger-value ${transaction.points > 0 ? 'positive' : 'negative'}`}><strong>{transaction.points > 0 ? '+' : ''}{transaction.points}</strong><small>Saldo {transaction.balanceAfter}</small></span>
        </article>
      ))}
    </div>
  )
}

function CustomerOrders({ detail }: { detail: CustomerDetail }) {
  if (!detail.orders.length) return <CustomerPanelEmpty icon={<ReceiptText />} title="Nenhum pedido encontrado" text="Os pedidos vinculados a este cadastro aparecerão aqui." />
  return (
    <div className="customer-order-receipts" aria-label="Histórico de pedidos">
      {detail.orders.map((order) => (
        <article key={order.id}>
          <div className="customer-order-number"><small>Pedido</small><strong>#{order.number}</strong></div>
          <div className="customer-order-copy"><strong>{translateEnum(order.fulfillment)}</strong><small>{formatDateTime(order.createdAt)}</small><span>{order.couponCode ? `Cupom ${order.couponCode}` : 'Sem cupom'}{order.loyaltyPointsRedeemed ? ` · ${order.loyaltyPointsRedeemed} pontos usados` : ''}</span></div>
          <StatusBadge status={translateEnum(order.status)} />
          <div className="customer-order-total"><small>{order.discount > 0 ? `${formatCurrency(order.discount)} de desconto` : 'Total do pedido'}</small><strong>{formatCurrency(order.total)}</strong></div>
        </article>
      ))}
    </div>
  )
}

function CustomerCoupons({ coupons }: { coupons: CustomerCoupon[] }) {
  const ordered = useMemo(() => [...coupons].sort((left, right) => couponPriority(left) - couponPriority(right) || left.endsAt.localeCompare(right.endsAt)), [coupons])
  if (!ordered.length) return <CustomerPanelEmpty icon={<TicketPercent />} title="Nenhuma campanha de cupom" text="Crie cupons em Fidelidade para disponibilizá-los aos clientes." />
  return (
    <div className="customer-coupon-wallet" aria-label="Cupons do cliente">
      {ordered.map((coupon) => (
        <article key={coupon.id} className={coupon.availability === 'Available' ? 'available' : ''}>
          <div className="customer-coupon-stub"><TicketPercent size={17} /><strong>{coupon.code}</strong></div>
          <div className="customer-coupon-copy"><strong>{coupon.name}</strong><span>{coupon.discountType === 'Percentage' ? `${coupon.value}% de desconto` : `${formatCurrency(coupon.value)} de desconto`}{coupon.minimumOrderAmount > 0 ? ` · mínimo ${formatCurrency(coupon.minimumOrderAmount)}` : ''}</span><small>Válido até {formatDate(coupon.endsAt)} · campanha da unidade</small></div>
          <div className="customer-coupon-state"><span className={`loyalty-state ${coupon.availability === 'Available' ? 'active' : ''}`}>{translateEnum(coupon.availability)}</span><small>{coupon.timesUsedByCustomer ? `Usado ${coupon.timesUsedByCustomer}x${coupon.lastUsedAt ? ` · última em ${formatDate(coupon.lastUsedAt)}` : ''}` : 'Ainda não utilizado'}</small></div>
        </article>
      ))}
    </div>
  )
}

function CustomerPanelEmpty({ icon, title, text }: { icon: React.ReactNode; title: string; text: string }) {
  return <div className="customer-panel-empty"><span>{icon}</span><strong>{title}</strong><p>{text}</p></div>
}

function CustomerDetailSkeleton() {
  return <div className="customer-detail-skeleton" aria-label="Carregando detalhes do cliente"><span /><span /><div><span /><span /><span /><span /></div><span /></div>
}

function LoyaltyAdjustmentModal({ detail, onClose, onSaved }: { detail: CustomerDetail; onClose: () => void; onSaved: (detail: CustomerDetail) => void }) {
  const [points, setPoints] = useState('')
  const [reason, setReason] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const toast = useToast()
  const numericPoints = Number(points)
  const projectedBalance = Number.isInteger(numericPoints) ? detail.customer.loyaltyPoints + numericPoints : detail.customer.loyaltyPoints

  async function submit(event: React.FormEvent) {
    event.preventDefault()
    if (!Number.isInteger(numericPoints) || numericPoints === 0) return setError('Informe uma quantidade inteira, positiva ou negativa, diferente de zero.')
    if (projectedBalance < 0) return setError('O ajuste não pode deixar o saldo de pontos negativo.')
    if (reason.trim().length < 5) return setError('Explique o motivo do ajuste com pelo menos 5 caracteres.')
    setSaving(true)
    setError('')
    try {
      const updated = await adminService.adjustCustomerLoyaltyPoints(detail.customer.id, { points: numericPoints, reason: reason.trim() })
      onSaved(updated)
      toast.success('Saldo ajustado', `${numericPoints > 0 ? '+' : ''}${numericPoints} pontos registrados para ${detail.customer.name}.`)
    } catch (requestError) {
      setError(getUserErrorMessage(requestError))
    } finally {
      setSaving(false)
    }
  }

  return (
    <Modal open title="Ajustar pontos" description="Toda alteração exige motivo e fica registrada no histórico e na auditoria." isBusy={saving} onClose={onClose}>
      <form onSubmit={submit} noValidate>
        <div className="modal-body loyalty-adjustment-form">
          <div className="loyalty-adjustment-balance"><span><Star size={18} /> Saldo atual</span><strong>{detail.customer.loyaltyPoints} pontos</strong><ChevronRight size={18} /><span><Sparkles size={18} /> Novo saldo</span><strong className={projectedBalance < 0 ? 'invalid' : ''}>{projectedBalance} pontos</strong></div>
          <label className="field-label">Quantidade de pontos<input autoFocus inputMode="numeric" type="number" step="1" min="-1000000" max="1000000" value={points} onChange={(event) => setPoints(event.target.value)} placeholder="Ex.: 50 ou -25" aria-describedby="adjustment-points-help" /><small id="adjustment-points-help">Use um valor positivo para crédito e negativo para débito.</small></label>
          <label className="field-label">Motivo do ajuste<textarea rows={3} maxLength={160} value={reason} onChange={(event) => setReason(event.target.value)} placeholder="Ex.: Correção de pontos da comanda 1047" /></label>
          {error && <p className="form-error-banner" role="alert">{error}</p>}
        </div>
        <div className="modal-footer"><button type="button" className="secondary-button" disabled={saving} onClick={onClose}>Cancelar</button><button className="primary-button" disabled={saving}>{saving ? 'Registrando...' : 'Confirmar ajuste'}</button></div>
      </form>
    </Modal>
  )
}

function couponPriority(coupon: CustomerCoupon) {
  if (coupon.availability === 'Available') return 0
  if (coupon.availability === 'Scheduled') return 1
  if (coupon.availability === 'Expired') return 2
  return 3
}

function formatBirthDate(value: string) {
  const date = new Date(`${value}T00:00:00`)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleDateString('pt-BR')
}

function formatMonthYear(value: string) {
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? 'data não informada' : date.toLocaleDateString('pt-BR', { month: 'short', year: 'numeric' })
}

function formatDate(value: string) {
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleDateString('pt-BR')
}

function formatDateTime(value: string) {
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })
}
