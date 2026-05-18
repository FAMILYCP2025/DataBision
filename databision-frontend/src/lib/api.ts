import axios from 'axios'
import type { AuthUser } from '../types'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? '/api',
  withCredentials: true,
})

let accessToken: string | null = null

export function setAccessToken(token: string | null) {
  accessToken = token
}

export function getAccessToken() {
  return accessToken
}

api.interceptors.request.use((config) => {
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`
  }
  return config
})

// ── Refresh deduplication ────────────────────────────────────────────────────
// Multiple 401s in flight must share ONE /auth/refresh call. Otherwise each
// retry rotates the refresh-cookie and only the last token survives, while
// earlier callers retry with stale tokens and end up in an auth loop.

export interface RefreshResult {
  accessToken: string
  expiresIn: number
  user: AuthUser
}

const DEV = import.meta.env.DEV
let refreshPromise: Promise<RefreshResult> | null = null
let onAuthCleared: (() => void) | null = null

export function registerAuthClearedHandler(fn: (() => void) | null) {
  onAuthCleared = fn
}

const REFRESH_TIMEOUT_MS = 8_000

export function refreshAccessToken(): Promise<RefreshResult> {
  if (refreshPromise) return refreshPromise

  if (DEV) console.log('[auth] REFRESH_STARTED')

  const timeoutPromise = new Promise<never>((_, reject) =>
    setTimeout(() => reject(new Error('REFRESH_TIMEOUT')), REFRESH_TIMEOUT_MS)
  )

  refreshPromise = Promise.race([
    api.post<{ data: RefreshResult }>('/auth/refresh'),
    timeoutPromise,
  ])
    .then((res) => {
      const payload = res.data.data
      setAccessToken(payload.accessToken)
      if (DEV) console.log('[auth] REFRESH_SUCCESS', { user: payload.user?.email })
      return payload
    })
    .catch((err: unknown) => {
      setAccessToken(null)
      if (err instanceof Error && err.message === 'REFRESH_TIMEOUT') {
        if (DEV) console.warn('[auth] REFRESH_TIMEOUT')
      } else if (axios.isAxiosError(err) && !err.response) {
        if (DEV) console.warn('[auth] REFRESH_NETWORK_ERROR')
      } else {
        if (DEV) console.warn('[auth] REFRESH_FAILED', { status: axios.isAxiosError(err) ? err.response?.status : undefined })
      }
      throw err
    })
    .finally(() => {
      refreshPromise = null
    })

  return refreshPromise
}

// ── 401 interceptor ──────────────────────────────────────────────────────────

const AUTH_BYPASS_URLS = ['/auth/login', '/auth/refresh', '/auth/logout']

function redirectToLogin() {
  const path = window.location.pathname
  const isClientContext = path.startsWith('/client')

  if (isClientContext) {
    const tenantFromStorage = localStorage.getItem('databision-tenant')
    const tenantFromQuery = new URLSearchParams(window.location.search).get('tenant')
    const tenant = tenantFromStorage ?? tenantFromQuery
    const tenantParam = tenant ? `?tenant=${tenant}` : ''
    window.location.href = `/client/login${tenantParam}`
  } else {
    window.location.href = '/admin/login'
  }
}

api.interceptors.response.use(
  (res) => res,
  async (error) => {
    const original = error.config

    const isAuthEndpoint = AUTH_BYPASS_URLS.some((u) =>
      original?.url?.includes(u)
    )

    if (
      error.response?.status === 401 &&
      !original._retry &&
      !isAuthEndpoint
    ) {
      original._retry = true
      try {
        const { accessToken: newToken } = await refreshAccessToken()
        original.headers.Authorization = `Bearer ${newToken}`
        return api(original)
      } catch {
        // Terminal failure: notify any registered consumer (AuthBootstrap)
        // so stores + React Query cache can be reset cleanly, then redirect.
        onAuthCleared?.()
        redirectToLogin()
      }
    }

    return Promise.reject(error)
  }
)

export default api
