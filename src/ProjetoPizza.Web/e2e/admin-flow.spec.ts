import { expect, test } from '@playwright/test'
import { stat } from 'node:fs/promises'

test('abre produto em modal e valida os campos em português', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('Usuário ou e-mail', { exact: true }).fill('admin@local.test')
  await page.getByLabel('Senha', { exact: true }).fill('senha-local')
  await page.getByRole('button', { name: 'Entrar no sistema' }).click()

  await page.goto('/admin/catalog/products')
  await page.getByRole('button', { name: 'Adicionar produto' }).click()
  const dialog = page.getByRole('dialog', { name: 'Novo produto' })
  await expect(dialog).toBeVisible()
  await dialog.getByLabel('Tipo').selectOption('Pizza')
  await dialog.getByRole('tab', { name: /Complementos/ }).click()
  await expect(dialog.getByRole('button', { name: 'Adicionar' })).toBeVisible()
  const dialogBox = await dialog.boundingBox()
  const quickCreateBox = await dialog.locator('.new-complement-card').boundingBox()
  expect(dialogBox).toBeTruthy()
  expect(quickCreateBox).toBeTruthy()
  expect(quickCreateBox!.x).toBeGreaterThanOrEqual(dialogBox!.x)
  expect(quickCreateBox!.x + quickCreateBox!.width).toBeLessThanOrEqual(dialogBox!.x + dialogBox!.width + 1)
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)
  await dialog.getByRole('tab', { name: 'Dados do produto' }).click()
  await page.getByRole('button', { name: 'Salvar produto' }).click()
  await expect(page.getByText('O nome é obrigatório.')).toBeVisible()
})

test('traduz o estado do chamado depois de assumir a solicitação', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('Usuário ou e-mail', { exact: true }).fill('admin@local.test')
  await page.getByLabel('Senha', { exact: true }).fill('senha-local')
  await page.getByRole('button', { name: 'Entrar no sistema' }).click()

  await page.goto('/admin/service-calls')
  const call = page.locator('.service-call-card', { hasText: 'Mesa 03' })
  await expect(call.getByText('Pendente', { exact: true })).toBeVisible()
  await call.getByRole('button', { name: 'Assumir' }).click()
  await expect(call.getByText('Assumido', { exact: true })).toBeVisible()
  await expect(call.getByText('Acknowledged', { exact: true })).toHaveCount(0)
})

test('edita preços de borda inteira e meia borda por tamanho', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('Usuário ou e-mail', { exact: true }).fill('admin@local.test')
  await page.getByLabel('Senha', { exact: true }).fill('senha-local')
  await page.getByRole('button', { name: 'Entrar no sistema' }).click()

  await page.goto('/admin/catalog/crusts')
  await page.locator('.management-card', { hasText: 'Catupiry' }).getByRole('button', { name: 'Editar' }).click()

  const dialog = page.getByRole('dialog', { name: 'Editar borda' })
  await expect(dialog.getByText('Preços por tamanho')).toBeVisible()
  await expect(dialog.getByLabel('Preço da borda inteira para pizza Broto')).toHaveValue('R$ 12,00')
  await expect(dialog.getByLabel('Preço da meia borda para pizza Broto')).toHaveValue('R$ 6,00')
  await expect(dialog.getByText('Na opção dividida, o total será a soma dos preços das duas metades escolhidas.')).toBeVisible()
})

test('mascara valores e divide a conta por forma de pagamento individual', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('Usuário ou e-mail', { exact: true }).fill('admin@local.test')
  await page.getByLabel('Senha', { exact: true }).fill('senha-local')
  await page.getByRole('button', { name: 'Entrar no sistema' }).click()

  await page.goto('/admin/tables/40000000-0000-0000-0000-000000000012')
  await page.getByRole('button', { name: 'Registrar pagamento' }).click()

  const dialog = page.getByRole('dialog', { name: 'Registrar pagamento' })
  await expect(dialog.getByText('Saldo da conta: R$ 152,35')).toBeVisible()
  await expect(dialog.getByText('A mesa solicitou divisão entre')).toBeVisible()
  await expect(dialog.getByRole('tab', { name: 'Dividir por pessoas' })).toHaveAttribute('aria-selected', 'true')
  await expect(dialog.getByLabel('Quantidade de pessoas')).toHaveValue('3')

  const people = dialog.locator('.split-payment-card')
  await expect(people).toHaveCount(3)
  await expect(people.nth(0)).toContainText('R$ 50,79')
  await expect(people.nth(1)).toContainText('R$ 50,78')
  await expect(people.nth(2)).toContainText('R$ 50,78')
  await expect(dialog.getByLabel('Forma de pagamento de Pessoa 1')).toHaveValue('70000000-0000-0000-0000-000000000001')
  await expect(dialog.getByRole('button', { name: 'Confirmar 3 pagamentos' })).toBeVisible()
})

