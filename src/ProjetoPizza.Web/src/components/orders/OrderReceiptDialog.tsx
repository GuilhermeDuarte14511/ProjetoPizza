import { Check, ChefHat, CircleAlert, LoaderCircle, Printer, ReceiptText } from 'lucide-react'
import { useState } from 'react'
import type { OrderReceipt } from '../../types/admin'
import { formatPhone } from '../../utils/phone'
import { Modal } from '../ui/Modal'

const currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })
type PrintState = 'idle' | 'printing' | 'queued' | 'error'

interface OrderReceiptDialogProps {
  receipt?: OrderReceipt
  context?: 'confirmation' | 'preview'
  onClose: () => void
  onPrintCustomerReceipt?: () => Promise<void>
  onPrintKitchenCommand?: () => Promise<void>
}

export function OrderReceiptDialog({ receipt, context = 'confirmation', onClose, onPrintCustomerReceipt, onPrintKitchenCommand }: OrderReceiptDialogProps) {
  const [customerState, setCustomerState] = useState<PrintState>('idle')
  const [kitchenState, setKitchenState] = useState<PrintState>('idle')
  if (!receipt) return null

  async function printCustomerReceipt() {
    if (!onPrintCustomerReceipt) {
      window.print()
      return
    }
    setCustomerState('printing')
    try {
      await onPrintCustomerReceipt()
      setCustomerState('queued')
    } catch {
      setCustomerState('error')
    }
  }

  async function printKitchenCommand() {
    if (!onPrintKitchenCommand) return
    setKitchenState('printing')
    try {
      await onPrintKitchenCommand()
      setKitchenState('queued')
    } catch {
      setKitchenState('error')
    }
  }

  const isPrinting = customerState === 'printing' || kitchenState === 'printing'
  return (
    <Modal open title={context === 'preview' ? `Comprovante do pedido #${receipt.number}` : `Pedido #${receipt.number} confirmado`} description={context === 'preview' ? 'Confira os itens e imprima pelo navegador.' : 'Pagamento registrado. Escolha os documentos que devem seguir para cada destino.'} size="large" isBusy={isPrinting} onClose={onClose}>
      {context === 'confirmation' && <div className="order-print-success" role="status"><span><Check size={20} /></span><div><strong>Venda concluída com sucesso</strong><p>O comprovante não possui valor fiscal e a comanda da cozinha não exibe preços.</p></div></div>}
      <div className="modal-body order-print-workspace">
        <section className="receipt-preview-column" aria-labelledby="customer-receipt-title">
          <div className="print-document-heading"><span><ReceiptText size={18} /></span><div><small>Documento 1</small><h3 id="customer-receipt-title">Comprovante do cliente</h3></div></div>
          <article className="thermal-receipt thermal-print-area" aria-label={`Comprovante não fiscal do pedido ${receipt.number}`}>
            <header><strong>FORNO 27</strong><span>PIZZERIA</span><b>COMPROVANTE NÃO FISCAL</b></header>
            <div className="receipt-divider" />
            <dl className="receipt-meta">
              <div><dt>Pedido</dt><dd>#{receipt.number}</dd></div>
              <div><dt>Data</dt><dd>{new Date(receipt.placedAt).toLocaleString('pt-BR')}</dd></div>
              <div><dt>Cliente</dt><dd>{receipt.customerName}</dd></div>
              {receipt.customerPhone && <div><dt>Telefone</dt><dd>{formatPhone(receipt.customerPhone)}</dd></div>}
              <div><dt>Atendimento</dt><dd>{formatFulfillment(receipt.fulfillment)}</dd></div>
              {receipt.deliveryAddress && <div className="wide"><dt>Endereço</dt><dd>{receipt.deliveryAddress}</dd></div>}
            </dl>
            <div className="receipt-divider" />
            <section className="receipt-items">
              {receipt.items.map((item) => <article key={item.id}><div className="receipt-item-title"><strong>{item.quantity}x {item.name}</strong><b>{currency.format(item.totalPrice)}</b></div><small>Unitário: {currency.format(item.unitPrice)}</small>{item.details.map((detail) => <span key={detail}>• {detail}</span>)}{item.notes && <p><strong>OBS:</strong> {item.notes}</p>}</article>)}
            </section>
            {receipt.notes && <div className="receipt-order-notes"><strong>OBSERVAÇÕES DO PEDIDO</strong><p>{receipt.notes}</p></div>}
            <div className="receipt-divider" />
            <dl className="receipt-totals">
              <div><dt>Subtotal</dt><dd>{currency.format(receipt.subtotal)}</dd></div>
              {receipt.deliveryFee > 0 && <div><dt>Taxa de entrega</dt><dd>{currency.format(receipt.deliveryFee)}</dd></div>}
              <div><dt>Desconto</dt><dd>- {currency.format(receipt.discount)}</dd></div>
              <div className="grand-total"><dt>TOTAL</dt><dd>{currency.format(receipt.total)}</dd></div>
            </dl>
            {receipt.payments.length > 0 && <><div className="receipt-divider" /><dl className="receipt-payments">{receipt.payments.map((payment, index) => <div key={`${payment.method}-${index}`}><dt>{payment.method}</dt><dd>{currency.format(payment.amount)}</dd></div>)}{receipt.changeAmount > 0 && <div><dt>Troco</dt><dd>{currency.format(receipt.changeAmount)}</dd></div>}</dl></>}
            <div className="receipt-divider" />
            <footer><strong>*** DOCUMENTO SEM VALOR FISCAL ***</strong><span>Obrigado pela preferência.</span></footer>
          </article>
        </section>

        <section className="kitchen-command-column" aria-labelledby="kitchen-command-title">
          <div className="print-document-heading"><span><ChefHat size={18} /></span><div><small>Documento 2</small><h3 id="kitchen-command-title">Comanda da cozinha</h3></div></div>
          <article className="kitchen-command-preview">
            <header><div><small>PEDIDO</small><strong>#{receipt.number}</strong></div><span>{formatFulfillment(receipt.fulfillment)}</span></header>
            <div className="kitchen-command-customer"><small>Cliente</small><strong>{receipt.customerName}</strong></div>
            <div className="kitchen-command-items">{receipt.items.map((item) => <article key={item.id}><strong>{item.quantity}x {item.name}</strong>{item.details.map((detail) => <span key={detail}>{removeKitchenPrice(detail)}</span>)}{item.notes && <b>OBS: {item.notes}</b>}</article>)}</div>
            {receipt.notes && <div className="kitchen-command-notes"><small>OBSERVAÇÕES GERAIS</small><strong>{receipt.notes}</strong></div>}
            <footer>SEM VALORES · USO DA PRODUÇÃO</footer>
          </article>

          <div className="print-flow-cards" aria-live="polite">
            <PrintActionCard number="1" title="Entregar ao cliente" description="Valores, pagamento, troco, itens e observações." state={customerState} actionLabel="Imprimir comprovante" onAction={() => void printCustomerReceipt()} />
            {onPrintKitchenCommand && <PrintActionCard number="2" title="Enviar para a cozinha" description="Itens e observações em destaque, sem preços." state={kitchenState} actionLabel="Imprimir comanda" onAction={() => void printKitchenCommand()} />}
          </div>
        </section>
      </div>
      <div className="modal-footer receipt-actions"><span className="print-queue-hint">{onPrintCustomerReceipt ? 'As impressões são enviadas para a fila da impressora térmica configurada.' : 'A impressão será aberta no navegador deste computador.'}</span><button type="button" className="secondary-button" disabled={isPrinting} onClick={onClose}>{context === 'preview' ? 'Fechar' : 'Concluir atendimento'}</button></div>
    </Modal>
  )
}

function PrintActionCard({ number, title, description, state, actionLabel, onAction }: { number: string; title: string; description: string; state: PrintState; actionLabel: string; onAction: () => void }) {
  return <article className={`print-flow-card ${state}`}><span className="print-step-number">{state === 'queued' ? <Check size={16} /> : number}</span><div><strong>{title}</strong><p>{description}</p>{state === 'error' && <small role="alert"><CircleAlert size={13} /> Não foi possível enviar. Verifique a impressora e tente novamente.</small>}</div><button type="button" className={state === 'queued' ? 'secondary-button' : 'primary-button'} disabled={state === 'printing'} onClick={onAction}>{state === 'printing' ? <><LoaderCircle className="spin" size={16} /> Enviando...</> : state === 'queued' ? <><Check size={16} /> Enfileirado</> : <><Printer size={16} /> {actionLabel}</>}</button></article>
}

function removeKitchenPrice(detail: string) {
  return detail.replace(/\s*\(\+\s*R\$.*\)$/, '')
}

function formatFulfillment(fulfillment: string) {
  if (fulfillment === 'Delivery') return 'ENTREGA'
  if (fulfillment === 'DineIn') return 'SALÃO'
  return 'RETIRADA'
}
