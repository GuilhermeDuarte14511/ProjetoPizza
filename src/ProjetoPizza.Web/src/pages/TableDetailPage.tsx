import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowLeft, CreditCard, Link2, MoveRight, Plus, ReceiptText, UserRoundCog } from 'lucide-react'
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
  const { data: tables } = useAdminQuery(queryKeys.tables, adminService.tables)
  const [isPaymentOpen, setPaymentOpen] = useState(false)
  const [isOpenTableModalOpen, setOpenTableModalOpen] = useState(false)
  const [managementAction, setManagementAction] = useState<'waiter' | 'link' | 'transfer'>()
  const [selectedManagementId, setSelectedManagementId] = useState('')
  const [busyAction, setBusyAction] = useState<'open' | 'bill' | 'management'>()
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

  const availableTables = tables.filter((table) => table.status === 'Livre' && !detail.linkedTables.some((linked) => linked.id === table.id))

  function openManagement(action: 'waiter' | 'link' | 'transfer') {
    setManagementAction(action)
    setSelectedManagementId(action === 'waiter' ? (detail.waiters.find((waiter) => waiter.name === detail.waiter)?.id ?? '') : '')
  }

  async function saveManagement() {
    if (!detail.sessionId || !managementAction || !selectedManagementId) return
    setBusyAction('management')
    try {
      if (managementAction === 'waiter') await adminService.assignTableWaiter(detail.sessionId, selectedManagementId)
      if (managementAction === 'link') await adminService.linkTable(detail.sessionId, selectedManagementId)
      if (managementAction === 'transfer') await adminService.transferTable(detail.sessionId, id, selectedManagementId)
      await refreshDetail()
      setManagementAction(undefined)
      toast.success(
        managementAction === 'waiter' ? 'Garçom atualizado' : managementAction === 'link' ? 'Mesas unidas' : 'Atendimento transferido',
        'A alteração já está disponível para a equipe.',
      )
    } catch (error) {
      toast.error('Não foi possível concluir a ação', getUserErrorMessage(error))
    } finally {
      setBusyAction(undefined)
    }
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
              <article className="table-order-detail" key={order.id}>
                <header>
                  <div><strong>Pedido #{order.number}</strong><span>{translateEnum(order.channel)}{order.placedAt ? ` · ${new Date(order.placedAt).toLocaleString('pt-BR')}` : ''}</span></div>
                  <StatusBadge status={order.status} />
                </header>
                <div className="table-order-items">
                  {order.items.map((item) => <article key={item.id}>
                    <span className="table-order-quantity">{item.quantity}×</span>
                    <div><strong>{item.name}</strong>{item.details.map((detailItem) => <small key={detailItem}>{detailItem}</small>)}{item.notes && <small className="table-order-note">Observação: {item.notes}</small>}</div>
                    <span className="table-order-unit"><small>{currency.format(item.unitPrice)} cada</small><strong>{currency.format(item.totalPrice)}</strong></span>
                  </article>)}
                  {!order.items.length && <div className="empty-inline">Este pedido não possui itens registrados.</div>}
                </div>
                {order.notes && <p className="table-order-general-note"><strong>Observações do pedido:</strong> {order.notes}</p>}
                <footer>
                  <span>Subtotal <strong>{currency.format(order.subtotal)}</strong></span>
                  {order.discount > 0 && <span>Desconto <strong>- {currency.format(order.discount)}</strong></span>}
                  {order.serviceFee > 0 && <span>Serviço <strong>{currency.format(order.serviceFee)}</strong></span>}
                  <span className="table-order-total">Total <strong>{currency.format(order.total)}</strong></span>
                </footer>
              </article>
            )) : <div className="empty-inline">Nenhum pedido registrado.</div>}
          </article>
        </div>
        <aside className="detail-sidebar">
          <article className="surface-card summary-card">
            <h2>Resumo</h2>
            {detail.sessionId && <div className="table-service-context"><span><b>Garçom</b>{detail.waiter ?? 'Não atribuído'}</span><span><b>Mesas</b>{detail.linkedTables.map((table) => table.name).join(', ')}</span></div>}
            <div className="summary-line"><span>Subtotal</span><strong>{currency.format(detail.subtotalAmount)}</strong></div>
            <div className="summary-line"><span>Taxa de serviço ({detail.serviceFeePercentage}%)</span><strong>{currency.format(detail.serviceFeeAmount)}</strong></div>
            <div className="summary-total"><span>Total</span><strong>{currency.format(detail.totalAmount)}</strong></div>
            {detail.requestedSplitCount && <div className="payment-request-note">Mesa solicitou divisão entre <strong>{detail.requestedSplitCount} pessoas</strong>.</div>}
            {hasPermission('operations:write') && <button className="primary-button full" disabled={!detail.billId} onClick={() => setPaymentOpen(true)}><CreditCard size={17} /> Registrar pagamento</button>}
          </article>
          <article className="surface-card quick-actions">
            <h2>Ações rápidas</h2>
            {hasPermission('operations:write') && <button disabled={!detail.sessionId || Boolean(detail.billId) || busyAction === 'bill'} onClick={() => void requestBill()}><ReceiptText size={17} /> {busyAction === 'bill' ? 'Solicitando...' : 'Solicitar conta'}</button>}
            {hasPermission('operations:write') && <button disabled={!detail.sessionId} onClick={() => openManagement('waiter')}><UserRoundCog size={17} /> Trocar garçom</button>}
            {hasPermission('operations:write') && <button disabled={!detail.sessionId || availableTables.length === 0} onClick={() => openManagement('link')}><Link2 size={17} /> Unir mesa livre</button>}
            {hasPermission('operations:write') && <button disabled={!detail.sessionId || availableTables.length === 0 || detail.linkedTables.length > 1} onClick={() => openManagement('transfer')}><MoveRight size={17} /> Transferir atendimento</button>}
          </article>
        </aside>
      </section>
      {isOpenTableModalOpen && <Modal open title={`Abrir ${detail.table.name}`} description="Informe quantas pessoas serão atendidas nesta mesa." isBusy={busyAction === 'open'} onClose={() => setOpenTableModalOpen(false)}>
        <form onSubmit={openTableForm.handleSubmit(openTable)} noValidate>
          <div className="modal-body"><div className="form-grid"><label className="field-label">Quantidade de pessoas<input type="number" min="1" max="50" autoFocus aria-invalid={Boolean(openTableForm.formState.errors.partySize)} {...openTableForm.register('partySize', { valueAsNumber: true })} /><FieldError message={openTableForm.formState.errors.partySize?.message} /></label></div></div>
          <div className="modal-footer"><button type="button" className="secondary-button" disabled={busyAction === 'open'} onClick={() => setOpenTableModalOpen(false)}>Cancelar</button><button className="primary-button" disabled={busyAction === 'open'} aria-busy={busyAction === 'open'}><Plus size={16} /> {busyAction === 'open' ? 'Abrindo...' : 'Abrir mesa'}</button></div>
        </form>
      </Modal>}
      {managementAction && <Modal
        open
        title={managementAction === 'waiter' ? 'Trocar garçom' : managementAction === 'link' ? 'Unir mesa ao atendimento' : 'Transferir atendimento'}
        description={managementAction === 'waiter' ? 'Escolha o responsável principal por esta comanda.' : managementAction === 'link' ? 'A mesa escolhida compartilhará a mesma comanda.' : 'A comanda será movida para a mesa escolhida.'}
        isBusy={busyAction === 'management'}
        onClose={() => setManagementAction(undefined)}
      >
        <div className="modal-body"><label className="field-label">{managementAction === 'waiter' ? 'Garçom responsável' : 'Mesa disponível'}<select autoFocus value={selectedManagementId} onChange={(event) => setSelectedManagementId(event.target.value)}><option value="">Selecione</option>{managementAction === 'waiter' ? detail.waiters.map((waiter) => <option key={waiter.id} value={waiter.id}>{waiter.name}</option>) : availableTables.map((table) => <option key={table.id} value={table.id}>{table.name} · {table.area} · {table.capacity} lugares</option>)}</select></label></div>
        <div className="modal-footer"><button type="button" className="secondary-button" disabled={busyAction === 'management'} onClick={() => setManagementAction(undefined)}>Cancelar</button><button type="button" className="primary-button" disabled={!selectedManagementId || busyAction === 'management'} onClick={() => void saveManagement()}>{busyAction === 'management' ? 'Salvando...' : 'Confirmar'}</button></div>
      </Modal>}
      {isPaymentOpen && detail.billId && <PaymentDialog billId={detail.billId} remainingAmount={detail.remainingAmount} requestedSplitCount={detail.requestedSplitCount} billItems={detail.billItems} methods={methods} onClose={() => setPaymentOpen(false)} onPaid={paymentCompleted} />}
    </>
  )
}