test('gera relatório financeiro como arquivo Excel', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('Usuário ou e-mail', { exact: true }).fill('admin@local.test')
  await page.getByLabel('Senha', { exact: true }).fill('senha-local')
  await page.getByRole('button', { name: 'Entrar no sistema' }).click()

  await page.goto('/admin/reports')
  const [download] = await Promise.all([
    page.waitForEvent('download'),
    page.getByRole('button', { name: 'Exportar Excel (.xlsx)' }).click(),
  ])

  expect(download.suggestedFilename()).toMatch(/^relatorio-financeiro-.+\.xlsx$/)
  const filePath = await download.path()
  expect(filePath).toBeTruthy()
  expect((await stat(filePath!)).size).toBeGreaterThan(5_000)
  await expect(page.getByText('Relatório Excel gerado')).toBeVisible()
})

test('exporta a lista de pedidos como PDF tabular', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('Usuário ou e-mail', { exact: true }).fill('admin@local.test')
  await page.getByLabel('Senha', { exact: true }).fill('senha-local')
  await page.getByRole('button', { name: 'Entrar no sistema' }).click()

  await page.goto('/admin/orders')
  const [download] = await Promise.all([
    page.waitForEvent('download'),
    page.getByRole('button', { name: 'Exportar pedidos em PDF' }).click(),
  ])

  expect(download.suggestedFilename()).toMatch(/^pedidos-\d{4}-\d{2}-\d{2}\.pdf$/)
  const filePath = await download.path()
  expect(filePath).toBeTruthy()
  expect((await stat(filePath!)).size).toBeGreaterThan(3_000)
  await expect(page.getByText('Relatório PDF gerado')).toBeVisible()
})

test('fecha e abre um novo turno de caixa pelo fluxo completo', async ({ page }, testInfo) => {
  await page.goto('/login')
  await page.getByLabel('Usuário ou e-mail', { exact: true }).fill('admin@local.test')
  await page.getByLabel('Senha', { exact: true }).fill('senha-local')
  await page.getByRole('button', { name: 'Entrar no sistema' }).click()

  await page.goto('/admin/cashier')
  await page.getByRole('button', { name: 'Fechar caixa', exact: true }).click()
  const confirmation = page.getByRole('alertdialog', { name: 'Confirmar fechamento do caixa?' })
  await confirmation.getByRole('button', { name: 'Fechar caixa' }).click()

  await expect(page.getByRole('heading', { name: 'Caixa fechado' })).toBeVisible()
  if (testInfo.project.name === 'chromium') {
    await expect(page.getByRole('button', { name: 'Caixa fechado. Acessar caixa' })).toBeVisible()
  }
  await page.getByRole('button', { name: 'Abrir caixa', exact: true }).click()

  const opening = page.getByRole('dialog', { name: 'Abrir caixa' })
  await expect(opening.getByLabel('Caixa')).toHaveValue('50000000-0000-0000-0000-000000000001')
  await opening.getByLabel('Fundo inicial').fill('250,00')
  await opening.getByRole('button', { name: 'Confirmar abertura' }).click()

  await expect(page.locator('.status-pill', { hasText: 'Caixa aberto' })).toBeVisible()
  if (testInfo.project.name === 'chromium') {
    await expect(page.getByRole('button', { name: 'Caixa aberto. Acessar caixa' })).toBeVisible()
  }
  await expect(page.getByText('R$ 250,00', { exact: true }).first()).toBeVisible()
})
