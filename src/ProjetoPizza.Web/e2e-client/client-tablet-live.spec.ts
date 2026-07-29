import { expect, test } from '@playwright/test'

test('ativa o tablet e navega no cardápio usando a API real', async ({ page }) => {
  await page.goto('/mesa')
  await page.getByLabel('Código do tablet').fill('DEV-TABLET-003')
  await page.getByRole('button', { name: 'Ativar tablet' }).click()

  await expect(page.getByRole('heading', { name: /bem-vindo/i })).toBeVisible({ timeout: 15_000 })
  await page.getByRole('button', { name: /ver cardápio/i }).click()

  await expect(page.getByRole('searchbox', { name: 'Buscar no cardápio' })).toBeVisible()
  await expect(page.locator('.client-product-card').first()).toBeVisible()
  const categoryToggle = page.locator('.client-category-toggle')
  const sidebar = page.getByRole('complementary', { name: 'Categorias do cardápio' })

  await expect(categoryToggle).toHaveAccessibleName('Expandir categorias')
  await expect(categoryToggle).toHaveAttribute('aria-expanded', 'false')
  await categoryToggle.click()
  await expect(categoryToggle).toHaveAccessibleName('Recolher categorias')
  await expect(categoryToggle).toHaveAttribute('aria-expanded', 'true')
  await expect(sidebar).toHaveClass(/is-expanded/)
  await page.getByRole('button', { name: /Abrir categoria/ }).first().click()
  await expect(categoryToggle).toHaveAccessibleName('Expandir categorias')
  await expect(categoryToggle).toHaveAttribute('aria-expanded', 'false')
  await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true)

  await page.getByRole('button', { name: /Escolher opções de Pizza Margherita/i }).click()
  await page.getByRole('radio', { name: /Broto/i }).click()
  await page.getByRole('button', { name: /Continuar/i }).click()
  await page.getByRole('radio', { name: /1 sabor/i }).click()
  await page.getByRole('button', { name: /Continuar/i }).click()
  await page.locator('.pizza-flavor-card', { hasText: 'Margherita' }).click()
  await page.getByRole('button', { name: /Continuar/i }).click()

  await expect(page.getByRole('heading', { name: 'Personalize os sabores' })).toBeVisible()
  await expect(page.getByText('Ingredientes adicionais')).toBeVisible()
  await page.getByRole('button', { name: 'Adicionar Bacon' }).click()
  await expect(page.locator('.pizza-extra-row', { hasText: 'Bacon' })).toHaveClass(/selected/)
  await expect(page.locator('.pizza-extra-row', { hasText: 'Bacon' }).getByText('1', { exact: true })).toBeVisible()
  await page.getByRole('button', { name: /Continuar/i }).click()

  await expect(page.getByRole('heading', { name: 'Escolha a borda' })).toBeVisible()
  await page.getByRole('tab', { name: /Duas metades/i }).click()
  const crustHalves = page.locator('.crust-half-panel')
  await crustHalves.nth(0).getByRole('radio', { name: /Catupiry/i }).click()
  await expect(crustHalves.nth(1).getByRole('radio', { name: /Catupiry/i })).toBeDisabled()
  await crustHalves.nth(1).getByRole('radio', { name: /Cheddar/i }).click()
  await page.getByRole('button', { name: /Continuar/i }).click()

  await expect(page.getByText('½ Catupiry + ½ Cheddar')).toBeVisible()
  await page.getByRole('button', { name: /Adicionar ao carrinho/i }).click()
  await expect(page.getByText('Borda: ½ Catupiry + ½ Cheddar')).toBeVisible()
})
