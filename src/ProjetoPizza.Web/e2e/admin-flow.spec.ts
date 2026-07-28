import { expect, test } from '@playwright/test'

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
