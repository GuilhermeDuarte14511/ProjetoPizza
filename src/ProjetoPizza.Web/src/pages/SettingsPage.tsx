import { CheckCircle2, Database, Download, Printer, Save, Settings2, Store, Wifi } from 'lucide-react'
import { type FormEvent, useMemo, useState } from 'react'
import { ConfirmDialog } from '../components/ui/ConfirmDialog'
import { CurrencyInput } from '../components/ui/CurrencyInput'
import { PageHeader } from '../components/ui/PageHeader'
import { useToast } from '../components/ui/toast'
import { ViewTransitionLink } from '../components/ui/ViewTransitionLink'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { queryKeys } from '../lib/queryKeys'
import { adminService } from '../services/adminService'
import { hasPermission } from '../services/authSession'
import type { Device } from '../types/admin'
import { getUserErrorMessage } from '../utils/errors'
import { translateEnum } from '../utils/presentation'

export type SettingsSection = 'general' | 'operation' | 'printers' | 'backup'

const tabs: Array<{ section: SettingsSection; label: string; icon: typeof Store }> = [
  { section: 'general', label: 'Dados da pizzaria', icon: Store },
  { section: 'operation', label: 'Operação', icon: Settings2 },
  { section: 'printers', label: 'Impressoras', icon: Printer },
  { section: 'backup', label: 'Backup e sistema', icon: Database },
]

export function SettingsPage({ section }: { section: SettingsSection }) {
  return (
    <>
      <PageHeader title="Configurações" description="Administre os dados e recursos da unidade principal." />
      <nav className="settings-tabs" aria-label="Seções de configurações" role="tablist">
        {tabs.map(({ section: item, label, icon: Icon }) => <ViewTransitionLink role="tab" aria-selected={item === section} key={item} href={`/admin/settings/${item}`} className={item === section ? 'active' : ''}><Icon size={15} /> {label}</ViewTransitionLink>)}
        <ViewTransitionLink role="tab" aria-selected={false} href="/admin/settings/pizza-rules"><Settings2 size={15} /> Regras de pizzas</ViewTransitionLink>
      </nav>
      {section === 'general' && <GeneralSettings />}
      {section === 'operation' && <OperationSettingsForm />}
      {section === 'printers' && <PrinterSettings />}
      {section === 'backup' && <BackupSettings />}
    </>
  )
}

function GeneralSettings() {
  const { data: initialSettings } = useAdminQuery(queryKeys.unitSettings, adminService.unitSettings)
  const [settings, setSettings] = useState(initialSettings)
  const [saved, setSaved] = useState(false)
  const [saving, setSaving] = useState(false)
  const toast = useToast()

  async function submit(event: FormEvent) {
    event.preventDefault()
    setSaving(true)
    try {
      await adminService.saveUnit({
        name: settings.name,
        legalName: settings.legalName,
        tradeName: settings.tradeName,
        cnpj: settings.cnpj,
        phone: settings.phone,
        administrativeEmail: settings.administrativeEmail,
      })
      setSaved(true)
      toast.success('Dados atualizados', 'As informações da pizzaria foram salvas.')
    } catch (error) {
      toast.error('Não foi possível salvar os dados', getUserErrorMessage(error))
    } finally {
      setSaving(false)
    }
  }

  return <form className="surface-card settings-form" onSubmit={submit}>
    <div className="card-heading"><div><h2>Identificação da unidade</h2><p>Dados exibidos nos documentos e canais administrativos.</p></div>{saved && <span className="saved-feedback"><CheckCircle2 size={16} /> Salvo</span>}</div>
    <div className="form-grid two-columns">
      <label className="field-label">Nome interno<input value={settings.name} onChange={(event) => setSettings({ ...settings, name: event.target.value })} required /></label>
      <label className="field-label">Nome fantasia<input value={settings.tradeName} onChange={(event) => setSettings({ ...settings, tradeName: event.target.value })} required /></label>
      <label className="field-label wide">Razão social<input value={settings.legalName} onChange={(event) => setSettings({ ...settings, legalName: event.target.value })} required /></label>
      <label className="field-label">CNPJ<input value={settings.cnpj} onChange={(event) => setSettings({ ...settings, cnpj: event.target.value })} required /></label>
      <label className="field-label">Telefone<input value={settings.phone ?? ''} onChange={(event) => setSettings({ ...settings, phone: event.target.value })} required /></label>
      <label className="field-label wide">E-mail administrativo<input type="email" value={settings.administrativeEmail ?? ''} onChange={(event) => setSettings({ ...settings, administrativeEmail: event.target.value })} required /></label>
    </div>
    <div className="form-actions">{hasPermission('admin:write') && <button className="primary-button" disabled={saving} aria-busy={saving}><Save size={16} /> {saving ? 'Salvando...' : 'Salvar alterações'}</button>}</div>
  </form>
}

