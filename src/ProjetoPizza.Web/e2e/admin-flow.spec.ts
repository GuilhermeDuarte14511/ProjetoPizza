import { expect, test } from '@playwright/test'
import { stat } from 'node:fs/promises'

test('abre produto em modal e valida os campos em português', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('Usuário ou e-mail', { exact: true }).fill('admin@local.test')
  await page.getByLabel('Senha', { exact: true }).fill('senha-local')
  await page.getByRole('button', { name: 'Entrar no sistema' }).click()

  await page.goto('/admin/catalog/products')
  await page.getByRole('button', { name: 'Adicionar produto' }).click()
  await expect(page.getByRole('dialog', { name: 'Novo produto' })).toBeVisible()
  await page.getByRole('button', { name: 'Salvar produto' }).click()
  await expect(page.getByText('O nome é obrigatório.')).toBeVisible()
})

test('mascara valores e divide a conta por forma de pagamento individual', async ({ page }) => {
  await page.goto('/login')
  await page.getByLabel('Usuário ou e-mail', { exact: true }).fill('admin@local.test')
  await page.getByLabel('Senha', { exact: true }).fill('senha-local')
  await page.getByRole('button', { name: 'Entrar no sistema' }).click()

  await page.goto('/admin/tables/40000000-0000-0000-0000-000000000012')
  await page.getByRole('button', { name: 'Registrar pagamento' }).click()

  const dialog = page.getByRole('dialog', { name: 'Registrar pagamento' })
  await expect(dialog.getByLabel('Valor do pagamento')).toHaveValue('R$ 138,50')
  await dialog.getByRole('tab', { name: 'Dividir por pessoas' }).click()
  await dialog.getByLabel('Quantidade de pessoas').fill('3')

  const people = dialog.locator('.split-payment-card')
  await expect(people).toHaveCount(3)
  await expect(people.nth(0)).toContainText('R$ 46,17')
  await expect(people.nth(1)).toContainText('R$ 46,17')
  await expect(people.nth(2)).toContainText('R$ 46,16')
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
