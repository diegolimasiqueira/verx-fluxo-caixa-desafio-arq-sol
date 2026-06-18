import { useState } from 'react'
import {
  Space, Typography, Card, DatePicker, Button, Statistic, Row, Col,
  Alert, Skeleton, Table, Tag, Segmented,
} from 'antd'
import { SearchOutlined, ArrowUpOutlined, ArrowDownOutlined, WalletOutlined } from '@ant-design/icons'
import { useQuery } from '@tanstack/react-query'
import type { ColumnsType } from 'antd/es/table'
import dayjs, { type Dayjs } from 'dayjs'
import { balanceService } from '@/services/balance'
import type { DailyBalance } from '@/types'

const { Title, Text, Paragraph } = Typography
const { RangePicker } = DatePicker

type Mode = 'day' | 'period'

function formatBRL(v: number) {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(v)
}

const PERIOD_COLUMNS: ColumnsType<DailyBalance> = [
  {
    title: 'Data',
    dataIndex: 'date',
    key: 'date',
    render: (d: string) => dayjs(d).format('DD/MM/YYYY'),
    sorter: (a, b) => a.date.localeCompare(b.date),
  },
  {
    title: 'Créditos',
    dataIndex: 'totalCredits',
    key: 'credits',
    align: 'right',
    render: (v: number) => <Text style={{ color: '#52c41a' }}>{formatBRL(v)}</Text>,
  },
  {
    title: 'Débitos',
    dataIndex: 'totalDebits',
    key: 'debits',
    align: 'right',
    render: (v: number) => <Text style={{ color: '#ff4d4f' }}>{formatBRL(v)}</Text>,
  },
  {
    title: 'Saldo Consolidado',
    dataIndex: 'consolidatedBalance',
    key: 'balance',
    align: 'right',
    render: (v: number) => (
      <Text strong style={{ color: v >= 0 ? '#52c41a' : '#ff4d4f' }}>
        {formatBRL(v)}
      </Text>
    ),
  },
  {
    title: 'Atualizado em',
    dataIndex: 'updatedAt',
    key: 'updatedAt',
    render: (d: string) => dayjs(d).format('DD/MM/YYYY HH:mm'),
  },
]

