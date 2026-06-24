import { api } from './api'
import type { LoginRequest, LoginResponse } from '@/types'

export const authService = {
  async login(data: LoginRequest): Promise<LoginResponse> {
    const res = await api.post<LoginResponse>('/api/auth/login', data)
    return res.data
  },
}
