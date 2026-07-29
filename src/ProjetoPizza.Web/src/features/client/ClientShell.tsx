import {
  BellRing,
  CakeSlice,
  ChevronRight,
  CircleUserRound,
  CupSoda,
  IceCreamBowl,
  PanelLeftClose,
  PanelLeftOpen,
  Pizza,
  ReceiptText,
  Search,
  ShoppingCart,
  Sparkles,
  Star,
  Utensils,
} from 'lucide-react'
import { useEffect, useState, type ReactNode } from 'react'
import type { ClientCategory, ClientSession } from '../../types/client'
import { formatCurrency } from '../../utils/money'

type ClientScreen = 'menu' | 'cart' | 'orders' | 'service' | 'bill'

interface ClientShellProps {
  session: ClientSession
  categories: ClientCategory[]
  activeCategoryId: string
  screen: ClientScreen
  search: string
  cartCount: number
  cartTotal: number
  onSearchChange: (value: string) => void
  onCategoryChange: (id: string) => void
  onNavigate: (screen: ClientScreen) => void
  children: ReactNode
}

const categoryIcons = [Pizza, Sparkles, CakeSlice, Utensils, CupSoda, IceCreamBowl]

export function ClientShell({
  session,
  categories,
  activeCategoryId,
  screen,
  search,
  cartCount,
  cartTotal,
  onSearchChange,
  onCategoryChange,
  onNavigate,
  children,
}: ClientShellProps) {
  const [isCategoryMenuExpanded, setCategoryMenuExpanded] = useState(false)

  useEffect(() => {
    if (!isCategoryMenuExpanded) return

    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setCategoryMenuExpanded(false)
    }

    document.addEventListener('keydown', closeOnEscape)
    return () => document.removeEventListener('keydown', closeOnEscape)
  }, [isCategoryMenuExpanded])

  function selectCategory(categoryId: string) {
    onNavigate('menu')
    onCategoryChange(categoryId)
    setCategoryMenuExpanded(false)
  }

  return (
    <div className="client-app">
      <a className="skip-link" href="#client-main">Ir para o conteúdo</a>
      <header className="client-topbar">
        <button
          type="button"
          className="client-category-toggle"
          aria-controls="client-category-menu"
          aria-expanded={isCategoryMenuExpanded}
          aria-label={isCategoryMenuExpanded ? 'Recolher categorias' : 'Expandir categorias'}
          onClick={() => setCategoryMenuExpanded((current) => !current)}
        >
          {isCategoryMenuExpanded
            ? <PanelLeftClose aria-hidden="true" />
            : <PanelLeftOpen aria-hidden="true" />}
          <span>Categorias</span>
        </button>
        <button className="client-wordmark" type="button" onClick={() => onNavigate('menu')} aria-label="Ir para o cardápio">
          <Pizza aria-hidden="true" />
          <span>Forno 27</span>
        </button>
        <span className="client-table-chip">Mesa {session.tableNumber}</span>
        <nav className="client-quick-nav" aria-label="Ações da mesa">
          <button type="button" aria-pressed={screen === 'orders'} className={screen === 'orders' ? 'active' : ''} onClick={() => onNavigate('orders')}>
            <ReceiptText aria-hidden="true" />
            Meus pedidos
          </button>
          <button type="button" aria-pressed={screen === 'service'} className={screen === 'service' ? 'active' : ''} onClick={() => onNavigate('service')}>
            <BellRing aria-hidden="true" />
            Chamar garçom
          </button>
          <button type="button" aria-pressed={screen === 'bill'} className={screen === 'bill' ? 'active' : ''} onClick={() => onNavigate('bill')}>
            <CircleUserRound aria-hidden="true" />
            Pedir conta
          </button>
        </nav>
        <label className="client-search">
          <Search aria-hidden="true" />
          <span className="sr-only">Buscar no cardápio</span>
          <input
            type="search"
            value={search}
            onChange={(event) => onSearchChange(event.target.value)}
            placeholder="Buscar no cardápio..."
          />
        </label>
        <button
          type="button"
          aria-pressed={screen === 'cart'}
          className={`client-cart-button ${screen === 'cart' ? 'active' : ''}`}
          onClick={() => onNavigate('cart')}
          aria-label={`Abrir carrinho com ${cartCount} itens, total ${formatCurrency(cartTotal)}`}
        >
          <ShoppingCart aria-hidden="true" />
          <span>Carrinho · {cartCount} {cartCount === 1 ? 'item' : 'itens'} · {formatCurrency(cartTotal)}</span>
        </button>
      </header>

      <aside
        id="client-category-menu"
        className={`client-sidebar ${isCategoryMenuExpanded ? 'is-expanded' : ''}`}
        aria-label="Categorias do cardápio"
      >
        <div>
          <strong>Categorias</strong>
          <span>Mesa {session.tableNumber}</span>
        </div>
        <button
          type="button"
          aria-label="Abrir destaques"
          aria-pressed={screen === 'menu' && activeCategoryId === 'featured'}
          title="Destaques"
          className={screen === 'menu' && activeCategoryId === 'featured' ? 'active' : ''}
          onClick={() => selectCategory('featured')}
        >
          <Star aria-hidden="true" />
          <span>Destaques</span>
          <ChevronRight aria-hidden="true" />
        </button>
        {categories.map((category, index) => {
          const Icon = categoryIcons[index % categoryIcons.length]
          return (
            <button
              type="button"
              aria-label={`Abrir categoria ${category.name}`}
              aria-pressed={screen === 'menu' && activeCategoryId === category.id}
              title={category.name}
              key={category.id}
              className={screen === 'menu' && activeCategoryId === category.id ? 'active' : ''}
              onClick={() => selectCategory(category.id)}
            >
              <Icon aria-hidden="true" />
              <span>{category.name}</span>
              <ChevronRight aria-hidden="true" />
            </button>
          )
        })}
      </aside>
      {isCategoryMenuExpanded && (
        <button
          type="button"
          className="client-sidebar-scrim"
          aria-label="Fechar menu de categorias"
          onClick={() => setCategoryMenuExpanded(false)}
        />
      )}

      <main id="client-main" className="client-main" tabIndex={-1}>
        <div className="client-view-transition" key={screen}>{children}</div>
      </main>
      <footer className="client-footer">
        <span>© 2026 Forno 27 Pizzeria · Mesa {session.tableNumber}</span>
        <span>Atendimento digital com acompanhamento da equipe</span>
      </footer>
    </div>
  )
}
