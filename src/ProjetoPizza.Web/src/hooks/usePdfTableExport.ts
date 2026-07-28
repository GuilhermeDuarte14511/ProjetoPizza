import { useState } from 'react'
import { useToast } from '../components/ui/toast'
import type { PdfTableReportInput } from '../features/reports/exportPdfTable'
import { adminService } from '../services/adminService'
import { getAuthenticatedUser } from '../services/authSession'
import { getUserErrorMessage } from '../utils/errors'

type PdfExportDefinition = Omit<PdfTableReportInput, 'unitName' | 'generatedBy'>

export function usePdfTableExport() {
  const [exporting, setExporting] = useState(false)
  const toast = useToast()

  async function exportPdf(definition: PdfExportDefinition) {
    setExporting(true)
    try {
      const [unit, exporter] = await Promise.all([
        adminService.unitSettings(),
        import('../features/reports/exportPdfTable'),
      ])
      const result = exporter.exportPdfTableReport({
        ...definition,
        unitName: unit.tradeName || unit.name,
        generatedBy: getAuthenticatedUser()?.displayName ?? 'Usuário administrativo',
      })
      toast.success('Relatório PDF gerado', `${result.fileName} contém ${result.rows} registro(s) em ${result.pages} página(s).`)
    } catch (error) {
      toast.error('Não foi possível gerar o PDF', getUserErrorMessage(error))
    } finally {
      setExporting(false)
    }
  }

  return { exportPdf, exporting }
}
