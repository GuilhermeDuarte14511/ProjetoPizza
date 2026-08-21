import { LoaderCircle, Pizza } from 'lucide-react'
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
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
import {
  clearClientCart,
  clearClientOrderDraft,
  loadClientCart,
  loadClientOrderDraft,
  saveClientCart,
  saveClientOrderDraft,
} from '../features/client/clientCartStorage'
import { createClientTelemetry, getClientBattery, type ClientBatteryManager } from '../lib/deviceTelemetry'
import { createUuid } from '../lib/uuid'
import {
  activateClientSession,
  activateClientProvisioning,
  cacheClientBootstrap,
  clearClientSessionToken,
  completeClientTableSession,
  createClientServiceCall,
  getClientBootstrap,
  getClientLoyaltyQuote,
  getCachedClientBootstrap,
  getClientState,
  getClientSessionToken,
  logoutClientTablet,
  requestClientBill,
  startClientTableSession,
  submitClientOrder,
  updateClientTelemetry,
} from '../services/clientService'
import { apiBaseUrl, ApiError } from '../api/httpClient'
import type {
  ClientBootstrap,
  ClientCartItem,
  ClientLoyaltyQuote,
  ClientOrder,
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
  const [editingCartItem, setEditingCartItem] = useState<ClientCartItem>()
  const [cart, setCart] = useState<ClientCartItem[]>([])
  const [loyaltyPhone, setLoyaltyPhone] = useState('')
  const [loyaltyBirthDate, setLoyaltyBirthDate] = useState('')
  const [couponCode, setCouponCode] = useState('')
  const [loyaltyPoints, setLoyaltyPoints] = useState(0)
  const [loyaltyQuote, setLoyaltyQuote] = useState<ClientLoyaltyQuote>()
  const [loyaltyQuoteTotal, setLoyaltyQuoteTotal] = useState(0)
  const [isMutating, setIsMutating] = useState(false)
  const [lastOrderNumber, setLastOrderNumber] = useState(0)
  const [isOffline, setIsOffline] = useState(!navigator.onLine)
  const [isRealtimeConnected, setIsRealtimeConnected] = useState(false)
  const [hasPendingOrderRecovery, setHasPendingOrderRecovery] = useState(false)

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
        if (data.session.tableSessionId) {
          clearClientCart(data.session.tableSessionId)
          clearClientOrderDraft(data.session.tableSessionId)
        }
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
        setHasPendingOrderRecovery(Boolean(data.session.tableSessionId && loadClientOrderDraft(data.session.tableSessionId)))
      })
      .catch((error) => {
        if (error instanceof ApiError && error.status === 0) {
          const cached = getCachedClientBootstrap()
          if (cached) {
            setBootstrap(cached)
            setCart(cached.session.tableSessionId ? loadClientCart(cached.session.tableSessionId) : [])
            setHasPendingOrderRecovery(Boolean(cached.session.tableSessionId && loadClientOrderDraft(cached.session.tableSessionId)))
            setIsOffline(true)
            return
          }
        }
        clearClientSessionToken()
        setActivationError(getUserErrorMessage(error))
      })
      .finally(() => setIsLoading(false))
    return () => controller.abort()
  }, [])

  const activeTableSessionId = bootstrap?.session.tableSessionId
  const activeDeviceId = bootstrap?.session.deviceId

  useEffect(() => {
    if (bootstrap) cacheClientBootstrap(bootstrap)
  }, [bootstrap])

  const refreshState = useCallback(async (signal?: AbortSignal) => {
    try {
      const state = await getClientState(signal)
      setBootstrap((current) => current ? { ...current, ...state } : current)
      setIsOffline(false)
      if (state.session.tableSessionId !== activeTableSessionId) {
        setCart(state.session.tableSessionId ? loadClientCart(state.session.tableSessionId) : [])
        setHasPendingOrderRecovery(Boolean(state.session.tableSessionId && loadClientOrderDraft(state.session.tableSessionId)))
        setScreen('welcome')
      }
      if (state.session.status === 'Closed' && state.session.clearTabletAfterTableClose) {
        setCart([])
        if (state.session.tableSessionId) {
          clearClientCart(state.session.tableSessionId)
          clearClientOrderDraft(state.session.tableSessionId)
        }
        setHasPendingOrderRecovery(false)
      }
    } catch (error) {
      if (signal?.aborted) return
      if (error instanceof ApiError && error.status === 401) {
        clearClientSessionToken()
        setBootstrap(undefined)
        setCart([])
        setScreen('welcome')
        setActivationError('O acesso deste tablet foi revogado ou encerrado. Faça uma nova ativação para continuar.')
        toast.error('Tablet desconectado', 'Ative novamente o dispositivo para continuar.')
        return
      }
      setIsOffline(true)
    }
  }, [activeTableSessionId, toast])

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
        await refreshState(controller.signal)
      }
      timeout = window.setTimeout(poll, 60_000)
    }

    timeout = window.setTimeout(poll, 60_000)
    return () => {
      if (timeout) window.clearTimeout(timeout)
      controller.abort()
    }
  }, [activeDeviceId, refreshState])

  useEffect(() => {
    const token = getClientSessionToken()
    if (!activeDeviceId || !token || !apiBaseUrl) return

    const connection = new HubConnectionBuilder()
      .withUrl(`${apiBaseUrl}/hubs/client?device_token=${encodeURIComponent(token)}`)
      .withAutomaticReconnect([0, 2_000, 5_000, 10_000, 30_000])
      .configureLogging(LogLevel.Warning)
      .build()

    connection.on('client:changed', (notification: { resource?: string }) => {
      const catalogResources = new Set(['products', 'categories', 'pizza-flavors', 'pizza-sizes', 'pizza-crusts', 'ingredients'])
      if (notification.resource && catalogResources.has(notification.resource)) {
        void getClientBootstrap().then((data) => setBootstrap(data)).catch(() => setIsOffline(true))
      } else {
        void refreshState()
      }
    })
    connection.onreconnecting(() => setIsRealtimeConnected(false))
    connection.onreconnected(() => {
      setIsRealtimeConnected(true)
      void refreshState()
    })
    connection.onclose(() => setIsRealtimeConnected(false))

    void connection.start()
      .then(() => {
        setIsRealtimeConnected(true)
        setIsOffline(false)
      })
      .catch(() => setIsRealtimeConnected(false))

    return () => {
      connection.off('client:changed')
      void connection.stop()
    }
  }, [activeDeviceId, refreshState])

  useEffect(() => {
    const handleOnline = () => {
      setIsOffline(false)
      void refreshState()
    }
    const handleOffline = () => setIsOffline(true)
    window.addEventListener('online', handleOnline)
    window.addEventListener('offline', handleOffline)
    return () => {
      window.removeEventListener('online', handleOnline)
      window.removeEventListener('offline', handleOffline)
    }
  }, [refreshState])

  useEffect(() => {
    if (!activeDeviceId || !getClientSessionToken()) return

    const controller = new AbortController()
    let battery: ClientBatteryManager | undefined

    const sendTelemetry = () => {
      if (controller.signal.aborted) return
      void updateClientTelemetry(createClientTelemetry(battery), controller.signal)
        .catch(() => undefined)
    }
    const handleVisibility = () => {
      if (document.visibilityState === 'visible') sendTelemetry()
    }

    void getClientBattery().then((manager) => {
      if (controller.signal.aborted) return
      battery = manager
      battery?.addEventListener('chargingchange', sendTelemetry)
      battery?.addEventListener('levelchange', sendTelemetry)
      sendTelemetry()
    })

    const interval = window.setInterval(sendTelemetry, 30_000)
    window.addEventListener('online', sendTelemetry)
    window.addEventListener('offline', sendTelemetry)
    document.addEventListener('visibilitychange', handleVisibility)

    return () => {
      controller.abort()
      window.clearInterval(interval)
      window.removeEventListener('online', sendTelemetry)
      window.removeEventListener('offline', sendTelemetry)
      document.removeEventListener('visibilitychange', handleVisibility)
      battery?.removeEventListener('chargingchange', sendTelemetry)
      battery?.removeEventListener('levelchange', sendTelemetry)
    }
  }, [activeDeviceId])

  const cartCount = cart.reduce((total, item) => total + item.quantity, 0)
  const cartTotal = cart.reduce((total, item) => total + item.quantity * item.unitPrice, 0)
  const validLoyaltyQuote = loyaltyQuoteTotal === cartTotal ? loyaltyQuote : undefined
  const cartEstimatedMinutes = cart.reduce((estimate, item) => {
    const product = bootstrap?.catalog.products.find((candidate) => candidate.id === item.productId)
    return Math.max(estimate, product?.preparationTimeMinutes ?? 0)
  }, 0)
  const cartSuggestions = bootstrap?.catalog.products
    .filter((product) => !cart.some((item) => item.productId === product.id) && product.productType !== 'Pizza' && (product.isPopular || product.isFeatured))
    .slice(0, 3) ?? []
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
      if (data.session.tableSessionId) {
        clearClientCart(data.session.tableSessionId)
        clearClientOrderDraft(data.session.tableSessionId)
      }
      setHasPendingOrderRecovery(false)
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
      if (data.session.tableSessionId) {
        clearClientCart(data.session.tableSessionId)
        clearClientOrderDraft(data.session.tableSessionId)
      }
      setHasPendingOrderRecovery(false)
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
      clearClientOrderDraft(completedTableSessionId)
      setCart([])
      setHasPendingOrderRecovery(false)
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
      if (activeTableSessionId) {
        clearClientCart(activeTableSessionId)
        clearClientOrderDraft(activeTableSessionId)
      }
      setBootstrap(undefined)
      setCart([])
      setHasPendingOrderRecovery(false)
      setScreen('welcome')
      setActivationError('Tablet desvinculado com segurança.')
    } catch (error) {
      toast.error('Não foi possível desvincular', getUserErrorMessage(error))
    } finally {
      setIsMutating(false)
    }
  }

  function addStandardProduct(product: ClientProduct) {
    if (hasPendingOrderRecovery) {
      toast.info('Pedido aguardando confirmação', 'Tente enviar novamente antes de alterar o carrinho.')
      setScreen('cart')
      return
    }
    setCart((current) => {
      const existing = current.find((item) => item.productId === product.id && !item.pizza)
      if (existing) {
        return current.map((item) => item.key === existing.key
          ? { ...item, quantity: Math.min(20, item.quantity + 1) }
          : item)
      }
      return [...current, {
        key: createUuid(),
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
    if (hasPendingOrderRecovery) {
      setBuilderProduct(undefined)
      setScreen('cart')
      toast.info('Pedido aguardando confirmação', 'Tente enviar novamente antes de alterar o carrinho.')
      return
    }
    setCart((current) => editingCartItem
      ? current.map((item) => item.key === editingCartItem.key ? { key: item.key, ...result } : item)
      : [...current, { key: createUuid(), ...result }])
    setBuilderProduct(undefined)
    setEditingCartItem(undefined)
    setScreen('cart')
    toast.success(editingCartItem ? 'Pizza atualizada' : 'Pizza adicionada', editingCartItem ? 'As alterações foram salvas no carrinho.' : 'Sua montagem está pronta no carrinho.')
  }

  function openPizzaBuilder(product: ClientProduct, item?: ClientCartItem) {
    if (hasPendingOrderRecovery) {
      toast.info('Pedido aguardando confirmação', 'Tente enviar novamente antes de alterar o carrinho.')
      setScreen('cart')
      return
    }
    setEditingCartItem(item)
    setBuilderProduct(product)
  }

  function editPizza(key: string) {
    const item = cart.find((candidate) => candidate.key === key)
    const product = bootstrap?.catalog.products.find((candidate) => candidate.id === item?.productId)
    if (!item?.pizza || !product) {
      toast.error('Item indisponível', 'Não foi possível abrir esta pizza para edição.')
      return
    }
    openPizzaBuilder(product, item)
  }

  function reorder(order: ClientOrder) {
    if (!bootstrap || hasPendingOrderRecovery) {
      toast.info('Pedido aguardando confirmação', 'Conclua a tentativa atual antes de repetir outro pedido.')
      return
    }
    const nextItems: ClientCartItem[] = []
    let skipped = 0
    for (const item of order.items) {
      const product = bootstrap.catalog.products.find((candidate) => candidate.id === item.productId)
      if (!product) {
        skipped += 1
        continue
      }
      if (!item.pizza) {
        nextItems.push({
          key: createUuid(),
          productId: product.id,
          name: product.name,
          quantity: item.quantity,
          unitPrice: product.price,
          notes: item.notes,
          imageUrl: getProductImage(product),
        })
        continue
      }
      const size = bootstrap.catalog.pizza.sizes.find((candidate) => candidate.id === item.pizza?.sizeId)
      const flavors = item.pizza.flavors
        .map((snapshot) => bootstrap.catalog.pizza.flavors.find((candidate) => candidate.id === snapshot.id))
        .filter((flavor) => flavor?.isAvailable)
      const crust = bootstrap.catalog.pizza.crusts.find((candidate) => candidate.id === item.pizza?.crustId)
      const secondCrust = bootstrap.catalog.pizza.crusts.find((candidate) => candidate.id === item.pizza?.secondCrustId)
      if (!size || flavors.length !== item.pizza.flavors.length || !crust?.isAvailable || (item.pizza.secondCrustId && !secondCrust?.isAvailable)) {
        skipped += 1
        continue
      }
      nextItems.push({
        key: createUuid(),
        productId: product.id,
        name: `Pizza ${size.name} · ${flavors.length} ${flavors.length === 1 ? 'sabor' : 'sabores'}`,
        quantity: item.quantity,
        unitPrice: item.unitPrice,
        notes: item.notes,
        imageUrl: getProductImage(product),
        pizza: {
          sizeId: size.id,
          sizeName: size.name,
          flavorIds: flavors.map((flavor) => flavor!.id),
          flavorNames: flavors.map((flavor) => flavor!.name),
          crustId: crust.id,
          crustName: crust.name,
          secondCrustId: secondCrust?.id,
          secondCrustName: secondCrust?.name,
          removedIngredientIds: item.modifiers.filter((modifier) => modifier.type === 'Remove' && modifier.ingredientId).map((modifier) => modifier.ingredientId!),
          extraIngredients: item.modifiers.filter((modifier) => modifier.type === 'Extra' && modifier.ingredientId).map((modifier) => ({
            ingredientId: modifier.ingredientId!,
            ingredientName: modifier.name,
            pizzaFlavorId: modifier.pizzaFlavorId,
            pizzaFlavorName: flavors.find((flavor) => flavor?.id === modifier.pizzaFlavorId)?.name,
            quantity: modifier.quantity,
            unitPrice: modifier.unitPrice,
          })),
        },
      })
    }
    if (!nextItems.length) {
      toast.error('Pedido indisponível', 'Os itens deste pedido não estão disponíveis no cardápio atual.')
      return
    }
    setCart((current) => [...current, ...nextItems])
    setScreen('cart')
    toast.success('Pedido adicionado ao carrinho', skipped ? `${nextItems.length} item(ns) incluído(s). ${skipped} indisponível(is) foi(ram) ignorado(s).` : 'Revise os itens e confirme quando estiver pronto.')
  }

  function changeQuantity(key: string, quantity: number) {
    if (hasPendingOrderRecovery) return
    if (quantity <= 0) {
      removeCartItem(key)
      return
    }
    setCart((current) => current.map((item) => item.key === key
      ? { ...item, quantity: Math.min(20, quantity) }
      : item))
  }

  function removeCartItem(key: string) {
    if (hasPendingOrderRecovery) return
    setCart((current) => current.filter((item) => item.key !== key))
    toast.info('Item removido', 'O carrinho foi atualizado.')
  }

  async function submitOrder() {
    if (!cart.length || !activeTableSessionId) return
    if (isOffline) {
      toast.info('Sem conexão', 'O carrinho está salvo neste tablet. Envie quando a rede voltar.')
      return
    }
    setIsMutating(true)
    const orderItems: SubmitClientOrder['items'] = cart.map((item) => ({
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
    }))
    const fingerprint = JSON.stringify({ orderItems, loyaltyPhone, loyaltyBirthDate, couponCode, loyaltyPoints })
    const storedDraft = loadClientOrderDraft(activeTableSessionId)
    const requestId = storedDraft?.fingerprint === fingerprint ? storedDraft.requestId : createUuid()
    const payload: SubmitClientOrder = {
      requestId,
      items: orderItems,
      customerPhone: loyaltyQuote ? loyaltyPhone : undefined,
      customerBirthDate: loyaltyQuote ? loyaltyBirthDate : undefined,
      couponCode: couponCode || undefined,
      loyaltyPoints,
    }
    saveClientOrderDraft(activeTableSessionId, {
      requestId,
      fingerprint,
      attemptedAt: new Date().toISOString(),
    })
    setHasPendingOrderRecovery(true)
    try {
      const order = await submitClientOrder(payload)
      clearClientOrderDraft(activeTableSessionId)
      clearClientCart(activeTableSessionId)
      setCart([])
      setHasPendingOrderRecovery(false)
      setLastOrderNumber(order.number)
      setScreen('orderSent')
      toast.success('Pedido enviado', `Pedido #${order.number} recebido pela cozinha.`)
      try {
        const fresh = await getClientBootstrap()
        setBootstrap(fresh)
      } catch {
        void refreshState()
      }
    } catch (error) {
      const isAmbiguousFailure = error instanceof ApiError && (error.status === 0 || error.status >= 500)
      if (isAmbiguousFailure) {
        setHasPendingOrderRecovery(true)
        toast.error('Confirmação pendente', 'A conexão caiu durante o envio. O carrinho foi bloqueado para uma nova tentativa segura.')
      } else {
        clearClientOrderDraft(activeTableSessionId)
        setHasPendingOrderRecovery(false)
        toast.error('Não foi possível enviar', getUserErrorMessage(error))
      }
    } finally {
      setIsMutating(false)
    }
  }

  async function applyLoyaltyBenefits() {
    if (!loyaltyPhone || !loyaltyBirthDate) { toast.info('Identifique-se', 'Informe telefone e data de nascimento.'); return }
    setIsMutating(true)
    try {
      setLoyaltyQuote(await getClientLoyaltyQuote({ phone: loyaltyPhone, birthDate: loyaltyBirthDate, orderAmount: cartTotal, couponCode: couponCode || undefined, loyaltyPoints }))
      setLoyaltyQuoteTotal(cartTotal)
      toast.success('Benefícios aplicados', 'O desconto foi validado com segurança.')
    } catch (error) { setLoyaltyQuote(undefined); toast.error('Não foi possível aplicar', getUserErrorMessage(error)) } finally { setIsMutating(false) }
  }

  async function sendServiceCall(typeId: string, details?: string) {
    setIsMutating(true)
    try {
      await createClientServiceCall(typeId, details)
      toast.success('Solicitação enviada', 'A equipe foi avisada e irá até sua mesa.')
      await refreshState()
      setScreen('service')
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
      {(isOffline || !isRealtimeConnected) && (
        <div className="client-network-banner" role="status">
          {isOffline
            ? 'Sem conexão. Seu carrinho está salvo neste tablet. O envio será liberado quando a rede voltar.'
            : 'Reconectando às atualizações em tempo real...'}
        </div>
      )}
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
            onBuildPizza={(product) => openPizzaBuilder(product)}
          />
        )}
        {screen === 'cart' && (
          <CartView
            items={cart}
            existingConsumption={existingConsumption}
            serviceFeePercentage={bootstrap.catalog.serviceFeePercentage}
            estimatedPreparationMinutes={cartEstimatedMinutes}
            suggestions={cartSuggestions}
            canSubmit={canSubmitOrders && !isOffline}
            isLocked={hasPendingOrderRecovery}
            blockedMessage={hasPendingOrderRecovery
              ? 'A conexão caiu durante uma tentativa. Reenvie o mesmo pedido com segurança antes de alterar o carrinho.'
              : isOffline
                ? 'O carrinho está salvo. Conecte-se à rede para enviar o pedido.'
                : orderBlockedMessage}
            isSubmitting={isMutating}
            loyalty={{ phone: loyaltyPhone, birthDate: loyaltyBirthDate, couponCode, points: loyaltyPoints, quote: validLoyaltyQuote }}
            onLoyaltyChange={(next) => { setLoyaltyPhone(next.phone); setLoyaltyBirthDate(next.birthDate); setCouponCode(next.couponCode); setLoyaltyPoints(next.points); setLoyaltyQuote(undefined) }}
            onApplyLoyalty={() => void applyLoyaltyBenefits()}
            onChangeQuantity={changeQuantity}
            onEdit={editPizza}
            onAddSuggestion={addStandardProduct}
            onRemove={removeCartItem}
            onContinue={() => setScreen('menu')}
            onSubmit={submitOrder}
          />
        )}
        {screen === 'orders' && (
          <OrdersView orders={bootstrap.orders} onMenu={() => setScreen('menu')} onBill={() => setScreen('bill')} onReorder={reorder} />
        )}
        {screen === 'service' && (
          <ServiceCallView types={bootstrap.serviceCallTypes} calls={bootstrap.serviceCalls} isSubmitting={isMutating} onSubmit={sendServiceCall} />
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
          initialValue={editingCartItem}
          onCancel={() => { setBuilderProduct(undefined); setEditingCartItem(undefined) }}
          onAdd={addPizza}
        />
      )}
    </>
  )
}
