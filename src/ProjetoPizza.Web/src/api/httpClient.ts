export const apiBaseUrl = (import.meta.env.VITE_API_URL ?? '').replace(/\/$/, '')
const tokenKey = 'projeto-pizza.access-token'
export const unauthorizedEventName = 'projeto-pizza:unauthorized'

export const isApiConfigured = apiBaseUrl.length > 0

export class ApiError extends Error {
  readonly status: number
  readonly traceId?: string

  constructor(status: number, message: string, traceId?: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.traceId = traceId
  }
}

export function setAccessToken(token: string) {
  localStorage.setItem(tokenKey, token)
}

export function clearAccessToken() {
  localStorage.removeItem(tokenKey)
}

export function getAccessToken() {
  return localStorage.getItem(tokenKey)
}

export async function requestJson<T>(
  path: string,
  init: RequestInit = {},
  signal?: AbortSignal,
): Promise<T> {
  if (!isApiConfigured) {
    throw new ApiError(0, 'VITE_API_URL is not configured; development mocks will be used.')
  }

  const token = getAccessToken()
  const headers = new Headers(init.headers)
  headers.set('Accept', 'application/json')
  if (init.body) headers.set('Content-Type', 'application/json')
  if (token) headers.set('Authorization', `Bearer ${token}`)

  let response: Response
  try {
    response = await fetch(`${apiBaseUrl}${path}`, { ...init, headers, signal })
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') throw error
    throw new ApiError(0, 'Network request failed.')
  }
  if (response.status === 401) {
    clearAccessToken()
    window.dispatchEvent(new Event(unauthorizedEventName))
  }

  if (!response.ok) {
    const problem = await response.json().catch(() => undefined) as { detail?: string; title?: string; traceId?: string } | undefined
    throw new ApiError(
      response.status,
      problem?.detail ?? problem?.title ?? `API request failed with status ${response.status}`,
      problem?.traceId,
    )
  }

  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}

export function getJson<T>(path: string, signal?: AbortSignal) {
  return requestJson<T>(path, {}, signal)
}

export function postJson<TResponse, TBody>(path: string, body: TBody, signal?: AbortSignal) {
  return requestJson<TResponse>(path, { method: 'POST', body: JSON.stringify(body) }, signal)
}

export function putJson<TResponse, TBody>(path: string, body: TBody, signal?: AbortSignal) {
  return requestJson<TResponse>(path, { method: 'PUT', body: JSON.stringify(body) }, signal)
}
