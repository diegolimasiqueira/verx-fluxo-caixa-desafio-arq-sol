import type { TestMetrics, E2eResults, LoadResults } from '@/types/testReports'

async function fetchJson<T>(path: string): Promise<T> {
  const res = await fetch(`${path}?t=${Date.now()}`, { cache: 'no-store' })
  if (!res.ok) throw new Error(`${path} não encontrado`)
  return res.json()
}

export async function fetchTestMetrics(): Promise<TestMetrics> {
  return fetchJson('/test-metrics.json')
}

export async function fetchE2eResults(): Promise<E2eResults> {
  return fetchJson('/e2e-results.json')
}

export async function fetchLoadResults(): Promise<LoadResults> {
  return fetchJson('/load-results.json')
}
