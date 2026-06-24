import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Form, Input, Button, Card, Typography, Alert, Space } from 'antd'
import { MailOutlined, LockOutlined, DollarCircleOutlined } from '@ant-design/icons'
import { useAuth } from '@/contexts/AuthContext'
import type { LoginRequest } from '@/types'

const { Title, Text } = Typography

export function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async (values: LoginRequest) => {
    setError(null)
    setLoading(true)
    try {
      await login(values)
      navigate('/')
    } catch {
      setError('E-mail ou senha inválidos.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div
      style={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        background: 'linear-gradient(135deg, #e6f4ff 0%, #f0f5ff 100%)',
      }}
    >
      <Card
        style={{ width: 400, boxShadow: '0 8px 32px rgba(0,0,0,0.08)' }}
        styles={{ body: { padding: '40px 40px 32px' } }}
      >
        <Space direction="vertical" align="center" style={{ width: '100%', marginBottom: 32 }}>
          <DollarCircleOutlined style={{ fontSize: 48, color: '#1677ff' }} />
          <Title level={3} style={{ margin: 0 }}>CashFlow Platform</Title>
          <Text type="secondary">Controle de fluxo de caixa</Text>
        </Space>

        {error && (
          <Alert message={error} type="error" showIcon style={{ marginBottom: 24 }} />
        )}

        <Form layout="vertical" onFinish={handleSubmit}>
          <Form.Item
            label="E-mail"
            name="email"
            rules={[
              { required: true, message: 'Informe o e-mail' },
              { type: 'email', message: 'E-mail inválido' },
            ]}
          >
            <Input prefix={<MailOutlined />} placeholder="admin@admin.com" size="large" />
          </Form.Item>

          <Form.Item
            label="Senha"
            name="password"
            rules={[{ required: true, message: 'Informe a senha' }]}
          >
            <Input.Password prefix={<LockOutlined />} placeholder="Senha" size="large" />
          </Form.Item>

          <Form.Item style={{ marginBottom: 0 }}>
            <Button
              type="primary"
              htmlType="submit"
              size="large"
              block
              loading={loading}
            >
              Entrar
            </Button>
          </Form.Item>
        </Form>
      </Card>
    </div>
  )
}
