import writeXlsxFile, { type Cell, type Row, type SheetData } from 'write-excel-file/browser'
import type { FinancialReport, ManagedOrder, Payment } from '../../types/admin'
import { translateEnum } from '../../utils/presentation'

const colors = {
  primary: '#A83300',
  primaryLight: '#FFF0E9',
  dark: '#252A34',
  muted: '#667085',
  border: '#D9DEE5',
  header: '#F2F4F7',
  white: '#FFFFFF',
  success: '#16794B',
}

const currencyFormat = '[$R$-pt-BR] #,##0.00'
const percentageFormat = '0.00%'
const dateTimeFormat = 'dd/mm/yyyy hh:mm'

export interface FinancialReportExportInput {
  report: FinancialReport
  orders: ManagedOrder[]
  payments: Payment[]
  period: { from: string; to: string }
  unitName: string
  generatedBy: string
  generatedAt?: Date
}

export interface FinancialReportExportResult {
  fileName: string
  orders: number
  payments: number
}

export async function exportFinancialReportExcel(input: FinancialReportExportInput): Promise<FinancialReportExportResult> {
  const filteredOrders = filterOrders(input.orders, input.period)
  const filteredPayments = filterPayments(input.payments, input.period)
  const sheets = createFinancialReportSheets({
    ...input,
    orders: filteredOrders,
    payments: filteredPayments,
  })
  const fileName = createFileName(input.unitName, input.period)

  await writeXlsxFile(sheets, { fontFamily: 'Aptos', fontSize: 10 }).toFile(fileName)

  return {
    fileName,
    orders: filteredOrders.length,
    payments: filteredPayments.length,
  }
}

export function createFinancialReportSheets(input: FinancialReportExportInput) {
  return [
    {
      data: createSummarySheet(input),
      sheet: 'Resumo Executivo',
      columns: widths(22, 18, 18, 18, 18, 18, 18, 18),
      stickyRowsCount: 5,
      showGridLines: false,
      zoomScale: 90,
    },
    {
      data: createOrdersSheet(input),
      sheet: 'Pedidos',
      columns: widths(12, 20, 17, 18, 18, 12, 52, 18),
      stickyRowsCount: 5,
      showGridLines: false,
      orientation: 'landscape' as const,
      zoomScale: 85,
    },
    {
      data: createPaymentsSheet(input),
      sheet: 'Pagamentos',
      columns: widths(20, 22, 22, 18, 18, 18, 18, 24, 38),
      stickyRowsCount: 5,
      showGridLines: false,
      orientation: 'landscape' as const,
      zoomScale: 85,
    },
  ]
}

function createSummarySheet({ report, period, unitName, generatedBy, generatedAt = new Date() }: FinancialReportExportInput): SheetData {
  const rows: SheetData = [
    titleRow(`${unitName} · Relatório financeiro`, 8),
    subtitleRow(`Período: ${formatDate(period.from)} a ${formatDate(period.to)}`, 8),
    metadataRow(`Gerado em ${formatDateTime(generatedAt)} por ${generatedBy}`, 8),
    emptyRow(8),
    sectionRow('RESUMO EXECUTIVO', 8),
    [
      metricLabel('Vendas brutas'), metricValue(report.grossSales, currencyFormat),
      metricLabel('Valor recebido'), metricValue(report.paidAmount, currencyFormat),
      metricLabel('Ticket médio'), metricValue(report.averageTicket, currencyFormat),
      metricLabel('Pedidos'), metricValue(report.orderCount, '#,##0'),
    ],
    [
      metricLabel('CMV estimado'), metricValue(report.foodCost, currencyFormat),
      metricLabel('Margem de contribuição'), metricValue(report.contributionMargin, currencyFormat),
      metricLabel('Tempo médio de preparo'), metricValue(report.averagePreparationMinutes, '0.0 "min"'),
      metricLabel('Dentro da meta'), metricValue(report.onTimeRate / 100, percentageFormat),
    ],
    emptyRow(8),
    sectionRow('VENDAS POR CANAL', 8),
    tableHeaderRow(['Canal', 'Pedidos', 'Faturamento', 'Participação'], 8),
    ...report.channels.map((item, index) => padRow([
      bodyCell(translateEnum(item.channel), index),
      numericCell(item.orders, '#,##0', index),
      numericCell(item.total, currencyFormat, index),
      numericCell(report.grossSales ? item.total / report.grossSales : 0, percentageFormat, index),
    ], 8)),
    padRow([
      totalLabel('Total'),
      totalValue(report.channels.reduce((total, item) => total + item.orders, 0), '#,##0'),
      totalValue(report.channels.reduce((total, item) => total + item.total, 0), currencyFormat),
      totalValue(report.grossSales ? report.channels.reduce((total, item) => total + item.total, 0) / report.grossSales : 0, percentageFormat),
    ], 8),
    emptyRow(8),
    sectionRow('FORMAS DE PAGAMENTO', 8),
    tableHeaderRow(['Método', 'Pagamentos', 'Valor recebido', 'Participação'], 8),
    ...report.paymentMethods.map((item, index) => padRow([
      bodyCell(item.method, index),
      numericCell(item.payments, '#,##0', index),
      numericCell(item.total, currencyFormat, index),
      numericCell(report.paidAmount ? item.total / report.paidAmount : 0, percentageFormat, index),
    ], 8)),
    padRow([
      totalLabel('Total'),
      totalValue(report.paymentMethods.reduce((total, item) => total + item.payments, 0), '#,##0'),
      totalValue(report.paymentMethods.reduce((total, item) => total + item.total, 0), currencyFormat),
      totalValue(report.paidAmount ? report.paymentMethods.reduce((total, item) => total + item.total, 0) / report.paidAmount : 0, percentageFormat),
    ], 8),
    emptyRow(8),
    sectionRow('DESEMPENHO DA PRODUÇÃO', 8),
    tableHeaderRow(['Praça', 'Tickets', 'Tempo médio', 'Dentro da meta'], 8),
    ...report.productionStations.map((item, index) => padRow([
      bodyCell(item.station, index),
      numericCell(item.tickets, '#,##0', index),
      numericCell(item.averagePreparationMinutes, '0.0 "min"', index),
      numericCell(item.onTimeRate / 100, percentageFormat, index),
    ], 8)),
    emptyRow(8),
    noteRow('Documento gerado pelo ProjetoPizza. Valores monetários em reais (BRL).', 8),
  ]

  return rows
}

