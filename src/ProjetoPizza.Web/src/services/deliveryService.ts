import { apiBaseUrl, ApiError } from '../api/httpClient'
import type { SubmitClientOrder } from '../types/client'
import type { DeliveryCatalog, DeliveryOrderPlaced, DeliveryTracking } from '../types/delivery'

async function publicRequest<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response
  try {
    response = await fetch(`${apiBaseUrl}${path}`, {
      ...init,
      headers: { Accept: 'application/json', ...(init?.body ? { 'Content-Type': 'application/json' } : {}) },
    })
  } catch {
    throw new ApiError(0, 'Sem conexão com a pizzaria. Seu pedido continua salvo nesta tela.')
  }
  if (!response.ok) {
    const problem = await response.json().catch(() => undefined) as { detail?: string; title?: string } | undefined
    throw new ApiError(response.status, problem?.detail ?? problem?.title ?? 'Não foi possível concluir o pedido.')
  }
  return response.json() as Promise<T>
}

export const deliveryService = {
  catalog: () => publicRequest<DeliveryCatalog>('/api/v1/delivery/catalog'),
  placeOrder: (command: {
    requestId: string
    customerName: string
    phone: string
    birthDate: string
    address: string
    notes?: string
    items: SubmitClientOrder['items']
  }) => publicRequest<DeliveryOrderPlaced>('/api/v1/delivery/orders', {
    method: 'POST',
    body: JSON.stringify(command),
  }),
  track: (token: string) => publicRequest<DeliveryTracking>(`/api/v1/delivery/tracking/${encodeURIComponent(token)}`),
}
