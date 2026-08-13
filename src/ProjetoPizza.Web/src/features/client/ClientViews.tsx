import {
  ArrowRight,
  BellRing,
  Check,
  CheckCircle2,
  CircleCheckBig,
  ChefHat,
  CircleDollarSign,
  Clock3,
  CreditCard,
  MousePointerClick,
  HelpCircle,
  LogOut,
  Minus,
  PackageCheck,
  Pencil,
  Pizza,
  Plus,
  Star,
  ReceiptText,
  RotateCcw,
  Send,
  Share2,
  ShoppingBag,
  Trash2,
  UsersRound,
  UtensilsCrossed,
  WalletCards,
  Wifi,
} from 'lucide-react'
import { QRCodeSVG } from 'qrcode.react'
import { type CSSProperties, type FormEvent, useEffect, useRef, useState } from 'react'
import type {
  ClientBill,
  ClientCartItem,
  ClientOrder,
  ClientProduct,
  ClientServiceCall,
  ClientSession,
} from '../../types/client'
import { formatCurrency } from '../../utils/money'
import { clientHeroImage, clientIdleImage, getProductImage } from './clientPresentation'

export function ActivationView({
  isSubmitting,
  error,
  onActivate,
}: {
  isSubmitting: boolean
  error?: string
  onActivate: (deviceCode: string) => void
}) {
  const [deviceCode, setDeviceCode] = useState(import.meta.env.DEV ? 'DEV-TABLET-002' : '')

  function submit(event: FormEvent) {
    event.preventDefault()
    if (deviceCode.trim()) onActivate(deviceCode.trim())
  }

  return (
    <main className="client-activation-page">
      <section>
        <span className="client-logo"><Pizza aria-hidden="true" /> Forno 27</span>
        <div>
          <span className="client-eyebrow">Ativação segura</span>
          <h1>Prepare o tablet da mesa</h1>
          <p>Informe o código cadastrado no painel. Depois da ativação, este tablet permanecerá vinculado à mesa até ser desconectado.</p>
        </div>
        <form onSubmit={submit}>
          <label>
            Código do tablet
            <input
              value={deviceCode}
              onChange={(event) => setDeviceCode(event.target.value)}
              maxLength={100}
              autoComplete="off"
              autoCapitalize="characters"
              aria-invalid={Boolean(error)}
              aria-describedby={error ? 'activation-error' : 'activation-hint'}
            />
          </label>
          <small id="activation-hint">
            {import.meta.env.DEV
              ? 'Para testar o ambiente local, use DEV-TABLET-002 ou DEV-TABLET-003.'
              : 'O código é fornecido no painel administrativo durante a vinculação do tablet.'}
          </small>
          {error && <p id="activation-error" role="alert">{error}</p>}
          <button type="submit" className="client-primary-action" disabled={isSubmitting || !deviceCode.trim()}>
            {isSubmitting ? 'Ativando...' : <>Ativar tablet <ArrowRight aria-hidden="true" /></>}
          </button>
        </form>
      </section>
      <aside style={{ backgroundImage: `linear-gradient(180deg, rgba(0,0,0,.05), rgba(45,15,3,.38)), url("${clientHeroImage}")` }}>
        <span><ChefHat aria-hidden="true" /> Experiência à mesa</span>
      </aside>
    </main>
  )
}