function createOrdersSheet({ orders, period, unitName, generatedBy, generatedAt = new Date() }: FinancialReportExportInput): SheetData {
  const rows: SheetData = [
    titleRow(`${unitName} · Pedidos`, 8),
    subtitleRow(`Período: ${formatDate(period.from)} a ${formatDate(period.to)}`, 8),
    metadataRow(`${orders.length} registro(s) · Gerado em ${formatDateTime(generatedAt)} por ${generatedBy}`, 8),
    emptyRow(8),
    tableHeaderRow(['Pedido', 'Data', 'Canal', 'Atendimento', 'Status', 'Itens', 'Descrição dos itens', 'Total'], 8),
    ...orders.map((order, index) => [
      bodyCell(`#${order.number}`, index),
      dateCell(order.createdAt, index),
      bodyCell(translateEnum(order.channel), index),
      bodyCell(translateEnum(order.fulfillment), index),
      bodyCell(translateEnum(order.status), index),
      numericCell(order.items.reduce((total, item) => total + item.quantity, 0), '#,##0', index),
      bodyCell(order.items.map((item) => `${item.quantity}x ${item.name}`).join(' · ') || 'Sem itens', index, true),
      numericCell(order.total, currencyFormat, index),
    ]),
    [
      totalLabel('TOTAL DO PERÍODO', 7),
      null, null, null, null, null, null,
      totalValue(orders.reduce((total, order) => total + order.total, 0), currencyFormat),
    ],
  ]

  return rows
}

function createPaymentsSheet({ payments, period, unitName, generatedBy, generatedAt = new Date() }: FinancialReportExportInput): SheetData {
  const rows: SheetData = [
    titleRow(`${unitName} · Pagamentos`, 9),
    subtitleRow(`Período: ${formatDate(period.from)} a ${formatDate(period.to)}`, 9),
    metadataRow(`${payments.length} registro(s) · Gerado em ${formatDateTime(generatedAt)} por ${generatedBy}`, 9),
    emptyRow(9),
    tableHeaderRow(['Data', 'Pagador', 'Método', 'Status', 'Valor', 'Recebido', 'Troco', 'Referência', 'Conta'], 9),
    ...payments.map((payment, index) => [
      dateCell(payment.paidAt, index),
      bodyCell(payment.payer ?? 'Pagamento único', index),
      bodyCell(payment.method, index),
      bodyCell(translateEnum(payment.status), index),
      numericCell(payment.amount, currencyFormat, index),
      numericCell(payment.receivedAmount, currencyFormat, index),
      numericCell(payment.changeAmount, currencyFormat, index),
      bodyCell(payment.externalReference ?? '—', index),
      bodyCell(payment.billId, index),
    ]),
    [
      totalLabel('TOTAL DO PERÍODO', 4),
      null, null, null,
      totalValue(payments.reduce((total, payment) => total + payment.amount, 0), currencyFormat),
      totalValue(payments.reduce((total, payment) => total + payment.receivedAmount, 0), currencyFormat),
      totalValue(payments.reduce((total, payment) => total + payment.changeAmount, 0), currencyFormat),
      null,
      null,
    ],
  ]

  return rows
}

function filterOrders(orders: ManagedOrder[], period: FinancialReportExportInput['period']) {
  return orders
    .filter((order) => isWithinPeriod(order.createdAt, period))
    .sort((left, right) => new Date(left.createdAt).getTime() - new Date(right.createdAt).getTime())
}

function filterPayments(payments: Payment[], period: FinancialReportExportInput['period']) {
  return payments
    .filter((payment) => payment.paidAt && isWithinPeriod(payment.paidAt, period))
    .sort((left, right) => new Date(left.paidAt!).getTime() - new Date(right.paidAt!).getTime())
}

