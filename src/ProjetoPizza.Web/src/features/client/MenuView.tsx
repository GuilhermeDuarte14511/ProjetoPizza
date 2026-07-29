import { Clock3, Flame, Plus, SearchX, Sparkles } from 'lucide-react'
import type { ClientProduct } from '../../types/client'
import { formatCurrency } from '../../utils/money'
import { getProductImage } from './clientPresentation'

interface MenuViewProps {
  products: ClientProduct[]
  categoryName: string
  isFeatured: boolean
  onAddProduct: (product: ClientProduct) => void
  onBuildPizza: (product: ClientProduct) => void
}

export function MenuView({
  products,
  categoryName,
  isFeatured,
  onAddProduct,
  onBuildPizza,
}: MenuViewProps) {
  const pizzaProducts = products.filter((product) => product.productType === 'Pizza')

  return (
    <section className="client-menu-view" aria-labelledby="menu-title">
      {isFeatured && pizzaProducts.length > 0 && (
        <article className="client-menu-hero">
          <div>
            <span className="client-eyebrow"><Flame aria-hidden="true" /> Experiência Forno 27</span>
            <h1 id="menu-title">Monte sua pizza do seu jeito.</h1>
            <p>Escolha o tamanho, até três sabores, personalize ingredientes e finalize com sua borda favorita.</p>
            <button type="button" className="client-primary-action" onClick={() => onBuildPizza(pizzaProducts[0])}>
              <Plus aria-hidden="true" />
              Montar minha pizza
            </button>
          </div>
        </article>
      )}

      <header className="client-section-heading">
        <div>
          <span className="client-eyebrow"><Sparkles aria-hidden="true" /> {isFeatured ? 'Seleção da casa' : 'Cardápio'}</span>
          <h1 id={isFeatured ? undefined : 'menu-title'}>{categoryName}</h1>
          <p>{products.length} {products.length === 1 ? 'opção disponível' : 'opções disponíveis'} para sua mesa.</p>
        </div>
      </header>

      {products.length === 0 ? (
        <div className="client-empty-state">
          <SearchX aria-hidden="true" />
          <h2>Nenhum item encontrado</h2>
          <p>Tente outra categoria ou altere os termos da busca.</p>
        </div>
      ) : (
        <div className="client-product-grid">
          {products.map((product) => {
            const isPizza = product.productType === 'Pizza'
            return (
              <article className="client-product-card" key={product.id}>
                <div className="client-product-image">
                  <img src={getProductImage(product)} alt="" loading="lazy" />
                  {product.isPopular && <span className="client-product-badge">Mais pedido</span>}
                </div>
                <div className="client-product-content">
                  <h2>{product.name}</h2>
                  <p>{product.description || defaultDescription(product)}</p>
                  {product.preparationTimeMinutes > 0 && (
                    <small><Clock3 aria-hidden="true" /> Aproximadamente {product.preparationTimeMinutes} min</small>
                  )}
                  <div>
                    <strong>{isPizza ? 'A partir de ' : ''}{formatCurrency(product.price)}</strong>
                    <button
                      type="button"
                      onClick={() => isPizza ? onBuildPizza(product) : onAddProduct(product)}
                      aria-label={isPizza ? `Escolher opções de ${product.name}` : `Adicionar ${product.name} ao carrinho`}
                    >
                      {isPizza ? 'Ver opções' : <><Plus aria-hidden="true" /> Adicionar</>}
                    </button>
                  </div>
                </div>
              </article>
            )
          })}
        </div>
      )}
    </section>
  )
}

function defaultDescription(product: ClientProduct) {
  if (product.productType === 'Pizza') return 'Massa artesanal, ingredientes selecionados e preparo no forno.'
  if (product.productType === 'Beverage') return 'Servido gelado para acompanhar seu pedido.'
  if (product.productType === 'Dessert') return 'Uma finalização especial para sua experiência.'
  return 'Preparado com ingredientes selecionados pela nossa cozinha.'
}
