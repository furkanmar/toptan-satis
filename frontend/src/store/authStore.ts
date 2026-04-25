import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { UserRole } from '../types'

interface AuthState {
  token: string | null
  role: UserRole | null
  userId: string | null
  displayName: string | null
  isAuthenticated: boolean
  login: (token: string, role: UserRole, userId: string, displayName: string) => void
  logout: () => void
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      role: null,
      userId: null,
      displayName: null,
      isAuthenticated: false,

      login: (token, role, userId, displayName) => {
        localStorage.setItem('token', token)
        set({ token, role, userId, displayName, isAuthenticated: true })
      },

      logout: () => {
        localStorage.removeItem('token')
        set({ token: null, role: null, userId: null, displayName: null, isAuthenticated: false })
      }
    }),
    {
      name: 'auth-storage',
      partialize: (state) => ({
        token: state.token,
        role: state.role,
        userId: state.userId,
        displayName: state.displayName,
        isAuthenticated: state.isAuthenticated
      })
    }
  )
)