function OperationSettingsForm() {
  const { data: initialSettings } = useAdminQuery(queryKeys.operationSettings, adminService.operationSettings)
  const [settings, setSettings] = useState(initialSettings)
  const [saved, setSaved] = useState(false)
  const [saving, setSaving] = useState(false)
  const toast = useToast()
  async function submit(event: FormEvent) {
    event.preventDefault()
    setSaving(true)
    try {
      await adminService.saveOperationSettings(settings)
      setSaved(true)
      toast.success('Operação atualizada', 'As regras operacionais foram salvas.')
    } catch (error) {
      toast.error('Não foi possível salvar a operação', getUserErrorMessage(error))
    } finally {
      setSaving(false)
    }
  }

  return <form className="surface-card settings-form" onSubmit={submit}>
    <div className="card-heading"><div><h2>Regras operacionais</h2><p>Parâmetros aplicados ao salão, delivery e tablets.</p></div>{saved && <span className="saved-feedback"><CheckCircle2 size={16} /> Salvo</span>}</div>
    <div className="settings-list">
      <Toggle label="Permitir mesa sem garçom responsável" checked={settings.allowTableWithoutWaiter} onChange={(value) => setSettings({ ...settings, allowTableWithoutWaiter: value })} />
      <Toggle label="Permitir pedidos sem caixa aberto" checked={settings.allowOrdersWithoutOpenCashShift} onChange={(value) => setSettings({ ...settings, allowOrdersWithoutOpenCashShift: value })} />
      <Toggle label="Limpar tablet ao fechar mesa" checked={settings.clearTabletAfterTableClose} onChange={(value) => setSettings({ ...settings, clearTabletAfterTableClose: value })} />
      <Toggle label="Som para novos pedidos de delivery" checked={settings.deliveryOrderSoundEnabled} onChange={(value) => setSettings({ ...settings, deliveryOrderSoundEnabled: value })} />
      <Toggle label="Som para chamados da mesa" checked={settings.tableCallSoundEnabled} onChange={(value) => setSettings({ ...settings, tableCallSoundEnabled: value })} />
    </div>
    <div className="form-grid three-columns">
      <label className="field-label">Taxa de serviço (%)<input type="number" min="0" max="100" value={settings.serviceFeePercentage} onChange={(event) => setSettings({ ...settings, serviceFeePercentage: Number(event.target.value) })} /></label>
      <label className="field-label">Taxa padrão delivery<CurrencyInput value={settings.defaultDeliveryFee} onCurrencyValueChange={(value) => setSettings({ ...settings, defaultDeliveryFee: value })} /></label>
      <label className="field-label">Tolerância de chamado (min)<input type="number" min="1" max="120" value={settings.tableCallToleranceMinutes} onChange={(event) => setSettings({ ...settings, tableCallToleranceMinutes: Number(event.target.value) })} /></label>
    </div>
    <div className="form-actions">{hasPermission('admin:write') && <button className="primary-button" disabled={saving} aria-busy={saving}><Save size={16} /> {saving ? 'Salvando...' : 'Salvar operação'}</button>}</div>
  </form>
}

