import { BarChart3, ChefHat, FileSpreadsheet, LoaderCircle, TrendingUp } from 'lucide-react'
import { useState } from 'react'
import { useToast } from '../components/ui/toast'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { queryKeys } from '../lib/queryKeys'
import { PageHeader } from '../components/ui/PageHeader'
import { adminService } from '../services/adminService'
import { getAuthenticatedUser } from '../services/authSession'
import { getUserErrorMessage } from '../utils/errors'
import { translateEnum } from '../utils/presentation'

const currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })
const initialToday = new Date().toISOString().slice(0, 10)
const initialThirtyDaysAgo = new Date(new Date().setDate(new Date().getDate() - 30)).toISOString().slice(0, 10)

export function FinancialReportsPage() {
  const [from, setFrom] = useState(initialThirtyDaysAgo)
  const [to, setTo] = useState(initialToday)
  const [period, setPeriod] = useState({ from: initialThirtyDaysAgo, to: initialToday })
  const [exporting, setExporting] = useState(false)
  const toast = useToast()
  const fromIso = new Date(`${period.from}T00:00:00`).toISOString()
  const toIso = new Date(`${period.to}T23:59:59`).toISOString()
  const { data: report, isRefreshing } = useAdminQuery(
    queryKeys.financialReport(fromIso, toIso),
    (signal) => adminService.financialReport(fromIso, toIso, signal),
  )

  function applyPeriod() {
    if (!from || !to || from > to) {
      toast.error('Período inválido', 'A data inicial deve ser anterior ou igual à data final.')
      return
    }
    setPeriod({ from, to })
  }

  async function exportExcel() {
    setExporting(true)
    try {
      const [orders, payments, unit, exporter] = await Promise.all([
        adminService.orders(),
        adminService.payments(),
        adminService.unitSettings(),
        import('../features/reports/exportFinancialReport'),
      ])
      const result = await exporter.exportFinancialReportExcel({
        report,
        orders,
        payments,
        period,
        unitName: unit.tradeName || unit.name,
        generatedBy: getAuthenticatedUser()?.displayName ?? 'Usuário administrativo',
      })
      toast.success('Relatório Excel gerado', `${result.fileName} contém ${result.orders} pedido(s) e ${result.payments} pagamento(s).`)
    } catch (error) {
      toast.error('Não foi possível gerar o relatório', getUserErrorMessage(error))
    } finally {
      setExporting(false)
    }
  }

  return (
    <>
      <PageHeader title="Relatórios financeiros" description="Analise os resultados e gere uma planilha completa para gestão." actions={<button className="secondary-button excel-export-button" disabled={exporting || isRefreshing} aria-busy={exporting} onClick={() => void exportExcel()}>{exporting ? <LoaderCircle className="spin-icon" size={16} /> : <FileSpreadsheet size={16} />} {exporting ? 'Gerando Excel...' : 'Exportar Excel (.xlsx)'}</button>} />
      <div className="toolbar report-period"><label>De<input type="date" value={from} onChange={(event) => setFrom(event.target.value)} /></label><label>Até<input type="date" value={to} onChange={(event) => setTo(event.target.value)} /></label><button className="primary-button" disabled={isRefreshing} onClick={applyPeriod}>{isRefreshing ? 'Carregando...' : 'Aplicar período'}</button></div>
      <aside className="report-export-info" aria-label="Conteúdo da exportação Excel"><FileSpreadsheet size={22} /><div><strong>Relatório gerencial em Excel</strong><span>Resumo executivo, pedidos detalhados e pagamentos em abas separadas, com totais, percentuais, filtros de período e valores numéricos prontos para análise.</span></div></aside>
      <section className="cash-metrics" aria-label="Indicadores financeiros e operacionais">
        <article><span>Vendas brutas</span><strong>{currency.format(report.grossSales)}</strong></article>
        <article><span>Valor recebido líquido</span><strong>{currency.format(report.paidAmount)}</strong></article>
        <article><span>CMV estimado</span><strong>{currency.format(report.foodCost)}</strong></article>
        <article><span>Margem de contribuição</span><strong>{currency.format(report.contributionMargin)}</strong><small>{report.contributionMarginPercentage.toLocaleString('pt-BR')}% das vendas</small></article>
        <article><span>Ticket médio</span><strong>{currency.format(report.averageTicket)}</strong></article>
        <article><span>Pedidos</span><strong>{report.orderCount}</strong></article>
        <article><span>Tempo médio de preparo</span><strong>{report.averagePreparationMinutes.toLocaleString('pt-BR')} min</strong><small>{report.completedTickets} tickets concluídos</small></article>
        <article><span>Produção dentro da meta</span><strong>{report.onTimeRate.toLocaleString('pt-BR')}%</strong></article>
      </section>
      <section className="dashboard-grid">
        <article className="surface-card report-card"><div className="card-heading"><div><h2>Vendas por canal</h2><p>Participação no período selecionado.</p></div><TrendingUp /></div>{report.channels.map((item) => <div className="report-row" key={item.channel}><span>{translateEnum(item.channel)}<small>{item.orders} pedidos</small></span><div className="report-bar"><i style={{ width: `${report.grossSales ? item.total / report.grossSales * 100 : 0}%` }} /></div><strong>{currency.format(item.total)}</strong></div>)}</article>
        <article className="surface-card report-card"><div className="card-heading"><div><h2>Formas de pagamento</h2><p>Recebimentos confirmados.</p></div><BarChart3 /></div>{report.paymentMethods.map((item) => <div className="report-row" key={item.method}><span>{item.method}<small>{item.payments} pagamentos</small></span><div className="report-bar"><i style={{ width: `${report.paidAmount ? item.total / report.paidAmount * 100 : 0}%` }} /></div><strong>{currency.format(item.total)}</strong></div>)}</article>
        <article className="surface-card report-card production-report-card"><div className="card-heading"><div><h2>Desempenho da produção</h2><p>Tempo entre início e conclusão por praça.</p></div><ChefHat /></div>{report.productionStations.length ? report.productionStations.map((item) => <div className="report-row" key={item.station}><span>{item.station}<small>{item.tickets} tickets · média de {item.averagePreparationMinutes.toLocaleString('pt-BR')} min</small></span><div className="report-bar"><i style={{ width: `${Math.min(item.onTimeRate, 100)}%` }} /></div><strong>{item.onTimeRate.toLocaleString('pt-BR')}%</strong></div>) : <p className="report-empty">Ainda não há tickets concluídos no período.</p>}</article>
      </section>
    </>
  )
}
