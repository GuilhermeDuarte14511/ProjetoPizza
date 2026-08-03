import { LoaderCircle, Pizza } from 'lucide-react'
import { useEffect, useMemo, useRef, useState } from 'react'
import { ClientShell } from '../features/client/ClientShell'
import {
  ActivationView,
  BillView,
  CartView,
  OrderSentView,
  OrdersView,
  ServiceCallView,
  StandbyView,
  ThankYouView,
  WelcomeView,
} from '../features/client/ClientViews'
import { MenuView } from '../features/client/MenuView'
import { PizzaBuilder, type PizzaBuilderResult } from '../features/client/PizzaBuilder'
import { getProductImage } from '../features/client/clientPresentation'
import { clearClientCart, loadClientCart, saveClientCart } from '../features/client/clientCartStorage'
import {
  activateClientSession,
  activateClientProvisioning,
  clearClientSessionToken,
  completeClientTableSession,
  createClientServiceCall,
  getClientBootstrap,
  getClientState,
  getClientSessionToken,
  logoutClientTablet,
  requestClientBill,
  startClientTableSession,
  submitClientOrder,
} from '../services/clientService'
import { ApiError } from '../api/httpClient'
import type {
  ClientBootstrap,
  ClientCartItem,
  ClientProduct,
  SubmitClientOrder,
} from '../types/client'
import { getUserErrorMessage } from '../utils/errors'
import { useToast } from '../components/ui/toast'

type ClientScreen = 'welcome' | 'menu' | 'cart' | 'orders' | 'service' | 'bill' | 'orderSent'

