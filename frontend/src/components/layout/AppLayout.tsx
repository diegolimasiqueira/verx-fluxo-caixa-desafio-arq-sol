import { useState } from 'react'
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { Layout, Menu, Button, Avatar, Dropdown, Typography, theme, Space } from 'antd'
import {
  DashboardOutlined,
  TransactionOutlined,
  BarChartOutlined,
  ExperimentOutlined,
  TeamOutlined,
  LogoutOutlined,
  UserOutlined,
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  DollarCircleOutlined,
} from '@ant-design/icons'
import { useAuth } from '@/contexts/AuthContext'

const { Sider, Header, Content } = Layout
const { Text } = Typography

const NAV_ITEMS = [
  { key: '/', icon: <DashboardOutlined />, label: <Link to="/">Dashboard</Link> },
  { key: '/launches', icon: <TransactionOutlined />, label: <Link to="/launches">Lançamentos</Link> },
  { key: '/balance', icon: <BarChartOutlined />, label: <Link to="/balance">Saldo Diário</Link> },
  { key: '/users', icon: <TeamOutlined />, label: <Link to="/users">Usuários</Link> },
  { key: '/tests', icon: <ExperimentOutlined />, label: <Link to="/tests">Testes & Cobertura</Link> },
]

export function AppLayout() {
  const [collapsed, setCollapsed] = useState(false)
  const { logout, userEmail } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const { token } = theme.useToken()

  const handleLogout = () => {
    logout()
    navigate('/login')
  }

  const userMenu = {
    items: [
      {
        key: 'logout',
        icon: <LogoutOutlined />,
        label: 'Sair',
        onClick: handleLogout,
        danger: true,
      },
    ],
  }

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sider
        collapsible
        collapsed={collapsed}
        trigger={null}
        style={{
          background: token.colorBgContainer,
          borderRight: `1px solid ${token.colorBorderSecondary}`,
        }}
      >
        <div
          style={{
            height: 64,
            display: 'flex',
            alignItems: 'center',
            justifyContent: collapsed ? 'center' : 'flex-start',
            padding: collapsed ? 0 : '0 24px',
            gap: 10,
            borderBottom: `1px solid ${token.colorBorderSecondary}`,
          }}
        >
          <DollarCircleOutlined style={{ fontSize: 22, color: token.colorPrimary }} />
          {!collapsed && (
            <Text strong style={{ fontSize: 15, color: token.colorPrimary, whiteSpace: 'nowrap' }}>
              CashFlow
            </Text>
          )}
        </div>

        <Menu
          mode="inline"
          selectedKeys={[location.pathname]}
          items={NAV_ITEMS}
          style={{ borderRight: 0, marginTop: 8 }}
        />
      </Sider>

      <Layout>
        <Header
          style={{
            background: token.colorBgContainer,
            borderBottom: `1px solid ${token.colorBorderSecondary}`,
            padding: '0 24px',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
          }}
        >
          <Button
            type="text"
            icon={collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
            onClick={() => setCollapsed(!collapsed)}
            style={{ fontSize: 16 }}
          />

          <Dropdown menu={userMenu} placement="bottomRight">
            <Space style={{ cursor: 'pointer' }}>
              <Avatar icon={<UserOutlined />} style={{ backgroundColor: token.colorPrimary }} />
              <Text>{userEmail ?? 'Usuário'}</Text>
            </Space>
          </Dropdown>
        </Header>

        <Content
          style={{
            margin: 24,
            padding: 0,
            minHeight: 280,
            overflow: 'auto',
          }}
        >
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  )
}
