import { FileText, LoaderCircle } from 'lucide-react'

interface PdfExportButtonProps {
  exporting: boolean
  onClick: () => void
  disabled?: boolean
  label?: string
}

export function PdfExportButton({ exporting, onClick, disabled, label = 'Exportar PDF' }: PdfExportButtonProps) {
  return (
    <button
      type="button"
      className="secondary-button pdf-export-button"
      disabled={disabled || exporting}
      aria-busy={exporting}
      onClick={onClick}
    >
      {exporting ? <LoaderCircle className="spin-icon" size={16} /> : <FileText size={16} />}
      {exporting ? 'Gerando PDF...' : label}
    </button>
  )
}
