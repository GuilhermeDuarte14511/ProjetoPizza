import * as Dialog from '@radix-ui/react-dialog'
import { X } from 'lucide-react'
import type { ReactNode } from 'react'

interface ModalProps {
  open: boolean
  title: string
  description?: string
  children: ReactNode
  onClose: () => void
  size?: 'medium' | 'large'
  isBusy?: boolean
}

export function Modal({
  open,
  title,
  description,
  children,
  onClose,
  size = 'medium',
  isBusy = false,
}: ModalProps) {
  return (
    <Dialog.Root open={open} onOpenChange={(nextOpen) => {
      if (!nextOpen && !isBusy) onClose()
    }}>
      <Dialog.Portal>
        <Dialog.Overlay className="modal-backdrop" />
        <Dialog.Content
          className={`modal-panel modal-${size}`}
          onEscapeKeyDown={(event) => {
            if (isBusy) event.preventDefault()
          }}
          onPointerDownOutside={(event) => {
            if (isBusy) event.preventDefault()
          }}
        >
          <header className="modal-header">
            <div className="modal-heading">
              <Dialog.Title>{title}</Dialog.Title>
              {description && <Dialog.Description>{description}</Dialog.Description>}
            </div>
            <Dialog.Close asChild>
              <button type="button" className="icon-button modal-close" aria-label="Fechar modal" disabled={isBusy}>
                <X size={19} />
              </button>
            </Dialog.Close>
          </header>
          {children}
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  )
}