export function StandbyView({
  session,
  isSubmitting,
  error,
  onStart,
  onLogout,
}: {
  session: ClientSession
  isSubmitting: boolean
  error?: string
  onStart: (guestCount: number) => void
  onLogout: () => void
}) {
  const [isChoosingGuests, setChoosingGuests] = useState(false)
  const [guestCount, setGuestCount] = useState(2)

  return (
    <main
      className="client-standby-page client-idle-reference"
      style={{ backgroundImage: `url("${clientIdleImage}")` }}
    >
      <div className="client-standby-ambient" aria-hidden="true">
        {Array.from({ length: 16 }, (_, index) => <i key={index} style={{ '--spark': index } as CSSProperties} />)}
      </div>
      <header>
        <span className="client-idle-badge"><UtensilsCrossed aria-hidden="true" /> {session.tableName}</span>
        <div className="client-idle-connectivity">
          <span className="client-idle-badge"><Wifi aria-hidden="true" /> Conexão ativa</span>
          <button type="button" onClick={onLogout} title="Desvincular este tablet" aria-label="Desvincular este tablet"><LogOut aria-hidden="true" /></button>
        </div>
      </header>

      <section className={isChoosingGuests ? 'is-choosing' : ''}>
        {!isChoosingGuests ? (
          <div className="client-idle-center">
            <h1>FORNO <span>27</span></h1>
            <small>Pizzeria Artigianale</small>
            <p>A verdadeira pizza artesanal espera por você</p>
            <button type="button" className="client-primary-action client-standby-start" onClick={() => setChoosingGuests(true)}>
              Toque para iniciar seu pedido <MousePointerClick aria-hidden="true" />
            </button>
          </div>
        ) : (
          <div className="client-guest-picker">
            <span className="client-eyebrow">Nova comanda</span>
            <h1>Quantas pessoas estão na mesa?</h1>
            <p>Essa informação ajuda nossa equipe a preparar um atendimento melhor.</p>
            <div role="group" aria-label="Quantidade de pessoas">
              <button type="button" aria-label="Diminuir quantidade de pessoas" disabled={guestCount <= 1 || isSubmitting} onClick={() => setGuestCount((value) => Math.max(1, value - 1))}><Minus aria-hidden="true" /></button>
              <output aria-live="polite"><strong>{guestCount}</strong><span>{guestCount === 1 ? 'pessoa' : 'pessoas'}</span></output>
              <button type="button" aria-label="Aumentar quantidade de pessoas" disabled={guestCount >= 50 || isSubmitting} onClick={() => setGuestCount((value) => Math.min(50, value + 1))}><Plus aria-hidden="true" /></button>
            </div>
            {error && <p className="client-standby-error" role="alert">{error}</p>}
            <footer>
              <button type="button" className="client-secondary-action" disabled={isSubmitting} onClick={() => setChoosingGuests(false)}>Voltar</button>
              <button type="button" className="client-primary-action" disabled={isSubmitting} aria-busy={isSubmitting} onClick={() => onStart(guestCount)}>
                {isSubmitting ? 'Abrindo comanda...' : 'Confirmar e ver cardápio'}
              </button>
            </footer>
          </div>
        )}
      </section>

      {!isChoosingGuests && (
        <aside className="client-idle-promo" aria-label="Sugestão do chef">
          <img src={clientIdleImage} alt="Pizza artesanal saindo do forno a lenha" />
          <div>
            <span>Sugestão do chef</span>
            <strong>Margherita Especial</strong>
            <p>Molho pelati, mozzarella di bufala e manjericão fresco.</p>
          </div>
          <span className="client-idle-promo-dots" aria-hidden="true"><i className="active" /><i /><i /></span>
        </aside>
      )}
    </main>
  )
}

export function WelcomeView({
  session,
  onMenu,
  onHelp,
}: {
  session: ClientSession
  onMenu: () => void
  onHelp: () => void
}) {
  return (
    <main className="client-welcome-page">
      <section>
        <span className="client-logo"><Pizza aria-hidden="true" /> Forno 27</span>
        <div>
          <h1>Bem-vindo à Pizzaria Forno 27</h1>
          <h2>Você está na Mesa {session.tableNumber}</h2>
          <p>Faça seus pedidos diretamente pelo tablet. Quando precisar, chame um de nossos garçons.</p>
        </div>
        <div className="client-welcome-actions">
          <button type="button" className="client-primary-action" onClick={onMenu}>
            <UtensilsCrossed aria-hidden="true" /> Ver cardápio
          </button>
          <button type="button" className="client-secondary-action" onClick={onHelp}>
            <HelpCircle aria-hidden="true" /> Como funciona
          </button>
        </div>
        <p className="client-waiter">
          <span><Check aria-hidden="true" /></span>
          Seu atendimento {session.waiterName ? <>será acompanhado por <strong>{session.waiterName}</strong>.</> : 'está conectado à nossa equipe.'}
        </p>
      </section>
      <aside style={{ backgroundImage: `linear-gradient(180deg, rgba(0,0,0,.02), rgba(45,15,3,.25)), url("${clientHeroImage}")` }}>
        <span><ChefHat aria-hidden="true" /> Forno a lenha</span>
      </aside>
    </main>
  )
}

