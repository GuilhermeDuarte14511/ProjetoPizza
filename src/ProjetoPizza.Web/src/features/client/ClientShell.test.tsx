import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { ClientShell } from './ClientShell'

const session = {
  deviceId: 'device-1',
  tableSessionId: 'session-1',
  restaurantName: 'Forno 27',
  tableNumber: 2,
  tableName: 'Mesa 2',
  guestCount: 2,
  status: 'Open',
  clearTabletAfterTableClose: true,
}

describe('ClientShell', () => {
  it('expande, seleciona e recolhe o menu lateral de categorias', async () => {
    const user = userEvent.setup()
    const onNavigate = vi.fn()
    const onCategoryChange = vi.fn()

    render(
      <ClientShell
        session={session}
        categories={[{ id: 'pizza', name: 'Pizzas', slug: 'pizzas', displayOrder: 1 }]}
        activeCategoryId="featured"
        screen="menu"
        search=""
        cartCount={0}
        cartTotal={0}
        onSearchChange={() => undefined}
        onCategoryChange={onCategoryChange}
        onNavigate={onNavigate}
      >
        <p>Cardápio</p>
      </ClientShell>,
    )

    const toggle = screen.getByRole('button', { name: 'Expandir categorias' })
    const sidebar = screen.getByRole('complementary', { name: 'Categorias do cardápio' })

    expect(toggle).toHaveAttribute('aria-expanded', 'false')
    await user.click(toggle)
    expect(toggle).toHaveAttribute('aria-expanded', 'true')
    expect(sidebar).toHaveClass('is-expanded')

    await user.click(screen.getByRole('button', { name: 'Abrir categoria Pizzas' }))

    expect(onNavigate).toHaveBeenCalledWith('menu')
    expect(onCategoryChange).toHaveBeenCalledWith('pizza')
    expect(sidebar).not.toHaveClass('is-expanded')
  })

  it('recolhe o menu lateral com Escape', async () => {
    const user = userEvent.setup()

    render(
      <ClientShell
        session={session}
        categories={[]}
        activeCategoryId="featured"
        screen="menu"
        search=""
        cartCount={0}
        cartTotal={0}
        onSearchChange={() => undefined}
        onCategoryChange={() => undefined}
        onNavigate={() => undefined}
      >
        <p>Cardápio</p>
      </ClientShell>,
    )

    await user.click(screen.getByRole('button', { name: 'Expandir categorias' }))
    await user.keyboard('{Escape}')

    expect(screen.getByRole('button', { name: 'Expandir categorias' })).toHaveAttribute('aria-expanded', 'false')
  })
})