export function BalancePage() {
  const [mode, setMode] = useState<Mode>('day')
  const [selectedDate, setSelectedDate] = useState<Dayjs>(dayjs())
  const [range, setRange] = useState<[Dayjs, Dayjs]>([dayjs().subtract(6, 'day'), dayjs()])
  const [queried, setQueried] = useState(false)

  const dateStr = selectedDate.format('YYYY-MM-DD')
  const fromStr = range[0].format('YYYY-MM-DD')
  const toStr = range[1].format('YYYY-MM-DD')

  const dayQuery = useQuery({
    queryKey: ['balance', dateStr],
    queryFn: () => balanceService.getByDate(dateStr),
    enabled: queried && mode === 'day',
    retry: false,
  })

  const periodQuery = useQuery({
    queryKey: ['balance', 'period', fromStr, toStr],
    queryFn: () => balanceService.getByPeriod(fromStr, toStr),
    enabled: queried && mode === 'period',
  })

  const handleSearch = () => setQueried(true)

  const balance = dayQuery.data
  const periodData = periodQuery.data ?? []

  const totalPeriodCredits = periodData.reduce((s, b) => s + b.totalCredits, 0)
  const totalPeriodDebits = periodData.reduce((s, b) => s + b.totalDebits, 0)

  return (
    <Space direction="vertical" size={24} style={{ width: '100%' }}>
      <div>
        <Title level={4} style={{ margin: 0 }}>Saldo Diário Consolidado</Title>
        <Text type="secondary">Consulte o saldo consolidado por data ou período</Text>
      </div>

      <Card title={<><SearchOutlined /> Filtros</>}>
        <Space direction="vertical" size={16} style={{ width: '100%' }}>
          <Segmented
            options={[
              { label: 'Por Data', value: 'day' },
              { label: 'Por Período', value: 'period' },
            ]}
            value={mode}
            onChange={(v) => { setMode(v as Mode); setQueried(false) }}
          />

          <Space wrap>
            {mode === 'day' ? (
              <DatePicker
                value={selectedDate}
                onChange={(d) => { d && setSelectedDate(d); setQueried(false) }}
                format="DD/MM/YYYY"
                allowClear={false}
                style={{ width: 180 }}
              />
            ) : (
              <RangePicker
                value={range}
                onChange={(dates) => {
                  if (dates?.[0] && dates?.[1]) {
                    setRange([dates[0], dates[1]])
                    setQueried(false)
                  }
                }}
                format="DD/MM/YYYY"
                allowClear={false}
                style={{ width: 280 }}
              />
            )}

            <Button type="primary" icon={<SearchOutlined />} onClick={handleSearch}>
              Consultar
            </Button>
          </Space>
        </Space>
      </Card>

      {/* Day result */}
      {queried && mode === 'day' && (
        <Card title={`Saldo de ${selectedDate.format('DD/MM/YYYY')}`}>
          {dayQuery.isLoading && <Skeleton active />}
          {dayQuery.isError && (
            <Alert
              message="Saldo não encontrado"
              description={`Nenhum saldo consolidado disponível para ${selectedDate.format('DD/MM/YYYY')}. Verifique se há lançamentos registrados nessa data e aguarde a consolidação assíncrona.`}
              type="info"
              showIcon
            />
          )}
          {balance && (
            <>
              <Row gutter={[24, 24]}>
                <Col xs={24} sm={8}>
                  <Statistic
                    title="Total Créditos"
                    value={balance.totalCredits}
                    precision={2}
                    valueStyle={{ color: '#52c41a' }}
                    prefix={<ArrowUpOutlined />}
                    formatter={(v) => formatBRL(Number(v))}
                  />
                </Col>
                <Col xs={24} sm={8}>
                  <Statistic
                    title="Total Débitos"
                    value={balance.totalDebits}
                    precision={2}
                    valueStyle={{ color: '#ff4d4f' }}
                    prefix={<ArrowDownOutlined />}
                    formatter={(v) => formatBRL(Number(v))}
                  />
                </Col>
                <Col xs={24} sm={8}>
                  <Statistic
                    title="Saldo Consolidado"
                    value={balance.consolidatedBalance}
                    precision={2}
                    valueStyle={{
                      color: balance.consolidatedBalance >= 0 ? '#52c41a' : '#ff4d4f',
                      fontSize: 28,
                    }}
                    prefix={<WalletOutlined />}
                    formatter={(v) => formatBRL(Number(v))}
                  />
                </Col>
              </Row>

              {balance.consolidatedBalance < 0 && (
                <Alert
                  style={{ marginTop: 16 }}
                  message="Saldo negativo"
                  description="Os débitos do dia superam os créditos registrados."
                  type="warning"
                  showIcon
                />
              )}

              <Paragraph type="secondary" style={{ marginTop: 16, marginBottom: 0, fontSize: 12 }}>
                Última atualização: {dayjs(balance.updatedAt).format('DD/MM/YYYY HH:mm:ss')}
              </Paragraph>
            </>
          )}
        </Card>
      )}

      {/* Period result */}
      {queried && mode === 'period' && (
        <Card
          title={`Período: ${range[0].format('DD/MM/YYYY')} a ${range[1].format('DD/MM/YYYY')}`}
        >
          {periodQuery.isLoading && <Skeleton active />}
          {!periodQuery.isLoading && (
            <>
              {periodData.length > 0 && (
                <Row gutter={16} style={{ marginBottom: 16 }}>
                  <Col>
                    <Tag color="success" icon={<ArrowUpOutlined />}>
                      Total créditos: {formatBRL(totalPeriodCredits)}
                    </Tag>
                  </Col>
                  <Col>
                    <Tag color="error" icon={<ArrowDownOutlined />}>
                      Total débitos: {formatBRL(totalPeriodDebits)}
                    </Tag>
                  </Col>
                  <Col>
                    <Tag
                      color={totalPeriodCredits - totalPeriodDebits >= 0 ? 'success' : 'error'}
                      icon={<WalletOutlined />}
                    >
                      Saldo período: {formatBRL(totalPeriodCredits - totalPeriodDebits)}
                    </Tag>
                  </Col>
                </Row>
              )}

              <Table
                columns={PERIOD_COLUMNS}
                dataSource={periodData}
                rowKey="date"
                pagination={false}
                locale={{ emptyText: 'Nenhum saldo encontrado no período.' }}
              />
            </>
          )}
        </Card>
      )}
    </Space>
  )
}
