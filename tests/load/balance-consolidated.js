import http from 'k6/http'
import { check, sleep } from 'k6'

const BFF = __ENV.BFF_URL || 'http://localhost:5000'
const BALANCE_BASE = __ENV.BALANCE_URL || BFF
const DATE = __ENV.BALANCE_DATE || '2026-06-17'

export const options = {
  scenarios: {
    balance_consolidated: {
      executor: 'constant-arrival-rate',
      rate: 50,
      timeUnit: '1s',
      duration: '30s',
      preAllocatedVUs: 60,
      maxVUs: 120,
    },
  },
  thresholds: {
    'http_req_failed{name:balance_by_date}': ['rate<0.05'],
    'http_req_duration{name:balance_by_date}': ['p(95)<500'],
  },
}

export function setup() {
  const login = http.post(
    `${BFF}/api/auth/login`,
    JSON.stringify({ email: 'admin@admin.com', password: 'Master@123' }),
    { headers: { 'Content-Type': 'application/json' }, tags: { name: 'login' } },
  )
  check(login, { 'login ok': (r) => r.status === 200 })
  const body = login.json()
  if (!body || !body.accessToken) {
    throw new Error(`login failed: status=${login.status}`)
  }
  return { token: body.accessToken }
}

export default function (data) {
  const res = http.get(`${BALANCE_BASE}/api/balance/${DATE}`, {
    headers: { Authorization: `Bearer ${data.token}` },
    tags: { name: 'balance_by_date' },
    responseCallback: http.expectedStatuses(200, 404),
  })
  check(res, {
    'status 200 or 404': (r) => r.status === 200 || r.status === 404,
  })
  sleep(0.01)
}
