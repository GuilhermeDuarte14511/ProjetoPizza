import { zodResolver } from '@hookform/resolvers/zod'
import { Banknote, CreditCard, QrCode, UsersRound, UserRound } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { paymentSchema, type PaymentFormData } from '../../features/admin/formSchemas'
import { adminService } from '../../services/adminService'
import type { PaymentMethod } from '../../types/admin'
import { getUserErrorMessage } from '../../utils/errors'
import { currency, splitMoneyEqually } from '../../utils/money'
import { CurrencyInput } from '../ui/CurrencyInput'
import { FieldError } from '../ui/FieldError'
import { Modal } from '../ui/Modal'
import { useToast } from '../ui/toast'

type PaymentMode = 'single' | 'split'

interface SplitPaymentDraft {
  key: string
  payer: string
  paymentMethodId: string
  amount: number
  receivedAmount: number
  externalReference: string
}

export function PaymentDialog({
  billId,
  remainingAmount,
  requestedSplitCount,
  methods,
  onClose,
  onPaid,
}: {
  billId: string
  remainingAmount: number
  requestedSplitCount?: number
  methods: PaymentMethod[]
  onClose: () => void
  onPaid: (amount: number) => void
}) {
  const activeMethods = useMemo(() => methods.filter((item) => item.isActive), [methods])
  const defaultMethodId = activeMethods[0]?.id ?? ''
  const initialPeople = Math.min(50, Math.max(2, requestedSplitCount ?? 2))
  const [mode, setMode] = useState<PaymentMode>(requestedSplitCount ? 'split' : 'single')
  const [people, setPeople] = useState(initialPeople)
  const [splitPayments, setSplitPayments] = useState<SplitPaymentDraft[]>(() => createSplitPayments(remainingAmount, initialPeople, defaultMethodId))
  const [splitError, setSplitError] = useState('')
  const [saving, setSaving] = useState(false)
  const toast = useToast()
  const form = useForm<PaymentFormData>({
    resolver: zodResolver(paymentSchema),
    defaultValues: {
      paymentMethodId: defaultMethodId,
      amount: remainingAmount,
      receivedAmount: remainingAmount,
      externalReference: '',
    },
  })
  const methodId = useWatch({ control: form.control, name: 'paymentMethodId' })
  const amount = useWatch({ control: form.control, name: 'amount' })
  const receivedAmount = useWatch({ control: form.control, name: 'receivedAmount' })
  const method = methods.find((item) => item.id === methodId)
  const change = useMemo(() => Math.max(0, (receivedAmount ?? 0) - (amount ?? 0)), [amount, receivedAmount])

  async function submitSingle(draft: PaymentFormData) {
    if (draft.amount > remainingAmount) {
      form.setError('amount', { message: 'O valor não pode ultrapassar o saldo da conta.' }, { shouldFocus: true })
      return
    }
    if (method?.allowsChange && draft.receivedAmount < draft.amount) {
      form.setError('receivedAmount', { message: 'O valor recebido deve cobrir o pagamento.' }, { shouldFocus: true })
      return
    }
    if (method?.requiresExternalReference && !draft.externalReference) {
      form.setError('externalReference', { message: 'A referência é obrigatória para esta forma de pagamento.' }, { shouldFocus: true })
      return
    }

    setSaving(true)
    try {
      await adminService.recordPayment({ billId, ...draft, externalReference: draft.externalReference || undefined })
      toast.success('Pagamento registrado', `${currency.format(draft.amount)} recebido com sucesso.`)
      onPaid(draft.amount)
    } catch (error) {
      toast.error('Pagamento não confirmado', getUserErrorMessage(error))
    } finally {
      setSaving(false)
    }
  }

  async function submitSplit() {
    const validationMessage = validateSplitPayments(splitPayments, activeMethods)
    if (validationMessage) {
      setSplitError(validationMessage)
      return
    }

    setSaving(true)
    setSplitError('')
    try {
      await adminService.recordSplitPayment({
        billId,
        payments: splitPayments.map(({ payer, paymentMethodId, amount: splitAmount, receivedAmount: received, externalReference }) => ({
          payer: payer.trim(),
          paymentMethodId,
          amount: splitAmount,
          receivedAmount: received,
          externalReference: externalReference.trim() || undefined,
        })),
      })
      toast.success('Conta dividida e recebida', `${people} pagamentos foram registrados, totalizando ${currency.format(remainingAmount)}.`)
      onPaid(remainingAmount)
    } catch (error) {
      toast.error('Não foi possível receber a conta dividida', getUserErrorMessage(error))
    } finally {
      setSaving(false)
    }
  }

  function changePeople(value: number) {
    const nextPeople = Math.min(50, Math.max(2, Math.trunc(value || 2)))
    setPeople(nextPeople)
    setSplitPayments((current) => createSplitPayments(remainingAmount, nextPeople, defaultMethodId, current))
    setSplitError('')
  }

  function updateSplit(index: number, patch: Partial<SplitPaymentDraft>) {
    setSplitPayments((current) => current.map((item, itemIndex) => itemIndex === index ? { ...item, ...patch } : item))
    setSplitError('')
  }

  return (
    <Modal open title="Registrar pagamento" description={`Saldo da conta: ${currency.format(remainingAmount)}`} size="large" isBusy={saving} onClose={onClose}>
      {requestedSplitCount && <div className="payment-request-note" role="status">A mesa solicitou divisão entre <strong>{requestedSplitCount} pessoas</strong>.</div>}
      <div className="payment-mode-tabs" role="tablist" aria-label="Modo de pagamento">
        <button type="button" role="tab" aria-selected={mode === 'single'} className={mode === 'single' ? 'active' : ''} onClick={() => setMode('single')}><UserRound size={17} /> Pagamento único</button>
        <button type="button" role="tab" aria-selected={mode === 'split'} className={mode === 'split' ? 'active' : ''} onClick={() => setMode('split')}><UsersRound size={17} /> Dividir por pessoas</button>
      </div>

      {mode === 'single' ? (
        <form onSubmit={form.handleSubmit(submitSingle)} noValidate>
          <div className="payment-methods" role="group" aria-label="Forma de pagamento">{activeMethods.map((item) => <button type="button" key={item.id} aria-pressed={methodId === item.id} className={methodId === item.id ? 'active' : ''} onClick={() => { form.setValue('paymentMethodId', item.id, { shouldDirty: true }); form.setValue('receivedAmount', amount ?? 0) }}>{paymentMethodIcon(item.code)}<span>{item.name}</span></button>)}</div>
          <div className="form-grid two-columns">
            <label className="field-label">Valor do pagamento<Controller control={form.control} name="amount" render={({ field }) => <CurrencyInput name={field.name} value={field.value} onBlur={field.onBlur} getInputRef={field.ref} aria-invalid={Boolean(form.formState.errors.amount)} onCurrencyValueChange={(value) => { field.onChange(value); if (!method?.allowsChange) form.setValue('receivedAmount', value) }} />} /><FieldError message={form.formState.errors.amount?.message} /></label>
            <label className="field-label">Valor recebido<Controller control={form.control} name="receivedAmount" render={({ field }) => <CurrencyInput name={field.name} value={field.value} disabled={!method?.allowsChange} onBlur={field.onBlur} getInputRef={field.ref} aria-invalid={Boolean(form.formState.errors.receivedAmount)} onCurrencyValueChange={field.onChange} />} /><FieldError message={form.formState.errors.receivedAmount?.message} /></label>
            {method?.requiresExternalReference && <label className="field-label wide">Referência/autorização<input aria-invalid={Boolean(form.formState.errors.externalReference)} {...form.register('externalReference')} /><FieldError message={form.formState.errors.externalReference?.message} /></label>}
          </div>
          <div className="payment-total"><span>Troco</span><strong>{currency.format(change)}</strong></div>
          <PaymentFooter saving={saving} disabled={!methodId} onClose={onClose} label="Confirmar pagamento" />
        </form>
      ) : (
        <form onSubmit={(event) => { event.preventDefault(); void submitSplit() }} noValidate>
          <div className="split-payment-heading">
            <label className="field-label">Quantidade de pessoas<input type="number" min="2" max="50" value={people} onChange={(event) => changePeople(Number(event.target.value))} /></label>
            <div><span>Valor médio por pessoa</span><strong>{currency.format(remainingAmount / people)}</strong><small>Os centavos são distribuídos automaticamente.</small></div>
          </div>
          <div className="split-payment-list">
            {splitPayments.map((payment, index) => {
              const selectedMethod = activeMethods.find((item) => item.id === payment.paymentMethodId)
              const splitChange = Math.max(0, payment.receivedAmount - payment.amount)
              return (
                <article className="split-payment-card" key={payment.key}>
                  <div className="split-payment-person">
                    <span>{index + 1}</span>
                    <label className="field-label">Pessoa<input aria-label={`Nome da pessoa ${index + 1}`} value={payment.payer} maxLength={100} onChange={(event) => updateSplit(index, { payer: event.target.value })} /></label>
                    <strong>{currency.format(payment.amount)}</strong>
                  </div>
                  <div className="form-grid three-columns">
                    <label className="field-label">Como pagou<select aria-label={`Forma de pagamento de ${payment.payer}`} value={payment.paymentMethodId} onChange={(event) => { const nextMethod = activeMethods.find((item) => item.id === event.target.value); updateSplit(index, { paymentMethodId: event.target.value, receivedAmount: payment.amount, externalReference: nextMethod?.requiresExternalReference ? payment.externalReference : '' }) }}><option value="">Selecione</option>{activeMethods.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>
                    <label className="field-label">Valor recebido<CurrencyInput value={payment.receivedAmount} disabled={!selectedMethod?.allowsChange} aria-label={`Valor recebido de ${payment.payer}`} onCurrencyValueChange={(value) => updateSplit(index, { receivedAmount: value })} /></label>
                    {selectedMethod?.requiresExternalReference ? <label className="field-label">Referência<input value={payment.externalReference} maxLength={100} onChange={(event) => updateSplit(index, { externalReference: event.target.value })} /></label> : <div className="split-change"><span>Troco</span><strong>{currency.format(splitChange)}</strong></div>}
                  </div>
                </article>
              )
            })}
          </div>
          <FieldError message={splitError} />
          <div className="payment-total"><span>Total distribuído</span><strong>{currency.format(splitPayments.reduce((total, item) => total + item.amount, 0))}</strong></div>
          <PaymentFooter saving={saving} disabled={!splitPayments.length} onClose={onClose} label={`Confirmar ${people} pagamentos`} />
        </form>
      )}
    </Modal>
  )
}

function createSplitPayments(total: number, people: number, defaultMethodId: string, current: SplitPaymentDraft[] = []): SplitPaymentDraft[] {
  return splitMoneyEqually(total, people).map((amount, index) => ({
    key: current[index]?.key ?? crypto.randomUUID(),
    payer: current[index]?.payer ?? `Pessoa ${index + 1}`,
    paymentMethodId: current[index]?.paymentMethodId ?? defaultMethodId,
    amount,
    receivedAmount: current[index]?.receivedAmount === current[index]?.amount ? amount : (current[index]?.receivedAmount ?? amount),
    externalReference: current[index]?.externalReference ?? '',
  }))
}

function validateSplitPayments(payments: SplitPaymentDraft[], methods: PaymentMethod[]) {
  for (const [index, payment] of payments.entries()) {
    const label = payment.payer.trim() || `Pessoa ${index + 1}`
    const method = methods.find((item) => item.id === payment.paymentMethodId)
    if (!payment.payer.trim()) return `Informe o nome da pessoa ${index + 1}.`
    if (!method) return `Selecione como ${label} pagou.`
    if (payment.receivedAmount < payment.amount) return `O valor recebido de ${label} não cobre a parte da conta.`
    if (method.requiresExternalReference && !payment.externalReference.trim()) return `Informe a referência do pagamento de ${label}.`
  }
  return ''
}

function paymentMethodIcon(code: string) {
  if (code === 'CASH') return <Banknote />
  if (code === 'PIX') return <QrCode />
  return <CreditCard />
}

function PaymentFooter({ saving, disabled, onClose, label }: { saving: boolean; disabled: boolean; onClose: () => void; label: string }) {
  return <div className="modal-footer"><button type="button" className="secondary-button" disabled={saving} onClick={onClose}>Cancelar</button><button className="primary-button" disabled={saving || disabled} aria-busy={saving}>{saving ? 'Confirmando...' : label}</button></div>
}
