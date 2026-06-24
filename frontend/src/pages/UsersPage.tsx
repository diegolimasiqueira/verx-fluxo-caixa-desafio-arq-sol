import { useState } from 'react'
import {
  Space, Typography, Card, Table, Button, Modal, Form, Input, App,
} from 'antd'
import { PlusOutlined, EditOutlined, KeyOutlined, TeamOutlined } from '@ant-design/icons'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import type { ColumnsType } from 'antd/es/table'
import dayjs from 'dayjs'
import { userService } from '@/services/users'
import type { CreateUserRequest, UpdateUserRequest, User } from '@/types'

const { Title, Text } = Typography

export function UsersPage() {
  const { message } = App.useApp()
  const queryClient = useQueryClient()
  const [createOpen, setCreateOpen] = useState(false)
  const [editUser, setEditUser] = useState<User | null>(null)
  const [passwordUser, setPasswordUser] = useState<User | null>(null)
  const [createForm] = Form.useForm<CreateUserRequest>()
  const [editForm] = Form.useForm<UpdateUserRequest>()
  const [passwordForm] = Form.useForm<{ password: string; confirm: string }>()

  const { data: users = [], isLoading } = useQuery({
    queryKey: ['users'],
    queryFn: () => userService.list(),
  })

  const createMutation = useMutation({
    mutationFn: userService.create,
    onSuccess: () => {
      message.success('Usuário criado')
      queryClient.invalidateQueries({ queryKey: ['users'] })
      setCreateOpen(false)
      createForm.resetFields()
    },
    onError: (err: { response?: { data?: { detail?: string } } }) => {
      message.error(err.response?.data?.detail ?? 'Erro ao criar usuário')
    },
  })

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateUserRequest }) =>
      userService.update(id, data),
    onSuccess: () => {
      message.success('Usuário atualizado')
      queryClient.invalidateQueries({ queryKey: ['users'] })
      setEditUser(null)
    },
    onError: (err: { response?: { data?: { detail?: string } } }) => {
      message.error(err.response?.data?.detail ?? 'Erro ao atualizar usuário')
    },
  })

  const passwordMutation = useMutation({
    mutationFn: ({ id, password }: { id: string; password: string }) =>
      userService.updatePassword(id, { password }),
    onSuccess: () => {
      message.success('Senha alterada')
      setPasswordUser(null)
      passwordForm.resetFields()
    },
    onError: () => message.error('Erro ao alterar senha'),
  })

  const columns: ColumnsType<User> = [
    { title: 'Nome', dataIndex: 'name', key: 'name' },
    { title: 'E-mail (login)', dataIndex: 'email', key: 'email' },
    {
      title: 'Perfil',
      dataIndex: 'role',
      key: 'role',
      width: 110,
      render: (role: string) => (role === 'admin' ? 'Admin' : 'Comerciante'),
    },
    {
      title: 'Atualizado em',
      dataIndex: 'updatedAt',
      key: 'updatedAt',
      width: 170,
      render: (d: string) => dayjs(d).format('DD/MM/YYYY HH:mm'),
    },
    {
      title: 'Ações',
      key: 'actions',
      width: 220,
      render: (_, record) => (
        <Space>
          <Button
            size="small"
            icon={<EditOutlined />}
            onClick={() => {
              setEditUser(record)
              editForm.setFieldsValue({ name: record.name, email: record.email })
            }}
          >
            Editar
          </Button>
          <Button
            size="small"
            icon={<KeyOutlined />}
            onClick={() => {
              setPasswordUser(record)
              passwordForm.resetFields()
            }}
          >
            Senha
          </Button>
        </Space>
      ),
    },
  ]

  return (
    <Space direction="vertical" size="large" style={{ width: '100%' }}>
      <div>
        <Title level={3} style={{ margin: 0 }}>
          <TeamOutlined /> Usuários
        </Title>
        <Text type="secondary">Gerencie os usuários com acesso ao sistema</Text>
      </div>

      <Card>
        <Space style={{ marginBottom: 16 }}>
          <Button type="primary" icon={<PlusOutlined />} onClick={() => setCreateOpen(true)}>
            Novo usuário
          </Button>
        </Space>

        <Table
          rowKey="id"
          columns={columns}
          dataSource={users}
          loading={isLoading}
          pagination={{ pageSize: 10 }}
        />
      </Card>

      <Modal
        title="Novo usuário"
        open={createOpen}
        onCancel={() => setCreateOpen(false)}
        onOk={() => createForm.submit()}
        confirmLoading={createMutation.isPending}
        okText="Criar"
      >
        <Form
          form={createForm}
          layout="vertical"
          onFinish={(values) => createMutation.mutate(values)}
        >
          <Form.Item name="name" label="Nome" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item
            name="email"
            label="E-mail (login)"
            rules={[{ required: true }, { type: 'email' }]}
          >
            <Input />
          </Form.Item>
          <Form.Item
            name="password"
            label="Senha"
            rules={[{ required: true, min: 6, message: 'Mínimo 6 caracteres' }]}
          >
            <Input.Password />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title="Editar usuário"
        open={!!editUser}
        onCancel={() => setEditUser(null)}
        onOk={() => editForm.submit()}
        confirmLoading={updateMutation.isPending}
        okText="Salvar"
      >
        <Form
          form={editForm}
          layout="vertical"
          onFinish={(values) => editUser && updateMutation.mutate({ id: editUser.id, data: values })}
        >
          <Form.Item name="name" label="Nome" rules={[{ required: true }]}>
            <Input />
          </Form.Item>
          <Form.Item
            name="email"
            label="E-mail (login)"
            rules={[{ required: true }, { type: 'email' }]}
          >
            <Input />
          </Form.Item>
        </Form>
      </Modal>

      <Modal
        title={`Alterar senha — ${passwordUser?.email ?? ''}`}
        open={!!passwordUser}
        onCancel={() => setPasswordUser(null)}
        onOk={() => passwordForm.submit()}
        confirmLoading={passwordMutation.isPending}
        okText="Alterar"
      >
        <Form
          form={passwordForm}
          layout="vertical"
          onFinish={({ password }) =>
            passwordUser && passwordMutation.mutate({ id: passwordUser.id, password })
          }
        >
          <Form.Item
            name="password"
            label="Nova senha"
            rules={[{ required: true, min: 6, message: 'Mínimo 6 caracteres' }]}
          >
            <Input.Password />
          </Form.Item>
          <Form.Item
            name="confirm"
            label="Confirmar senha"
            dependencies={['password']}
            rules={[
              { required: true },
              ({ getFieldValue }) => ({
                validator(_, value) {
                  if (!value || getFieldValue('password') === value) return Promise.resolve()
                  return Promise.reject(new Error('Senhas não conferem'))
                },
              }),
            ]}
          >
            <Input.Password />
          </Form.Item>
        </Form>
      </Modal>
    </Space>
  )
}
