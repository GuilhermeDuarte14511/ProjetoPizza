import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowLeft, Cake, MapPin, Minus, PackageCheck, Phone, Plus, Search, ShoppingCart, Trash2, Truck, UserPlus } from 'lucide-react'
import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useLocation } from 'wouter'
import { FieldError } from '../components/ui/FieldError'
import { CurrencyInput } from '../components/ui/CurrencyInput'
import { Modal } from '../components/ui/Modal'
import { OrderReceiptDialog } from '../components/orders/OrderReceiptDialog'
import { CounterCheckoutDialog } from '../components/orders/CounterCheckoutDialog'
import { PageHeader } from '../components/ui/PageHeader'
import { useToast } from '../components/ui/toast'
import { ViewTransitionLink } from '../components/ui/ViewTransitionLink'
import { PizzaBuilder, type PizzaBuilderResult } from '../features/client/PizzaBuilder'
import { getProductImage } from '../features/client/clientPresentation'
import { customerSchema, type CustomerFormData } from '../features/admin/formSchemas'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { createUuid } from '../lib/uuid'
import { queryKeys } from '../lib/queryKeys'
import { adminService } from '../services/adminService'
import type { CounterPaymentDraft, CreateAdministrativeOrder, OrderReceipt } from '../types/admin'
import type { ClientCartItem, ClientProduct } from '../types/client'
import { getUserErrorMessage } from '../utils/errors'

const currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })
const emptyCustomer: CustomerFormData = { name: '', phone: '', birthDate: '', isActive: true }

