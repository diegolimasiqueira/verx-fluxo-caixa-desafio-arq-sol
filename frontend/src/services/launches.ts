import { launchApi } from './api'
import type { Launch, RegisterLaunchRequest } from '@/types'

export const launchService = {
  async register(data: RegisterLaunchRequest): Promise<Launch> {
    const res = await launchApi.post<Launch>('/api/launches', data)
    return res.data
  },

  async getByDate(date: string): Promise<Launch[]> {
    const res = await launchApi.get<Launch[]>('/api/launches', { params: { date } })
    return res.data
  },

  async getByPeriod(from: string, to: string): Promise<Launch[]> {
    const res = await launchApi.get<Launch[]>('/api/launches/period', { params: { from, to } })
    return res.data
  },
}