function PrinterSettings() {
  const { data: devices, setData: setDevices } = useAdminQuery(queryKeys.devices, adminService.devices)
  const [busy, setBusy] = useState<string>()
  const [pendingPrinter, setPendingPrinter] = useState<Device>()
  const toast = useToast()
  const printers = useMemo(() => devices.filter((device) => device.type === 'Printer'), [devices])

  async function toggle(printer: Device) {
    const updated = { ...printer, status: printer.status === 'Online' ? 'Offline' : 'Online' }
    setBusy(printer.id)
    try {
      await adminService.updateDevice(updated)
      setDevices((current) => current.map((device) => device.id === printer.id ? updated : device))
      toast.success('Impressora atualizada', `${printer.name} agora está ${translateEnum(updated.status).toLowerCase()}.`)
    } catch (error) {
      toast.error('Não foi possível atualizar a impressora', getUserErrorMessage(error))
    } finally {
      setBusy(undefined)
    }
  }

  return <><section className="management-grid">
    {printers.map((printer) => <article className="surface-card management-card" key={printer.id}>
      <div className="device-icon"><Printer /></div>
      <div><span className={`status-pill ${printer.status === 'Online' ? 'success' : 'danger'}`}>{translateEnum(printer.status)}</span><h2>{printer.name}</h2><p>{printer.platform} · {printer.ipAddress ?? 'Conexão local'}</p><small>{printer.serialNumber}</small></div>
      {hasPermission('admin:write') && <button className="secondary-button" disabled={busy === printer.id} onClick={() => printer.status === 'Online' ? setPendingPrinter(printer) : void toggle(printer)}><Wifi size={16} /> {busy === printer.id ? 'Atualizando...' : printer.status === 'Online' ? 'Desconectar' : 'Conectar/testar'}</button>}
    </article>)}
  </section><ConfirmDialog open={Boolean(pendingPrinter)} title="Desconectar impressora?" description={`A ${pendingPrinter?.name ?? 'impressora'} deixará de receber novos trabalhos até ser reconectada.`} confirmLabel="Desconectar" tone="danger" busy={Boolean(pendingPrinter && busy === pendingPrinter.id)} onOpenChange={(open) => !open && setPendingPrinter(undefined)} onConfirm={() => { if (pendingPrinter) void toggle(pendingPrinter).finally(() => setPendingPrinter(undefined)) }} /></>
}

function BackupSettings() {
  const { data: snapshot, setData: setSnapshot, refresh: refreshSnapshot } = useAdminQuery(queryKeys.systemSnapshot, adminService.systemSnapshot)
  const [generated, setGenerated] = useState(false)
  const [generating, setGenerating] = useState(false)
  const toast = useToast()
  async function downloadSnapshot() {
    setGenerating(true)
    try {
      const { data: current } = await refreshSnapshot()
      if (!current) throw new Error('O servidor não retornou os dados do snapshot.')
      setSnapshot(current)
      const url = URL.createObjectURL(new Blob([JSON.stringify(current, null, 2)], { type: 'application/json' }))
      const anchor = document.createElement('a')
      anchor.href = url
      anchor.download = `projeto-pizza-snapshot-${new Date().toISOString().slice(0, 10)}.json`
      anchor.click()
      URL.revokeObjectURL(url)
      setGenerated(true)
      toast.success('Snapshot gerado', 'O arquivo foi preparado e baixado.')
    } catch (error) {
      toast.error('Não foi possível gerar o snapshot', getUserErrorMessage(error))
    } finally {
      setGenerating(false)
    }
  }

  return <section className="settings-grid">
    <article className="surface-card backup-card">
      <Database size={28} />
      <div><h2>Snapshot administrativo</h2><p>Exporta dados de identificação e contadores do sistema sem incluir senhas ou tokens.</p></div>
      {hasPermission('admin:write') && <button className="primary-button" disabled={generating} aria-busy={generating} onClick={() => void downloadSnapshot()}><Download size={16} /> {generating ? 'Gerando...' : 'Gerar e baixar'}</button>}
      {generated && <span className="saved-feedback"><CheckCircle2 size={16} /> Arquivo gerado</span>}
    </article>
    <article className="surface-card system-summary">
      <h2>Informações do sistema</h2>
      <dl><div><dt>Último snapshot</dt><dd>{new Date(snapshot.generatedAt).toLocaleString('pt-BR')}</dd></div><div><dt>Produtos</dt><dd>{snapshot.products}</dd></div><div><dt>Mesas</dt><dd>{snapshot.tables}</dd></div><div><dt>Pedidos</dt><dd>{snapshot.orders}</dd></div><div><dt>Dispositivos</dt><dd>{snapshot.devices}</dd></div></dl>
    </article>
  </section>
}

function Toggle({ label, checked, onChange }: { label: string; checked: boolean; onChange: (value: boolean) => void }) {
  return <label className="toggle-row"><span>{label}</span><input type="checkbox" checked={checked} onChange={(event) => onChange(event.target.checked)} /></label>
}
