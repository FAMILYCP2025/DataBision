import { create } from 'zustand'
import type { AuthUser } from '../types'
import { setAccessToken } from '../lib/api'

interface AuthState {
  user: AuthUser | null
  isAuthenticated: boolean
  setAuth: (user: AuthUser, token: string) => void
  clearAuth: () => void
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isAuthenticated: false,

  setAuth(user, token) {
    setAccessToken(token)
    set({ user, isAuthenticated: true })
  },

  clearAuth() {
    setAccessToken(null)
    set({ user: null, isAuthenticated: false })
  },
}))
