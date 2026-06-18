import axios from 'axios'

const LAUNCH_API_URL = import.meta.env.VITE_LAUNCH_API_URL ?? 'http://localhost:5001'
const BALANCE_API_URL = import.meta.env.VITE_BALANCE_API_URL ?? 'http://localhost:5002'

export const launchApi = axios.create({ baseURL: LAUNCH_API_URL })
export const balanceApi = axios.create({ baseURL: BALANCE_API_URL })

function applyInterceptors(instance: ReturnType<typeof axios.create>) {
  instance.interceptors.request.use((config) => {
    const token = localStorage.getItem('cashflow_token')
    if (token) config.headers.Authorization = `Bearer ${token}`
    return config
  })

  instance.interceptors.response.use(
    (r) => r,
    (error) => {
      if (error.response?.status === 401) {
        localStorage.removeItem('cashflow_token')
        window.location.href = '/login'
      }
      return Promise.reject(error)
    },
  )
}

applyInterceptors(launchApi)
applyInterceptors(balanceApi)
