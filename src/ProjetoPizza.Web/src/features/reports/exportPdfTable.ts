import { jsPDF } from 'jspdf'
import { autoTable } from 'jspdf-autotable'

type RgbColor = [number, number, number]

const colors: Record<string, RgbColor> = {
  primary: [168, 51, 0],
  primaryLight: [255, 240, 233],
  dark: [37, 42, 52],
  muted: [102, 112, 133],
  border: [217, 222, 229],
  header: [242, 244, 247],
  white: [255, 255, 255],
}

export interface PdfReportMetric {
  label: string
  value: string
}

export interface PdfTableReportInput {
  title: string
  subtitle?: string
  fileName: string
  unitName: string
  generatedBy: string
  columns: string[]
  rows: string[][]
  metrics?: PdfReportMetric[]
  orientation?: 'portrait' | 'landscape'
  rightAlignedColumns?: number[]
  generatedAt?: Date
}

export interface PdfExportResult {
  fileName: string
  rows: number
  pages: number
}

export function createPdfTableDocument(input: PdfTableReportInput) {
  const generatedAt = input.generatedAt ?? new Date()
  const document = new jsPDF({
    orientation: input.orientation ?? (input.columns.length > 6 ? 'landscape' : 'portrait'),
    unit: 'mm',
    format: 'a4',
    compress: true,
  })

  document.setProperties({
    title: input.title,
    subject: input.subtitle ?? 'Relatório operacional',
    author: input.generatedBy,
    creator: 'ProjetoPizza',
  })

  const pageWidth = document.internal.pageSize.getWidth()
  drawCoverHeader(document, input, pageWidth)

  let tableStartY = 39
  if (input.subtitle) {
    document.setFont('helvetica', 'normal')
    document.setFontSize(9)
    document.setTextColor(...colors.muted)
    document.text(input.subtitle, 12, tableStartY, { maxWidth: pageWidth - 24 })
    tableStartY += 9
  }

  if (input.metrics?.length) {
    drawMetrics(document, input.metrics.slice(0, 4), tableStartY, pageWidth)
    tableStartY += 19
  }

  const columnStyles = Object.fromEntries(
    (input.rightAlignedColumns ?? []).map((columnIndex) => [columnIndex, { halign: 'right' as const }]),
  )
  const rows = input.rows.length
    ? input.rows
    : [[{ content: 'Nenhum registro encontrado para os filtros selecionados.', colSpan: input.columns.length, styles: { halign: 'center' as const, textColor: colors.muted } }]]

  autoTable(document, {
    startY: tableStartY,
    head: [input.columns],
    body: rows,
    theme: 'grid',
    margin: { top: 18, right: 12, bottom: 17, left: 12 },
    styles: {
      font: 'helvetica',
      fontSize: input.columns.length > 7 ? 6.8 : 7.5,
      cellPadding: 2.2,
      overflow: 'linebreak',
      lineColor: colors.border,
      lineWidth: 0.15,
      valign: 'middle',
      textColor: colors.dark,
    },
    headStyles: {
      fillColor: colors.primary,
      textColor: colors.white,
      fontStyle: 'bold',
      minCellHeight: 9,
    },
    alternateRowStyles: { fillColor: colors.header },
    columnStyles,
    rowPageBreak: 'avoid',
    showHead: 'everyPage',
    didDrawPage: ({ pageNumber }) => {
      if (pageNumber === 1) return
      document.setFont('helvetica', 'bold')
      document.setFontSize(8)
      document.setTextColor(...colors.dark)
      document.text(`${input.unitName} · ${input.title}`, 12, 10)
      document.setDrawColor(...colors.primary)
      document.setLineWidth(0.6)
      document.line(12, 13, pageWidth - 12, 13)
    },
  })

  drawFooters(document, generatedAt, input.generatedBy)
  return document
}

export function exportPdfTableReport(input: PdfTableReportInput): PdfExportResult {
  const fileName = ensurePdfExtension(input.fileName)
  const document = createPdfTableDocument(input)
  document.save(fileName)

  return {
    fileName,
    rows: input.rows.length,
    pages: document.getNumberOfPages(),
  }
}

function drawCoverHeader(document: jsPDF, input: PdfTableReportInput, pageWidth: number) {
  document.setFillColor(...colors.dark)
  document.rect(0, 0, pageWidth, 29, 'F')
  document.setFillColor(...colors.primary)
  document.rect(0, 29, pageWidth, 2.5, 'F')

  document.setFont('helvetica', 'bold')
  document.setFontSize(17)
  document.setTextColor(...colors.white)
  document.text(input.title, 12, 13)

  document.setFont('helvetica', 'normal')
  document.setFontSize(9)
  document.text(input.unitName, 12, 21)
}

function drawMetrics(document: jsPDF, metrics: PdfReportMetric[], top: number, pageWidth: number) {
  const gap = 3
  const width = (pageWidth - 24 - gap * (metrics.length - 1)) / metrics.length

  metrics.forEach((metric, index) => {
    const left = 12 + index * (width + gap)
    document.setFillColor(...colors.primaryLight)
    document.setDrawColor(...colors.border)
    document.setLineWidth(0.15)
    document.roundedRect(left, top, width, 14, 1.5, 1.5, 'FD')

    document.setFont('helvetica', 'normal')
    document.setFontSize(6.8)
    document.setTextColor(...colors.muted)
    document.text(metric.label.toUpperCase(), left + 3, top + 5)

    document.setFont('helvetica', 'bold')
    document.setFontSize(10)
    document.setTextColor(...colors.primary)
    document.text(metric.value, left + 3, top + 11)
  })
}

function drawFooters(document: jsPDF, generatedAt: Date, generatedBy: string) {
  const pages = document.getNumberOfPages()
  const generatedLabel = `Gerado em ${generatedAt.toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })} por ${generatedBy}`

  for (let page = 1; page <= pages; page += 1) {
    document.setPage(page)
    const pageWidth = document.internal.pageSize.getWidth()
    const pageHeight = document.internal.pageSize.getHeight()

    document.setDrawColor(...colors.border)
    document.setLineWidth(0.2)
    document.line(12, pageHeight - 12, pageWidth - 12, pageHeight - 12)
    document.setFont('helvetica', 'normal')
    document.setFontSize(6.8)
    document.setTextColor(...colors.muted)
    document.text(generatedLabel, 12, pageHeight - 7)
    document.text(`Página ${page} de ${pages}`, pageWidth - 12, pageHeight - 7, { align: 'right' })
  }
}

function ensurePdfExtension(fileName: string) {
  const sanitized = fileName
    .replace(/\.pdf$/i, '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[^a-zA-Z0-9._-]+/g, '-')
    .replace(/^-+|-+$/g, '')

  return `${sanitized || 'relatorio'}.pdf`
}