export function CartView({
  items,
  existingConsumption,
  serviceFeePercentage,
  estimatedPreparationMinutes,
  suggestions,
  canSubmit,
  isLocked,
  blockedMessage,
  isSubmitting,
  onChangeQuantity,
  onEdit,
  onAddSuggestion,
  onRemove,
  onContinue,
  onSubmit,
}: {
  items: ClientCartItem[]
  existingConsumption: number
  serviceFeePercentage: number
  estimatedPreparationMinutes: number
  suggestions: ClientProduct[]
  canSubmit: boolean
  isLocked: boolean
  blockedMessage?: string
  isSubmitting: boolean
  onChangeQuantity: (key: string, quantity: number) => void
  onEdit: (key: string) => void
  onAddSuggestion: (product: ClientProduct) => void
  onRemove: (key: string) => void
  onContinue: () => void
  onSubmit: () => void
}) {
  const subtotal = items.reduce((total, item) => total + item.unitPrice * item.quantity, 0)
  const fee = Math.round(subtotal * serviceFeePercentage) / 100
  const itemCount = items.reduce((total, item) => total + item.quantity, 0)

  return (
    <section className="client-cart-view">
      <header className="client-section-heading">
        <div>
          <span className="client-eyebrow"><ShoppingBag aria-hidden="true" /> Revise antes de enviar</span>
          <h1>Seu carrinho</h1>
          <p>Os valores serão conferidos novamente pelo servidor ao confirmar.</p>
        </div>
      </header>

      {items.length === 0 ? (
        <div className="client-empty-state">
          <ShoppingBag aria-hidden="true" />
          <h2>Seu carrinho está vazio</h2>
          <p>Explore o cardápio e adicione seus favoritos.</p>
          <button type="button" className="client-primary-action" onClick={onContinue}>Ver cardápio</button>
        </div>
      ) : (
        <div className="client-cart-layout">
          <div className="client-cart-list">
            {items.map((item) => (
              <article className="client-cart-item" key={item.key}>
                <img src={item.imageUrl || clientHeroImage} alt="" />
                <div>
                  <header>
                    <h2>{item.name}</h2>
                    <strong>{formatCurrency(item.unitPrice * item.quantity)}</strong>
                  </header>
                  {item.pizza && (
                    <ul>
                      {item.pizza.flavorNames.map((flavor) => <li key={flavor}>1/{item.pizza?.flavorNames.length} {flavor}</li>)}
                      <li>{item.pizza.secondCrustName
                        ? `Borda: ½ ${item.pizza.crustName} + ½ ${item.pizza.secondCrustName}`
                        : `Borda: ${item.pizza.crustName}`}</li>
                      {item.pizza.removedIngredientIds.length > 0 && <li>{item.pizza.removedIngredientIds.length} ingrediente(s) removido(s)</li>}
                      {(item.pizza.extraIngredients ?? []).map((extra) => (
                        <li key={`${extra.ingredientId}-${extra.pizzaFlavorId ?? 'whole'}`}>
                          + {extra.quantity}× {extra.ingredientName}{extra.pizzaFlavorName ? ` em ${extra.pizzaFlavorName}` : ''}
                        </li>
                      ))}
                    </ul>
                  )}
                  {item.notes && <p>Observação: {item.notes}</p>}
                  <footer>
                    <div className="client-cart-item-actions">
                      {item.pizza && <button type="button" className="client-edit-action" disabled={isLocked} onClick={() => onEdit(item.key)}><Pencil aria-hidden="true" /> Editar</button>}
                      <button type="button" className="client-remove-action" disabled={isLocked} onClick={() => onRemove(item.key)}>
                        <Trash2 aria-hidden="true" /> Remover
                      </button>
                    </div>
                    <div className="client-quantity-control">
                      <button type="button" disabled={isLocked} onClick={() => onChangeQuantity(item.key, item.quantity - 1)} aria-label={`Diminuir quantidade de ${item.name}`}>
                        <Minus aria-hidden="true" />
                      </button>
                      <strong>{item.quantity}</strong>
                      <button type="button" disabled={isLocked} onClick={() => onChangeQuantity(item.key, item.quantity + 1)} aria-label={`Aumentar quantidade de ${item.name}`}>
                        <Plus aria-hidden="true" />
                      </button>
                    </div>
                  </footer>
                </div>
              </article>
            ))}
            {suggestions.length > 0 && (
              <section className="client-cart-suggestions" aria-labelledby="cart-suggestions-title">
                <header><h2 id="cart-suggestions-title">Combina com seu pedido</h2><p>Itens rápidos para completar a mesa.</p></header>
                <div>{suggestions.map((product) => <article key={product.id}><img src={getProductImage(product)} alt="" loading="lazy" /><span><strong>{product.name}</strong><small>{formatCurrency(product.price)}</small></span><button type="button" disabled={isLocked} onClick={() => onAddSuggestion(product)}><Plus aria-hidden="true" /> Adicionar</button></article>)}</div>
              </section>
            )}
          </div>
          <aside className="client-order-summary">
            <h2>Resumo do pedido</h2>
            <div><span>Subtotal ({itemCount} itens)</span><strong>{formatCurrency(subtotal)}</strong></div>
            <div className="accent"><span>Taxa de serviço ({serviceFeePercentage}%)</span><strong>{formatCurrency(fee)}</strong></div>
            {existingConsumption > 0 && <small>Consumo atual da mesa: {formatCurrency(existingConsumption)}</small>}
            {estimatedPreparationMinutes > 0 && <p className="client-preparation-estimate"><Clock3 aria-hidden="true" /><span>Previsão de preparo<strong>{estimatedPreparationMinutes}-{estimatedPreparationMinutes + 5} min</strong></span></p>}
            <div className="client-summary-total"><span>Total a enviar</span><strong>{formatCurrency(subtotal + fee)}</strong></div>
            {!canSubmit && blockedMessage && (
              <p className="client-order-blocked" role="status">{blockedMessage}</p>
            )}
            <button type="button" className="client-primary-action" onClick={onSubmit} disabled={isSubmitting || !canSubmit}>
              <Send aria-hidden="true" /> {isSubmitting ? 'Enviando...' : isLocked ? 'Verificar e reenviar' : 'Confirmar e enviar pedido'}
            </button>
            <button type="button" className="client-secondary-action" disabled={isLocked} onClick={onContinue}>Continuar escolhendo</button>
          </aside>
        </div>
      )}
    </section>
  )
}

