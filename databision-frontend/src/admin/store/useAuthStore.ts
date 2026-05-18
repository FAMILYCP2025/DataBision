import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { AuthUser } from '../../types'
import { setAccessToken } from '../../lib/api'

interface AuthState {
  user: AuthUser | null
  accessToken: string | null
  isAuthenticated: boolean
  setAuth: (user: AuthUser, token: string) => void
  restoreSession: (user: AuthUser, token: string) => void
  clearAuth: () => void
}

// Persisted shape: user / isAuthenticated only. accessToken stays in memory
// and is recovered via the httpOnly refresh-token cookie on the first protected
// request after page load (handled by the response interceptor in lib/api.ts).
export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      accessToken: null,
      isAuthenticated: false,

      setAuth: (user, token) => {
        setAccessToken(token)
        set({ user, accessToken: token, isAuthenticated: true })
      },

      restoreSession: (user, token) => {
        setAccessToken(token)
        set({ user, accessToken: token, isAuthenticated: true })
      },

      clearAuth: () => {
        setAccessToken(null)
        set({ user: null, accessToken: null, isAuthenticated: false })
      },
    }),
    {
      name: 'databision-admin-auth',
      partialize: (state) => ({
        user: state.user,
        isAuthenticated: state.isAuthenticated,
      }),
    }
  )
)
