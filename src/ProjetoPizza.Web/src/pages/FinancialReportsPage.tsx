import { BarChart3, Download, TrendingUp } from 'lucide-react'
import { useState } from 'react'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { queryKeys } from '../lib/queryKeys'
import { PageHeader } from '../components/ui/PageHeader'
import { adminService } from '../services/adminService'
import { translateEnum } from '../utils/presentation'

const currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })
const initialToday = new Date().toISOString().slice(0, 10)
const initialThirtyDaysAgo = new Date(new Date().setDate(new Date().getDate() - 30)).toISOString().slice(0, 10)

export function FinancialReportsPage() {
  const [from, setFrom] = useState(initialThirtyDaysAgo)
  const [to, setTo] = useState(initialToday)
  const [period, setPeriod] = useState({ from: initialThirtyDaysAgo, to: initialToday })
  const fromIso = new Date(`${period.from}T00:00:00`).toISOString()
  const toIso = new Date(`${period.to}T23:59:59`).toISOString()
  const { data: report, isRefreshing } = useAdminQuery(
    queryKeys.financialReport(fromIso, toIso),
    (signal) => adminService.financialReport(fromIso, toIso, signal),
  )


  return (
    <>
      <PageHeader title="Relatórios financeiros" description="Analise faturamento, canais e formas de pagamento." actions={<button className="secondary-button" onClick={() => window.print()}><Download size={16} /> Exportar relatório</button>} />
      <div className="toolbar report-period"><label>De<input type="date" value={from} onChange={(event) => setFrom(event.target.value)} /></label><label>Até<input type="date" value={to} onChange={(event) => setTo(event.target.value)} /></label><button className="primary-button" disabled={isRefreshing} onClick={() => setPeriod({ from, to })}>{isRefreshing ? 'Carregando...' : 'Aplicar período'}</button></div>
      <section className="cash-metrics"><article><span>Vendas brutas</span><strong>{currency.format(report.grossSales)}</strong></article><article><span>Valor recebido</span><strong>{currency.format(report.paidAmount)}</strong></article><article><span>Ticket médio</span><strong>{currency.format(report.averageTicket)}</strong></article><article><span>Pedidos</span><strong>{report.orderCount}</strong></article></section>
      <section className="dashboard-grid">
        <article className="surface-card report-card"><div className="card-heading"><div><h2>Vendas por canal</h2><p>Participação no período selecionado.</p></div><TrendingUp /></div>{report.channels.map((item) => <div className="report-row" key={item.channel}><span>{translateEnum(item.channel)}<small>{item.orders} pedidos</small></span><div className="report-bar"><i style={{ width: `${report.grossSales ? item.total / report.grossSales * 100 : 0}%` }} /></div><strong>{currency.format(item.total)}</strong></div>)}</article>
        <article className="surface-card report-card"><div className="card-heading"><div><h2>Formas de pagamento</h2><p>Recebimentos confirmados.</p></div><BarChart3 /></div>{report.paymentMethods.map((item) => <div className="report-row" key={item.method}><span>{item.method}<small>{item.payments} pagamentos</small></span><div className="report-bar"><i style={{ width: `${report.paidAmount ? item.total / report.paidAmount * 100 : 0}%` }} /></div><strong>{currency.format(item.total)}</strong></div>)}</article>
      </section>
    </>
  )
}
