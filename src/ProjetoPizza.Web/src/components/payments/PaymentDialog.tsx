import { zodResolver } from '@hookform/resolvers/zod'
import { Banknote, CreditCard, QrCode } from 'lucide-react'
import { useMemo, useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { paymentSchema, type PaymentFormData } from '../../features/admin/formSchemas'
import { adminService } from '../../services/adminService'
import type { PaymentMethod } from '../../types/admin'
import { getUserErrorMessage } from '../../utils/errors'
import { FieldError } from '../ui/FieldError'
import { Modal } from '../ui/Modal'
import { useToast } from '../ui/toast'

const currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })

export function PaymentDialog({
  billId,
  remainingAmount,
  methods,
  onClose,
  onPaid,
}: {
  billId: string
  remainingAmount: number
  methods: PaymentMethod[]
  onClose: () => void
  onPaid: (amount: number) => void
}) {
  const [saving, setSaving] = useState(false)
  const toast = useToast()
  const form = useForm<PaymentFormData>({
    resolver: zodResolver(paymentSchema),
    defaultValues: {
      paymentMethodId: methods.find((item) => item.isActive)?.id ?? '',
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

  async function submit(draft: PaymentFormData) {
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

  return <Modal open title="Registrar pagamento" description={`Saldo da conta: ${currency.format(remainingAmount)}`} isBusy={saving} onClose={onClose}>
    <form onSubmit={form.handleSubmit(submit)} noValidate>
      <div className="payment-methods" role="group" aria-label="Forma de pagamento">{methods.filter((item) => item.isActive).map((item) => <button type="button" key={item.id} aria-pressed={methodId === item.id} className={methodId === item.id ? 'active' : ''} onClick={() => { form.setValue('paymentMethodId', item.id, { shouldDirty: true }); form.setValue('receivedAmount', amount ?? 0) }}>{item.code === 'CASH' ? <Banknote /> : item.code === 'PIX' ? <QrCode /> : <CreditCard />}<span>{item.name}</span></button>)}</div>
      <div className="form-grid two-columns">
        <label className="field-label">Valor do pagamento<input type="number" min=".01" max={remainingAmount} step=".01" aria-invalid={Boolean(form.formState.errors.amount)} {...form.register('amount', { valueAsNumber: true, onChange: (event) => { if (!method?.allowsChange) form.setValue('receivedAmount', Number(event.target.value)) } })} /><FieldError message={form.formState.errors.amount?.message} /></label>
        <label className="field-label">Valor recebido<input type="number" min={amount ?? 0} step=".01" disabled={!method?.allowsChange} aria-invalid={Boolean(form.formState.errors.receivedAmount)} {...form.register('receivedAmount', { valueAsNumber: true })} /><FieldError message={form.formState.errors.receivedAmount?.message} /></label>
        {method?.requiresExternalReference && <label className="field-label wide">Referência/autorização<input aria-invalid={Boolean(form.formState.errors.externalReference)} {...form.register('externalReference')} /><FieldError message={form.formState.errors.externalReference?.message} /></label>}
      </div>
      <div className="payment-total"><span>Troco</span><strong>{currency.format(change)}</strong></div>
      <div className="modal-footer"><button type="button" className="secondary-button" disabled={saving} onClick={onClose}>Cancelar</button><button className="primary-button" disabled={saving || !methodId} aria-busy={saving}>{saving ? 'Confirmando...' : 'Confirmar pagamento'}</button></div>
    </form>
  </Modal>
}
