import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowDownToLine, ArrowUpFromLine, Calculator, LockKeyhole, Plus, WalletCards } from 'lucide-react'
import { useState } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { CurrencyInput } from '../components/ui/CurrencyInput'
import { ConfirmDialog } from '../components/ui/ConfirmDialog'
import { FieldError } from '../components/ui/FieldError'
import { Modal } from '../components/ui/Modal'
import { PageHeader } from '../components/ui/PageHeader'
import { useToast } from '../components/ui/toast'
import { cashCloseSchema, cashMovementSchema, type CashCloseFormData, type CashMovementFormData } from '../features/admin/formSchemas'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { queryKeys } from '../lib/queryKeys'
import { adminService } from '../services/adminService'
import { hasPermission } from '../services/authSession'
import { getUserErrorMessage } from '../utils/errors'
import { translateEnum } from '../utils/presentation'

const currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })

export function CashierPage() {
  const { data: shift, setData: setShift } = useAdminQuery(queryKeys.cashShift, adminService.cashShift)
  const [isMovementOpen, setMovementOpen] = useState(false)
  const [isCloseConfirmationOpen, setCloseConfirmationOpen] = useState(false)
  const [savingMovement, setSavingMovement] = useState(false)
  const [closing, setClosing] = useState(false)
  const toast = useToast()
  const movementForm = useForm<CashMovementFormData>({
    resolver: zodResolver(cashMovementSchema),
    defaultValues: { type: 'Supply', amount: 0, description: '', reason: '' },
  })
  const closeForm = useForm<CashCloseFormData>({
    resolver: zodResolver(cashCloseSchema),
    defaultValues: { countedCashAmount: shift?.expectedCashAmount ?? 0, notes: '' },
  })
  const counted = useWatch({ control: closeForm.control, name: 'countedCashAmount' }) ?? 0

  async function addMovement(draft: CashMovementFormData) {
    if (!shift) return
    setSavingMovement(true)
    try {
      await adminService.registerCashMovement(draft)
      const signed = draft.type === 'Supply' ? draft.amount : -draft.amount
      setShift({ ...shift, expectedCashAmount: shift.expectedCashAmount + signed, movements: [{ id: crypto.randomUUID(), ...draft, createdAt: new Date().toISOString() }, ...shift.movements] })
      movementForm.reset()
      setMovementOpen(false)
      toast.success('Movimentação registrada', `${translateEnum(draft.type)} de ${currency.format(draft.amount)} salva.`)
    } catch (error) {
      toast.error('Não foi possível registrar a movimentação', getUserErrorMessage(error))
    } finally {
      setSavingMovement(false)
    }
  }

  async function closeShift() {
    if (!shift) return
    const draft = closeForm.getValues()
    setClosing(true)
    try {
      await adminService.closeCashShift(draft.countedCashAmount, draft.notes)
      setShift({ ...shift, status: 'Closed', countedCashAmount: draft.countedCashAmount, differenceAmount: draft.countedCashAmount - shift.expectedCashAmount })
      setCloseConfirmationOpen(false)
      toast.success('Caixa fechado', 'A conferência final foi registrada com sucesso.')
    } catch (error) {
      toast.error('Não foi possível fechar o caixa', getUserErrorMessage(error))
    } finally {
      setClosing(false)
    }
  }

  if (!shift) return <><PageHeader title="Caixa" description="Nenhum turno de caixa está aberto." /><div className="empty-state"><WalletCards size={34} /><h2>Caixa fechado</h2><p>A abertura de um novo turno será disponibilizada na próxima etapa operacional.</p></div></>

  return (
    <>
      <PageHeader title="Fechamento de caixa" description={`${shift.register} · Operador ${shift.operator}`} actions={<span className={`status-pill ${shift.status === 'Open' ? 'success' : 'neutral'}`}>{shift.status === 'Open' ? 'Caixa aberto' : 'Caixa fechado'}</span>} />
      <section className="cash-metrics">
        <article><span>Fundo de abertura</span><strong>{currency.format(shift.openingAmount)}</strong></article>
        <article><span>Saldo esperado</span><strong>{currency.format(shift.expectedCashAmount)}</strong></article>
        <article><span>Movimentações</span><strong>{shift.movements.length}</strong></article>
        <article><span>Diferença</span><strong>{currency.format((shift.countedCashAmount ?? counted) - shift.expectedCashAmount)}</strong></article>
      </section>
      <section className="detail-layout">
        <div className="detail-main">
          <article className="surface-card">
            <div className="card-heading"><div><h2>Movimentações do turno</h2><p>Entradas, sangrias e vendas registradas.</p></div>{shift.status === 'Open' && hasPermission('operations:write') && <button className="secondary-button" onClick={() => setMovementOpen(true)}><Plus size={16} /> Novo movimento</button>}</div>
            <div className="data-list">{shift.movements.map((movement) => <div className="data-row" key={movement.id}><span className={`movement-icon ${movement.type === 'Withdrawal' ? 'out' : 'in'}`}>{movement.type === 'Withdrawal' ? <ArrowUpFromLine size={16} /> : <ArrowDownToLine size={16} />}</span><span><strong>{movement.description}</strong><small>{new Date(movement.createdAt).toLocaleString('pt-BR')} · {translateEnum(movement.type)}</small></span><strong>{currency.format(movement.amount)}</strong></div>)}</div>
          </article>
        </div>
        <aside className="detail-sidebar">
          <form className="surface-card closing-card" onSubmit={closeForm.handleSubmit(() => setCloseConfirmationOpen(true))} noValidate>
            <Calculator size={24} />
            <h2>Conferência final</h2>
            <p>Informe o valor contado fisicamente no caixa.</p>
            <label className="field-label">Valor contado<Controller control={closeForm.control} name="countedCashAmount" render={({ field }) => <CurrencyInput name={field.name} value={field.value} disabled={shift.status !== 'Open'} onBlur={field.onBlur} getInputRef={field.ref} aria-invalid={Boolean(closeForm.formState.errors.countedCashAmount)} onCurrencyValueChange={field.onChange} />} /><FieldError message={closeForm.formState.errors.countedCashAmount?.message} /></label>
            <div className="summary-line"><span>Esperado</span><strong>{currency.format(shift.expectedCashAmount)}</strong></div>
            <div className="summary-line"><span>Diferença</span><strong>{currency.format(counted - shift.expectedCashAmount)}</strong></div>
            <label className="field-label">Observações<textarea disabled={shift.status !== 'Open'} {...closeForm.register('notes')} /><FieldError message={closeForm.formState.errors.notes?.message} /></label>
            {hasPermission('operations:write') && <button className="primary-button full" disabled={shift.status !== 'Open' || closing} aria-busy={closing}><LockKeyhole size={16} /> {closing ? 'Fechando...' : 'Fechar caixa'}</button>}
          </form>
        </aside>
      </section>
      {isMovementOpen && <Modal open title="Novo movimento" description="Registre um suprimento ou uma sangria manual." isBusy={savingMovement} onClose={() => setMovementOpen(false)}>
        <form onSubmit={movementForm.handleSubmit(addMovement)} noValidate>
          <div className="modal-body"><div className="form-grid two-columns">
            <label className="field-label">Tipo<select {...movementForm.register('type')}><option value="Supply">Suprimento</option><option value="Withdrawal">Sangria</option></select></label>
            <label className="field-label">Valor<Controller control={movementForm.control} name="amount" render={({ field }) => <CurrencyInput name={field.name} value={field.value} onBlur={field.onBlur} getInputRef={field.ref} aria-invalid={Boolean(movementForm.formState.errors.amount)} onCurrencyValueChange={field.onChange} />} /><FieldError message={movementForm.formState.errors.amount?.message} /></label>
            <label className="field-label wide">Descrição<input aria-invalid={Boolean(movementForm.formState.errors.description)} {...movementForm.register('description')} /><FieldError message={movementForm.formState.errors.description?.message} /></label>
            <label className="field-label wide">Motivo<input aria-invalid={Boolean(movementForm.formState.errors.reason)} {...movementForm.register('reason')} /><FieldError message={movementForm.formState.errors.reason?.message} /></label>
          </div></div>
          <div className="modal-footer"><button type="button" className="secondary-button" disabled={savingMovement} onClick={() => setMovementOpen(false)}>Cancelar</button><button className="primary-button" disabled={savingMovement} aria-busy={savingMovement}>{savingMovement ? 'Registrando...' : 'Registrar movimento'}</button></div>
        </form>
      </Modal>}
      <ConfirmDialog open={isCloseConfirmationOpen} title="Confirmar fechamento do caixa?" description={`O saldo contado é ${currency.format(counted)}. Esta ação encerra o turno e não pode ser desfeita pela interface.`} confirmLabel="Fechar caixa" tone="danger" busy={closing} onOpenChange={setCloseConfirmationOpen} onConfirm={() => void closeShift()} />
    </>
  )
}
