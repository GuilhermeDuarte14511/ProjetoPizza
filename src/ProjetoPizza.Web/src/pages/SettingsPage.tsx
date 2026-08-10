import { CheckCircle2, Database, Download, Plus, Printer, Save, Settings2, Store, Wifi } from 'lucide-react'
import { type FormEvent, useEffect, useMemo, useState } from 'react'
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
        <ViewTransitionLink role="tab" aria-selected={false} href="/admin/settings/structure"><Settings2 size={15} /> Estrutura operacional</ViewTransitionLink>
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
  const { data: printJobs, refresh: refreshPrintJobs } = useAdminQuery(queryKeys.printJobs, adminService.printJobs)
  const [busy, setBusy] = useState<string>()
  const [pendingPrinter, setPendingPrinter] = useState<Device>()
  const [showForm, setShowForm] = useState(false)
  const [draft, setDraft] = useState<{ id?: string; name: string; host: string; port: number; paperWidthMm: number; autoPrintKitchenTickets: boolean; autoPrintCustomerReceipts: boolean; autoPrintFiscalDocuments: boolean; isActive: boolean }>({ name: '', host: '', port: 9100, paperWidthMm: 80, autoPrintKitchenTickets: false, autoPrintCustomerReceipts: true, autoPrintFiscalDocuments: false, isActive: true })
  const toast = useToast()
  const printers = useMemo(() => devices.filter((device) => device.type === 'Printer'), [devices])

  useEffect(() => {
    const timer = window.setInterval(() => void refreshPrintJobs(), 5_000)
    return () => window.clearInterval(timer)
  }, [refreshPrintJobs])

  async function toggle(printer: Device) {
    const updated = { ...printer, status: printer.status === 'Online' ? 'Offline' : 'Online' }
    setBusy(printer.id)
    try {
      await adminService.updateDevice(updated)
      setDevices((current) => current.map((device) => device.id === printer.id ? updated : device))
      toast.success('Cadastro da impressora atualizado', `${printer.name} foi marcada como ${translateEnum(updated.status).toLowerCase()}; nenhum teste físico foi executado.`)
    } catch (error) {
      toast.error('Não foi possível atualizar a impressora', getUserErrorMessage(error))
    } finally {
      setBusy(undefined)
    }
  }

  async function savePrinter(event: FormEvent) {
    event.preventDefault()
    setBusy('new')
    try {
      const result = await adminService.savePrinter(draft) as { id: string }
      const savedPrinter: Device = {
        id: result.id, name: draft.name, serialNumber: 'Nova impressora', type: 'Printer', platform: 'ESC/POS TCP',
        status: draft.isActive ? 'Online' : 'Offline', isCharging: false, networkStatus: 'Network', ipAddress: draft.host,
        isLocked: false, printerPort: draft.port, paperWidthMm: draft.paperWidthMm,
        autoPrintKitchenTickets: draft.autoPrintKitchenTickets, autoPrintCustomerReceipts: draft.autoPrintCustomerReceipts,
        autoPrintFiscalDocuments: draft.autoPrintFiscalDocuments,
      }
      setDevices((current) => draft.id
        ? current.map((device) => device.id === draft.id ? { ...device, ...savedPrinter, serialNumber: device.serialNumber } : device)
        : [...current, savedPrinter])
      setShowForm(false)
      setDraft({ name: '', host: '', port: 9100, paperWidthMm: 80, autoPrintKitchenTickets: false, autoPrintCustomerReceipts: true, autoPrintFiscalDocuments: false, isActive: true })
      toast.success('Impressora adicionada', 'A impressora de rede foi salva. Execute o teste físico antes de usar.')
    } catch (error) { toast.error('Não foi possível salvar a impressora', getUserErrorMessage(error)) } finally { setBusy(undefined) }
  }

  async function testPrinter(printer: Device) {
    setBusy(printer.id)
    try {
      await adminService.testPrinter(printer.id)
      await refreshPrintJobs()
      toast.success('Teste enfileirado', 'A API enviará uma página ESC/POS para a impressora. Confira o papel e o status abaixo.')
    } catch (error) { toast.error('Não foi possível testar a impressora', getUserErrorMessage(error)) } finally { setBusy(undefined) }
  }

  return <><div className="settings-inline-actions">{hasPermission('admin:write') && <button className="primary-button" onClick={() => { setDraft({ name: '', host: '', port: 9100, paperWidthMm: 80, autoPrintKitchenTickets: false, autoPrintCustomerReceipts: true, autoPrintFiscalDocuments: false, isActive: true }); setShowForm(true) }}><Plus size={16} /> Adicionar impressora de rede</button>}</div>
  {showForm && <form className="surface-card settings-form" onSubmit={savePrinter}><div className="card-heading"><div><h2>{draft.id ? 'Editar' : 'Nova'} impressora ESC/POS</h2><p>Conexão TCP na rede local. A porta mais comum é 9100.</p></div></div><div className="form-grid three-columns"><label className="field-label">Nome<input required maxLength={100} value={draft.name} onChange={(event) => setDraft({ ...draft, name: event.target.value })} /></label><label className="field-label">IP ou host<input required maxLength={255} value={draft.host} onChange={(event) => setDraft({ ...draft, host: event.target.value })} placeholder="192.168.1.100" /></label><label className="field-label">Porta<input required type="number" min={1} max={65535} value={draft.port} onChange={(event) => setDraft({ ...draft, port: Number(event.target.value) })} /></label><label className="field-label">Papel<select value={draft.paperWidthMm} onChange={(event) => setDraft({ ...draft, paperWidthMm: Number(event.target.value) })}><option value={80}>80 mm</option><option value={58}>58 mm</option></select></label><div className="check-stack"><label className="check-label"><input type="checkbox" checked={draft.autoPrintKitchenTickets} onChange={(event) => setDraft({ ...draft, autoPrintKitchenTickets: event.target.checked })} /> Tickets da cozinha</label><label className="check-label"><input type="checkbox" checked={draft.autoPrintCustomerReceipts} onChange={(event) => setDraft({ ...draft, autoPrintCustomerReceipts: event.target.checked })} /> Comprovantes</label><label className="check-label"><input type="checkbox" checked={draft.autoPrintFiscalDocuments} disabled /> NFC-e automática (aguardando configuração fiscal)</label></div></div><div className="form-actions"><button className="primary-button" disabled={busy === 'new'}><Save size={16} /> Salvar impressora</button></div></form>}
  <section className="management-grid">
    {printers.map((printer) => <article className="surface-card management-card" key={printer.id}>
      <div className="device-icon"><Printer /></div>
      <div><span className={`status-pill ${printer.status === 'Online' ? 'success' : 'danger'}`}>{translateEnum(printer.status)}</span><h2>{printer.name}</h2><p>{printer.platform} · {printer.ipAddress ?? 'Não configurada'}:{printer.printerPort ?? 9100}</p><small>{printer.paperWidthMm ?? 80} mm · {printer.autoPrintCustomerReceipts ? 'comprovantes automáticos' : 'impressão manual'}</small></div>
      {hasPermission('admin:write') && <div className="management-card-actions"><button className="secondary-button" onClick={() => { setDraft({ id: printer.id, name: printer.name, host: printer.ipAddress ?? '', port: printer.printerPort ?? 9100, paperWidthMm: printer.paperWidthMm ?? 80, autoPrintKitchenTickets: Boolean(printer.autoPrintKitchenTickets), autoPrintCustomerReceipts: Boolean(printer.autoPrintCustomerReceipts), autoPrintFiscalDocuments: false, isActive: printer.status === 'Online' }); setShowForm(true) }}><Settings2 size={16} /> Configurar</button><button className="secondary-button" disabled={busy === printer.id} onClick={() => void testPrinter(printer)}><Printer size={16} /> Testar</button><button className="secondary-button" disabled={busy === printer.id} onClick={() => printer.status === 'Online' ? setPendingPrinter(printer) : void toggle(printer)}><Wifi size={16} /> {busy === printer.id ? 'Atualizando...' : printer.status === 'Online' ? 'Marcar offline' : 'Marcar online'}</button></div>}
    </article>)}
  </section>{printJobs.length > 0 && <section className="surface-card print-job-history"><h2>Fila de impressão</h2>{printJobs.slice(0, 10).map((job) => <div key={job.id}><span>{job.printerName} · {translateEnum(job.documentType)}</span><strong>{translateEnum(job.status)}</strong>{job.lastError && <small>{job.lastError}</small>}</div>)}</section>}<section className="surface-card fiscal-readiness-card"><div><span className="status-pill danger">Não configurada</span><h2>Emissão fiscal NFC-e</h2><p>A impressão atual é um comprovante não fiscal. Para autorizar NFC-e faltam UF, credenciamento, regime, cadastro tributário dos produtos, certificado e CSC válidos. Nenhum XML ou protocolo é simulado.</p></div></section><ConfirmDialog open={Boolean(pendingPrinter)} title="Marcar impressora como offline?" description={`A fila deixará de enviar trabalhos para ${pendingPrinter?.name ?? 'impressora'} até ela voltar a ficar online.`} confirmLabel="Marcar offline" tone="danger" busy={Boolean(pendingPrinter && busy === pendingPrinter.id)} onOpenChange={(open) => !open && setPendingPrinter(undefined)} onConfirm={() => { if (pendingPrinter) void toggle(pendingPrinter).finally(() => setPendingPrinter(undefined)) }} /></>
}

