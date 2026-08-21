import { apiBaseUrl, ApiError, isApiConfigured } from '../api/httpClient'
import type {
  ClientActivation,
  ClientBill,
  ClientBootstrap,
  ClientOrder,
  ClientLoyaltyQuote,
  ClientState,
  ClientTelemetry,
  StartClientTableSession,
  SubmitClientOrder,
} from '../types/client'

const sessionTokenKey = 'projeto-pizza.client-session'
const bootstrapCacheKey = 'projeto-pizza.client-bootstrap'

export function getCachedClientBootstrap() {
  const value = localStorage.getItem(bootstrapCacheKey)
  if (!value) return undefined
  try {
    return JSON.parse(value) as ClientBootstrap
  } catch {
    localStorage.removeItem(bootstrapCacheKey)
    return undefined
  }
}

export function cacheClientBootstrap(bootstrap: ClientBootstrap) {
  localStorage.setItem(bootstrapCacheKey, JSON.stringify(bootstrap))
}

export function getClientSessionToken() {
  const persistentToken = localStorage.getItem(sessionTokenKey)
  if (persistentToken) return persistentToken

  const previousSessionToken = sessionStorage.getItem(sessionTokenKey)
  if (previousSessionToken) {
    localStorage.setItem(sessionTokenKey, previousSessionToken)
    sessionStorage.removeItem(sessionTokenKey)
  }
  return previousSessionToken
}

export function setClientSessionToken(token: string) {
  localStorage.setItem(sessionTokenKey, token)
  sessionStorage.removeItem(sessionTokenKey)
}

export function clearClientSessionToken() {
  localStorage.removeItem(sessionTokenKey)
  localStorage.removeItem(bootstrapCacheKey)
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
  cacheClientBootstrap(activation.bootstrap)
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
    .then((bootstrap) => {
      cacheClientBootstrap(bootstrap)
      return bootstrap
    })
}

export function getClientState(signal?: AbortSignal) {
  return clientRequest<ClientState>('/api/v1/client/state', {}, signal)
}

export function updateClientTelemetry(telemetry: ClientTelemetry, signal?: AbortSignal) {
  return clientRequest<void>('/api/v1/client/telemetry', {
    method: 'POST',
    body: JSON.stringify(telemetry),
  }, signal)
}

export function startClientTableSession(command: StartClientTableSession) {
  return clientRequest<ClientBootstrap>('/api/v1/client/table-sessions', {
    method: 'POST',
    body: JSON.stringify(command),
  })
}

export function completeClientTableSession() {
  return clientRequest<ClientBootstrap>('/api/v1/client/table-sessions/complete', {
    method: 'POST',
  })
}

export async function logoutClientTablet() {
  await clientRequest<void>('/api/v1/client/logout', { method: 'POST' })
  clearClientSessionToken()
}

export function submitClientOrder(order: SubmitClientOrder) {
  return clientRequest<ClientOrder>('/api/v1/client/orders', {
    method: 'POST',
    body: JSON.stringify(order),
  })
}

export function getClientLoyaltyQuote(command: { phone: string; birthDate: string; orderAmount: number; couponCode?: string; loyaltyPoints?: number }) {
  return clientRequest<ClientLoyaltyQuote>('/api/v1/client/loyalty/lookup', { method: 'POST', body: JSON.stringify(command) })
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
