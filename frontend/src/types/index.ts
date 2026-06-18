export type LaunchType = 'credit' | 'debit'

export interface Launch {
  id: string
  date: string
  amount: number
  type: LaunchType
  description: string
  createdAt: string
}

export interface DailyBalance {
  date: string
  totalCredits: number
  totalDebits: number
  consolidatedBalance: number
  updatedAt: string
}

export interface RegisterLaunchRequest {
  date: string
  amount: number
  type: LaunchType
  description: string
}

export interface LoginRequest {
  username: string
  password: string
}

export interface LoginResponse {
  accessToken: string
  tokenType: string
  expiresIn: number
}

export interface ApiError {
  title: string
  detail: string
  status: number
}
