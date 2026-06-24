import { useState } from 'react'
import {
  Space, Typography, Card, Form, InputNumber, Select, Input, Button,
  DatePicker, Table, Tag, App, Row, Col, Divider,
} from 'antd'
import { PlusOutlined, SearchOutlined, ArrowUpOutlined, ArrowDownOutlined } from '@ant-design/icons'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import type { ColumnsType } from 'antd/es/table'
import dayjs from 'dayjs'
import type { Dayjs } from 'dayjs'
import { launchService } from '@/services/launches'
import type { Launch, RegisterLaunchRequest } from '@/types'

const { Title, Text } = Typography

function formatBRL(v: number) {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(v)
}

const COLUMNS: ColumnsType<Launch> = [
  {
    title: 'Data',
    dataIndex: 'date',
    key: 'date',
    width: 110,
    render: (d: string) => dayjs(d).format('DD/MM/YYYY'),
    sorter: (a, b) => a.date.localeCompare(b.date),
  },
  {
    title: 'Tipo',
    dataIndex: 'type',
    key: 'type',
    width: 110,
    render: (t: Launch['type']) =>
      t === 'credit' ? (
        <Tag color="success" icon={<ArrowUpOutlined />}>Crédito</Tag>
      ) : (
        <Tag color="error" icon={<ArrowDownOutlined />}>Débito</Tag>
      ),
  },
  {
    title: 'Descrição',
    dataIndex: 'description',
    key: 'description',
    ellipsis: true,
  },
  {
    title: 'Valor',
    dataIndex: 'amount',
    key: 'amount',
    width: 140,
    align: 'right',
    render: (a: number, record: Launch) => (
      <Text strong style={{ color: record.type === 'credit' ? '#52c41a' : '#ff4d4f' }}>
        {record.type === 'credit' ? '+' : '-'} {formatBRL(a)}
      </Text>
    ),
    sorter: (a, b) => a.amount - b.amount,
  },
  {
    title: 'Registrado em',
    dataIndex: 'createdAt',
    key: 'createdAt',
    width: 160,
    defaultSortOrder: 'descend' as const,
    sorter: (a: Launch, b: Launch) => a.createdAt.localeCompare(b.createdAt),
    render: (d: string) => dayjs(d).format('DD/MM/YYYY HH:mm'),
  },
]

