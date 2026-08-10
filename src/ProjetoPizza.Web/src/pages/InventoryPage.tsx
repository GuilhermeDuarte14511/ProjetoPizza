import { AlertTriangle, Boxes, Pencil, Plus, Save, SlidersHorizontal } from 'lucide-react'
import { type FormEvent, useState } from 'react'
import { Modal } from '../components/ui/Modal'
import { PageHeader } from '../components/ui/PageHeader'
import { useToast } from '../components/ui/toast'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { queryKeys } from '../lib/queryKeys'
import { adminService } from '../services/adminService'
import { hasPermission } from '../services/authSession'
import type { InventoryItem } from '../types/admin'
import { getUserErrorMessage } from '../utils/errors'

export function InventoryPage() {
  const { data: items, refresh } = useAdminQuery(queryKeys.inventory, adminService.inventory)
  const [editing, setEditing] = useState<InventoryItem | 'new'>()
  const [adjusting, setAdjusting] = useState<InventoryItem>()
  const [saving, setSaving] = useState(false)
  const toast = useToast()

  async function saveItem(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    setSaving(true)
    try {
      await adminService.saveInventoryItem({
        id: editing === 'new' ? undefined : editing?.id,
        name: String(data.get('name') ?? ''),
        sku: String(data.get('sku') ?? ''),
        unitOfMeasure: String(data.get('unitOfMeasure') ?? ''),
        minimumStock: Number(data.get('minimumStock') ?? 0),
        isActive: data.get('isActive') === 'on',
      })
      await refresh()
      setEditing(undefined)
      toast.success('Item de estoque salvo', 'O cadastro está disponível para controle e alertas.')
    } catch (error) {
      toast.error('Não foi possível salvar o item', getUserErrorMessage(error))
    } finally { setSaving(false) }
  }

  async function adjust(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!adjusting) return
    const data = new FormData(event.currentTarget)
    setSaving(true)
    try {
      await adminService.adjustInventory(adjusting.id, Number(data.get('quantityDelta')), String(data.get('reason') ?? ''))
      await refresh()
      setAdjusting(undefined)
      toast.success('Estoque ajustado', 'O saldo e a movimentação foram registrados.')
    } catch (error) {
      toast.error('Não foi possível ajustar o estoque', getUserErrorMessage(error))
    } finally { setSaving(false) }
  }

  return <>
    <PageHeader title="Estoque" description="Saldos, estoque mínimo e ajustes auditados da unidade." actions={hasPermission('admin:write') && <button className="primary-button" onClick={() => setEditing('new')}><Plus size={16} /> Novo item</button>} />
    <section className="surface-card">
      <div className="responsive-table"><table><thead><tr><th>Item</th><th>SKU</th><th>Atual</th><th>Reservado</th><th>Disponível</th><th>Mínimo</th><th>Status</th><th aria-label="Ações" /></tr></thead><tbody>
        {items.map((item) => <tr key={item.id}><td><strong>{item.name}</strong></td><td>{item.sku}</td><td>{item.currentQuantity.toLocaleString('pt-BR')} {item.unitOfMeasure}</td><td>{item.reservedQuantity.toLocaleString('pt-BR')}</td><td>{item.availableQuantity.toLocaleString('pt-BR')} {item.unitOfMeasure}</td><td>{item.minimumStock.toLocaleString('pt-BR')} {item.unitOfMeasure}</td><td><span className={`status-pill ${item.isLowStock ? 'danger' : item.isActive ? 'success' : 'neutral'}`}>{item.isLowStock ? 'Estoque baixo' : item.isActive ? 'Normal' : 'Inativo'}</span></td><td><div className="table-actions">{hasPermission('admin:write') && <><button className="icon-button" aria-label={`Editar ${item.name}`} onClick={() => setEditing(item)}><Pencil size={15} /></button><button className="secondary-button" onClick={() => setAdjusting(item)}><SlidersHorizontal size={15} /> Ajustar</button></>}</div></td></tr>)}
      </tbody></table></div>
      {!items.length && <div className="empty-state compact"><Boxes size={30} /><h2>Nenhum item cadastrado</h2><p>Cadastre os insumos para ativar alertas reais no dashboard.</p></div>}
    </section>
    {editing && <Modal open title={editing === 'new' ? 'Novo item de estoque' : 'Editar item de estoque'} description="O saldo é alterado somente por movimentações de ajuste." isBusy={saving} onClose={() => setEditing(undefined)}><form onSubmit={saveItem}><div className="modal-body"><div className="form-grid two-columns">
      <label className="field-label wide">Nome<input name="name" defaultValue={editing === 'new' ? '' : editing.name} required autoFocus /></label>
      <label className="field-label">SKU<input name="sku" defaultValue={editing === 'new' ? '' : editing.sku} required /></label>
      <label className="field-label">Unidade<input name="unitOfMeasure" defaultValue={editing === 'new' ? 'kg' : editing.unitOfMeasure} required /></label>
      <label className="field-label">Estoque mínimo<input name="minimumStock" type="number" min="0" step="0.001" defaultValue={editing === 'new' ? 0 : editing.minimumStock} required /></label>
      <label className="check-label"><input name="isActive" type="checkbox" defaultChecked={editing === 'new' || editing.isActive} /> Item ativo</label>
    </div></div><div className="modal-footer"><button type="button" className="secondary-button" onClick={() => setEditing(undefined)}>Cancelar</button><button className="primary-button" disabled={saving}><Save size={16} /> Salvar</button></div></form></Modal>}
    {adjusting && <Modal open title={`Ajustar ${adjusting.name}`} description={`Saldo disponível atual: ${adjusting.availableQuantity.toLocaleString('pt-BR')} ${adjusting.unitOfMeasure}. Use valor negativo para baixa.`} isBusy={saving} onClose={() => setAdjusting(undefined)}><form onSubmit={adjust}><div className="modal-body"><aside className="cash-opening-note"><AlertTriangle size={20} /><span><strong>Ajuste auditado</strong>O saldo nunca poderá ficar negativo.</span></aside><div className="form-grid"><label className="field-label">Quantidade (+ entrada / - baixa)<input name="quantityDelta" type="number" step="0.001" required autoFocus /></label><label className="field-label">Motivo<input name="reason" maxLength={300} required /></label></div></div><div className="modal-footer"><button type="button" className="secondary-button" onClick={() => setAdjusting(undefined)}>Cancelar</button><button className="primary-button" disabled={saving}><SlidersHorizontal size={16} /> Registrar ajuste</button></div></form></Modal>}
  </>
}
