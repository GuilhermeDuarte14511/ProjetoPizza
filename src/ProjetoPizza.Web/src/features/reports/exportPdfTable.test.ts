import { describe, expect, it } from 'vitest'
import { createPdfTableDocument } from './exportPdfTable'

const baseInput = {
  title: 'Relatório de pedidos',
  subtitle: 'Canal: Todos',
  fileName: 'pedidos.pdf',
  unitName: 'Forno 27',
  generatedBy: 'Administrador',
  columns: ['Pedido', 'Data', 'Status', 'Total'],
  generatedAt: new Date('2026-07-28T12:00:00-03:00'),
}

describe('createPdfTableDocument', () => {
  it('gera um PDF com metadados e conteúdo tabular', () => {
    const document = createPdfTableDocument({
      ...baseInput,
      rows: [['#1024', '28/07/2026 09:40', 'Concluído', 'R$ 138,50']],
      metrics: [{ label: 'Pedidos', value: '1' }],
      rightAlignedColumns: [3],
    })

    expect(document.getNumberOfPages()).toBe(1)
    expect(document.output('arraybuffer').byteLength).toBeGreaterThan(1_000)
  })

  it('pagina listas extensas sem cortar uma linha entre páginas', () => {
    const document = createPdfTableDocument({
      ...baseInput,
      rows: Array.from({ length: 140 }, (_, index) => [
        `#${index + 1}`,
        '28/07/2026 09:40',
        'Concluído',
        'R$ 50,00',
      ]),
    })

    expect(document.getNumberOfPages()).toBeGreaterThan(1)
  })
})
