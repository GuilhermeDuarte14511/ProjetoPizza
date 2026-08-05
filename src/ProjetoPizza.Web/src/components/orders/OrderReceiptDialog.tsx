import { Printer } from 'lucide-react'
import type { OrderReceipt } from '../../types/admin'
import { Modal } from '../ui/Modal'

const currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })

export function OrderReceiptDialog({ receipt, onClose }: { receipt?: OrderReceipt; onClose: () => void }) {
  if (!receipt) return null

  return (
    <Modal open title={`Pedido #${receipt.number} criado`} description="Confira a comanda antes de imprimir na impressora térmica." size="large" onClose={onClose}>
      <div className="modal-body receipt-preview-shell">
        <article className="thermal-receipt thermal-print-area" aria-label={`Comanda não fiscal do pedido ${receipt.number}`}>
          <header>
            <strong>FORNO 27</strong>
            <span>PIZZERIA</span>
            <b>COMANDA NÃO FISCAL</b>
          </header>
          <div className="receipt-divider" />
          <dl className="receipt-meta">
            <div><dt>Pedido</dt><dd>#{receipt.number}</dd></div>
            <div><dt>Data</dt><dd>{new Date(receipt.placedAt).toLocaleString('pt-BR')}</dd></div>
            <div><dt>Cliente</dt><dd>{receipt.customerName}</dd></div>
            {receipt.customerPhone && <div><dt>Telefone</dt><dd>{formatPhone(receipt.customerPhone)}</dd></div>}
            <div><dt>Atendimento</dt><dd>{receipt.fulfillment === 'Delivery' ? 'ENTREGA' : 'RETIRADA'}</dd></div>
            {receipt.deliveryAddress && <div className="wide"><dt>Endereço</dt><dd>{receipt.deliveryAddress}</dd></div>}
          </dl>
          <div className="receipt-divider" />
          <section className="receipt-items">
            {receipt.items.map((item) => (
              <article key={item.id}>
                <div className="receipt-item-title"><strong>{item.quantity}x {item.name}</strong><b>{currency.format(item.totalPrice)}</b></div>
                <small>Unitário: {currency.format(item.unitPrice)}</small>
                {item.details.map((detail) => <span key={detail}>• {detail}</span>)}
                {item.notes && <p><strong>OBS:</strong> {item.notes}</p>}
              </article>
            ))}
          </section>
          {receipt.notes && <div className="receipt-order-notes"><strong>OBSERVAÇÕES DO PEDIDO</strong><p>{receipt.notes}</p></div>}
          <div className="receipt-divider" />
          <dl className="receipt-totals">
            <div><dt>Subtotal</dt><dd>{currency.format(receipt.subtotal)}</dd></div>
            {receipt.deliveryFee > 0 && <div><dt>Taxa de entrega</dt><dd>{currency.format(receipt.deliveryFee)}</dd></div>}
            <div><dt>Desconto</dt><dd>- {currency.format(receipt.discount)}</dd></div>
            <div className="grand-total"><dt>TOTAL</dt><dd>{currency.format(receipt.total)}</dd></div>
          </dl>
          <div className="receipt-divider" />
          <footer>
            <strong>*** DOCUMENTO NÃO FISCAL ***</strong>
            <span>Produzir e conferir antes da entrega.</span>
          </footer>
        </article>
      </div>
      <div className="modal-footer receipt-actions"><button type="button" className="secondary-button" onClick={onClose}>Fechar</button><button type="button" className="primary-button" onClick={() => window.print()}><Printer size={16} /> Imprimir comanda</button></div>
    </Modal>
  )
}

function formatPhone(phone: string) {
  if (phone.length === 11) return `(${phone.slice(0, 2)}) ${phone.slice(2, 7)}-${phone.slice(7)}`
  if (phone.length === 10) return `(${phone.slice(0, 2)}) ${phone.slice(2, 6)}-${phone.slice(6)}`
  return phone
}
