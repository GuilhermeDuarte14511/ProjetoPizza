import { Pencil, Plus, Save, Settings2 } from 'lucide-react'
import { type FormEvent, type ReactNode, useState } from 'react'
import { Modal } from '../components/ui/Modal'
import { PageHeader } from '../components/ui/PageHeader'
import { useToast } from '../components/ui/toast'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { queryKeys } from '../lib/queryKeys'
import { adminService } from '../services/adminService'
import { hasPermission } from '../services/authSession'
import type { CashRegister, DiningAreaSetting, PaymentMethod, ProductionStationSetting, RestaurantTableSetting, ServiceCallTypeSetting } from '../types/admin'
import { getUserErrorMessage } from '../utils/errors'

type StructureKind = 'area' | 'table' | 'register' | 'payment' | 'station' | 'callType'
type StructureItem = DiningAreaSetting | RestaurantTableSetting | CashRegister | PaymentMethod | ProductionStationSetting | ServiceCallTypeSetting

export function OperationalStructurePage() {
  const areasQuery = useAdminQuery(queryKeys.diningAreas, adminService.diningAreas)
  const tablesQuery = useAdminQuery(queryKeys.tableSettings, adminService.tableSettings)
  const registersQuery = useAdminQuery(queryKeys.cashRegisters, adminService.cashRegisters)
  const methodsQuery = useAdminQuery(queryKeys.paymentMethods, adminService.paymentMethods)
  const stationsQuery = useAdminQuery(queryKeys.productionStations, adminService.productionStations)
  const callTypesQuery = useAdminQuery(queryKeys.serviceCallTypes, adminService.serviceCallTypes)
  const [editing, setEditing] = useState<{ kind: StructureKind; item?: StructureItem }>()
  const [saving, setSaving] = useState(false)
  const toast = useToast()

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!editing) return
    const data = new FormData(event.currentTarget)
    const id = editing.item?.id
    const text = (name: string) => String(data.get(name) ?? '').trim()
    const number = (name: string) => Number(data.get(name) ?? 0)
    const checked = (name: string) => data.get(name) === 'on'
    setSaving(true)
    try {
      switch (editing.kind) {
        case 'area': await adminService.saveDiningArea({ id, name: text('name'), displayOrder: number('displayOrder'), isActive: checked('isActive') }); break
        case 'table': await adminService.saveTableSetting({ id, diningAreaId: text('diningAreaId'), number: number('number'), name: text('name'), capacity: number('capacity'), displayOrder: number('displayOrder'), isActive: checked('isActive') }); break
        case 'register': await adminService.saveCashRegister({ id, name: text('name'), code: text('code'), isActive: checked('isActive') }); break
        case 'payment': await adminService.savePaymentMethod({ id, name: text('name'), code: text('code'), displayOrder: number('displayOrder'), requiresExternalReference: checked('requiresExternalReference'), allowsChange: checked('allowsChange'), isActive: checked('isActive') }); break
        case 'station': await adminService.saveProductionStation({ id, name: text('name'), code: text('code'), targetPreparationMinutes: number('targetPreparationMinutes'), displayOrder: number('displayOrder'), isActive: checked('isActive') }); break
        case 'callType': await adminService.saveServiceCallType({ id, name: text('name'), code: text('code'), isActive: checked('isActive') }); break
      }
      await Promise.all([areasQuery.refresh(), tablesQuery.refresh(), registersQuery.refresh(), methodsQuery.refresh(), stationsQuery.refresh(), callTypesQuery.refresh()])
      setEditing(undefined)
      toast.success('Cadastro salvo', 'A estrutura operacional foi atualizada e auditada.')
    } catch (error) {
      toast.error('Não foi possível salvar', getUserErrorMessage(error))
    } finally {
      setSaving(false)
    }
  }

  return <>
    <PageHeader title="Estrutura operacional" description="Cadastros necessários para instalar e operar a unidade sem intervenção técnica." />
    <section className="structure-grid">
      <StructureCard title="Áreas do salão" onAdd={() => setEditing({ kind: 'area' })}>{areasQuery.data.map((item) => <StructureRow key={item.id} title={item.name} detail={`Ordem ${item.displayOrder}`} active={item.isActive} onEdit={() => setEditing({ kind: 'area', item })} />)}</StructureCard>
      <StructureCard title="Mesas" onAdd={() => setEditing({ kind: 'table' })}>{tablesQuery.data.map((item) => <StructureRow key={item.id} title={`${item.name} · nº ${item.number}`} detail={`${item.areaName} · ${item.capacity} lugares`} active={item.isActive} onEdit={() => setEditing({ kind: 'table', item })} />)}</StructureCard>
      <StructureCard title="Caixas" onAdd={() => setEditing({ kind: 'register' })}>{registersQuery.data.map((item) => <StructureRow key={item.id} title={item.name} detail={item.code} active={item.isActive} onEdit={() => setEditing({ kind: 'register', item })} />)}</StructureCard>
      <StructureCard title="Formas de pagamento" onAdd={() => setEditing({ kind: 'payment' })}>{methodsQuery.data.map((item) => <StructureRow key={item.id} title={item.name} detail={item.code} active={item.isActive} onEdit={() => setEditing({ kind: 'payment', item })} />)}</StructureCard>
      <StructureCard title="Estações de produção" onAdd={() => setEditing({ kind: 'station' })}>{stationsQuery.data.map((item) => <StructureRow key={item.id} title={item.name} detail={`${item.code} · meta ${item.targetPreparationMinutes} min`} active={item.isActive} onEdit={() => setEditing({ kind: 'station', item })} />)}</StructureCard>
      <StructureCard title="Tipos de chamado" onAdd={() => setEditing({ kind: 'callType' })}>{callTypesQuery.data.map((item) => <StructureRow key={item.id} title={item.name} detail={item.code} active={item.isActive} onEdit={() => setEditing({ kind: 'callType', item })} />)}</StructureCard>
    </section>
    {editing && <StructureModal editing={editing} areas={areasQuery.data} saving={saving} onClose={() => setEditing(undefined)} onSubmit={save} />}
  </>
}

