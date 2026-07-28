import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { axe } from 'vitest-axe'
import { Modal } from './Modal'

describe('Modal', () => {
  it('expõe título e descrição e permite fechar por teclado', async () => {
    const user = userEvent.setup()
    const onClose = vi.fn()
    render(
      <Modal open title="Novo produto" description="Dados do produto" onClose={onClose}>
        <button>Salvar</button>
      </Modal>,
    )

    expect(screen.getByRole('dialog', { name: 'Novo produto' })).toHaveAccessibleDescription('Dados do produto')
    await user.keyboard('{Escape}')
    expect(onClose).toHaveBeenCalledOnce()
  })

  it('não possui violações básicas de acessibilidade', async () => {
    render(
      <Modal open title="Editar produto" description="Dados comerciais" onClose={() => undefined}>
        <label htmlFor="name">Nome</label>
        <input id="name" />
        <button>Salvar</button>
      </Modal>,
    )

    expect((await axe(document.body)).violations).toEqual([])
  })
})
