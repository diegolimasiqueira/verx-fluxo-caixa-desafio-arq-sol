import { api } from './api'
import type { DailyBalance } from '@/types'

export const balanceService = {
  async getByDate(date: string): Promise<DailyBalance> {
    const res = await api.get<DailyBalance>(`/api/balance/${date}`)
    return res.data
  },

  async getByPeriod(from: string, to: string): Promise<DailyBalance[]> {
    const res = await api.get<DailyBalance[]>('/api/balance', { params: { from, to } })
    return res.data
  },
}
