export interface BackendServiceMetrics {
  key: string
  name: string
  tests: number
  passed: number
  failed: number
  lineCoverage: number
  branchCoverage: number
  reportPath: string
}

export interface TestMetrics {
  generatedAt: string
  backend: {
    total: number
    passed: number
    failed: number
    allPassing: boolean
    services: BackendServiceMetrics[]
  }
}

export interface E2eScenario {
  name: string
  status: string
  durationMs: number
}

export interface E2eResults {
  status: 'passed' | 'failed' | 'pending'
  message?: string
  generatedAt: string | null
  total: number
  passed: number
  failed: number
  durationMs: number
  scenarios: E2eScenario[]
}

export interface LoadResults {
  status: 'passed' | 'failed' | 'pending'
  message?: string
  generatedAt: string | null
  targetRps: number
  achievedRps: number
  durationSeconds: number
  totalRequests: number
  errorRate: number
  p95LatencyMs: number
  endpoint?: string
  thresholds: {
    maxErrorRate: number
    errorRatePassed: boolean
    minAchievedRps: number
    targetRpsPassed: boolean
  }
  notes?: string[]
}
