import { useState } from 'react'
import { Space, Typography, Card, Tabs, Tag, Alert, Button } from 'antd'
import {
  ExperimentOutlined,
  CheckCircleOutlined,
  ExportOutlined,
} from '@ant-design/icons'

const { Title, Text } = Typography

interface ReportEntry {
  key: string
  label: string
  path: string
  tests: number
  color: string
}

const REPORTS: ReportEntry[] = [
  {
    key: 'launch',
    label: 'Launch Service',
    path: '/coverage/launch/index.html',
    tests: 10,
    color: 'blue',
  },
  {
    key: 'balance',
    label: 'Daily Balance Service',
    path: '/coverage/balance/index.html',
    tests: 4,
    color: 'green',
  },
  {
    key: 'worker',
    label: 'Daily Balance Worker',
    path: '/coverage/worker/index.html',
    tests: 4,
    color: 'purple',
  },
]

function ReportFrame({ path }: { path: string }) {
  return (
    <iframe
      src={path}
      title="Coverage Report"
      style={{
        width: '100%',
        height: 'calc(100vh - 320px)',
        minHeight: 600,
        border: 'none',
        borderRadius: 8,
        background: '#fff',
      }}
    />
  )
}

export function CoverageReportsPage() {
  const [activeTab, setActiveTab] = useState('launch')

  const tabItems = REPORTS.map((r) => ({
    key: r.key,
    label: (
      <Space>
        <Tag color={r.color}>{r.tests} testes</Tag>
        {r.label}
      </Space>
    ),
    children: (
      <Card
        size="small"
        extra={
          <Button
            icon={<ExportOutlined />}
            size="small"
            href={r.path}
            target="_blank"
            rel="noopener noreferrer"
          >
            Abrir em nova aba
          </Button>
        }
        styles={{ body: { padding: 0, borderRadius: 8, overflow: 'hidden' } }}
      >
        <ReportFrame path={r.path} />
      </Card>
    ),
  }))

  return (
    <Space direction="vertical" size={24} style={{ width: '100%' }}>
      <div>
        <Title level={4} style={{ margin: 0 }}>
          <ExperimentOutlined style={{ marginRight: 8 }} />
          Relatórios de Cobertura de Testes
        </Title>
        <Text type="secondary">
          Gerado com Coverlet + ReportGenerator — 18 testes no total
        </Text>
      </div>

      <Alert
        type="success"
        showIcon
        icon={<CheckCircleOutlined />}
        message={
          <Space>
            <Text strong>Todos os testes passando</Text>
            <Tag color="success">10 — Launch Service</Tag>
            <Tag color="success">4 — Daily Balance Service</Tag>
            <Tag color="success">4 — Daily Balance Worker</Tag>
          </Space>
        }
      />

      <Tabs
        activeKey={activeTab}
        onChange={setActiveTab}
        items={tabItems}
        type="card"
        size="large"
      />
    </Space>
  )
}
