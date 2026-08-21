import { BellRing, ChefHat, CreditCard, LayoutGrid, Pencil, Plus, Save, Settings2, Store, Table2, Trash2, type LucideIcon } from 'lucide-react'
import { useQueryClient } from '@tanstack/react-query'
import { type FormEvent, type ReactNode, useState } from 'react'
import { ConfirmDialog } from '../components/ui/ConfirmDialog'
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

const structureLabels: Record<StructureKind, string> = {
  area: 'área do salão',
  table: 'mesa',
  register: 'caixa',
  payment: 'forma de pagamento',
  station: 'estação de produção',
  callType: 'tipo de chamado',
}

export function OperationalStructurePage() {
  const areasQuery = useAdminQuery(queryKeys.diningAreas, adminService.diningAreas)
  const tablesQuery = useAdminQuery(queryKeys.tableSettings, adminService.tableSettings)
  const registersQuery = useAdminQuery(queryKeys.cashRegisters, adminService.cashRegisters)
  const methodsQuery = useAdminQuery(queryKeys.paymentMethods, adminService.paymentMethods)
  const stationsQuery = useAdminQuery(queryKeys.productionStations, adminService.productionStations)
  const callTypesQuery = useAdminQuery(queryKeys.serviceCallTypes, adminService.serviceCallTypes)
  const [editing, setEditing] = useState<{ kind: StructureKind; item?: StructureItem }>()
  const [saving, setSaving] = useState(false)
  const [tableToDelete, setTableToDelete] = useState<RestaurantTableSetting>()
  const [deleting, setDeleting] = useState(false)
  const queryClient = useQueryClient()
  const toast = useToast()
  const totalRecords = areasQuery.data.length + tablesQuery.data.length + registersQuery.data.length + methodsQuery.data.length + stationsQuery.data.length + callTypesQuery.data.length
  const activeRecords = areasQuery.data.filter((item) => item.isActive).length + tablesQuery.data.filter((item) => item.isActive).length + registersQuery.data.filter((item) => item.isActive).length + methodsQuery.data.filter((item) => item.isActive).length + stationsQuery.data.filter((item) => item.isActive).length + callTypesQuery.data.filter((item) => item.isActive).length

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
      await queryClient.invalidateQueries({ queryKey: queryKeys.tables })
      setEditing(undefined)
      toast.success('Cadastro salvo', 'A estrutura operacional foi atualizada e auditada.')
    } catch (error) {
      toast.error('Não foi possível salvar', getUserErrorMessage(error))
    } finally {
      setSaving(false)
    }
  }

  async function deleteTable() {
    if (!tableToDelete) return
    setDeleting(true)
    try {
      await adminService.deleteTableSetting(tableToDelete.id)
      tablesQuery.setData((current) => current.filter((table) => table.id !== tableToDelete.id))
      await queryClient.invalidateQueries({ queryKey: queryKeys.tables })
      toast.success('Mesa excluída', `${tableToDelete.name} foi removida da estrutura da unidade.`)
      setTableToDelete(undefined)
    } catch (error) {
      toast.error('Não foi possível excluir a mesa', getUserErrorMessage(error))
    } finally {
      setDeleting(false)
    }
  }

  return <>
    <PageHeader
      title="Estrutura operacional"
      description="Organize os cadastros que sustentam o salão, o caixa, a produção e os tablets."
      actions={<div className="structure-header-summary"><span className="structure-header-dot" /> <strong>{activeRecords} ativos</strong><span>de {totalRecords} cadastros</span></div>}
    />
    <section className="structure-overview" aria-label="Resumo da estrutura operacional">
      <span className="structure-overview-icon" aria-hidden="true"><Settings2 size={19} /></span>
      <div className="structure-overview-copy"><strong>Configuração da unidade</strong><span>Os dados abaixo aparecem para a equipe na operação diária.</span></div>
      <div className="structure-overview-metrics"><span><strong>{areasQuery.data.length}</strong><small>áreas</small></span><span><strong>{tablesQuery.data.length}</strong><small>mesas</small></span><span><strong>{registersQuery.data.length}</strong><small>caixas</small></span></div>
    </section>
    <section className="structure-grid">
      <StructureCard title="Áreas do salão" description="Organize a ordem e a disponibilidade do salão." count={areasQuery.data.length} emptyLabel="área" icon={LayoutGrid} onAdd={() => setEditing({ kind: 'area' })}>{areasQuery.data.map((item) => <StructureRow key={item.id} title={item.name} detail={`Ordem de exibição · ${item.displayOrder}`} active={item.isActive} icon={LayoutGrid} onEdit={() => setEditing({ kind: 'area', item })} />)}</StructureCard>
      <StructureCard title="Mesas" description="Defina número, capacidade e área de cada mesa." count={tablesQuery.data.length} emptyLabel="mesa" icon={Table2} onAdd={() => setEditing({ kind: 'table' })}>{tablesQuery.data.map((item) => <StructureRow key={item.id} title={`${item.name} · nº ${item.number}`} detail={`${item.areaName} · ${item.capacity} lugares`} active={item.isActive} icon={Table2} onEdit={() => setEditing({ kind: 'table', item })} onDelete={() => setTableToDelete(item)} />)}</StructureCard>
      <StructureCard title="Caixas" description="Cadastre os pontos de recebimento da unidade." count={registersQuery.data.length} emptyLabel="caixa" icon={Store} onAdd={() => setEditing({ kind: 'register' })}>{registersQuery.data.map((item) => <StructureRow key={item.id} title={item.name} detail={`Código operacional · ${item.code}`} active={item.isActive} icon={Store} onEdit={() => setEditing({ kind: 'register', item })} />)}</StructureCard>
      <StructureCard title="Formas de pagamento" description="Escolha como os pedidos podem ser pagos." count={methodsQuery.data.length} emptyLabel="forma de pagamento" icon={CreditCard} onAdd={() => setEditing({ kind: 'payment' })}>{methodsQuery.data.map((item) => <StructureRow key={item.id} title={item.name} detail={`Código operacional · ${item.code}`} active={item.isActive} icon={CreditCard} onEdit={() => setEditing({ kind: 'payment', item })} />)}</StructureCard>
      <StructureCard title="Estações de produção" description="Acompanhe metas e pontos de preparo." count={stationsQuery.data.length} emptyLabel="estação" icon={ChefHat} onAdd={() => setEditing({ kind: 'station' })}>{stationsQuery.data.map((item) => <StructureRow key={item.id} title={item.name} detail={`${item.code} · meta de ${item.targetPreparationMinutes} min`} active={item.isActive} icon={ChefHat} onEdit={() => setEditing({ kind: 'station', item })} />)}</StructureCard>
      <StructureCard title="Tipos de chamado" description="Atalhos que a equipe pode solicitar no salão." count={callTypesQuery.data.length} emptyLabel="tipo de chamado" icon={BellRing} onAdd={() => setEditing({ kind: 'callType' })}>{callTypesQuery.data.map((item) => <StructureRow key={item.id} title={item.name} detail={`Código operacional · ${item.code}`} active={item.isActive} icon={BellRing} onEdit={() => setEditing({ kind: 'callType', item })} />)}</StructureCard>
    </section>
    {editing && <StructureModal editing={editing} areas={areasQuery.data} saving={saving} onClose={() => setEditing(undefined)} onSubmit={save} />}
    <ConfirmDialog open={Boolean(tableToDelete)} title={`Excluir ${tableToDelete?.name ?? 'mesa'}?`} description="A mesa só pode ser excluída se nunca tiver participado de um atendimento e não possuir tablet vinculado. Se já houver histórico, desative o cadastro." confirmLabel="Excluir mesa" tone="danger" busy={deleting} onOpenChange={(open) => !open && setTableToDelete(undefined)} onConfirm={() => void deleteTable()} />
  </>
}

