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
  clearAuth: () => void
}

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

      clearAuth: () => {
        setAccessToken(null)
        set({ user: null, accessToken: null, isAuthenticated: false })
      },
    }),
    {
      name: 'databision-client-auth',
      onRehydrateStorage: () => (state) => {
        if (state?.accessToken) {
          setAccessToken(state.accessToken)
        }
      },
    }
  )
)