function BackupSettings() {
  const { data: snapshot, setData: setSnapshot, refresh: refreshSnapshot } = useAdminQuery(queryKeys.systemSnapshot, adminService.systemSnapshot)
  const { data: backups, setData: setBackups } = useAdminQuery(queryKeys.backups, adminService.backups)
  const [generated, setGenerated] = useState(false)
  const [generating, setGenerating] = useState(false)
  const [creatingBackup, setCreatingBackup] = useState(false)
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

  async function createDatabaseBackup() {
    setCreatingBackup(true)
    try {
      const backup = await adminService.createBackup()
      setBackups((current) => [backup, ...current.filter((item) => item.fileName !== backup.fileName)])
      toast.success('Backup físico concluído', 'O PostgreSQL gerou um arquivo restaurável com pg_restore.')
    } catch (error) {
      toast.error('Não foi possível gerar o backup', getUserErrorMessage(error))
    } finally {
      setCreatingBackup(false)
    }
  }

  async function downloadBackup(fileName: string) {
    try {
      const blob = await adminService.downloadBackup(fileName)
      const url = URL.createObjectURL(blob)
      const anchor = document.createElement('a')
      anchor.href = url
      anchor.download = fileName
      anchor.click()
      URL.revokeObjectURL(url)
    } catch (error) {
      toast.error('Não foi possível baixar o backup', getUserErrorMessage(error))
    }
  }

  return <section className="settings-grid">
    <article className="surface-card backup-card">
      <Database size={28} />
      <div><h2>Backup físico do PostgreSQL</h2><p>Gera um arquivo no formato custom do PostgreSQL, adequado para restauração com pg_restore. A rotina automática respeita o intervalo e a retenção configurados no servidor.</p></div>
      {hasPermission('admin:write') && <button className="primary-button" disabled={creatingBackup} aria-busy={creatingBackup} onClick={() => void createDatabaseBackup()}><Database size={16} /> {creatingBackup ? 'Executando pg_dump...' : 'Criar backup agora'}</button>}
      <div className="responsive-table wide"><table><thead><tr><th>Arquivo</th><th>Tipo</th><th>Criação</th><th>Tamanho</th><th aria-label="Ações" /></tr></thead><tbody>
        {backups.map((backup) => <tr key={backup.fileName}><td><strong>{backup.fileName}</strong></td><td>{backup.type}</td><td>{new Date(backup.createdAt).toLocaleString('pt-BR')}</td><td>{formatBytes(backup.sizeBytes)}</td><td><button className="secondary-button" onClick={() => void downloadBackup(backup.fileName)}><Download size={15} /> Baixar</button></td></tr>)}
      </tbody></table></div>
      {!backups.length && <div className="empty-inline">Nenhum backup físico disponível.</div>}
    </article>
    <article className="surface-card backup-card">
      <Database size={28} />
      <div><h2>Snapshot administrativo (JSON)</h2><p>Exportação auxiliar de identificação e contadores; não substitui o backup físico acima.</p></div>
      {hasPermission('admin:write') && <button className="primary-button" disabled={generating} aria-busy={generating} onClick={() => void downloadSnapshot()}><Download size={16} /> {generating ? 'Gerando...' : 'Gerar e baixar'}</button>}
      {generated && <span className="saved-feedback"><CheckCircle2 size={16} /> Arquivo gerado</span>}
    </article>
    <article className="surface-card system-summary">
      <h2>Informações do sistema</h2>
      <dl><div><dt>Último snapshot</dt><dd>{new Date(snapshot.generatedAt).toLocaleString('pt-BR')}</dd></div><div><dt>Produtos</dt><dd>{snapshot.products}</dd></div><div><dt>Mesas</dt><dd>{snapshot.tables}</dd></div><div><dt>Pedidos</dt><dd>{snapshot.orders}</dd></div><div><dt>Dispositivos</dt><dd>{snapshot.devices}</dd></div></dl>
    </article>
  </section>
}

function formatBytes(value: number) {
  if (value < 1024) return `${value} B`
  if (value < 1024 ** 2) return `${(value / 1024).toFixed(1)} KB`
  return `${(value / 1024 ** 2).toFixed(1)} MB`
}

function Toggle({ label, checked, onChange }: { label: string; checked: boolean; onChange: (value: boolean) => void }) {
  return <label className="toggle-row"><span>{label}</span><input type="checkbox" checked={checked} onChange={(event) => onChange(event.target.checked)} /></label>
}
