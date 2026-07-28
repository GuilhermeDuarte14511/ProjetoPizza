import { CreditCard, Download, Search } from 'lucide-react'
import { useMemo, useState } from 'react'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { queryKeys } from '../lib/queryKeys'
import { PageHeader } from '../components/ui/PageHeader'
import { StatusBadge } from '../components/ui/StatusBadge'
import { adminService } from '../services/adminService'

const currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })

export function PaymentsPage() {
  const { data: payments } = useAdminQuery(queryKeys.payments, adminService.payments)
  const [search, setSearch] = useState('')
  const visible = useMemo(() => payments.filter((payment) => `${payment.method} ${payment.externalReference ?? ''} ${payment.billId}`.toLowerCase().includes(search.toLowerCase())), [payments, search])

  function exportCsv() {
    const rows = [['Data', 'Método', 'Status', 'Valor', 'Referência'], ...visible.map((item) => [item.paidAt ?? '', item.method, item.status, String(item.amount), item.externalReference ?? ''])]
    const url = URL.createObjectURL(new Blob([rows.map((row) => row.join(';')).join('\n')], { type: 'text/csv' }))
    const anchor = document.createElement('a'); anchor.href = url; anchor.download = 'pagamentos.csv'; anchor.click(); URL.revokeObjectURL(url)
  }

  return (
    <>
      <PageHeader title="Pagamentos" description="Consulte os recebimentos e referências da operação." actions={<button className="secondary-button" onClick={exportCsv}><Download size={16} /> Exportar CSV</button>} />
      <div className="toolbar"><div className="toolbar-search"><Search size={17} /><input aria-label="Buscar pagamento" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar método, conta ou referência..." /></div></div>
      <section className="surface-card data-table-card">
        <div className="data-table-header payment-grid"><span>Pagamento</span><span>Método</span><span>Status</span><span>Recebido</span><span>Troco</span></div>
        {visible.map((payment) => <div className="data-table-row payment-grid" key={payment.id}><span className="cell-title"><CreditCard size={17} /><span><strong>{payment.id.slice(0, 8)}</strong><small>{payment.paidAt ? new Date(payment.paidAt).toLocaleString('pt-BR') : 'Pendente'}</small></span></span><span>{payment.method}<small>{payment.externalReference}</small></span><StatusBadge status={payment.status} /><strong>{currency.format(payment.receivedAmount)}</strong><span>{currency.format(payment.changeAmount)}</span></div>)}
      </section>
    </>
  )
}