function StructureCard({ title, description, count, emptyLabel, icon: Icon, onAdd, children }: { title: string; description: string; count: number; emptyLabel: string; icon: LucideIcon; onAdd: () => void; children: ReactNode }) {
  return <article className="surface-card structure-card">
    <header className="structure-card-header">
      <span className="structure-card-icon" aria-hidden="true"><Icon size={18} /></span>
      <div className="structure-card-heading"><div><h2>{title}</h2><p>{description}</p></div><span className="structure-card-count">{count}</span></div>
      {hasPermission('admin:write') && <button type="button" className="structure-add-button" onClick={onAdd}><Plus size={15} /> Adicionar</button>}
    </header>
    {count > 0 ? <div className="structure-list" role="list">{children}</div> : <div className="structure-empty" role="status"><span className="structure-empty-icon" aria-hidden="true"><Plus size={18} /></span><div><strong>Nenhum {emptyLabel} cadastrado</strong><span>Adicione o primeiro para começar a configurar a unidade.</span></div>{hasPermission('admin:write') && <button type="button" className="structure-empty-action" onClick={onAdd}>Adicionar</button>}</div>}
  </article>
}

function StructureRow({ title, detail, active, icon: Icon, onEdit, onDelete }: { title: string; detail: string; active: boolean; icon: LucideIcon; onEdit: () => void; onDelete?: () => void }) {
  return <div className="structure-row" role="listitem"><span className="structure-row-icon" aria-hidden="true"><Icon size={16} /></span><span className="structure-row-copy"><strong>{title}</strong><small>{detail}</small></span><span className={`status-pill ${active ? 'success' : 'neutral'}`}><span className="status-dot" aria-hidden="true" />{active ? 'Ativo' : 'Inativo'}</span>{hasPermission('admin:write') && <span className="structure-row-actions"><button type="button" className="icon-button" data-tooltip="Editar" aria-label={`Editar ${title}`} onClick={onEdit}><Pencil size={15} /></button>{onDelete && <button type="button" className="icon-button danger-icon-button" data-tooltip="Excluir" aria-label={`Excluir ${title}`} onClick={onDelete}><Trash2 size={15} /></button>}</span>}</div>
}

function StructureModal({ editing, areas, saving, onClose, onSubmit }: { editing: { kind: StructureKind; item?: StructureItem }; areas: DiningAreaSetting[]; saving: boolean; onClose: () => void; onSubmit: (event: FormEvent<HTMLFormElement>) => void }) {
  const item = editing.item
  const field = (name: string) => String((item as unknown as Record<string, unknown> | undefined)?.[name] ?? '')
  const boolean = (name: string, fallback = true) => item ? Boolean((item as unknown as Record<string, unknown>)[name]) : fallback
  const label = structureLabels[editing.kind]
  return <Modal open title={`${item ? 'Editar' : 'Adicionar'} ${label}`} description="Preencha os dados usados na operação. As alterações são validadas e registradas na auditoria." isBusy={saving} size="large" onClose={onClose}>
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
    </div></div><div className="modal-footer"><button type="button" className="secondary-button" disabled={saving} onClick={onClose}>Cancelar</button><button type="submit" className="primary-button" disabled={saving}><Save size={16} /> {saving ? 'Salvando...' : item ? 'Salvar alterações' : 'Adicionar cadastro'}</button></div></form>
  </Modal>
}
