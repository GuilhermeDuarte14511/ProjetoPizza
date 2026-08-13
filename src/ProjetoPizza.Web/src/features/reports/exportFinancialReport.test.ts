import { describe, expect, it } from 'vitest'
import type { FinancialReportExportInput } from './exportFinancialReport'
import { createFinancialReportSheets } from './exportFinancialReport'

function isCellObject(cell: unknown): cell is { value?: unknown; format?: string; align?: string } {
  return cell !== null && cell !== undefined && typeof cell === 'object' && 'value' in cell
}

const input: FinancialReportExportInput = {
  report: {
    from: '2026-07-01T03:00:00.000Z',
    to: '2026-07-31T02:59:59.000Z',
    grossSales: 150,
    paidAmount: 150,
    foodCost: 45,
    contributionMargin: 105,
    contributionMarginPercentage: 70,
    averageTicket: 75,
    orderCount: 2,
    completedTickets: 2,
    averagePreparationMinutes: 15,
    onTimeRate: 100,
    channels: [{ channel: 'DineIn', orders: 2, total: 150 }],
    paymentMethods: [{ method: 'Pix', payments: 2, total: 150 }],
    productionStations: [{ station: 'Forno', tickets: 2, averagePreparationMinutes: 15, onTimeRate: 100 }],
  },
  orders: [{
    id: 'order-1',
    number: 1024,
    channel: 'DineIn',
    fulfillment: 'DineIn',
    status: 'Completed',
    subtotal: 150,
    discount: 0,
    total: 150,
    createdAt: '2026-07-15T15:00:00.000Z',
    items: [{ id: 'item-1', name: 'Pizza grande', quantity: 2, unitPrice: 75, totalPrice: 150, status: 'Ready' }],
  }],
  payments: [{
    id: 'payment-1',
    billId: 'bill-1',
    payer: 'Pessoa 1',
    method: 'Pix',
    status: 'Paid',
    amount: 150,
    receivedAmount: 150,
    changeAmount: 0,
    paidAt: '2026-07-15T15:10:00.000Z',
  }],
  period: { from: '2026-07-01', to: '2026-07-31' },
  unitName: 'Forno 27',
  generatedBy: 'Administrador',
  generatedAt: new Date('2026-07-28T15:00:00.000Z'),
}

describe('createFinancialReportSheets', () => {
  it('creates an executive workbook with detailed orders and payments', () => {
    const sheets = createFinancialReportSheets(input)

    expect(sheets.map((sheet) => sheet.sheet)).toEqual(['Resumo Executivo', 'Pedidos', 'Pagamentos'])
    expect(sheets[0].data.flat().some((cell) => isCellObject(cell) && cell.value === 'RESUMO EXECUTIVO')).toBe(true)
    expect(sheets[1].data.flat().some((cell) => isCellObject(cell) && cell.value === '#1024')).toBe(true)
    expect(sheets[2].data.flat().some((cell) => isCellObject(cell) && cell.value === 'Pessoa 1')).toBe(true)
  })

  it('uses native numeric values with Excel currency formatting', () => {
    const sheets = createFinancialReportSheets(input)
    const monetaryCell = sheets[0].data.flat().find((cell) => isCellObject(cell) && cell.value === 150 && cell.format?.includes('R$'))

    expect(monetaryCell).toMatchObject({ value: 150, align: 'right' })
  })
})
