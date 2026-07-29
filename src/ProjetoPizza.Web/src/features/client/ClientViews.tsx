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
  HelpCircle,
  Minus,
  PackageCheck,
  Pizza,
  Plus,
  Star,
  ReceiptText,
  Send,
  Share2,
  ShoppingBag,
  ThumbsUp,
  Trash2,
  UsersRound,
  UtensilsCrossed,
  WalletCards,
} from 'lucide-react'
import { type FormEvent, useState } from 'react'
import type {
  ClientBill,
  ClientCartItem,
  ClientOrder,
  ClientSession,
} from '../../types/client'
import { formatCurrency } from '../../utils/money'
import { clientHeroImage } from './clientPresentation'

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
          <p>Informe o código cadastrado no painel. O dispositivo precisa estar vinculado a uma mesa com atendimento aberto.</p>
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
  canSubmit,
  blockedMessage,
  isSubmitting,
  onChangeQuantity,
  onRemove,
  onContinue,
  onSubmit,
}: {
  items: ClientCartItem[]
  existingConsumption: number
  serviceFeePercentage: number
  canSubmit: boolean
  blockedMessage?: string
  isSubmitting: boolean
  onChangeQuantity: (key: string, quantity: number) => void
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
                    <button type="button" className="client-remove-action" onClick={() => onRemove(item.key)}>
                      <Trash2 aria-hidden="true" /> Remover
                    </button>
                    <div className="client-quantity-control">
                      <button type="button" onClick={() => onChangeQuantity(item.key, item.quantity - 1)} aria-label={`Diminuir quantidade de ${item.name}`}>
                        <Minus aria-hidden="true" />
                      </button>
                      <strong>{item.quantity}</strong>
                      <button type="button" onClick={() => onChangeQuantity(item.key, item.quantity + 1)} aria-label={`Aumentar quantidade de ${item.name}`}>
                        <Plus aria-hidden="true" />
                      </button>
                    </div>
                  </footer>
                </div>
              </article>
            ))}
          </div>
          <aside className="client-order-summary">
            <h2>Resumo do pedido</h2>
            <div><span>Subtotal ({itemCount} itens)</span><strong>{formatCurrency(subtotal)}</strong></div>
            <div className="accent"><span>Taxa de serviço ({serviceFeePercentage}%)</span><strong>{formatCurrency(fee)}</strong></div>
            {existingConsumption > 0 && <small>Consumo atual da mesa: {formatCurrency(existingConsumption)}</small>}
            <div className="client-summary-total"><span>Total a enviar</span><strong>{formatCurrency(subtotal + fee)}</strong></div>
            {!canSubmit && blockedMessage && (
              <p className="client-order-blocked" role="status">{blockedMessage}</p>
            )}
            <button type="button" className="client-primary-action" onClick={onSubmit} disabled={isSubmitting || !canSubmit}>
              <Send aria-hidden="true" /> {isSubmitting ? 'Enviando...' : 'Confirmar e enviar pedido'}
            </button>
            <button type="button" className="client-secondary-action" onClick={onContinue}>Continuar escolhendo</button>
          </aside>
        </div>
      )}
    </section>
  )
}

export function OrdersView({ orders, onMenu, onBill }: { orders: ClientOrder[]; onMenu: () => void; onBill: () => void }) {
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
                    <strong>{formatCurrency(order.total)}</strong>
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
  isSubmitting,
  onSubmit,
}: {
  types: Array<{ id: string; code: string; name: string }>
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

export function ThankYouView({ onFinish }: { onFinish: () => void }) {
  const feedbackUrl = import.meta.env.VITE_FEEDBACK_URL || 'mailto:atendimento@forno27.local?subject=Avaliação%20da%20experiência'

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
      <section>
        <span className="client-thank-you-icon"><CircleCheckBig aria-hidden="true" /></span>
        <h1>Obrigado pela preferência!</h1>
        <p className="client-thank-you-lead">Esperamos que sua experiência na Forno 27 tenha sido deliciosa.</p>
        <div className="client-thank-you-info">
          <BellRing aria-hidden="true" />
          <p>Pagamento confirmado. Em breve nossa equipe virá até a mesa para os procedimentos finais.</p>
        </div>
        <div className="client-thank-you-social">
          <div>
            <small>Avalie-nos</small>
            <a href={feedbackUrl} target="_blank" rel="noreferrer" aria-label="Avaliar experiência na Forno 27"><Star aria-hidden="true" /></a>
          </div>
          <i aria-hidden="true" />
          <div>
            <small>Siga-nos nas redes</small>
            <nav aria-label="Redes sociais">
              <a href={import.meta.env.VITE_SOCIAL_URL || feedbackUrl} target="_blank" rel="noreferrer" aria-label="Acessar perfil da Forno 27"><ThumbsUp aria-hidden="true" /></a>
              <button type="button" aria-label="Compartilhar a Forno 27" onClick={() => void shareRestaurant()}><Share2 aria-hidden="true" /></button>
            </nav>
          </div>
        </div>
        <button type="button" className="client-secondary-action client-finish-session" onClick={onFinish}>Finalizar atendimento neste tablet</button>
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
