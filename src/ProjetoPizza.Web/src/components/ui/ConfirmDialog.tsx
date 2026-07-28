import * as AlertDialog from '@radix-ui/react-alert-dialog'
import type { ReactNode } from 'react'

interface ConfirmDialogProps {
  open: boolean
  title: string
  description: string
  confirmLabel?: string
  busy?: boolean
  tone?: 'default' | 'danger'
  onConfirm: () => void
  onOpenChange: (open: boolean) => void
  children?: ReactNode
}

export function ConfirmDialog({
  open,
  title,
  description,
  confirmLabel = 'Confirmar',
  busy = false,
  tone = 'default',
  onConfirm,
  onOpenChange,
  children,
}: ConfirmDialogProps) {
  return (
    <AlertDialog.Root open={open} onOpenChange={(nextOpen) => !busy && onOpenChange(nextOpen)}>
      {children && <AlertDialog.Trigger asChild>{children}</AlertDialog.Trigger>}
      <AlertDialog.Portal>
        <AlertDialog.Overlay className="modal-backdrop" />
        <AlertDialog.Content className="confirm-panel">
          <AlertDialog.Title>{title}</AlertDialog.Title>
          <AlertDialog.Description>{description}</AlertDialog.Description>
          <div className="confirm-actions">
            <AlertDialog.Cancel asChild>
              <button className="secondary-button" disabled={busy}>Cancelar</button>
            </AlertDialog.Cancel>
            <AlertDialog.Action asChild>
              <button className={tone === 'danger' ? 'danger-button' : 'primary-button'} disabled={busy} onClick={onConfirm}>
                {busy ? 'Processando...' : confirmLabel}
              </button>
            </AlertDialog.Action>
          </div>
        </AlertDialog.Content>
      </AlertDialog.Portal>
    </AlertDialog.Root>
  )
}