export function OrdersView({ orders, onMenu, onBill, onReorder }: { orders: ClientOrder[]; onMenu: () => void; onBill: () => void; onReorder: (order: ClientOrder) => void }) {
  const total = orders.filter((order) => order.status !== 'Cancelled').reduce((sum, order) => sum + order.total, 0)
  return (
    <section className="client-orders-view">
      <header className="client-section-heading client-heading-with-metric">
        <div>
          <span className="client-eyebrow"><ReceiptText aria-hidden="true" /> Acompanhamento ao vivo</span>
          <h1>Meus pedidos</h1>
          <p>Acompanhe tudo o que foi enviado nesta sessão.</p>
        </div>
        <div className="client-consumption-metric"><CircleDollarSign aria-hidden="true" /><span>Consumo da mesa<strong>{formatCurrency(total)}</strong></span></div>
      </header>
      {orders.length === 0 ? (
        <div className="client-empty-state">
          <ReceiptText aria-hidden="true" />
          <h2>Nenhum pedido enviado</h2>
          <p>Quando você confirmar o carrinho, o andamento aparecerá aqui.</p>
          <button type="button" className="client-primary-action" onClick={onMenu}>Ver cardápio</button>
        </div>
      ) : (
        <>
          <div className="client-orders-grid">
            {orders.map((order) => {
              const progress = getOrderProgress(order.status)
              return (
                <article className="client-order-card" key={order.id}>
                  <header>
                    <span><small>Pedido</small><strong>#{order.number}</strong></span>
                    <OrderStatus status={order.status} />
                  </header>
                  <div className="client-order-timeline" aria-label={`Andamento: ${progress.label}`}>
                    {['Recebido', 'Confirmado', 'Em preparo', 'Pronto'].map((label, index) => (
                      <span key={label} className={index <= progress.step ? 'complete' : ''}>
                        <i>{index < progress.step ? <Check aria-hidden="true" /> : index + 1}</i>
                        <small>{label}</small>
                      </span>
                    ))}
                  </div>
                  <ul>
                    {order.items.map((item) => (
                      <li className="client-order-line" key={item.id}>
                        <span>
                          {item.quantity}× {item.name}
                          {(item.modifiers ?? []).filter((modifier) => modifier.type === 'Extra').map((modifier) => (
                            <small key={`${modifier.name}-${modifier.pizzaFlavorId ?? 'whole'}`}>
                              + {modifier.quantity}× {modifier.name}
                            </small>
                          ))}
                        </span>
                        <strong>{formatCurrency(item.totalPrice)}</strong>
                      </li>
                    ))}
                  </ul>
                  <footer>
                    <small>{order.placedAt ? new Date(order.placedAt).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' }) : 'Agora'}</small>
                    <span><strong>{formatCurrency(order.total)}</strong><button type="button" onClick={() => onReorder(order)}><RotateCcw aria-hidden="true" /> Pedir novamente</button></span>
                  </footer>
                </article>
              )
            })}
          </div>
          <div className="client-orders-actions">
            <button type="button" className="client-secondary-action" onClick={onMenu}>Fazer novo pedido</button>
            <button type="button" className="client-primary-action" onClick={onBill}><WalletCards aria-hidden="true" /> Solicitar conta</button>
          </div>
        </>
      )}
    </section>
  )
}

export function ServiceCallView({
  types,
  calls,
  isSubmitting,
  onSubmit,
}: {
  types: Array<{ id: string; code: string; name: string }>
  calls: ClientServiceCall[]
  isSubmitting: boolean
  onSubmit: (typeId: string, details?: string) => void
}) {
  const [selectedId, setSelectedId] = useState('')
  const [details, setDetails] = useState('')

  return (
    <section className="client-service-view">
      <header className="client-section-heading centered">
        <div>
          <span className="client-eyebrow"><BellRing aria-hidden="true" /> Atendimento</span>
          <h1>Como podemos ajudar?</h1>
          <p>Escolha um motivo. A equipe receberá o chamado imediatamente.</p>
        </div>
      </header>
      {calls.length > 0 && (
        <section className="client-service-status" aria-labelledby="service-status-title">
          <h2 id="service-status-title">Seus chamados</h2>
          {calls.map((call) => <div key={call.id}><span><strong>{call.typeName}</strong><small>{new Date(call.createdAt).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}</small></span><b className={`status-${call.status.toLowerCase()}`}>{serviceCallStatus(call.status)}</b></div>)}
        </section>
      )}
      <div className="client-service-grid" role="radiogroup" aria-label="Motivo do chamado">
        {types.map((type, index) => {
          const icons = [BellRing, UtensilsCrossed, CreditCard, HelpCircle]
          const Icon = icons[index % icons.length]
          return (
            <button
              type="button"
              role="radio"
              aria-checked={selectedId === type.id}
              className={selectedId === type.id ? 'selected' : ''}
              key={type.id}
              onClick={() => setSelectedId(type.id)}
            >
              <Icon aria-hidden="true" />
              <strong>{type.name}</strong>
              <small>{serviceCallDescription(type.code)}</small>
              {selectedId === type.id && <CheckCircle2 aria-hidden="true" />}
            </button>
          )
        })}
      </div>
      <label className="client-service-details">
        Detalhes adicionais <small>(opcional)</small>
        <textarea
          value={details}
          onChange={(event) => setDetails(event.target.value)}
          maxLength={500}
          placeholder="Conte rapidamente como podemos ajudar..."
        />
      </label>
      <button
        type="button"
        className="client-primary-action client-service-submit"
        disabled={!selectedId || isSubmitting}
        onClick={() => onSubmit(selectedId, details.trim() || undefined)}
      >
        <BellRing aria-hidden="true" /> {isSubmitting ? 'Enviando solicitação...' : 'Enviar solicitação'}
      </button>
    </section>
  )
}

export function BillView({
  bill,
  guestCount,
  isSubmitting,
  onRequest,
}: {
  bill: ClientBill
  guestCount: number
  isSubmitting: boolean
  onRequest: (splitCount?: number) => void
}) {
  const [split, setSplit] = useState(false)
  const [people, setPeople] = useState(Math.max(2, guestCount))
  const alreadyRequested = bill.status !== 'Open'

  return (
    <section className="client-bill-view">
      <header className="client-section-heading">
        <div>
          <span className="client-eyebrow"><WalletCards aria-hidden="true" /> Encerrar consumo</span>
          <h1>Solicitar a conta</h1>
          <p>Confira os valores e avise como prefere organizar o pagamento.</p>
        </div>
      </header>
      <div className="client-bill-layout">
        <div className="client-bill-breakdown">
          <header><ReceiptText aria-hidden="true" /><span><strong>Resumo da mesa</strong><small>Valores atualizados desta sessão</small></span></header>
          <div><span>Subtotal</span><strong>{formatCurrency(bill.subtotal)}</strong></div>
          <div><span>Taxa de serviço ({bill.serviceFeePercentage}%)</span><strong>{formatCurrency(bill.serviceFeeAmount)}</strong></div>
          {bill.paid > 0 && <div><span>Já pago</span><strong>- {formatCurrency(bill.paid)}</strong></div>}
          <div className="client-bill-total"><span>Total da conta</span><strong>{formatCurrency(bill.total)}</strong></div>
        </div>
        <aside className="client-bill-choice">
          {alreadyRequested ? (
            <div className="client-bill-requested">
              <CheckCircle2 aria-hidden="true" />
              <h2>Conta solicitada</h2>
              <p>A equipe já recebeu seu pedido e irá até a mesa para finalizar.</p>
              {bill.requestedSplitCount && <span>Divisão solicitada: <strong>{bill.requestedSplitCount} pessoas</strong></span>}
              <strong>Saldo: {formatCurrency(bill.remaining)}</strong>
            </div>
          ) : (
            <>
              <h2>Como pretendem pagar?</h2>
              <div className="client-payment-choice" role="radiogroup" aria-label="Preferência de pagamento">
                <button type="button" role="radio" aria-checked={!split} className={!split ? 'selected' : ''} onClick={() => setSplit(false)}>
                  <CreditCard aria-hidden="true" /><span><strong>Pagar junto</strong><small>Uma única conta para a mesa</small></span>
                </button>
                <button type="button" role="radio" aria-checked={split} className={split ? 'selected' : ''} onClick={() => setSplit(true)}>
                  <UsersRound aria-hidden="true" /><span><strong>Dividir a conta</strong><small>Veja uma estimativa por pessoa</small></span>
                </button>
              </div>
              {split && (
                <div className="client-split-estimate">
                  <label>
                    Pessoas
                    <div className="client-quantity-control">
                      <button type="button" onClick={() => setPeople((current) => Math.max(2, current - 1))} aria-label="Diminuir pessoas"><Minus aria-hidden="true" /></button>
                      <strong>{people}</strong>
                      <button type="button" onClick={() => setPeople((current) => Math.min(50, current + 1))} aria-label="Aumentar pessoas"><Plus aria-hidden="true" /></button>
                    </div>
                  </label>
                  <span>Estimativa por pessoa<strong>{formatCurrency(Math.round((bill.remaining / people) * 100) / 100)}</strong></span>
                  <small>A divisão final e as formas de pagamento serão confirmadas com o caixa.</small>
                </div>
              )}
              <button type="button" className="client-primary-action" onClick={() => onRequest(split ? people : undefined)} disabled={isSubmitting || bill.total <= 0}>
                <Send aria-hidden="true" /> {isSubmitting ? 'Solicitando...' : 'Solicitar conta'}
              </button>
            </>
          )}
        </aside>
      </div>
    </section>
  )
}

export function OrderSentView({ orderNumber, onOrders, onMenu }: { orderNumber: number; onOrders: () => void; onMenu: () => void }) {
  return (
    <section className="client-success-view">
      <span><PackageCheck aria-hidden="true" /></span>
      <small>Pedido #{orderNumber}</small>
      <h1>Pedido enviado para a cozinha!</h1>
      <p>A equipe já recebeu seu pedido. Você pode acompanhar cada etapa em “Meus pedidos”.</p>
      <div>
        <button type="button" className="client-primary-action" onClick={onOrders}><Clock3 aria-hidden="true" /> Acompanhar pedido</button>
        <button type="button" className="client-secondary-action" onClick={onMenu}>Voltar ao cardápio</button>
      </div>
    </section>
  )
}

export function ThankYouView({ onFinish, isFinishing = false }: { onFinish: () => void; isFinishing?: boolean }) {
  const [secondsLeft, setSecondsLeft] = useState(20)
  const finishRef = useRef(onFinish)
  const finishTriggeredRef = useRef(false)
  const googleReviewUrl = import.meta.env.VITE_GOOGLE_REVIEW_URL || import.meta.env.VITE_FEEDBACK_URL || ''
  const instagramUrl = import.meta.env.VITE_INSTAGRAM_URL || import.meta.env.VITE_SOCIAL_URL || ''
  const hasGoogleReviewUrl = /^https?:\/\//i.test(googleReviewUrl)
  const hasInstagramUrl = /^https?:\/\//i.test(instagramUrl)

  useEffect(() => {
    finishRef.current = onFinish
  }, [onFinish])

  useEffect(() => {
    const timer = window.setInterval(() => {
      setSecondsLeft((current) => {
        if (current <= 1) {
          window.clearInterval(timer)
          if (!finishTriggeredRef.current) {
            finishTriggeredRef.current = true
            finishRef.current()
          }
          return 0
        }
        return current - 1
      })
    }, 1_000)
    return () => window.clearInterval(timer)
  }, [])

  function finishNow() {
    if (finishTriggeredRef.current) return
    finishTriggeredRef.current = true
    onFinish()
  }

  async function shareRestaurant() {
    const shareData = {
      title: 'Forno 27 Pizzeria',
      text: 'Conheça a Forno 27 Pizzeria.',
      url: window.location.origin,
    }
    if (navigator.share) {
      await navigator.share(shareData).catch(() => undefined)
      return
    }
    await navigator.clipboard?.writeText(`${shareData.text} ${shareData.url}`).catch(() => undefined)
  }

  return (
    <main
      className="client-thank-you-page"
      style={{ backgroundImage: `linear-gradient(rgba(45, 49, 55, .48), rgba(45, 49, 55, .48)), url("${clientHeroImage}")` }}
    >
      <div className="client-celebration" aria-hidden="true">
        {Array.from({ length: 24 }, (_, index) => <i key={index} style={{ '--confetti': index } as CSSProperties} />)}
      </div>
      <section>
        <span className="client-thank-you-icon"><CircleCheckBig aria-hidden="true" /></span>
        <h1>Obrigado pela visita!</h1>
        <p className="client-thank-you-lead">Foi muito bom ter você à mesa. Volte sempre!</p>
        <div className="client-thank-you-info">
          <BellRing aria-hidden="true" />
          <p>Pagamento confirmado. Até a próxima!</p>
        </div>
        <div className="client-thank-you-social">
          <div>
            <small>Avalie-nos no Google</small>
            {hasGoogleReviewUrl ? (
              <a href={googleReviewUrl} target="_blank" rel="noreferrer" aria-label="Avaliar experiência no Google">
                <QRCodeSVG value={googleReviewUrl} size={82} level="M" marginSize={1} title="QR Code para avaliação no Google" />
              </a>
            ) : <span className="client-review-unavailable"><Star aria-hidden="true" /><small>Em breve</small></span>}
          </div>
          <i aria-hidden="true" />
          <div>
            <small>Siga-nos no Instagram</small>
            <nav aria-label="Redes sociais">
              {hasInstagramUrl && <a href={instagramUrl} target="_blank" rel="noreferrer" aria-label="Acessar Instagram da Forno 27"><strong aria-hidden="true">@</strong></a>}
              <button type="button" aria-label="Compartilhar a Forno 27" onClick={() => void shareRestaurant()}><Share2 aria-hidden="true" /></button>
            </nav>
          </div>
        </div>
        <div className="client-thank-you-countdown" aria-live="polite">
          <svg viewBox="0 0 44 44" aria-hidden="true">
            <circle cx="22" cy="22" r="19" />
            <circle cx="22" cy="22" r="19" style={{ strokeDashoffset: 119.4 * (1 - secondsLeft / 20) }} />
          </svg>
          <span><strong>{secondsLeft}</strong><small>seg</small></span>
          <p>{isFinishing ? 'Preparando a mesa...' : 'A tela inicial voltará automaticamente'}</p>
        </div>
        <button type="button" className="client-secondary-action client-finish-session" disabled={isFinishing} onClick={finishNow}>
          Preparar para o próximo atendimento
        </button>
      </section>
    </main>
  )
}

function OrderStatus({ status }: { status: string }) {
  const { label } = getOrderProgress(status)
  return <span className={`client-order-status status-${status.toLowerCase()}`}>{label}</span>
}

function getOrderProgress(status: string) {
  const progress: Record<string, { label: string; step: number }> = {
    Draft: { label: 'Rascunho', step: 0 },
    Submitted: { label: 'Recebido', step: 0 },
    Accepted: { label: 'Confirmado', step: 1 },
    InProduction: { label: 'Em preparo', step: 2 },
    Ready: { label: 'Pronto', step: 3 },
    Completed: { label: 'Entregue', step: 3 },
    Cancelled: { label: 'Cancelado', step: 0 },
  }
  return progress[status] ?? { label: status, step: 0 }
}

function serviceCallDescription(code: string) {
  const descriptions: Record<string, string> = {
    WAITER: 'Preciso falar com um garçom',
    UTENSILS: 'Talheres, pratos ou guardanapos',
    ORDER_PROBLEM: 'Algo não está certo com o pedido',
    BILL: 'Tenho uma dúvida sobre a conta',
  }
  return descriptions[code] ?? 'Solicitar auxílio da equipe'
}

function serviceCallStatus(status: string) {
  const labels: Record<string, string> = {
    Pending: 'Enviado',
    Acknowledged: 'Equipe a caminho',
    Completed: 'Concluído',
  }
  return labels[status] ?? status
}
