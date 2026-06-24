import { createContext, useContext, useState, useCallback, type ReactNode } from 'react'
import { authService } from '@/services/auth'
import type { LoginRequest } from '@/types'

interface AuthContextValue {
  isAuthenticated: boolean
  userEmail: string | null
  login: (data: LoginRequest) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(
    () => !!localStorage.getItem('cashflow_token'),
  )
  const [userEmail, setUserEmail] = useState<string | null>(
    () => localStorage.getItem('cashflow_user_email'),
  )

  const login = useCallback(async (data: LoginRequest) => {
    const response = await authService.login(data)
    localStorage.setItem('cashflow_token', response.accessToken)
    localStorage.setItem('cashflow_user_email', data.email)
    setUserEmail(data.email)
    setIsAuthenticated(true)
  }, [])

  const logout = useCallback(() => {
    localStorage.removeItem('cashflow_token')
    localStorage.removeItem('cashflow_user_email')
    setUserEmail(null)
    setIsAuthenticated(false)
  }, [])

  return (
    <AuthContext.Provider value={{ isAuthenticated, userEmail, login, logout }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider')
  return ctx
}
