import { CreditCard, RotateCcw, Search, ShieldCheck } from 'lucide-react'
import { type FormEvent, useMemo, useState } from 'react'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { usePdfTableExport } from '../hooks/usePdfTableExport'
import { queryKeys } from '../lib/queryKeys'
import { PageHeader } from '../components/ui/PageHeader'
import { PdfExportButton } from '../components/ui/PdfExportButton'
import { Modal } from '../components/ui/Modal'
import { CurrencyInput } from '../components/ui/CurrencyInput'
import { StatusBadge } from '../components/ui/StatusBadge'
import { adminService } from '../services/adminService'
import { hasPermission } from '../services/authSession'
import type { Payment } from '../types/admin'
import { getUserErrorMessage } from '../utils/errors'
import { translateEnum } from '../utils/presentation'
import { useToast } from '../components/ui/toast'

const currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })

export function PaymentsPage() {
  const { data: payments, setData: setPayments } = useAdminQuery(queryKeys.payments, adminService.payments)
  const [search, setSearch] = useState('')
  const [refunding, setRefunding] = useState<Payment>()
  const [refundAmount, setRefundAmount] = useState(0)
  const [refundReason, setRefundReason] = useState('')
  const [busy, setBusy] = useState(false)
  const toast = useToast()
  const { exportPdf, exporting } = usePdfTableExport()
  const visible = useMemo(() => payments.filter((payment) => `${payment.payer ?? ''} ${payment.method} ${payment.status} ${payment.externalReference ?? ''} ${payment.billId}`.toLowerCase().includes(search.toLowerCase())), [payments, search])

  function exportPayments() {
    void exportPdf({
      title: 'Relatório de pagamentos',
      subtitle: search ? `Busca aplicada: ${search}` : 'Todos os pagamentos disponíveis',
      fileName: `pagamentos-${new Date().toISOString().slice(0, 10)}.pdf`,
      orientation: 'landscape',
      columns: ['Data', 'Pagador', 'Método', 'Status', 'Valor', 'Recebido', 'Troco', 'Referência'],
      rows: visible.map((payment) => [
        payment.paidAt ? new Date(payment.paidAt).toLocaleString('pt-BR') : 'Pendente',
        payment.payer ?? 'Pagamento único',
        payment.method,
        translateEnum(payment.status),
        currency.format(payment.amount),
        currency.format(payment.receivedAmount),
        currency.format(payment.changeAmount),
        payment.externalReference ?? '-',
      ]),
      metrics: [
        { label: 'Pagamentos', value: String(visible.length) },
        { label: 'Valor', value: currency.format(visible.reduce((sum, payment) => sum + payment.amount, 0)) },
        { label: 'Recebido', value: currency.format(visible.reduce((sum, payment) => sum + payment.receivedAmount, 0)) },
        { label: 'Troco', value: currency.format(visible.reduce((sum, payment) => sum + payment.changeAmount, 0)) },
      ],
      rightAlignedColumns: [4, 5, 6],
    })
  }

  function openRefund(payment: Payment) {
    const refundable = payment.amount - (payment.refundedAmount ?? 0)
    setRefunding(payment)
    setRefundAmount(refundable)
    setRefundReason('')
  }

  async function refund(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!refunding || !refundReason.trim()) return
    setBusy(true)
    try {
      await adminService.refundPayment(refunding.id, refundAmount, refundReason.trim())
      const refundedAmount = (refunding.refundedAmount ?? 0) + refundAmount
      setPayments((current) => current.map((payment) => payment.id === refunding.id ? {
        ...payment,
        refundedAmount,
        refundedAt: new Date().toISOString(),
        refundReason: refundReason.trim(),
        status: refundedAmount >= payment.amount ? 'Refunded' : 'PartiallyRefunded',
      } : payment))
      toast.success('Estorno autorizado', `${currency.format(refundAmount)} foi registrado e a conta foi reaberta quando necessário.`)
      setRefunding(undefined)
    } catch (error) {
      toast.error('Não foi possível estornar', getUserErrorMessage(error))
    } finally {
      setBusy(false)
    }
  }

  return (
    <>
      <PageHeader title="Pagamentos" description="Consulte os recebimentos e referências da operação." actions={<PdfExportButton exporting={exporting} onClick={exportPayments} label="Exportar pagamentos em PDF" />} />
      <div className="toolbar"><div className="toolbar-search"><Search size={17} /><input aria-label="Buscar pagamento" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar método, conta ou referência..." /></div></div>
      <section className="surface-card data-table-card">
        <div className="data-table-header payment-grid"><span>Pagamento</span><span>Método</span><span>Status</span><span>Recebido</span><span>Estornado</span><span>Ação</span></div>
        {visible.map((payment) => { const refundable = payment.amount - (payment.refundedAmount ?? 0); return <div className="data-table-row payment-grid" key={payment.id}><span className="cell-title"><CreditCard size={17} /><span><strong>{payment.payer ?? payment.id.slice(0, 8)}</strong><small>{payment.paidAt ? new Date(payment.paidAt).toLocaleString('pt-BR') : 'Pendente'}</small></span></span><span>{payment.method}<small>{payment.externalReference}</small></span><StatusBadge status={payment.status} /><strong>{currency.format(payment.receivedAmount)}</strong><span>{currency.format(payment.refundedAmount ?? 0)}</span><span>{hasPermission('admin:write') && refundable > 0 && ['Paid', 'PartiallyRefunded'].includes(payment.status) ? <button className="secondary-button compact" onClick={() => openRefund(payment)}><RotateCcw size={14} /> Estornar</button> : <small>Sem ação</small>}</span></div> })}
      </section>
      {refunding && <Modal open title="Autorizar estorno" description={`Pagamento de ${currency.format(refunding.amount)} via ${refunding.method}.`} isBusy={busy} onClose={() => setRefunding(undefined)}><form onSubmit={refund}><div className="modal-body"><aside className="form-note"><ShieldCheck size={19} /><span><strong>Operação financeira auditada</strong>Estornos em dinheiro exigem caixa aberto. Estornos de contas fechadas reabrem o saldo pendente.</span></aside><label className="field-label">Valor do estorno<CurrencyInput value={refundAmount} max={refunding.amount - (refunding.refundedAmount ?? 0)} onCurrencyValueChange={setRefundAmount} required /></label><label className="field-label">Motivo<textarea rows={3} maxLength={500} value={refundReason} onChange={(event) => setRefundReason(event.target.value)} required /></label></div><div className="modal-footer"><button type="button" className="secondary-button" onClick={() => setRefunding(undefined)}>Voltar</button><button className="danger-button" disabled={busy || refundAmount <= 0 || !refundReason.trim()}>{busy ? 'Processando...' : 'Autorizar estorno'}</button></div></form></Modal>}
    </>
  )
}