export function NewOrderPage() {
  const [, navigate] = useLocation()
  const toast = useToast()
  const { data: orderCatalog } = useAdminQuery(queryKeys.orderCatalog, adminService.orderCatalog)
  const { data: customers, setData: setCustomers } = useAdminQuery(queryKeys.customers, adminService.customers)
  const { data: paymentMethods } = useAdminQuery(queryKeys.paymentMethods, adminService.paymentMethods)
  const [requestId] = useState(createUuid)
  const [fulfillment, setFulfillment] = useState<'Pickup' | 'Delivery'>('Pickup')
  const [customerId, setCustomerId] = useState('')
  const [customerSearch, setCustomerSearch] = useState('')
  const [productSearch, setProductSearch] = useState('')
  const [categoryId, setCategoryId] = useState('all')
  const [deliveryAddress, setDeliveryAddress] = useState('')
  const [notes, setNotes] = useState('')
  const [discountAmount, setDiscountAmount] = useState(0)
  const [cart, setCart] = useState<ClientCartItem[]>([])
  const [builderProduct, setBuilderProduct] = useState<ClientProduct>()
  const [saving, setSaving] = useState(false)
  const [customerModal, setCustomerModal] = useState(false)
  const [savingCustomer, setSavingCustomer] = useState(false)
  const [createdReceipt, setCreatedReceipt] = useState<OrderReceipt>()
  const [checkoutOpen, setCheckoutOpen] = useState(false)
  const customerForm = useForm<CustomerFormData>({ resolver: zodResolver(customerSchema), defaultValues: emptyCustomer })
  const activeCustomers = customers.filter((customer) => customer.isActive)
  const customerMatches = useMemo(() => {
    const text = customerSearch.toLocaleLowerCase('pt-BR')
    const digits = customerSearch.replace(/\D/g, '')
    return activeCustomers.filter((customer) =>
      customer.name.toLocaleLowerCase('pt-BR').includes(text) || (digits && customer.phone.includes(digits))).slice(0, 8)
  }, [activeCustomers, customerSearch])
  const products = useMemo(() => orderCatalog.catalog.products.filter((product) =>
    (categoryId === 'all' || product.categoryId === categoryId) &&
    product.name.toLocaleLowerCase('pt-BR').includes(productSearch.toLocaleLowerCase('pt-BR'))),
  [categoryId, orderCatalog.catalog.products, productSearch])
  const subtotal = cart.reduce((total, item) => total + item.unitPrice * item.quantity, 0)
  const deliveryFee = fulfillment === 'Delivery' ? orderCatalog.defaultDeliveryFee : 0
  const total = Math.max(0, subtotal + deliveryFee - discountAmount)
  const selectedCustomer = customers.find((customer) => customer.id === customerId)

  function addProduct(product: ClientProduct) {
    if (product.productType === 'Pizza') {
      setBuilderProduct(product)
      return
    }
    setCart((current) => {
      const existing = current.find((item) => item.productId === product.id && !item.pizza)
      return existing
        ? current.map((item) => item.key === existing.key ? { ...item, quantity: Math.min(20, item.quantity + 1) } : item)
        : [...current, { key: createUuid(), productId: product.id, name: product.name, quantity: 1, unitPrice: product.price, imageUrl: getProductImage(product) }]
    })
  }

  function addPizza(result: PizzaBuilderResult) {
    setCart((current) => [...current, { key: createUuid(), ...result }])
    setBuilderProduct(undefined)
  }

  function changeQuantity(key: string, quantity: number) {
    setCart((current) => quantity <= 0
      ? current.filter((item) => item.key !== key)
      : current.map((item) => item.key === key ? { ...item, quantity: Math.min(20, quantity) } : item))
  }

  function openCustomerModal() {
    customerForm.reset({ ...emptyCustomer, phone: customerSearch.replace(/\D/g, '') })
    setCustomerModal(true)
  }

  async function saveCustomer(draft: CustomerFormData) {
    setSavingCustomer(true)
    try {
      const saved = await adminService.saveCustomer(draft)
      setCustomers((current) => [...current, saved].sort((a, b) => a.name.localeCompare(b.name, 'pt-BR')))
      setCustomerId(saved.id)
      setCustomerSearch(saved.name)
      setCustomerModal(false)
      toast.success('Cliente cadastrado', `${saved.name} foi selecionado para o pedido.`)
    } catch (error) {
      toast.error('Não foi possível salvar o cliente', getUserErrorMessage(error))
    } finally {
      setSavingCustomer(false)
    }
  }

  function validateOrder() {
    if (!selectedCustomer) {
      toast.error('Selecione o cliente', 'Busque pelo nome ou telefone antes de criar o pedido.')
      return false
    }
    if (!cart.length) {
      toast.error('Pedido vazio', 'Adicione ao menos um produto ao pedido.')
      return false
    }
    if (fulfillment === 'Delivery' && !deliveryAddress.trim()) {
      toast.error('Informe o endereço', 'O endereço é obrigatório para calcular e registrar a entrega.')
      return false
    }
    if (discountAmount > subtotal + deliveryFee) {
      toast.error('Desconto inválido', 'O desconto não pode ultrapassar o valor do pedido.')
      return false
    }
    if (total <= 0) {
      toast.error('Total inválido', 'O pedido de balcão precisa ter um valor positivo para registrar o pagamento.')
      return false
    }
    return true
  }

  function buildCommand(): CreateAdministrativeOrder {
    return {
      requestId,
      customerId,
      fulfillment,
      deliveryAddress: fulfillment === 'Delivery' ? deliveryAddress.trim() : undefined,
      discountAmount,
      notes: notes.trim() || undefined,
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
  }

  function reviewAndReceive() {
    if (!validateOrder()) return
    if (!paymentMethods.some((method) => method.isActive)) {
      toast.error('Forma de pagamento indisponível', 'Cadastre e ative ao menos uma forma de pagamento antes de concluir a venda.')
      return
    }
    setCheckoutOpen(true)
  }

  async function submit(payment?: CounterPaymentDraft) {
    if (!validateOrder()) return
    const command = buildCommand()

    setSaving(true)
    try {
      const created = payment
        ? await adminService.checkoutCounterOrder({ order: command, payment })
        : await adminService.createOrder(command)
      setCheckoutOpen(false)
      toast.success(payment ? 'Venda concluída' : 'Pedido criado', payment
        ? `Pagamento do pedido #${created.number} registrado. Escolha as impressões.`
        : `Pedido #${created.number} enviado para a produção.`)
      setCreatedReceipt(created.receipt)
    } catch (error) {
      toast.error('Não foi possível criar o pedido', getUserErrorMessage(error))
    } finally {
      setSaving(false)
    }
  }

  async function printCustomerReceipt() {
    if (!createdReceipt) return
    try {
      await adminService.printCustomerReceipt(createdReceipt.id)
      toast.success('Comprovante na fila', `O comprovante não fiscal do pedido #${createdReceipt.number} será impresso.`)
    } catch (error) {
      toast.error('Falha ao imprimir comprovante', getUserErrorMessage(error))
      throw error
    }
  }

  async function printKitchenCommand() {
    if (!createdReceipt) return
    try {
      const result = await adminService.printKitchenCommand(createdReceipt.id)
      toast.success('Comanda na fila', `${result.jobIds.length} ${result.jobIds.length === 1 ? 'comanda foi enviada' : 'comandas foram enviadas'} para a cozinha.`)
    } catch (error) {
      toast.error('Falha ao imprimir comanda', getUserErrorMessage(error))
      throw error
    }
  }

  return (
    <>
      <ViewTransitionLink className="back-link" href="/admin/orders"><ArrowLeft size={16} /> Voltar para pedidos</ViewTransitionLink>
      <PageHeader title="Novo pedido" description="Registre pedidos recebidos no balcão ou por telefone." />

      <section className="admin-order-layout">
        <div className="admin-order-main">
          <article className="surface-card order-customer-card">
            <div className="card-heading"><div><h2>1. Cliente e atendimento</h2><p>Localize pelo telefone para evitar cadastros duplicados.</p></div><button className="secondary-button" onClick={openCustomerModal}><UserPlus size={16} /> Novo cliente</button></div>
            <div className="customer-picker">
              <label className="field-label">Buscar cliente<div className="order-search-field"><Search size={17} /><input value={customerSearch} onChange={(event) => { setCustomerSearch(event.target.value); setCustomerId('') }} placeholder="Nome ou telefone..." /></div></label>
              {customerSearch && !selectedCustomer && <div className="customer-picker-results">
                {customerMatches.map((customer) => <button type="button" key={customer.id} onClick={() => { setCustomerId(customer.id); setCustomerSearch(customer.name) }}><strong>{customer.name}</strong><span><Phone size={13} /> {customer.phone}</span></button>)}
                {!customerMatches.length && <button type="button" className="create-customer-result" onClick={openCustomerModal}><UserPlus size={15} /> Cadastrar “{customerSearch}”</button>}
              </div>}
              <div className="fulfillment-choice" role="radiogroup" aria-label="Tipo de atendimento">
                <button type="button" role="radio" aria-checked={fulfillment === 'Pickup'} className={fulfillment === 'Pickup' ? 'selected' : ''} onClick={() => setFulfillment('Pickup')}><PackageCheck /><span><strong>Retirada</strong><small>Cliente busca no balcão</small></span></button>
                <button type="button" role="radio" aria-checked={fulfillment === 'Delivery'} className={fulfillment === 'Delivery' ? 'selected' : ''} onClick={() => setFulfillment('Delivery')}><Truck /><span><strong>Entrega</strong><small>Pedido recebido por telefone</small></span></button>
              </div>
            </div>
            {selectedCustomer && <div className="selected-customer"><strong>{selectedCustomer.name}</strong><span><Phone size={14} /> {selectedCustomer.phone}</span><span><Cake size={14} /> {new Date(`${selectedCustomer.birthDate}T00:00:00`).toLocaleDateString('pt-BR')}</span></div>}
            {fulfillment === 'Delivery' && <label className="field-label delivery-address"><MapPin size={15} /> Endereço completo<textarea value={deliveryAddress} maxLength={500} onChange={(event) => setDeliveryAddress(event.target.value)} placeholder="Rua, número, bairro, complemento e referência" /></label>}
          </article>

          <article className="surface-card order-product-picker">
            <div className="card-heading"><div><h2>2. Itens do pedido</h2><p>Escolha produtos disponíveis no cardápio atual.</p></div></div>
            <div className="order-catalog-toolbar">
              <div className="order-search-field"><Search size={17} /><input aria-label="Buscar produto" value={productSearch} onChange={(event) => setProductSearch(event.target.value)} placeholder="Buscar produto..." /></div>
              <div className="order-category-tabs"><button className={categoryId === 'all' ? 'active' : ''} onClick={() => setCategoryId('all')}>Todos</button>{orderCatalog.catalog.categories.map((category) => <button className={categoryId === category.id ? 'active' : ''} key={category.id} onClick={() => setCategoryId(category.id)}>{category.name}</button>)}</div>
            </div>
            <div className="admin-order-products">
              {products.map((product) => <article key={product.id}>
                <img src={getProductImage(product)} alt="" />
                <div><strong>{product.name}</strong><small>{product.description || (product.productType === 'Pizza' ? 'Escolha tamanho, sabores e borda.' : 'Disponível para o pedido.')}</small><span>{currency.format(product.price)}</span></div>
                <button className="secondary-button" onClick={() => addProduct(product)}>{product.productType === 'Pizza' ? 'Montar pizza' : <><Plus size={15} /> Adicionar</>}</button>
              </article>)}
            </div>
          </article>
        </div>

        <aside className="surface-card admin-order-summary">
          <header><ShoppingCart /><div><h2>Resumo do pedido</h2><small>{cart.reduce((sum, item) => sum + item.quantity, 0)} itens</small></div></header>
          <div className="admin-order-cart">
            {cart.map((item) => <article key={item.key}>
              <div><strong>{item.name}</strong>{item.pizza && <small>{item.pizza.flavorNames.join(' · ')}</small>}<span>{currency.format(item.unitPrice * item.quantity)}</span></div>
              <footer><button onClick={() => changeQuantity(item.key, item.quantity - 1)} aria-label={`Diminuir ${item.name}`}><Minus size={14} /></button><strong>{item.quantity}</strong><button onClick={() => changeQuantity(item.key, item.quantity + 1)} aria-label={`Aumentar ${item.name}`}><Plus size={14} /></button><button className="remove" onClick={() => changeQuantity(item.key, 0)} aria-label={`Remover ${item.name}`}><Trash2 size={14} /></button></footer>
            </article>)}
            {!cart.length && <div className="empty-inline">Adicione produtos para montar o pedido.</div>}
          </div>
          <label className="field-label">Observações<textarea value={notes} maxLength={1000} onChange={(event) => setNotes(event.target.value)} placeholder="Ex.: retirar no balcão às 20h" /></label>
          <label className="field-label order-discount-field">Desconto<CurrencyInput value={discountAmount} onCurrencyValueChange={setDiscountAmount} /></label>
          <div className="order-summary-lines"><div><span>Subtotal</span><strong>{currency.format(subtotal)}</strong></div>{fulfillment === 'Delivery' && <div><span>Taxa de entrega</span><strong>{currency.format(deliveryFee)}</strong></div>}<div><span>Desconto</span><strong>- {currency.format(discountAmount)}</strong></div><div className="total"><span>Total</span><strong>{currency.format(total)}</strong></div></div>
          <button className="primary-button full" disabled={saving || !cart.length || !selectedCustomer} onClick={() => fulfillment === 'Pickup' ? reviewAndReceive() : void submit()}>{saving ? 'Confirmando...' : fulfillment === 'Pickup' ? 'Revisar e receber pagamento' : 'Confirmar e enviar para produção'}</button>
          <small className="order-price-note">Preço, disponibilidade e taxa são confirmados novamente pelo servidor.</small>
        </aside>
      </section>

      {builderProduct && <PizzaBuilder product={builderProduct} catalog={orderCatalog.catalog.pizza} onCancel={() => setBuilderProduct(undefined)} onAdd={addPizza} />}
      {checkoutOpen && <CounterCheckoutDialog open orderTotal={total} itemCount={cart.reduce((sum, item) => sum + item.quantity, 0)} customerName={selectedCustomer?.name ?? 'Cliente'} methods={paymentMethods} saving={saving} onClose={() => setCheckoutOpen(false)} onConfirm={(payment) => void submit(payment)} />}
      <OrderReceiptDialog receipt={createdReceipt} onPrintCustomerReceipt={createdReceipt?.fulfillment === 'Pickup' ? printCustomerReceipt : undefined} onPrintKitchenCommand={createdReceipt?.fulfillment === 'Pickup' ? printKitchenCommand : undefined} onClose={() => { const number = createdReceipt?.number; setCreatedReceipt(undefined); navigate(number ? `/admin/orders?search=${number}` : '/admin/orders') }} />
      {customerModal && <Modal open title="Novo cliente" description="Cadastro rápido durante o atendimento." isBusy={savingCustomer} onClose={() => setCustomerModal(false)}>
        <form onSubmit={customerForm.handleSubmit(saveCustomer)} noValidate>
          <div className="modal-body"><div className="form-grid two-columns">
            <label className="field-label wide">Nome completo<input autoFocus aria-invalid={Boolean(customerForm.formState.errors.name)} {...customerForm.register('name')} /><FieldError message={customerForm.formState.errors.name?.message} /></label>
            <label className="field-label">Telefone<input inputMode="tel" aria-invalid={Boolean(customerForm.formState.errors.phone)} {...customerForm.register('phone')} /><FieldError message={customerForm.formState.errors.phone?.message} /></label>
            <label className="field-label">Data de nascimento<input type="date" aria-invalid={Boolean(customerForm.formState.errors.birthDate)} {...customerForm.register('birthDate')} /><FieldError message={customerForm.formState.errors.birthDate?.message} /></label>
          </div></div>
          <div className="modal-footer"><button type="button" className="secondary-button" disabled={savingCustomer} onClick={() => setCustomerModal(false)}>Cancelar</button><button className="primary-button" disabled={savingCustomer}>{savingCustomer ? 'Cadastrando...' : 'Cadastrar e selecionar'}</button></div>
        </form>
      </Modal>}
    </>
  )
}