export function LaunchesPage() {
  const { message } = App.useApp()
  const queryClient = useQueryClient()
  const [form] = Form.useForm<Omit<RegisterLaunchRequest, 'date'> & { date: Dayjs }>()
  const [filterDate, setFilterDate] = useState<Dayjs>(dayjs())

  const dateStr = filterDate.format('YYYY-MM-DD')

  const { data: launches = [], isLoading } = useQuery({
    queryKey: ['launches', dateStr],
    queryFn: () => launchService.getByDate(dateStr),
  })

  const mutation = useMutation({
    mutationFn: (data: RegisterLaunchRequest) => launchService.register(data),
    onSuccess: () => {
      message.success('Lançamento registrado com sucesso!')
      form.resetFields()
      form.setFieldsValue({ date: dayjs(), type: 'credit' })
      queryClient.invalidateQueries({ queryKey: ['launches'] })
      queryClient.invalidateQueries({ queryKey: ['balance'] })
    },
    onError: () => {
      message.error('Erro ao registrar lançamento.')
    },
  })

  const handleSubmit = (values: Omit<RegisterLaunchRequest, 'date'> & { date: Dayjs }) => {
    mutation.mutate({
      date: values.date.format('YYYY-MM-DD'),
      amount: values.amount,
      type: values.type,
      description: values.description,
    })
  }

  const totalCredits = launches.filter((l) => l.type === 'credit').reduce((s, l) => s + l.amount, 0)
  const totalDebits = launches.filter((l) => l.type === 'debit').reduce((s, l) => s + l.amount, 0)

  return (
    <Space direction="vertical" size={24} style={{ width: '100%' }}>
      <div>
        <Title level={4} style={{ margin: 0 }}>Lançamentos</Title>
        <Text type="secondary">Registre e consulte débitos e créditos</Text>
      </div>

      {/* Register form */}
      <Card title={<><PlusOutlined /> Registrar Novo Lançamento</>}>
        <Form
          form={form}
          layout="vertical"
          onFinish={handleSubmit}
          initialValues={{ date: dayjs(), type: 'credit' }}
        >
          <Row gutter={16}>
            <Col xs={24} sm={12} md={6}>
              <Form.Item
                label="Data"
                name="date"
                rules={[{ required: true, message: 'Informe a data' }]}
              >
                <DatePicker style={{ width: '100%' }} format="DD/MM/YYYY" />
              </Form.Item>
            </Col>

            <Col xs={24} sm={12} md={4}>
              <Form.Item
                label="Tipo"
                name="type"
                rules={[{ required: true }]}
              >
                <Select>
                  <Select.Option value="credit">
                    <Tag color="success" icon={<ArrowUpOutlined />}>Crédito</Tag>
                  </Select.Option>
                  <Select.Option value="debit">
                    <Tag color="error" icon={<ArrowDownOutlined />}>Débito</Tag>
                  </Select.Option>
                </Select>
              </Form.Item>
            </Col>

            <Col xs={24} sm={12} md={6}>
              <Form.Item
                label="Valor (R$)"
                name="amount"
                rules={[
                  { required: true, message: 'Informe o valor' },
                  { type: 'number', min: 0.01, message: 'Valor deve ser maior que zero' },
                ]}
              >
                <InputNumber
                  style={{ width: '100%' }}
                  min={0.01}
                  step={0.01}
                  precision={2}
                  placeholder="0,00"
                  decimalSeparator=","
                />
              </Form.Item>
            </Col>

            <Col xs={24} sm={12} md={8}>
              <Form.Item
                label="Descrição"
                name="description"
                rules={[
                  { required: true, message: 'Informe a descrição' },
                  { max: 255, message: 'Máximo 255 caracteres' },
                ]}
              >
                <Input placeholder="Ex: Venda de produto, Pagamento de fornecedor..." />
              </Form.Item>
            </Col>
          </Row>

          <Button
            type="primary"
            htmlType="submit"
            icon={<PlusOutlined />}
            loading={mutation.isPending}
            size="large"
          >
            Registrar Lançamento
          </Button>
        </Form>
      </Card>

      {/* Filter + table */}
      <Card
        title={<><SearchOutlined /> Consultar Lançamentos</>}
        extra={
          <Space>
            <Text type="secondary">Data:</Text>
            <DatePicker
              value={filterDate}
              onChange={(d) => d && setFilterDate(d)}
              format="DD/MM/YYYY"
              allowClear={false}
            />
          </Space>
        }
      >
        {launches.length > 0 && (
          <>
            <Row gutter={16} style={{ marginBottom: 16 }}>
              <Col>
                <Text>
                  Total créditos:{' '}
                  <Text strong style={{ color: '#52c41a' }}>{formatBRL(totalCredits)}</Text>
                </Text>
              </Col>
              <Col>
                <Divider type="vertical" />
                <Text>
                  Total débitos:{' '}
                  <Text strong style={{ color: '#ff4d4f' }}>{formatBRL(totalDebits)}</Text>
                </Text>
              </Col>
              <Col>
                <Divider type="vertical" />
                <Text>
                  Saldo do dia:{' '}
                  <Text
                    strong
                    style={{ color: totalCredits - totalDebits >= 0 ? '#52c41a' : '#ff4d4f' }}
                  >
                    {formatBRL(totalCredits - totalDebits)}
                  </Text>
                </Text>
              </Col>
            </Row>
          </>
        )}

        <Table
          columns={COLUMNS}
          dataSource={launches}
          rowKey="id"
          loading={isLoading}
          pagination={{ pageSize: 10, showSizeChanger: true }}
          locale={{ emptyText: 'Nenhum lançamento encontrado para esta data.' }}
        />
      </Card>
    </Space>
  )
}
