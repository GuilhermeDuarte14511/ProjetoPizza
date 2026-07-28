import type { ReactNode } from 'react'
import { Toaster } from 'sonner'

export function ToastProvider({ children }: { children: ReactNode }) {
  return (
    <>
      {children}
      <Toaster
        position="top-right"
        richColors
        closeButton
        expand
        visibleToasts={4}
        containerAriaLabel="Notificações"
        toastOptions={{ className: 'app-toast', closeButtonAriaLabel: 'Fechar notificação' }}
      />
    </>
  )
}