function StructureCard({ title, onAdd, children }: { title: string; onAdd: () => void; children: ReactNode }) {
  return <article className="surface-card structure-card"><div className="card-heading"><div><h2>{title}</h2><p>Cadastro ativo na unidade.</p></div>{hasPermission('admin:write') && <button className="secondary-button" onClick={onAdd}><Plus size={15} /> Adicionar</button>}</div><div className="structure-list">{children}</div></article>
}

function StructureRow({ title, detail, active, onEdit }: { title: string; detail: string; active: boolean; onEdit: () => void }) {
  return <div><Settings2 size={18} /><span><strong>{title}</strong><small>{detail}</small></span><span className={`status-pill ${active ? 'success' : 'neutral'}`}>{active ? 'Ativo' : 'Inativo'}</span>{hasPermission('admin:write') && <button className="icon-button" aria-label={`Editar ${title}`} onClick={onEdit}><Pencil size={15} /></button>}</div>
}

function StructureModal({ editing, areas, saving, onClose, onSubmit }: { editing: { kind: StructureKind; item?: StructureItem }; areas: DiningAreaSetting[]; saving: boolean; onClose: () => void; onSubmit: (event: FormEvent<HTMLFormElement>) => void }) {
  const item = editing.item
  const field = (name: string) => String((item as unknown as Record<string, unknown> | undefined)?.[name] ?? '')
  const boolean = (name: string, fallback = true) => item ? Boolean((item as unknown as Record<string, unknown>)[name]) : fallback
  return <Modal open title={item ? 'Editar cadastro' : 'Novo cadastro'} description="As alterações são validadas pelo domínio e registradas na auditoria." isBusy={saving} size="large" onClose={onClose}>
    <form onSubmit={onSubmit}><div className="modal-body"><div className="form-grid three-columns">
      {editing.kind === 'table' && <label className="field-label">Área<select name="diningAreaId" defaultValue={field('diningAreaId')} required>{areas.filter((area) => area.isActive || area.id === field('diningAreaId')).map((area) => <option key={area.id} value={area.id}>{area.name}</option>)}</select></label>}
      {editing.kind === 'table' && <label className="field-label">Número<input name="number" type="number" min="1" defaultValue={field('number')} required /></label>}
      <label className="field-label">Nome<input name="name" defaultValue={field('name')} maxLength={120} required autoFocus /></label>
      {editing.kind !== 'area' && editing.kind !== 'table' && <label className="field-label">Código<input name="code" defaultValue={field('code')} maxLength={50} required /></label>}
      {editing.kind === 'table' && <label className="field-label">Capacidade<input name="capacity" type="number" min="1" defaultValue={field('capacity') || 4} required /></label>}
      {['area', 'table', 'payment', 'station'].includes(editing.kind) && <label className="field-label">Ordem<input name="displayOrder" type="number" min="0" defaultValue={field('displayOrder') || 0} required /></label>}
      {editing.kind === 'station' && <label className="field-label">Meta (min)<input name="targetPreparationMinutes" type="number" min="1" defaultValue={field('targetPreparationMinutes') || 15} required /></label>}
      {editing.kind === 'payment' && <label className="check-label"><input name="requiresExternalReference" type="checkbox" defaultChecked={boolean('requiresExternalReference', false)} /> Exigir referência externa</label>}
      {editing.kind === 'payment' && <label className="check-label"><input name="allowsChange" type="checkbox" defaultChecked={boolean('allowsChange', false)} /> Permitir troco</label>}
      <label className="check-label wide"><input name="isActive" type="checkbox" defaultChecked={boolean('isActive')} /> Cadastro ativo</label>
    </div></div><div className="modal-footer"><button type="button" className="secondary-button" disabled={saving} onClick={onClose}>Cancelar</button><button className="primary-button" disabled={saving}><Save size={16} /> {saving ? 'Salvando...' : 'Salvar'}</button></div></form>
  </Modal>
}
