import { CreditCard, Search } from 'lucide-react'
import { useMemo, useState } from 'react'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { usePdfTableExport } from '../hooks/usePdfTableExport'
import { queryKeys } from '../lib/queryKeys'
import { PageHeader } from '../components/ui/PageHeader'
import { PdfExportButton } from '../components/ui/PdfExportButton'
import { StatusBadge } from '../components/ui/StatusBadge'
import { adminService } from '../services/adminService'
import { translateEnum } from '../utils/presentation'

const currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })

export function PaymentsPage() {
  const { data: payments } = useAdminQuery(queryKeys.payments, adminService.payments)
  const [search, setSearch] = useState('')
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
        payment.externalReference ?? '—',
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

  return (
    <>
      <PageHeader title="Pagamentos" description="Consulte os recebimentos e referências da operação." actions={<PdfExportButton exporting={exporting} onClick={exportPayments} label="Exportar pagamentos em PDF" />} />
      <div className="toolbar"><div className="toolbar-search"><Search size={17} /><input aria-label="Buscar pagamento" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar método, conta ou referência..." /></div></div>
      <section className="surface-card data-table-card">
        <div className="data-table-header payment-grid"><span>Pagamento</span><span>Método</span><span>Status</span><span>Recebido</span><span>Troco</span></div>
        {visible.map((payment) => <div className="data-table-row payment-grid" key={payment.id}><span className="cell-title"><CreditCard size={17} /><span><strong>{payment.payer ?? payment.id.slice(0, 8)}</strong><small>{payment.paidAt ? new Date(payment.paidAt).toLocaleString('pt-BR') : 'Pendente'}</small></span></span><span>{payment.method}<small>{payment.externalReference}</small></span><StatusBadge status={payment.status} /><strong>{currency.format(payment.receivedAmount)}</strong><span>{currency.format(payment.changeAmount)}</span></div>)}
      </section>
    </>
  )
}