function isWithinPeriod(value: string, period: FinancialReportExportInput['period']) {
  const timestamp = new Date(value).getTime()
  const from = new Date(`${period.from}T00:00:00`).getTime()
  const to = new Date(`${period.to}T23:59:59.999`).getTime()
  return timestamp >= from && timestamp <= to
}

function titleRow(title: string, columns: number): Row {
  return padRow([{
    value: title,
    columnSpan: columns,
    height: 34,
    backgroundColor: colors.dark,
    textColor: colors.white,
    fontSize: 18,
    fontWeight: 'bold',
    alignVertical: 'center',
  }], columns)
}

function subtitleRow(subtitle: string, columns: number): Row {
  return padRow([{
    value: subtitle,
    columnSpan: columns,
    height: 25,
    backgroundColor: colors.primary,
    textColor: colors.white,
    fontSize: 11,
    fontWeight: 'bold',
    alignVertical: 'center',
  }], columns)
}

function metadataRow(value: string, columns: number): Row {
  return padRow([{
    value,
    columnSpan: columns,
    height: 22,
    backgroundColor: colors.primaryLight,
    textColor: colors.muted,
    fontSize: 9,
    alignVertical: 'center',
  }], columns)
}

function sectionRow(title: string, columns: number): Row {
  return padRow([{
    value: title,
    columnSpan: columns,
    height: 24,
    backgroundColor: colors.header,
    textColor: colors.dark,
    fontWeight: 'bold',
    bottomBorderColor: colors.primary,
    bottomBorderStyle: 'medium',
    alignVertical: 'center',
  }], columns)
}

function tableHeaderRow(labels: string[], columns: number): Row {
  return padRow(labels.map((label) => ({
    value: label,
    height: 24,
    backgroundColor: colors.dark,
    textColor: colors.white,
    fontWeight: 'bold' as const,
    borderColor: colors.dark,
    borderStyle: 'thin' as const,
    alignVertical: 'center' as const,
  })), columns)
}

function metricLabel(value: string): Cell {
  return {
    value,
    height: 31,
    backgroundColor: colors.header,
    textColor: colors.muted,
    fontWeight: 'bold',
    borderColor: colors.border,
    borderStyle: 'thin',
    alignVertical: 'center',
  }
}

function metricValue(value: number, format: string): Cell {
  return {
    value,
    format,
    height: 31,
    backgroundColor: colors.white,
    textColor: colors.primary,
    fontSize: 13,
    fontWeight: 'bold',
    borderColor: colors.border,
    borderStyle: 'thin',
    align: 'right',
    alignVertical: 'center',
  }
}

function bodyCell(value: string, index: number, wrap = false): Cell {
  return {
    value,
    height: wrap ? 31 : 24,
    backgroundColor: index % 2 ? colors.header : colors.white,
    borderColor: colors.border,
    borderStyle: 'thin',
    alignVertical: 'center',
    wrap,
  }
}

function numericCell(value: number, format: string, index: number): Cell {
  return {
    value,
    format,
    height: 24,
    backgroundColor: index % 2 ? colors.header : colors.white,
    borderColor: colors.border,
    borderStyle: 'thin',
    align: 'right',
    alignVertical: 'center',
  }
}

function dateCell(value: string | undefined, index: number): Cell {
  if (!value) return bodyCell('—', index)
  return {
    ...bodyCell('', index),
    value: new Date(value),
    type: Date,
    format: dateTimeFormat,
  }
}

function totalLabel(value: string, columnSpan = 1): Cell {
  return {
    value,
    columnSpan,
    height: 26,
    backgroundColor: colors.primaryLight,
    textColor: colors.primary,
    fontWeight: 'bold',
    borderColor: colors.primary,
    borderStyle: 'thin',
    alignVertical: 'center',
  }
}

function totalValue(value: number, format: string): Cell {
  return {
    value,
    format,
    height: 26,
    backgroundColor: colors.primaryLight,
    textColor: colors.success,
    fontWeight: 'bold',
    borderColor: colors.primary,
    borderStyle: 'thin',
    align: 'right',
    alignVertical: 'center',
  }
}

function noteRow(value: string, columns: number): Row {
  return padRow([{
    value,
    columnSpan: columns,
    height: 24,
    textColor: colors.muted,
    fontStyle: 'italic',
    fontSize: 9,
  }], columns)
}

function emptyRow(columns: number): Row {
  return Array.from({ length: columns }, () => null)
}

function padRow(cells: Cell[], columns: number): Row {
  return [...cells, ...Array.from({ length: Math.max(0, columns - cells.length) }, () => null)]
}

function widths(...values: number[]) {
  return values.map((width) => ({ width }))
}

function formatDate(value: string) {
  const [year, month, day] = value.split('-')
  return `${day}/${month}/${year}`
}

function formatDateTime(value: Date) {
  return value.toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })
}

function createFileName(unitName: string, period: FinancialReportExportInput['period']) {
  const unit = unitName
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[^a-zA-Z0-9]+/g, '-')
    .replace(/^-|-$/g, '')
    .toLowerCase()
  return `relatorio-financeiro-${unit || 'pizzaria'}-${period.from}-a-${period.to}.xlsx`
}
