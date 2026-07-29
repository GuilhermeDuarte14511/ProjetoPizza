import { apiBaseUrl, ApiError, isApiConfigured } from '../api/httpClient'
import type {
  ClientActivation,
  ClientBill,
  ClientBootstrap,
  ClientOrder,
  ClientState,
  SubmitClientOrder,
} from '../types/client'

const sessionTokenKey = 'projeto-pizza.client-session'

export function getClientSessionToken() {
  return sessionStorage.getItem(sessionTokenKey)
}

export function setClientSessionToken(token: string) {
  sessionStorage.setItem(sessionTokenKey, token)
}

export function clearClientSessionToken() {
  sessionStorage.removeItem(sessionTokenKey)
}

async function clientRequest<T>(path: string, init: RequestInit = {}, signal?: AbortSignal): Promise<T> {
  if (!isApiConfigured) {
    throw new ApiError(0, 'VITE_API_URL is not configured.')
  }

  const headers = new Headers(init.headers)
  headers.set('Accept', 'application/json')
  if (init.body) headers.set('Content-Type', 'application/json')
  const token = getClientSessionToken()
  if (token) headers.set('X-Device-Session', token)

  let response: Response
  try {
    response = await fetch(`${apiBaseUrl}${path}`, { ...init, headers, signal })
  } catch {
    throw new ApiError(0, 'Network request failed.')
  }

  if (response.status === 401) clearClientSessionToken()
  if (!response.ok) {
    const problem = await response.json().catch(() => undefined) as {
      detail?: string
      title?: string
      traceId?: string
    } | undefined
    throw new ApiError(
      response.status,
      problem?.detail ?? problem?.title ?? `API request failed with status ${response.status}`,
      problem?.traceId,
    )
  }

  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}

async function activateClient(credentials: { deviceCode?: string; provisioningToken?: string }) {
  const activation = await clientRequest<ClientActivation>('/api/v1/client/sessions', {
    method: 'POST',
    body: JSON.stringify(credentials),
  })
  setClientSessionToken(activation.token)
  return activation.bootstrap
}

export function activateClientSession(deviceCode: string) {
  return activateClient({ deviceCode })
}

export function activateClientProvisioning(provisioningToken: string) {
  return activateClient({ provisioningToken })
}

export function getClientBootstrap(signal?: AbortSignal) {
  return clientRequest<ClientBootstrap>('/api/v1/client/bootstrap', {}, signal)
}

export function getClientState(signal?: AbortSignal) {
  return clientRequest<ClientState>('/api/v1/client/state', {}, signal)
}

export function submitClientOrder(order: SubmitClientOrder) {
  return clientRequest<ClientOrder>('/api/v1/client/orders', {
    method: 'POST',
    body: JSON.stringify(order),
  })
}

export function createClientServiceCall(serviceCallTypeId: string, details?: string) {
  return clientRequest<{ id: string; status: string }>('/api/v1/client/service-calls', {
    method: 'POST',
    body: JSON.stringify({ serviceCallTypeId, details: details || null }),
  })
}

export function requestClientBill(splitCount?: number) {
  return clientRequest<ClientBill>('/api/v1/client/bill-requests', {
    method: 'POST',
    body: JSON.stringify({ splitCount: splitCount ?? null }),
  })
}
