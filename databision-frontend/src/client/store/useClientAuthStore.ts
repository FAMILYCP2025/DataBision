import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { AuthUser } from '../../types'
import { setAccessToken } from '../../lib/api'

interface ClientAuthState {
  user: AuthUser | null
  accessToken: string | null
  isAuthenticated: boolean
  tenant: string | null
  setAuth: (user: AuthUser, token: string, tenant: string | null) => void
  restoreSession: (user: AuthUser, token: string, tenant: string | null) => void
  clearAuth: () => void
}

// Persisted shape: user / isAuthenticated / tenant. accessToken stays in memory
// only — it is recovered via the httpOnly refresh-token cookie on the first
// protected request after page load (handled by the response interceptor in lib/api.ts).
export const useClientAuthStore = create<ClientAuthState>()(
  persist(
    (set) => ({
      user: null,
      accessToken: null,
      isAuthenticated: false,
      tenant: null,

      setAuth: (user, token, tenant) => {
        setAccessToken(token)
        if (tenant) {
          localStorage.setItem('databision-tenant', tenant)
        }
        set({ user, accessToken: token, isAuthenticated: true, tenant })
      },

      // Used by AuthBootstrap on silent refresh — does NOT write to localStorage
      // because the tenant is already persisted from the original login.
      restoreSession: (user, token, tenant) => {
        setAccessToken(token)
        set({ user, accessToken: token, isAuthenticated: true, tenant })
      },

      clearAuth: () => {
        setAccessToken(null)
        set({ user: null, accessToken: null, isAuthenticated: false })
      },
    }),
    {
      name: 'databision-client-auth',
      partialize: (state) => ({
        user: state.user,
        isAuthenticated: state.isAuthenticated,
        tenant: state.tenant,
      }),
    }
  )
)
