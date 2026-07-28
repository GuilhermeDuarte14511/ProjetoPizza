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
