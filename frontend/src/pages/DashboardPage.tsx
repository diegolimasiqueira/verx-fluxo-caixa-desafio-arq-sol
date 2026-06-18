import { useQuery } from '@tanstack/react-query'
import { Row, Col, Card, Statistic, Typography, Button, Space, Skeleton, Alert, Tag } from 'antd'
import {
  ArrowUpOutlined,
  ArrowDownOutlined,
  WalletOutlined,
  PlusCircleOutlined,
  EyeOutlined,
} from '@ant-design/icons'
import { Link } from 'react-router-dom'
import dayjs from 'dayjs'
import { balanceService } from '@/services/balance'
import { launchService } from '@/services/launches'
import type { Launch } from '@/types'

const { Title, Text } = Typography

const TODAY = dayjs().format('YYYY-MM-DD')
const WEEK_AGO = dayjs().subtract(6, 'day').format('YYYY-MM-DD')

function formatBRL(value: number) {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value)
}

function RecentLaunchTag({ type }: { type: Launch['type'] }) {
  return type === 'credit' ? (
    <Tag color="success" icon={<ArrowUpOutlined />}>Crédito</Tag>
  ) : (
    <Tag color="error" icon={<ArrowDownOutlined />}>Débito</Tag>
  )
}

export function DashboardPage() {
  const todayBalance = useQuery({
    queryKey: ['balance', TODAY],
    queryFn: () => balanceService.getByDate(TODAY),
    retry: false,
  })

  const weekLaunches = useQuery({
    queryKey: ['launches', 'period', WEEK_AGO, TODAY],
    queryFn: () => launchService.getByPeriod(WEEK_AGO, TODAY),
  })

  const weekCredits = weekLaunches.data
    ?.filter((l) => l.type === 'credit')
    .reduce((sum, l) => sum + l.amount, 0) ?? 0

  const weekDebits = weekLaunches.data
    ?.filter((l) => l.type === 'debit')
    .reduce((sum, l) => sum + l.amount, 0) ?? 0

  const recentLaunches = weekLaunches.data?.slice(-5).reverse() ?? []

  return (
    <Space direction="vertical" size={24} style={{ width: '100%' }}>
      <div>
        <Title level={4} style={{ margin: 0 }}>Dashboard</Title>
        <Text type="secondary">Visão geral do fluxo de caixa</Text>
      </div>

      {/* Stat cards */}
      <Row gutter={[16, 16]}>
        <Col xs={24} sm={12} lg={8}>
          <Card>
            {todayBalance.isLoading ? (
              <Skeleton active paragraph={{ rows: 1 }} />
            ) : todayBalance.isError ? (
              <Statistic
                title="Saldo Hoje"
                value="Sem dados"
                valueStyle={{ color: '#8c8c8c', fontSize: 18 }}
                prefix={<WalletOutlined />}
              />
            ) : (
              <Statistic
                title="Saldo Consolidado Hoje"
                value={todayBalance.data!.consolidatedBalance}
                precision={2}
                valueStyle={{
                  color: todayBalance.data!.consolidatedBalance >= 0 ? '#52c41a' : '#ff4d4f',
                }}
                prefix={<WalletOutlined />}
                formatter={(v) => formatBRL(Number(v))}
              />
            )}
          </Card>
        </Col>

        <Col xs={24} sm={12} lg={8}>
          <Card>
            {weekLaunches.isLoading ? (
              <Skeleton active paragraph={{ rows: 1 }} />
            ) : (
              <Statistic
                title="Créditos (7 dias)"
                value={weekCredits}
                precision={2}
                valueStyle={{ color: '#52c41a' }}
                prefix={<ArrowUpOutlined />}
                formatter={(v) => formatBRL(Number(v))}
              />
            )}
          </Card>
        </Col>

        <Col xs={24} sm={12} lg={8}>
          <Card>
            {weekLaunches.isLoading ? (
              <Skeleton active paragraph={{ rows: 1 }} />
            ) : (
              <Statistic
                title="Débitos (7 dias)"
                value={weekDebits}
                precision={2}
                valueStyle={{ color: '#ff4d4f' }}
                prefix={<ArrowDownOutlined />}
                formatter={(v) => formatBRL(Number(v))}
              />
            )}
          </Card>
        </Col>
      </Row>

      {/* Quick actions */}
      <Card title="Ações Rápidas">
        <Space wrap>
          <Link to="/launches">
            <Button type="primary" icon={<PlusCircleOutlined />} size="large">
              Registrar Lançamento
            </Button>
          </Link>
          <Link to="/balance">
            <Button icon={<EyeOutlined />} size="large">
              Consultar Saldo
            </Button>
          </Link>
        </Space>
      </Card>

      {/* Recent launches */}
      <Card
        title="Lançamentos Recentes (últimos 7 dias)"
        extra={<Link to="/launches"><Button type="link">Ver todos</Button></Link>}
      >
        {weekLaunches.isLoading && <Skeleton active />}
        {weekLaunches.isError && (
          <Alert message="Erro ao carregar lançamentos" type="error" showIcon />
        )}
        {!weekLaunches.isLoading && recentLaunches.length === 0 && (
          <Text type="secondary">Nenhum lançamento nos últimos 7 dias.</Text>
        )}
        {recentLaunches.map((launch) => (
          <div
            key={launch.id}
            style={{
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              padding: '10px 0',
              borderBottom: '1px solid #f0f0f0',
            }}
          >
            <Space>
              <RecentLaunchTag type={launch.type} />
              <div>
                <Text strong>{launch.description}</Text>
                <br />
                <Text type="secondary" style={{ fontSize: 12 }}>
                  {dayjs(launch.date).format('DD/MM/YYYY')}
                </Text>
              </div>
            </Space>
            <Text
              strong
              style={{ color: launch.type === 'credit' ? '#52c41a' : '#ff4d4f' }}
            >
              {launch.type === 'credit' ? '+' : '-'} {formatBRL(launch.amount)}
            </Text>
          </div>
        ))}
      </Card>
    </Space>
  )
}
