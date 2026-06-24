import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  Space, Typography, Card, Tabs, Tag, Alert, Button, Table, Statistic, Row, Col, Skeleton, Descriptions,
} from 'antd'
import {
  ExperimentOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  ExportOutlined,
  ThunderboltOutlined,
  GlobalOutlined,
  CloudServerOutlined,
  ClockCircleOutlined,
} from '@ant-design/icons'
import { fetchE2eResults, fetchLoadResults, fetchTestMetrics } from '@/services/testReports'
import type { BackendServiceMetrics } from '@/types/testReports'

const { Title, Text, Paragraph } = Typography

const SERVICE_COLORS: Record<string, string> = {
  bff: 'geekblue',
  launch: 'blue',
  balance: 'green',
  worker: 'purple',
}

function ReportFrame({ path }: { path: string }) {
  return (
    <iframe
      key={path}
      src={path}
      title="Coverage Report"
      style={{
        width: '100%',
        height: 'calc(100vh - 380px)',
        minHeight: 500,
        border: 'none',
        borderRadius: 8,
        background: '#fff',
      }}
    />
  )
}

function CoverageTab({ services, cacheVersion }: { services: BackendServiceMetrics[]; cacheVersion?: string }) {
  const [activeTab, setActiveTab] = useState(services[0]?.key ?? 'bff')
  const reportSrc = (path: string) => (cacheVersion ? `${path}?v=${encodeURIComponent(cacheVersion)}` : path)

  const tabItems = services.map((r) => ({
    key: r.key,
    label: (
      <Space>
        <Tag color={SERVICE_COLORS[r.key] ?? 'default'}>{r.tests} testes</Tag>
        {r.name}
        <Text type="secondary" style={{ fontSize: 12 }}>
          {r.lineCoverage}% / {r.branchCoverage}%
        </Text>
      </Space>
    ),
    children: (
      <Card
        size="small"
        extra={
          <Button
            icon={<ExportOutlined />}
            size="small"
            href={r.reportPath}
            target="_blank"
            rel="noopener noreferrer"
          >
            Abrir em nova aba
          </Button>
        }
        styles={{ body: { padding: 0, borderRadius: 8, overflow: 'hidden' } }}
      >
        <ReportFrame path={reportSrc(r.reportPath)} />
      </Card>
    ),
  }))

  return (
    <Tabs activeKey={activeTab} onChange={setActiveTab} items={tabItems} type="card" size="large" />
  )
}

