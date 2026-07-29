import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowLeft, CreditCard, Plus, ReceiptText } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useRoute } from 'wouter'
import { PaymentDialog } from '../components/payments/PaymentDialog'
import { FieldError } from '../components/ui/FieldError'
import { Modal } from '../components/ui/Modal'
import { PageHeader } from '../components/ui/PageHeader'
import { StatusBadge } from '../components/ui/StatusBadge'
import { useToast } from '../components/ui/toast'
import { openTableSchema, type OpenTableFormData } from '../features/admin/formSchemas'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { queryKeys } from '../lib/queryKeys'
import { adminService } from '../services/adminService'
import { hasPermission } from '../services/authSession'
import { getUserErrorMessage } from '../utils/errors'
import { translateEnum } from '../utils/presentation'

const currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })

export function TableDetailPage() {
  const [, params] = useRoute('/admin/tables/:id')
  const id = params?.id ?? ''
  const { data: detail, setData: setDetail, refresh: refreshDetail } = useAdminQuery(queryKeys.table(id), (signal) => adminService.table(id, signal))
  const { data: methods } = useAdminQuery(queryKeys.paymentMethods, adminService.paymentMethods)
  const [isPaymentOpen, setPaymentOpen] = useState(false)
  const [isOpenTableModalOpen, setOpenTableModalOpen] = useState(false)
  const [busyAction, setBusyAction] = useState<'open' | 'bill'>()
  const toast = useToast()
  const openTableForm = useForm<OpenTableFormData>({
    resolver: zodResolver(openTableSchema),
    defaultValues: { partySize: 2 },
  })

  async function openTable({ partySize }: OpenTableFormData) {
    setBusyAction('open')
    try {
      await adminService.openTable(id, partySize)
      await refreshDetail()
      setOpenTableModalOpen(false)
      toast.success('Mesa aberta', `Atendimento iniciado para ${partySize} pessoa(s).`)
    } catch (error) {
      toast.error('Não foi possível abrir a mesa', getUserErrorMessage(error))
    } finally {
      setBusyAction(undefined)
    }
  }

  async function requestBill() {
    if (!detail.sessionId) return
    setBusyAction('bill')
    try {
      const result = await adminService.requestBill(detail.sessionId) as { id: string }
      setDetail({ ...detail, billId: result.id, table: { ...detail.table, status: 'Conta solicitada' } })
      toast.success('Conta solicitada', 'A comanda está pronta para receber o pagamento.')
    } catch (error) {
      toast.error('Não foi possível solicitar a conta', getUserErrorMessage(error))
    } finally {
      setBusyAction(undefined)
    }
  }

  function paymentCompleted(amount: number) {
    const remaining = Math.max(0, detail.remainingAmount - amount)
    setDetail({ ...detail, remainingAmount: remaining, table: { ...detail.table, status: remaining === 0 ? 'Livre' : 'Pagamento pendente' } })
    setPaymentOpen(false)
  }

  return (
    <>
      <Link className="back-link" href="/admin/tables"><ArrowLeft size={16} /> Voltar para mesas</Link>
      <PageHeader title={detail.table.name} description={detail.sessionNumber ? `Comanda #${detail.sessionNumber} · ${detail.table.area}` : 'Mesa disponível para novo atendimento'} actions={<><StatusBadge status={detail.table.status} />{detail.table.status === 'Livre' && hasPermission('operations:write') && <button className="primary-button" onClick={() => setOpenTableModalOpen(true)}><Plus size={16} /> Abrir mesa</button>}</>} />
      <section className="detail-layout">
        <div className="detail-main">
          <article className="surface-card">
            <div className="card-heading"><div><h2>Pedidos da comanda</h2><p>Itens registrados neste atendimento.</p></div></div>
            {detail.orders.length ? detail.orders.map((order) => (
              <div className="order-row" key={order.number}><div><strong>Pedido #{order.number}</strong><span>{translateEnum(order.channel)}</span></div><StatusBadge status={order.status} /><strong>{currency.format(order.total)}</strong></div>
            )) : <div className="empty-inline">Nenhum pedido registrado.</div>}
          </article>
        </div>
        <aside className="detail-sidebar">
          <article className="surface-card summary-card">
            <h2>Resumo</h2>
            <div className="summary-line"><span>Subtotal</span><strong>{currency.format(detail.subtotalAmount)}</strong></div>
            <div className="summary-line"><span>Taxa de serviço ({detail.serviceFeePercentage}%)</span><strong>{currency.format(detail.serviceFeeAmount)}</strong></div>
            <div className="summary-total"><span>Total</span><strong>{currency.format(detail.totalAmount)}</strong></div>
            {detail.requestedSplitCount && <div className="payment-request-note">Mesa solicitou divisão entre <strong>{detail.requestedSplitCount} pessoas</strong>.</div>}
            {hasPermission('operations:write') && <button className="primary-button full" disabled={!detail.billId} onClick={() => setPaymentOpen(true)}><CreditCard size={17} /> Registrar pagamento</button>}
          </article>
          <article className="surface-card quick-actions">
            <h2>Ações rápidas</h2>
            {hasPermission('operations:write') && <button disabled={!detail.sessionId || Boolean(detail.billId) || busyAction === 'bill'} onClick={() => void requestBill()}><ReceiptText size={17} /> {busyAction === 'bill' ? 'Solicitando...' : 'Solicitar conta'}</button>}
          </article>
        </aside>
      </section>
      {isOpenTableModalOpen && <Modal open title={`Abrir ${detail.table.name}`} description="Informe quantas pessoas serão atendidas nesta mesa." isBusy={busyAction === 'open'} onClose={() => setOpenTableModalOpen(false)}>
        <form onSubmit={openTableForm.handleSubmit(openTable)} noValidate>
          <div className="modal-body"><div className="form-grid"><label className="field-label">Quantidade de pessoas<input type="number" min="1" max="50" autoFocus aria-invalid={Boolean(openTableForm.formState.errors.partySize)} {...openTableForm.register('partySize', { valueAsNumber: true })} /><FieldError message={openTableForm.formState.errors.partySize?.message} /></label></div></div>
          <div className="modal-footer"><button type="button" className="secondary-button" disabled={busyAction === 'open'} onClick={() => setOpenTableModalOpen(false)}>Cancelar</button><button className="primary-button" disabled={busyAction === 'open'} aria-busy={busyAction === 'open'}><Plus size={16} /> {busyAction === 'open' ? 'Abrindo...' : 'Abrir mesa'}</button></div>
        </form>
      </Modal>}
      {isPaymentOpen && detail.billId && <PaymentDialog billId={detail.billId} remainingAmount={detail.remainingAmount} requestedSplitCount={detail.requestedSplitCount} methods={methods} onClose={() => setPaymentOpen(false)} onPaid={paymentCompleted} />}
    </>
  )
}
