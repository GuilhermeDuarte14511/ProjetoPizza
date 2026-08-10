import { ArrowLeft, LoaderCircle, MapPin, Minus, Plus, ShoppingBag, Trash2, Truck } from 'lucide-react'
import { useEffect, useMemo, useRef, useState } from 'react'
import { MenuView } from '../features/client/MenuView'
import { PizzaBuilder, type PizzaBuilderResult } from '../features/client/PizzaBuilder'
import { getProductImage } from '../features/client/clientPresentation'
import { createUuid } from '../lib/uuid'
import { deliveryService } from '../services/deliveryService'
import type { ClientCartItem, ClientProduct } from '../types/client'
import type { DeliveryCatalog, DeliveryTracking } from '../types/delivery'
import { formatCurrency } from '../utils/money'
import { getUserErrorMessage } from '../utils/errors'

const trackingKey = 'projeto-pizza.delivery-tracking'

export function DeliveryPage() {
  const [catalog, setCatalog] = useState<DeliveryCatalog>()
  const [cart, setCart] = useState<ClientCartItem[]>([])
  const [builderProduct, setBuilderProduct] = useState<ClientProduct>()
  const [screen, setScreen] = useState<'menu' | 'checkout' | 'tracking'>('menu')
  const [tracking, setTracking] = useState<DeliveryTracking>()
  const [trackingToken, setTrackingToken] = useState(() => localStorage.getItem(trackingKey) ?? '')
  const [error, setError] = useState<string>()
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const requestId = useRef(createUuid())

  useEffect(() => {
    deliveryService.catalog().then(setCatalog).catch((reason) => setError(getUserErrorMessage(reason))).finally(() => setLoading(false))
  }, [])

  useEffect(() => {
    if (!trackingToken || screen !== 'tracking') return
    let cancelled = false
    const refresh = () => deliveryService.track(trackingToken).then((value) => !cancelled && setTracking(value)).catch(() => undefined)
    void refresh()
    const timer = window.setInterval(refresh, 15_000)
    return () => { cancelled = true; window.clearInterval(timer) }
  }, [screen, trackingToken])

  const total = useMemo(() => cart.reduce((sum, item) => sum + item.unitPrice * item.quantity, 0) + (catalog?.deliveryFee ?? 0), [cart, catalog])

  function addProduct(product: ClientProduct) {
    setCart((current) => {
      const existing = current.find((item) => item.productId === product.id && !item.pizza)
      return existing
        ? current.map((item) => item.key === existing.key ? { ...item, quantity: item.quantity + 1 } : item)
        : [...current, { key: createUuid(), productId: product.id, name: product.name, quantity: 1, unitPrice: product.price, imageUrl: getProductImage(product) }]
    })
  }

  function addPizza(result: PizzaBuilderResult) {
    setCart((current) => [...current, { key: createUuid(), ...result }])
    setBuilderProduct(undefined)
  }

  function changeQuantity(key: string, delta: number) {
    setCart((current) => current
      .map((item) => item.key === key ? { ...item, quantity: item.quantity + delta } : item)
      .filter((item) => item.quantity > 0))
  }

  async function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!cart.length) return
    const form = new FormData(event.currentTarget)
    setSubmitting(true)
    setError(undefined)
    try {
      const placed = await deliveryService.placeOrder({
        requestId: requestId.current,
        customerName: String(form.get('customerName') ?? ''),
        phone: String(form.get('phone') ?? ''),
        birthDate: String(form.get('birthDate') ?? ''),
        address: String(form.get('address') ?? ''),
        notes: String(form.get('notes') ?? '') || undefined,
        items: cart.map((item) => ({
          productId: item.productId,
          quantity: item.quantity,
          notes: item.notes,
          pizza: item.pizza ? {
            sizeId: item.pizza.sizeId,
            flavorIds: item.pizza.flavorIds,
            crustId: item.pizza.crustId,
            secondCrustId: item.pizza.secondCrustId,
            removedIngredientIds: item.pizza.removedIngredientIds,
            extraIngredients: item.pizza.extraIngredients.map((extra) => ({
              ingredientId: extra.ingredientId,
              pizzaFlavorId: extra.pizzaFlavorId,
              quantity: extra.quantity,
            })),
          } : undefined,
        })),
      })
      localStorage.setItem(trackingKey, placed.trackingToken)
      setTrackingToken(placed.trackingToken)
      setScreen('tracking')
      setCart([])
    } catch (reason) {
      setError(getUserErrorMessage(reason))
    } finally {
      setSubmitting(false)
    }
  }

  if (loading) return <main className="delivery-loading"><LoaderCircle className="spin" /><p>Carregando cardápio…</p></main>
  if (!catalog) return <main className="delivery-loading"><p>{error ?? 'Cardápio indisponível.'}</p><button onClick={() => window.location.reload()}>Tentar novamente</button></main>

  if (screen === 'tracking') return (
    <main className="delivery-public-page"><header className="delivery-public-header"><strong>Forno 27</strong><button onClick={() => setScreen('menu')}>Novo pedido</button></header>
      <section className="delivery-tracking-card"><Truck /><span className="eyebrow">Pedido #{tracking?.number ?? '…'}</span><h1>{deliveryStatusText(tracking?.deliveryStatus)}</h1><p>{tracking?.driverName ? `${tracking.driverName} está responsável pela entrega.` : 'Acompanhe aqui; esta tela atualiza automaticamente.'}</p>
        <ol className="delivery-timeline">{['AwaitingPreparation', 'ReadyForDispatch', 'Dispatched', 'Delivered'].map((status) => <li className={isReached(tracking?.deliveryStatus, status) ? 'done' : ''} key={status}>{deliveryStatusText(status)}</li>)}</ol>
        {tracking && <div className="delivery-tracking-summary"><span><MapPin /> {tracking.address}</span><strong>{formatCurrency(tracking.total)}</strong></div>}
      </section>
    </main>
  )

  return (
    <main className="delivery-public-page">
      <header className="delivery-public-header"><strong>Forno 27 · Delivery</strong><button onClick={() => setScreen(screen === 'checkout' ? 'menu' : 'checkout')}><ShoppingBag /> {cart.length} · {formatCurrency(total)}</button></header>
      {error && <div className="delivery-error" role="alert">{error}</div>}
      {screen === 'menu' ? <MenuView products={catalog.catalog.products} categoryName="Cardápio para entrega" isFeatured onAddProduct={addProduct} onBuildPizza={setBuilderProduct} /> : (
        <section className="delivery-checkout"><button className="secondary-button" onClick={() => setScreen('menu')}><ArrowLeft /> Continuar escolhendo</button><h1>Finalizar delivery</h1>
          <div className="delivery-cart-list">{cart.map((item) => <article key={item.key}><img src={item.imageUrl} alt="" /><div><strong>{item.name}</strong><small>{item.pizza?.flavorNames.join(' / ')}</small></div><button onClick={() => changeQuantity(item.key, -1)}><Minus /></button><span>{item.quantity}</span><button onClick={() => changeQuantity(item.key, 1)}><Plus /></button><strong>{formatCurrency(item.unitPrice * item.quantity)}</strong><button aria-label={`Remover ${item.name}`} onClick={() => changeQuantity(item.key, -item.quantity)}><Trash2 /></button></article>)}</div>
          <div className="delivery-total"><span>Taxa de entrega</span><strong>{formatCurrency(catalog.deliveryFee)}</strong><span>Total</span><strong>{formatCurrency(total)}</strong></div>
          <form className="delivery-form" onSubmit={submit}><label>Nome<input name="customerName" required maxLength={120} /></label><label>Telefone<input name="phone" required inputMode="tel" /></label><label>Data de nascimento<input name="birthDate" required type="date" /></label><label className="wide">Endereço completo<textarea name="address" required maxLength={500} rows={3} /></label><label className="wide">Observações<textarea name="notes" maxLength={1000} rows={2} /></label><button className="primary-button wide" disabled={!cart.length || submitting}>{submitting ? 'Enviando…' : `Confirmar pedido · ${formatCurrency(total)}`}</button></form>
        </section>
      )}
      {builderProduct && <PizzaBuilder product={builderProduct} catalog={catalog.catalog.pizza} onCancel={() => setBuilderProduct(undefined)} onAdd={addPizza} />}
    </main>
  )
}

const statuses = ['AwaitingPreparation', 'ReadyForDispatch', 'Dispatched', 'Delivered']
function isReached(current = '', candidate: string) { return statuses.indexOf(candidate) <= statuses.indexOf(current) }
function deliveryStatusText(status?: string) {
  return ({ AwaitingPreparation: 'Pedido recebido', ReadyForDispatch: 'Pronto para sair', Dispatched: 'Saiu para entrega', Delivered: 'Pedido entregue', Failed: 'Problema na entrega', Cancelled: 'Pedido cancelado' } as Record<string, string>)[status ?? ''] ?? 'Carregando andamento…'
}
