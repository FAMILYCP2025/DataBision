import { useEffect, useState, type ReactNode } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { useAuthStore } from '../admin/store/useAuthStore'
import { useClientAuthStore } from '../client/store/useClientAuthStore'
import {
  getAccessToken,
  refreshAccessToken,
  registerAuthClearedHandler,
} from '../lib/api'
import SessionSplash from './SessionSplash'

type AuthContext = 'admin' | 'client'

interface Props {
  context: AuthContext
  children: ReactNode
}

const DEV = import.meta.env.DEV

/**
 * AuthBootstrap — global gate before protected routes render.
 *
 *  - Silently refreshes the in-memory accessToken using the httpOnly cookie
 *    when a persisted session is rehydrated from localStorage.
 *  - Shows a splash while validating to prevent partial dashboard render.
 *  - Wires the api.ts terminal-401 handler so stores + React Query cache
 *    are cleaned together on session loss.
 *  - Clears the React Query cache on logout transitions and on tenant
 *    switches to prevent stale cross-tenant data leakage.
 */
export default function AuthBootstrap({ context, children }: Props) {
  const qc = useQueryClient()
  const [hydrating, setHydrating] = useState(true)

  // ── Detect tenant mismatch on mount (client only) ─────────────────────────
  // If the URL ?tenant=X differs from the persisted session's tenant, the
  // previous session is invalid for this URL — clear it before doing anything.
  useEffect(() => {
    if (context !== 'client') return
    const urlTenant = new URLSearchParams(window.location.search).get('tenant')
    const storedTenant = useClientAuthStore.getState().tenant
    if (urlTenant && storedTenant && urlTenant !== storedTenant) {
      if (DEV) console.warn('[auth] TENANT_MISMATCH', { from: storedTenant, to: urlTenant })
      useClientAuthStore.getState().clearAuth()
      void qc.cancelQueries().then(() => qc.clear())
      try { localStorage.removeItem('databision-tenant') } catch { /* ignore */ }
    }
  }, [context, qc])

  // ── Silent refresh on mount ───────────────────────────────────────────────
  useEffect(() => {
    let cancelled = false

    const persistedAuth = context === 'admin'
      ? useAuthStore.getState().isAuthenticated
      : useClientAuthStore.getState().isAuthenticated

    const needsRefresh = persistedAuth && !getAccessToken()

    if (!needsRefresh) {
      setHydrating(false)
      return () => { cancelled = true }
    }

    refreshAccessToken()
      .then((res) => {
        if (cancelled) return
        if (context === 'admin') {
          useAuthStore.getState().restoreSession(res.user, res.accessToken)
        } else {
          const tenant = useClientAuthStore.getState().tenant
          useClientAuthStore.getState().restoreSession(res.user, res.accessToken, tenant)
        }
      })
      .catch(async () => {
        if (cancelled) return
        await qc.cancelQueries()
        qc.clear()
        if (context === 'admin') useAuthStore.getState().clearAuth()
        else useClientAuthStore.getState().clearAuth()
      })
      .finally(() => {
        if (!cancelled) setHydrating(false)
      })

    return () => { cancelled = true }
  }, [context, qc])

  // ── Wire api.ts terminal-401 handler ──────────────────────────────────────
  useEffect(() => {
    registerAuthClearedHandler(() => {
      if (context === 'admin') useAuthStore.getState().clearAuth()
      else useClientAuthStore.getState().clearAuth()
      void qc.cancelQueries().then(() => qc.clear())
    })
    return () => registerAuthClearedHandler(null)
  }, [context, qc])

  // ── React Query cache cleanup on logout transition ────────────────────────
  useEffect(() => {
    if (context === 'admin') {
      return useAuthStore.subscribe((state, prev) => {
        if (prev.isAuthenticated && !state.isAuthenticated) {
          if (DEV) console.log('[auth] LOGOUT_DETECTED', { context })
          void qc.cancelQueries().then(() => qc.clear())
        }
      })
    }
    return useClientAuthStore.subscribe((state, prev) => {
      if (prev.isAuthenticated && !state.isAuthenticated) {
        if (DEV) console.log('[auth] LOGOUT_DETECTED', { context })
        void qc.cancelQueries().then(() => qc.clear())
      }
    })
  }, [context, qc])

  // ── Tenant switch within an authenticated session (client only) ───────────
  useEffect(() => {
    if (context !== 'client') return
    return useClientAuthStore.subscribe((state, prev) => {
      if (state.tenant !== prev.tenant && prev.tenant !== null && state.tenant !== null) {
        if (DEV) console.warn('[auth] TENANT_SWITCHED', { from: prev.tenant, to: state.tenant })
        void qc.cancelQueries().then(() => qc.clear())
      }
    })
  }, [context, qc])

  if (hydrating) return <SessionSplash />
  return <>{children}</>
}
