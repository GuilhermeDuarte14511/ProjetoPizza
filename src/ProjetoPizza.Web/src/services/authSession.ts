import { clearAccessToken, isApiConfigured, setAccessToken } from '../api/httpClient'
import type { AuthenticatedUser, AuthenticationResult } from '../types/admin'

const userKey = 'projeto-pizza.user'

export function saveAuthentication(result: AuthenticationResult) {
  setAccessToken(result.accessToken)
  localStorage.setItem(userKey, JSON.stringify(result.user))
}

export function getAuthenticatedUser(): AuthenticatedUser | undefined {
  const raw = localStorage.getItem(userKey)
  if (!raw) return undefined
  try {
    return JSON.parse(raw) as AuthenticatedUser
  } catch {
    return undefined
  }
}

export function hasPermission(permission: string) {
  if (!isApiConfigured) return true
  return getAuthenticatedUser()?.permissions.includes(permission) ?? false
}

export function logout() {
  clearAccessToken()
  localStorage.removeItem(userKey)
}
