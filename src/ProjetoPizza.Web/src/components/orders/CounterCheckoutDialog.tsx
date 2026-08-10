import { Banknote, CheckCircle2, CreditCard, QrCode, ReceiptText, ShieldCheck } from 'lucide-react'
import { useMemo, useState } from 'react'
import type { CounterPaymentDraft, PaymentMethod } from '../../types/admin'
import { currency } from '../../utils/money'
import { CurrencyInput } from '../ui/CurrencyInput'
import { Modal } from '../ui/Modal'

interface CounterCheckoutDialogProps {
  open: boolean
  orderTotal: number
  itemCount: number
  customerName: string
  methods: PaymentMethod[]
  saving: boolean
  onClose: () => void
  onConfirm: (payment: CounterPaymentDraft) => void
}

export function CounterCheckoutDialog({
  open,
  orderTotal,
  itemCount,
  customerName,
  methods,
  saving,
  onClose,
  onConfirm,
}: CounterCheckoutDialogProps) {
  const activeMethods = useMemo(() => methods.filter((method) => method.isActive), [methods])
  const [paymentMethodId, setPaymentMethodId] = useState(activeMethods[0]?.id ?? '')
  const [receivedAmount, setReceivedAmount] = useState(orderTotal)
  const [externalReference, setExternalReference] = useState('')
  const [error, setError] = useState('')
  const selectedMethod = activeMethods.find((method) => method.id === paymentMethodId)
  const changeAmount = Math.max(0, receivedAmount - orderTotal)

  function selectMethod(method: PaymentMethod) {
    setPaymentMethodId(method.id)
    setReceivedAmount(orderTotal)
    setExternalReference('')
    setError('')
  }

  function submit(event: React.FormEvent) {
    event.preventDefault()
    if (!selectedMethod) {
      setError('Selecione a forma de pagamento.')
      return
    }
    if (receivedAmount < orderTotal) {
      setError('O valor recebido deve cobrir o total do pedido.')
      return
    }
    if (!selectedMethod.allowsChange && receivedAmount !== orderTotal) {
      setError(`${selectedMethod.name} não permite registrar troco.`)
      return
    }
    if (selectedMethod.requiresExternalReference && !externalReference.trim()) {
      setError('Informe a referência da transação confirmada externamente.')
      return
    }

    onConfirm({
      paymentMethodId: selectedMethod.id,
      receivedAmount,
      externalReference: externalReference.trim() || undefined,
    })
  }

  return (
    <Modal
      open={open}
      title="Revisar e receber pedido"
      description="Confirme o pagamento antes de liberar as impressões do cliente e da cozinha."
      size="large"
      isBusy={saving}
      onClose={onClose}
    >
      <form className="counter-checkout" onSubmit={submit} noValidate>
        <div className="counter-checkout-body">
          <section className="checkout-payment-panel" aria-labelledby="checkout-payment-title">
            <div className="checkout-section-heading">
              <span><CreditCard size={18} /></span>
              <div><h3 id="checkout-payment-title">Forma de pagamento</h3><p>O sistema apenas registra uma transação já confirmada pelo operador.</p></div>
            </div>
            <div className="checkout-payment-methods" role="radiogroup" aria-label="Forma de pagamento do pedido">
              {activeMethods.map((method) => (
                <button
                  key={method.id}
                  type="button"
                  role="radio"
                  aria-checked={method.id === paymentMethodId}
                  className={method.id === paymentMethodId ? 'selected' : ''}
                  onClick={() => selectMethod(method)}
                >
                  {paymentIcon(method.code)}
                  <strong>{method.name}</strong>
                  {method.id === paymentMethodId && <CheckCircle2 className="checkout-method-check" size={17} aria-hidden="true" />}
                </button>
              ))}
            </div>

            <div className="checkout-values-grid">
              <label className="field-label">Total do pedido<CurrencyInput value={orderTotal} disabled aria-label="Total do pedido" onCurrencyValueChange={() => undefined} /></label>
              <label className="field-label">Valor recebido<CurrencyInput value={receivedAmount} disabled={!selectedMethod?.allowsChange} aria-label="Valor recebido" onCurrencyValueChange={(value) => { setReceivedAmount(value); setError('') }} /></label>
            </div>
            {selectedMethod?.requiresExternalReference && (
              <label className="field-label checkout-reference">Referência da transação
                <input value={externalReference} maxLength={100} onChange={(event) => { setExternalReference(event.target.value); setError('') }} placeholder="Código confirmado no Pix ou terminal" />
                <small>Esta tela não autoriza o pagamento; apenas registra a confirmação feita fora do sistema.</small>
              </label>
            )}
            <div className="checkout-change" aria-live="polite"><span>Troco</span><strong>{currency.format(changeAmount)}</strong></div>
            {error && <div className="checkout-error" role="alert">{error}</div>}
          </section>

          <aside className="checkout-order-card" aria-label="Resumo final do pedido">
            <div className="checkout-order-icon"><ReceiptText size={25} /></div>
            <span>Pedido para retirada</span>
            <h3>{customerName}</h3>
            <p>{itemCount} {itemCount === 1 ? 'item' : 'itens'} · retirada no balcão</p>
            <div className="checkout-order-total"><small>Total a receber</small><strong>{currency.format(orderTotal)}</strong></div>
            <div className="checkout-security-note"><ShieldCheck size={18} /><span>Valores e disponibilidade serão conferidos novamente pelo servidor.</span></div>
          </aside>
        </div>
        <footer className="modal-footer counter-checkout-footer">
          <button type="button" className="secondary-button" disabled={saving} onClick={onClose}>Voltar ao pedido</button>
          <button className="primary-button" disabled={saving || !activeMethods.length} aria-busy={saving}>{saving ? 'Confirmando venda...' : `Confirmar pagamento de ${currency.format(orderTotal)}`}</button>
        </footer>
      </form>
    </Modal>
  )
}

function paymentIcon(code: string) {
  if (code === 'CASH') return <Banknote size={23} aria-hidden="true" />
  if (code === 'PIX') return <QrCode size={23} aria-hidden="true" />
  return <CreditCard size={23} aria-hidden="true" />
}
