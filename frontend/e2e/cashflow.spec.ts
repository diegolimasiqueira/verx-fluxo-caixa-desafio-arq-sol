import { test, expect } from '@playwright/test'

const ADMIN = { email: 'admin@admin.com', password: 'Master@123' }

test.describe('Autenticação', () => {
  test('login com credenciais válidas redireciona ao dashboard', async ({ page }) => {
    await page.goto('/login')
    await expect(page.getByRole('heading', { name: 'CashFlow Platform' })).toBeVisible()
    await page.getByPlaceholder('admin@admin.com').fill(ADMIN.email)
    await page.getByPlaceholder('Senha').fill(ADMIN.password)
    await page.getByRole('button', { name: 'Entrar' }).click()
    await expect(page).toHaveURL('/')
    await expect(page.getByRole('heading', { name: 'Dashboard', level: 4 })).toBeVisible()
  })

  test('login inválido exibe erro', async ({ page }) => {
    await page.goto('/login')
    await page.getByPlaceholder('admin@admin.com').fill('wrong@test.com')
    await page.getByPlaceholder('Senha').fill('wrong')
    await page.getByRole('button', { name: 'Entrar' }).click()
    await expect(page.getByRole('alert')).toContainText(/E-mail ou senha inválidos/i)
  })
})

test.describe('Fluxo de negócio', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/login')
    await page.getByPlaceholder('admin@admin.com').fill(ADMIN.email)
    await page.getByPlaceholder('Senha').fill(ADMIN.password)
    await page.getByRole('button', { name: 'Entrar' }).click()
    await expect(page).toHaveURL('/')
  })

  test('registrar lançamento e consultar saldo consolidado', async ({ page }) => {
    const description = `E2E ${Date.now()}`

    await page.getByRole('link', { name: 'Lançamentos' }).click()
    await expect(page.getByRole('heading', { name: /Lançamentos/i })).toBeVisible()

    await page.getByPlaceholder('Ex: Venda de produto, Pagamento de fornecedor...').fill(description)
    await page.locator('.ant-input-number-input').fill('250')

    const [listRefresh] = await Promise.all([
      page.waitForResponse(
        res => res.url().includes('/api/launches') && res.request().method() === 'GET' && res.status() === 200,
        { timeout: 15_000 }
      ),
      page.getByRole('button', { name: 'Registrar Lançamento' }).click(),
    ])
    await expect(page.getByText('Lançamento registrado com sucesso!')).toBeVisible({ timeout: 10_000 })
    expect(listRefresh.ok()).toBeTruthy()
    await expect(page.getByText(description)).toBeVisible({ timeout: 10_000 })

    await page.getByRole('link', { name: 'Saldo Diário' }).click()
    await expect(page.getByRole('heading', { name: 'Saldo Diário Consolidado' })).toBeVisible()
    await page.getByRole('button', { name: 'Consultar' }).click()

    const balanceTitle = page.locator('.ant-statistic-title').filter({ hasText: /^Saldo Consolidado$/ })

    for (let attempt = 0; attempt < 6; attempt++) {
      if (await balanceTitle.isVisible()) break
      await page.waitForTimeout(3000)
      await page.getByRole('button', { name: 'Consultar' }).click()
    }

    await expect(balanceTitle).toBeVisible({ timeout: 5_000 })
  })

  test('admin acessa gerenciamento de usuários', async ({ page }) => {
    await page.getByRole('link', { name: 'Usuários' }).click()
    await expect(page.getByRole('heading', { name: /Usuários/i })).toBeVisible()
    await expect(page.getByRole('table').getByText('admin@admin.com')).toBeVisible()
  })

  test('página de testes carrega métricas', async ({ page }) => {
    await page.getByRole('link', { name: /Testes/i }).click()
    await expect(page.getByRole('heading', { name: /Relatórios de Testes/i })).toBeVisible()
    const metrics = await page.request.get('/test-metrics.json')
    expect(metrics.ok()).toBeTruthy()
  })
})