export function ClientTabletPage() {
  const toast = useToast()
  const provisioningToken = new URLSearchParams(window.location.hash.replace(/^#/, '')).get('provisioningToken')
  const provisioningAttempted = useRef(false)
  const [bootstrap, setBootstrap] = useState<ClientBootstrap>()
  const [isLoading, setIsLoading] = useState(Boolean(getClientSessionToken() || provisioningToken))
  const [activationError, setActivationError] = useState<string>()
  const [startError, setStartError] = useState<string>()
  const [screen, setScreen] = useState<ClientScreen>('welcome')
  const [activeCategoryId, setActiveCategoryId] = useState('featured')
  const [search, setSearch] = useState('')
  const [builderProduct, setBuilderProduct] = useState<ClientProduct>()
  const [cart, setCart] = useState<ClientCartItem[]>([])
  const [isMutating, setIsMutating] = useState(false)
  const [lastOrderNumber, setLastOrderNumber] = useState(0)

  useEffect(() => {
    const previousTitle = document.title
    document.title = 'Forno 27 | Cardápio da mesa'
    return () => {
      document.title = previousTitle
    }
  }, [])

  useEffect(() => {
    if (!provisioningToken || getClientSessionToken() || provisioningAttempted.current) return
    provisioningAttempted.current = true
    setActivationError(undefined)

    activateClientProvisioning(provisioningToken)
      .then((data) => {
        setBootstrap(data)
        setCart([])
        if (data.session.tableSessionId) clearClientCart(data.session.tableSessionId)
        setScreen('welcome')
        const cleanUrl = new URL(window.location.href)
        cleanUrl.hash = ''
        window.history.replaceState({}, '', `${cleanUrl.pathname}${cleanUrl.search}${cleanUrl.hash}`)
        toast.success('Tablet vinculado', `Mesa ${data.session.tableNumber} conectada com sucesso.`)
      })
      .catch((error) => {
        setActivationError(getUserErrorMessage(error))
      })
      .finally(() => setIsLoading(false))
  }, [provisioningToken, toast])

  useEffect(() => {
    const token = getClientSessionToken()
    if (!token) return

    const controller = new AbortController()
    getClientBootstrap(controller.signal)
      .then((data) => {
        setBootstrap(data)
        setCart(data.session.tableSessionId ? loadClientCart(data.session.tableSessionId) : [])
      })
      .catch((error) => {
        clearClientSessionToken()
        setActivationError(getUserErrorMessage(error))
      })
      .finally(() => setIsLoading(false))
    return () => controller.abort()
  }, [])

  const activeTableSessionId = bootstrap?.session.tableSessionId
  const activeDeviceId = bootstrap?.session.deviceId
  useEffect(() => {
    if (!activeTableSessionId) return
    saveClientCart(activeTableSessionId, cart)
  }, [activeTableSessionId, cart])

  useEffect(() => {
    if (!activeDeviceId || !getClientSessionToken()) return

    const controller = new AbortController()
    let timeout: number | undefined

    const poll = async () => {
      if (document.visibilityState === 'visible') {
        try {
          const state = await getClientState(controller.signal)
          setBootstrap((current) => current ? { ...current, ...state } : current)
          if (state.session.tableSessionId !== activeTableSessionId) {
            setCart(state.session.tableSessionId ? loadClientCart(state.session.tableSessionId) : [])
            setScreen('welcome')
          }
          if (state.session.status === 'Closed' && state.session.clearTabletAfterTableClose) {
            setCart([])
            if (state.session.tableSessionId) clearClientCart(state.session.tableSessionId)
          }
        } catch (error) {
          if (controller.signal.aborted) return
          if (error instanceof ApiError && error.status === 401) {
            clearClientSessionToken()
            setBootstrap(undefined)
            setCart([])
            setScreen('welcome')
            setActivationError('O acesso deste tablet foi revogado ou encerrado. Faça uma nova ativação para continuar.')
            toast.error('Tablet desconectado', 'Ative novamente o dispositivo para continuar.')
            return
          }
        }
      }
      timeout = window.setTimeout(poll, 8_000)
    }

    timeout = window.setTimeout(poll, 8_000)
    return () => {
      if (timeout) window.clearTimeout(timeout)
      controller.abort()
    }
  }, [activeDeviceId, activeTableSessionId, toast])

  const cartCount = cart.reduce((total, item) => total + item.quantity, 0)
  const cartTotal = cart.reduce((total, item) => total + item.quantity * item.unitPrice, 0)
  const existingConsumption = bootstrap?.orders
    .filter((order) => order.status !== 'Cancelled')
    .reduce((total, order) => total + order.total, 0) ?? 0
  const visibleProducts = useMemo(() => {
    if (!bootstrap) return []
    const normalizedSearch = search.trim().toLocaleLowerCase('pt-BR')
    return bootstrap.catalog.products.filter((product) => {
      const matchesCategory = activeCategoryId === 'featured'
        ? product.isFeatured || product.isPopular || product.productType === 'Pizza'
        : product.categoryId === activeCategoryId
      const matchesSearch = !normalizedSearch ||
        product.name.toLocaleLowerCase('pt-BR').includes(normalizedSearch) ||
        product.description?.toLocaleLowerCase('pt-BR').includes(normalizedSearch)
      return matchesCategory && matchesSearch
    })
  }, [activeCategoryId, bootstrap, search])
  const categoryName = activeCategoryId === 'featured'
    ? 'Destaques da casa'
    : bootstrap?.catalog.categories.find((category) => category.id === activeCategoryId)?.name ?? 'Cardápio'
  const canSubmitOrders = bootstrap?.session.status === 'Open'
  const orderBlockedMessage = bootstrap?.session.status === 'BillRequested'
    ? 'A conta desta mesa já foi solicitada. Fale com a equipe para incluir novos itens.'
    : bootstrap?.session.status === 'PaymentPending'
      ? 'O pagamento desta mesa já está em andamento.'
      : bootstrap?.session.status === 'Open'
        ? undefined
        : 'Este atendimento não aceita novos pedidos no momento.'

  async function activate(deviceCode: string) {
    setIsMutating(true)
    setActivationError(undefined)
    try {
      const data = await activateClientSession(deviceCode)
      setBootstrap(data)
      setCart([])
      if (data.session.tableSessionId) clearClientCart(data.session.tableSessionId)
      setScreen('welcome')
      toast.success('Tablet ativado', `Mesa ${data.session.tableNumber} conectada com sucesso.`)
    } catch (error) {
      setActivationError(getUserErrorMessage(error))
    } finally {
      setIsMutating(false)
    }
  }

  async function startTableSession(guestCount: number) {
    setIsMutating(true)
    setStartError(undefined)
    try {
      const data = await startClientTableSession({ guestCount })
      setBootstrap(data)
      setCart([])
      if (data.session.tableSessionId) clearClientCart(data.session.tableSessionId)
      setScreen('welcome')
      toast.success('Comanda iniciada', `Atendimento aberto para ${guestCount} pessoa(s).`)
    } catch (error) {
      setStartError(getUserErrorMessage(error))
    } finally {
      setIsMutating(false)
    }
  }

  async function finishTableSession() {
    if (!bootstrap?.session.tableSessionId || isMutating) return
    setIsMutating(true)
    const completedTableSessionId = bootstrap.session.tableSessionId
    try {
      const data = await completeClientTableSession()
      clearClientCart(completedTableSessionId)
      setCart([])
      setBootstrap(data)
      setScreen('welcome')
    } catch (error) {
      toast.error('Não foi possível preparar a mesa', getUserErrorMessage(error))
    } finally {
      setIsMutating(false)
    }
  }

  async function logoutTablet() {
    if (!window.confirm('Desvincular este tablet? Será necessário ativá-lo novamente pelo painel administrativo.')) return
    setIsMutating(true)
    try {
      await logoutClientTablet()
      setBootstrap(undefined)
      setCart([])
      setScreen('welcome')
      setActivationError('Tablet desvinculado com segurança.')
    } catch (error) {
      toast.error('Não foi possível desvincular', getUserErrorMessage(error))
    } finally {
      setIsMutating(false)
    }
  }

  function addStandardProduct(product: ClientProduct) {
    setCart((current) => {
      const existing = current.find((item) => item.productId === product.id && !item.pizza)
      if (existing) {
        return current.map((item) => item.key === existing.key
          ? { ...item, quantity: Math.min(20, item.quantity + 1) }
          : item)
      }
      return [...current, {
        key: crypto.randomUUID(),
        productId: product.id,
        name: product.name,
        quantity: 1,
        unitPrice: product.price,
        imageUrl: getProductImage(product),
      }]
    })
    toast.success('Item adicionado', `${product.name} foi adicionado ao carrinho.`)
  }

  function addPizza(result: PizzaBuilderResult) {
    setCart((current) => [...current, { key: crypto.randomUUID(), ...result }])
    setBuilderProduct(undefined)
    setScreen('cart')
    toast.success('Pizza adicionada', 'Sua montagem está pronta no carrinho.')
  }

  function changeQuantity(key: string, quantity: number) {
    if (quantity <= 0) {
      removeCartItem(key)
      return
    }
    setCart((current) => current.map((item) => item.key === key
      ? { ...item, quantity: Math.min(20, quantity) }
      : item))
  }

  function removeCartItem(key: string) {
    setCart((current) => current.filter((item) => item.key !== key))
    toast.info('Item removido', 'O carrinho foi atualizado.')
  }

  async function submitOrder() {
    if (!cart.length) return
    setIsMutating(true)
    const payload: SubmitClientOrder = {
      requestId: crypto.randomUUID(),
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
          extraIngredients: (item.pizza.extraIngredients ?? []).map((extra) => ({
            ingredientId: extra.ingredientId,
            pizzaFlavorId: extra.pizzaFlavorId,
            quantity: extra.quantity,
          })),
        } : undefined,
      })),
    }
    try {
      const order = await submitClientOrder(payload)
      setCart([])
      setLastOrderNumber(order.number)
      const fresh = await getClientBootstrap()
      setBootstrap(fresh)
      setScreen('orderSent')
      toast.success('Pedido enviado', `Pedido #${order.number} recebido pela cozinha.`)
    } catch (error) {
      toast.error('Não foi possível enviar', getUserErrorMessage(error))
    } finally {
      setIsMutating(false)
    }
  }

  async function sendServiceCall(typeId: string, details?: string) {
    setIsMutating(true)
    try {
      await createClientServiceCall(typeId, details)
      toast.success('Solicitação enviada', 'A equipe foi avisada e irá até sua mesa.')
      setScreen('menu')
    } catch (error) {
      toast.error('Não foi possível chamar a equipe', getUserErrorMessage(error))
    } finally {
      setIsMutating(false)
    }
  }

  async function requestBill(splitCount?: number) {
    setIsMutating(true)
    try {
      const bill = await requestClientBill(splitCount)
      setBootstrap((current) => current ? { ...current, bill, session: { ...current.session, status: 'BillRequested' } } : current)
      toast.success('Conta solicitada', 'A equipe foi avisada e irá finalizar o atendimento.')
    } catch (error) {
      toast.error('Não foi possível solicitar a conta', getUserErrorMessage(error))
    } finally {
      setIsMutating(false)
    }
  }

  if (isLoading) {
    return (
      <main className="client-loading">
        <span><Pizza aria-hidden="true" /></span>
        <LoaderCircle className="spin" aria-hidden="true" />
        <h1>Preparando sua mesa...</h1>
      </main>
    )
  }

  if (!bootstrap) {
    return <ActivationView isSubmitting={isMutating} error={activationError} onActivate={activate} />
  }

  if (bootstrap.session.status === 'Idle') {
    return (
      <StandbyView
        session={bootstrap.session}
        isSubmitting={isMutating}
        error={startError}
        onStart={(guestCount) => void startTableSession(guestCount)}
        onLogout={() => void logoutTablet()}
      />
    )
  }

  if (bootstrap.session.status === 'Closed' && bootstrap.bill.status === 'Paid') {
    return <ThankYouView isFinishing={isMutating} onFinish={() => void finishTableSession()} />
  }

  if (screen === 'welcome') {
    return (
      <WelcomeView
        session={bootstrap.session}
        onMenu={() => setScreen('menu')}
        onHelp={() => {
          toast.info('Como funciona', 'Escolha no cardápio, confirme o carrinho e acompanhe o preparo em “Meus pedidos”.')
          setScreen('menu')
        }}
      />
    )
  }

  return (
    <>
      <ClientShell
        session={bootstrap.session}
        categories={bootstrap.catalog.categories}
        activeCategoryId={activeCategoryId}
        screen={screen === 'orderSent' ? 'orders' : screen}
        search={search}
        cartCount={cartCount}
        cartTotal={cartTotal}
        onSearchChange={(value) => {
          setSearch(value)
          setScreen('menu')
        }}
        onCategoryChange={setActiveCategoryId}
        onNavigate={setScreen}
      >
        {screen === 'menu' && (
          <MenuView
            products={visibleProducts}
            categoryName={categoryName}
            isFeatured={activeCategoryId === 'featured'}
            onAddProduct={addStandardProduct}
            onBuildPizza={setBuilderProduct}
          />
        )}
        {screen === 'cart' && (
          <CartView
            items={cart}
            existingConsumption={existingConsumption}
            serviceFeePercentage={bootstrap.catalog.serviceFeePercentage}
            canSubmit={canSubmitOrders}
            blockedMessage={orderBlockedMessage}
            isSubmitting={isMutating}
            onChangeQuantity={changeQuantity}
            onRemove={removeCartItem}
            onContinue={() => setScreen('menu')}
            onSubmit={submitOrder}
          />
        )}
        {screen === 'orders' && (
          <OrdersView orders={bootstrap.orders} onMenu={() => setScreen('menu')} onBill={() => setScreen('bill')} />
        )}
        {screen === 'service' && (
          <ServiceCallView types={bootstrap.serviceCallTypes} isSubmitting={isMutating} onSubmit={sendServiceCall} />
        )}
        {screen === 'bill' && (
          <BillView bill={bootstrap.bill} guestCount={bootstrap.session.guestCount} isSubmitting={isMutating} onRequest={requestBill} />
        )}
        {screen === 'orderSent' && (
          <OrderSentView orderNumber={lastOrderNumber} onOrders={() => setScreen('orders')} onMenu={() => setScreen('menu')} />
        )}
      </ClientShell>
      {builderProduct && (
        <PizzaBuilder
          key={builderProduct.id}
          product={builderProduct}
          catalog={bootstrap.catalog.pizza}
          onCancel={() => setBuilderProduct(undefined)}
          onAdd={addPizza}
        />
      )}
    </>
  )
}
