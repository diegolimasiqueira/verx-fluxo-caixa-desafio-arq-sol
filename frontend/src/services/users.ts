import { api } from './api'
import type {
  CreateUserRequest,
  UpdatePasswordRequest,
  UpdateUserRequest,
  User,
} from '@/types'

export const userService = {
  async list(): Promise<User[]> {
    const res = await api.get<User[]>('/api/users')
    return res.data
  },

  async create(data: CreateUserRequest): Promise<User> {
    const res = await api.post<User>('/api/users', data)
    return res.data
  },

  async update(id: string, data: UpdateUserRequest): Promise<User> {
    const res = await api.put<User>(`/api/users/${id}`, data)
    return res.data
  },

  async updatePassword(id: string, data: UpdatePasswordRequest): Promise<void> {
    await api.put(`/api/users/${id}/password`, data)
  },
}