export function CoverageReportsPage() {
  const metricsQuery = useQuery({ queryKey: ['test-metrics'], queryFn: fetchTestMetrics, staleTime: 0 })
  const e2eQuery = useQuery({ queryKey: ['e2e-results'], queryFn: fetchE2eResults, staleTime: 0 })
  const loadQuery = useQuery({ queryKey: ['load-results'], queryFn: fetchLoadResults, staleTime: 0 })

  const metrics = metricsQuery.data
  const e2e = e2eQuery.data
  const load = loadQuery.data

  const backendOk = metrics?.backend.allPassing ?? false
  const allCoverage100 = metrics?.backend.services.every(
    (s) => s.lineCoverage >= 100 && s.branchCoverage >= 100,
  ) ?? false
  const minLineCoverage = metrics
    ? Math.min(...metrics.backend.services.map((s) => s.lineCoverage))
    : 0
  const minBranchCoverage = metrics
    ? Math.min(...metrics.backend.services.map((s) => s.branchCoverage))
    : 0
  const e2eOk = e2e?.status === 'passed'
  const loadOk = load?.status === 'passed'
  const e2ePending = e2e?.status === 'pending'
  const loadPending = load?.status === 'pending'
  const loadFailed = load?.status === 'failed'

  const mainTabs = [
    {
      key: 'backend',
      label: (
        <Space>
          <ExperimentOutlined />
          Backend
          {metrics && <Tag color={backendOk ? 'success' : 'error'}>{metrics.backend.total}</Tag>}
        </Space>
      ),
      children: metricsQuery.isLoading ? (
        <Skeleton active paragraph={{ rows: 6 }} />
      ) : metrics ? (
        <Space direction="vertical" size={16} style={{ width: '100%' }}>
          <Alert
            type={backendOk ? 'success' : 'error'}
            showIcon
            message={
              <Space wrap>
                <Text strong>
                  {backendOk ? 'Todos os testes backend passando' : 'Falhas nos testes backend'}
                </Text>
                {metrics.backend.services.map((s) => (
                  <Tag
                    key={s.key}
                    color={s.failed === 0 && s.lineCoverage >= 100 && s.branchCoverage >= 100 ? 'success' : 'warning'}
                  >
                    {s.passed}/{s.tests} — {s.lineCoverage}%/{s.branchCoverage}% — {s.name}
                  </Tag>
                ))}
              </Space>
            }
            description={
              <Text type="secondary">
                Coverlet + ReportGenerator — {metrics.backend.total} testes — cobertura mínima{' '}
                {minLineCoverage}% linha / {minBranchCoverage}% branch
                {metrics.generatedAt && ` — ${new Date(metrics.generatedAt).toLocaleString('pt-BR')}`}
              </Text>
            }
          />
          <CoverageTab services={metrics.backend.services} cacheVersion={metrics.generatedAt} />
        </Space>
      ) : (
        <Alert type="warning" message="Execute ./build.sh para gerar test-metrics.json" />
      ),
    },
    {
      key: 'e2e',
      label: (
        <Space>
          <GlobalOutlined />
          E2E
          {e2e && !e2ePending && (
            <Tag color={e2eOk ? 'success' : 'error'}>{e2e.passed}/{e2e.total}</Tag>
          )}
        </Space>
      ),
      children: e2eQuery.isLoading ? (
        <Skeleton active />
      ) : e2e ? (
        <Space direction="vertical" size={16} style={{ width: '100%' }}>
          <Alert
            type={e2ePending ? 'info' : e2eOk ? 'success' : 'error'}
            showIcon
            icon={e2ePending ? <ClockCircleOutlined /> : e2eOk ? <CheckCircleOutlined /> : <CloseCircleOutlined />}
            message={
              e2ePending
                ? 'E2E ainda não executado'
                : e2eOk
                  ? `E2E Playwright — ${e2e.passed}/${e2e.total} cenários OK`
                  : `E2E falhou — ${e2e.failed} cenário(s)`
            }
            description={e2ePending ? e2e.message : `Duração: ${(e2e.durationMs / 1000).toFixed(1)}s`}
          />
          {!e2ePending && (
            <Table
              size="small"
              pagination={false}
              rowKey="name"
              dataSource={e2e.scenarios}
              columns={[
                { title: 'Cenário', dataIndex: 'name', key: 'name' },
                {
                  title: 'Status',
                  dataIndex: 'status',
                  key: 'status',
                  width: 100,
                  render: (s: string) => (
                    <Tag color={s === 'passed' ? 'success' : 'error'}>{s}</Tag>
                  ),
                },
                {
                  title: 'Duração',
                  dataIndex: 'durationMs',
                  key: 'durationMs',
                  width: 100,
                  render: (ms: number) => `${ms}ms`,
                },
              ]}
            />
          )}
          <Paragraph type="secondary">
            Playwright contra {import.meta.env.VITE_E2E_BASE_URL ?? 'http://localhost:3000'} — login, lançamento, saldo, usuários.
            Comando: <Text code>./scripts/run-quality-gates.sh</Text>
          </Paragraph>
        </Space>
      ) : null,
    },
    {
      key: 'load',
      label: (
        <Space>
          <ThunderboltOutlined />
          Carga (50 req/s)
          {load && !loadPending && (
            <Tag color={loadOk ? 'success' : 'error'}>{load.achievedRps} req/s</Tag>
          )}
        </Space>
      ),
      children: loadQuery.isLoading ? (
        <Skeleton active />
      ) : load ? (
        <Space direction="vertical" size={16} style={{ width: '100%' }}>
          <Alert
            type={loadPending ? 'info' : loadOk ? 'success' : 'error'}
            showIcon
            message={
              loadPending
                ? 'Teste de carga ainda não executado'
                : loadOk
                  ? `NFR atendido — ${load.achievedRps} req/s (meta: ${load.targetRps})`
                  : 'NFR não validado nesta execução'
            }
            description={
              loadPending || (loadFailed && !load.totalRequests)
                ? load.message
                : load.endpoint
            }
          />
          {!loadPending && load.totalRequests > 0 && (
            <>
              <Row gutter={16}>
                <Col xs={12} sm={6}>
                  <Card><Statistic title="Req/s atingido" value={load.achievedRps} suffix={`/ ${load.targetRps}`} /></Card>
                </Col>
                <Col xs={12} sm={6}>
                  <Card><Statistic title="Taxa de erro" value={(load.errorRate * 100).toFixed(2)} suffix="%" /></Card>
                </Col>
                <Col xs={12} sm={6}>
                  <Card><Statistic title="P95 latência" value={load.p95LatencyMs} suffix="ms" /></Card>
                </Col>
                <Col xs={12} sm={6}>
                  <Card><Statistic title="Total requisições" value={load.totalRequests} /></Card>
                </Col>
              </Row>
              <Descriptions bordered size="small" column={1}>
                <Descriptions.Item label="Erro ≤ 5%">
                  <Tag color={load.thresholds.errorRatePassed ? 'success' : 'error'}>
                    {(load.errorRate * 100).toFixed(2)}% — {load.thresholds.errorRatePassed ? 'OK' : 'FALHA'}
                  </Tag>
                </Descriptions.Item>
                <Descriptions.Item label="Throughput ≥ 95% da meta (47,5 req/s)">
                  <Tag color={load.thresholds.targetRpsPassed ? 'success' : 'error'}>
                    {load.achievedRps} req/s — {load.thresholds.targetRpsPassed ? 'OK' : 'FALHA'}
                  </Tag>
                </Descriptions.Item>
                <Descriptions.Item label="Cache">
                  IMemoryCache (TTL 5 min) no Daily Balance Service — leitura do consolidado
                </Descriptions.Item>
              </Descriptions>
              {load.notes?.map((n) => (
                <Text key={n} type="secondary" style={{ display: 'block' }}>{n}</Text>
              ))}
            </>
          )}
          <Paragraph type="secondary">
            k6 — <Text code>tests/load/balance-consolidated.js</Text> — executor constant-arrival-rate 50/s por 30s.
          </Paragraph>
        </Space>
      ) : null,
    },
    {
      key: 'scale',
      label: (
        <Space>
          <CloudServerOutlined />
          Escalabilidade
        </Space>
      ),
      children: (
        <Space direction="vertical" size={16} style={{ width: '100%' }}>
          <Alert
            type="warning"
            showIcon
            message="MVP local — sem autoscaling ativo"
            description="Docker Compose sobe 1 container por serviço (réplicas fixas). HPA e KEDA não estão instalados nem aplicados neste ambiente; o teste de carga valida capacidade do serviço, não escala pods."
          />

          <Descriptions bordered column={1} title="Ambiente atual (Docker Compose)">
            <Descriptions.Item label="Daily Balance Service">1 réplica fixa — sem HPA</Descriptions.Item>
            <Descriptions.Item label="Daily Balance Worker">1 réplica fixa — sem KEDA</Descriptions.Item>
            <Descriptions.Item label="Validação do RNF (50 req/s)">
              {load && load.status === 'passed' && load.totalRequests > 0 ? (
                <Tag color="success">
                  k6: {load.achievedRps} req/s, erro {(load.errorRate * 100).toFixed(2)}% — aba Carga
                </Tag>
              ) : loadPending ? (
                <Text type="secondary">Execute ./scripts/run-quality-gates.sh</Text>
              ) : (
                <Tag color="default">Teste de carga pendente ou não validado</Tag>
              )}
            </Descriptions.Item>
            <Descriptions.Item label="Cache (implementado)">
              IMemoryCache no Daily Balance Service (TTL 5 min)
            </Descriptions.Item>
          </Descriptions>

          <Descriptions bordered column={1} title="Evolução produção (Kubernetes — só artefatos YAML, não deployados)">
            <Descriptions.Item label="Daily Balance Service">
              <Tag>referência</Tag> HPA CPU 70% / memória 80%, 2–10 réplicas
              <br />
              <Text code>infra/k8s/daily-balance-service-hpa.yaml</Text>
            </Descriptions.Item>
            <Descriptions.Item label="Daily Balance Worker">
              <Tag>referência</Tag> KEDA fila RabbitMQ, 1–8 réplicas
              <br />
              <Text code>infra/k8s/daily-balance-worker-keda.yaml</Text>
            </Descriptions.Item>
            <Descriptions.Item label="Requisito (RNF)">
              50 req/s consolidado diário, máx. 5% perda — <Text code>Documentos/01-requisitos.md</Text>
            </Descriptions.Item>
          </Descriptions>
        </Space>
      ),
    },
  ]

  return (
    <Space direction="vertical" size={24} style={{ width: '100%' }}>
      <div>
        <Title level={4} style={{ margin: 0 }}>
          <ExperimentOutlined style={{ marginRight: 8 }} />
          Relatórios de Testes
        </Title>
        <Text type="secondary">
          Backend (xUnit + Testcontainers) · E2E (Playwright) · Carga (k6)
          {metrics && allCoverage100 ? ' · Cobertura 100%' : metrics ? ` · Cobertura min ${minLineCoverage}%` : ''}
        </Text>
      </div>

      <Tabs defaultActiveKey="backend" items={mainTabs} size="large" />
    </Space>
  )
}
