import { toast } from 'sonner'

const toastApi = {
  success: (title: string, message?: string) => toast.success(title, { description: message }),
  error: (title: string, message?: string) => toast.error(title, { description: message, duration: 7000 }),
  info: (title: string, message?: string) => toast.info(title, { description: message }),
}

export function useToast() {
  return toastApi
}
