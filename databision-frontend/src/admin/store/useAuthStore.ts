import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { AuthUser } from '../../types'
import { setAccessToken } from '../../lib/api'

interface AuthState {
  user: AuthUser | null
  accessToken: string | null
  isAuthenticated: boolean
  setAuth: (user: AuthUser, token: string) => void
  clearAuth: () => void
}

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

      clearAuth: () => {
        setAccessToken(null)
        set({ user: null, accessToken: null, isAuthenticated: false })
      },
    }),
    {
      name: 'databision-admin-auth',
      onRehydrateStorage: () => (state) => {
        if (state?.accessToken) {
          setAccessToken(state.accessToken)
        }
      },
    }
  )
)
