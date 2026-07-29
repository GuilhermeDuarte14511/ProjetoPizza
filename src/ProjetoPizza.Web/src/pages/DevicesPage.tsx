import {
  Battery,
  BatteryCharging,
  Copy,
  ExternalLink,
  Link2,
  LockKeyhole,
  MonitorSmartphone,
  Plus,
  RefreshCw,
  Search,
  UnlockKeyhole,
  Wifi,
} from 'lucide-react'
import { QRCodeSVG } from 'qrcode.react'
import { useMemo, useState, type FormEvent } from 'react'
import { ConfirmDialog } from '../components/ui/ConfirmDialog'
import { Modal } from '../components/ui/Modal'
import { PageHeader } from '../components/ui/PageHeader'
import { useToast } from '../components/ui/toast'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { queryKeys } from '../lib/queryKeys'
import { adminService } from '../services/adminService'
import { hasPermission } from '../services/authSession'
import type { Device, DeviceProvisioning } from '../types/admin'
import { getUserErrorMessage } from '../utils/errors'
import { translateEnum } from '../utils/presentation'

type ProvisioningMode = 'create' | 'link'

interface TabletDraft {
  name: string
  platform: string
  linkedTableId: string
}

const emptyDraft: TabletDraft = {
  name: '',
  platform: 'iPadOS',
  linkedTableId: '',
}

export function DevicesPage() {
  const { data: devices, setData: setDevices, refresh: refreshDevices, isRefreshing } = useAdminQuery(queryKeys.devices, adminService.devices)
  const { data: tables } = useAdminQuery(queryKeys.tables, adminService.tables)
  const [search, setSearch] = useState('')
  const [busy, setBusy] = useState<string>()
  const [pendingDevice, setPendingDevice] = useState<Device>()
  const [provisioningMode, setProvisioningMode] = useState<ProvisioningMode>()
  const [selectedDevice, setSelectedDevice] = useState<Device>()
  const [draft, setDraft] = useState<TabletDraft>(emptyDraft)
  const [provisioning, setProvisioning] = useState<DeviceProvisioning>()
  const toast = useToast()
  const visible = useMemo(
    () => devices.filter((device) =>
      `${device.name} ${device.serialNumber} ${device.status}`.toLowerCase().includes(search.toLowerCase())),
    [devices, search],
  )
  const tableNames = useMemo(
    () => new Map(tables.map((table) => [table.id, table.name])),
    [tables],
  )
  const activationUrl = provisioning
    ? `${window.location.origin}/mesa#provisioningToken=${encodeURIComponent(provisioning.activationToken)}`
    : ''

  async function toggleLock(device: Device) {
    setBusy(device.id)
    try {
      const updated = { ...device, isLocked: !device.isLocked, status: !device.isLocked ? 'Blocked' : 'Offline' }
      await adminService.updateDevice(updated)
      setDevices((current) => current.map((item) => item.id === device.id ? updated : item))
      toast.success(device.isLocked ? 'Dispositivo desbloqueado' : 'Dispositivo bloqueado', `${device.name} foi atualizado.`)
    } catch (error) {
      toast.error('Não foi possível atualizar o dispositivo', getUserErrorMessage(error))
    } finally {
      setBusy(undefined)
    }
  }

  async function refresh() {
    try {
      await refreshDevices()
      toast.success('Dispositivos atualizados', 'A lista foi sincronizada.')
    } catch (error) {
      toast.error('Não foi possível atualizar os dispositivos', getUserErrorMessage(error))
    }
  }

  function openCreateTablet() {
    setSelectedDevice(undefined)
    setProvisioning(undefined)
    setDraft({ ...emptyDraft, linkedTableId: tables[0]?.id ?? '' })
    setProvisioningMode('create')
  }

  function openLinkTablet(device: Device) {
    setSelectedDevice(device)
    setProvisioning(undefined)
    setDraft({
      name: device.name,
      platform: device.platform,
      linkedTableId: device.linkedTableId ?? tables[0]?.id ?? '',
    })
    setProvisioningMode('link')
  }

  function closeProvisioning() {
    if (busy === 'provisioning') return
    setProvisioningMode(undefined)
    setSelectedDevice(undefined)
    setProvisioning(undefined)
  }

  async function submitProvisioning(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!draft.linkedTableId) {
      toast.error('Selecione uma mesa', 'O tablet precisa ser vinculado a uma mesa.')
      return
    }
    if (provisioningMode === 'create' && !draft.name.trim()) {
      toast.error('Informe o nome do tablet', 'Use um nome que facilite a identificação do dispositivo.')
      return
    }

    setBusy('provisioning')
    try {
      const result = provisioningMode === 'create'
        ? await adminService.createCustomerTablet({
            name: draft.name.trim(),
            platform: draft.platform,
            linkedTableId: draft.linkedTableId,
          })
        : await adminService.provisionCustomerTablet(selectedDevice!.id, draft.linkedTableId)
      setProvisioning(result)
      setDevices((current) => {
        const exists = current.some((device) => device.id === result.device.id)
        return exists
          ? current.map((device) => device.id === result.device.id ? result.device : device)
          : [...current, result.device]
      })
      toast.success(
        provisioningMode === 'create' ? 'Tablet adicionado' : 'Link de ativação renovado',
        `${result.device.name} foi vinculado à ${tableNames.get(result.device.linkedTableId ?? '') ?? 'mesa selecionada'}.`,
      )
    } catch (error) {
      toast.error('Não foi possível vincular o tablet', getUserErrorMessage(error))
    } finally {
      setBusy(undefined)
    }
  }

  async function copyActivationUrl() {
    try {
      await navigator.clipboard.writeText(activationUrl)
      toast.success('URL copiada', 'Cole o endereço no navegador do tablet.')
    } catch {
      toast.error('Não foi possível copiar', 'Selecione a URL e copie manualmente.')
    }
  }

  return (
    <>
      <PageHeader
        title="Tablets e dispositivos"
        description="Cadastre, vincule às mesas e monitore os dispositivos do atendimento."
        actions={(
          <>
            <button className="secondary-button" disabled={isRefreshing} onClick={() => void refresh()}>
              <RefreshCw className={isRefreshing ? 'spin' : ''} size={16} /> {isRefreshing ? 'Atualizando...' : 'Atualizar'}
            </button>
            {hasPermission('admin:write') && (
              <button className="primary-button" onClick={openCreateTablet}>
                <Plus size={16} /> Adicionar novo tablet
              </button>
            )}
          </>
        )}
      />
      <section className="cash-metrics">
        <article><span>Total</span><strong>{devices.length}</strong></article>
        <article><span>Conectados</span><strong>{devices.filter((item) => item.status === 'Online').length}</strong></article>
        <article><span>Com bateria baixa</span><strong>{devices.filter((item) => (item.batteryPercentage ?? 100) < 25).length}</strong></article>
        <article><span>Bloqueados</span><strong>{devices.filter((item) => item.isLocked).length}</strong></article>
      </section>
      <div className="toolbar">
        <div className="toolbar-search">
          <Search size={17} />
          <input aria-label="Buscar dispositivo" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar dispositivo..." />
        </div>
      </div>
      <section className="surface-card data-table-card">
        <div className="data-table-header device-grid"><span>Dispositivo</span><span>Status</span><span>Bateria</span><span>Rede</span><span>Ações</span></div>
        {visible.map((device) => (
          <div className="data-table-row device-grid" key={device.id}>
            <span className="cell-title">
              <MonitorSmartphone size={18} />
              <span>
                <strong>{device.name}</strong>
                <small>{device.serialNumber} · {device.platform} · {device.linkedTableId ? tableNames.get(device.linkedTableId) ?? 'Mesa vinculada' : 'Sem mesa'}</small>
              </span>
            </span>
            <span className={`status-pill ${device.status === 'Online' ? 'success' : device.status === 'Blocked' ? 'danger' : 'warning'}`}>{translateEnum(device.status)}</span>
            <span className="cell-title">{device.isCharging ? <BatteryCharging size={17} /> : <Battery size={17} />} {device.batteryPercentage == null ? '—' : `${device.batteryPercentage}%`}</span>
            <span><Wifi size={15} /> {device.networkStatus ?? 'Sem rede'}<small>{device.ipAddress}</small></span>
            {hasPermission('admin:write') && (
              <span className="table-actions">
                {device.type === 'CustomerTablet' && (
                  <button className="secondary-button" disabled={device.isLocked} onClick={() => openLinkTablet(device)}>
                    <Link2 size={15} /> Vincular
                  </button>
                )}
                <button className="secondary-button" disabled={busy === device.id} onClick={() => device.isLocked ? void toggleLock(device) : setPendingDevice(device)}>
                  {device.isLocked ? <UnlockKeyhole size={15} /> : <LockKeyhole size={15} />}
                  {device.isLocked ? 'Desbloquear' : 'Bloquear'}
                </button>
              </span>
            )}
          </div>
        ))}
      </section>

      {provisioningMode && (
        <Modal
          open
          title={provisioningMode === 'create' ? 'Adicionar novo tablet' : `Vincular ${selectedDevice?.name ?? 'tablet'}`}
          description={provisioning
            ? 'Abra esta URL no tablet ou leia o QR Code. A credencial expira e só pode ser usada uma vez.'
            : 'Escolha a mesa que receberá o cardápio digital.'}
          size="large"
          isBusy={busy === 'provisioning'}
          onClose={closeProvisioning}
        >
          {provisioning ? (
            <div className="tablet-provisioning-result">
              <div className="tablet-qr-card">
                <QRCodeSVG
                  value={activationUrl}
                  size={220}
                  level="M"
                  marginSize={2}
                  title={`Ativar ${provisioning.device.name}`}
                />
              </div>
              <div className="tablet-activation-details">
                <span className="eyebrow">Link seguro de ativação</span>
                <h3>{provisioning.device.name}</h3>
                <p>
                  {tableNames.get(provisioning.device.linkedTableId ?? '') ?? 'Mesa vinculada'} · válido até{' '}
                  {new Date(provisioning.expiresAt).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}
                </p>
                <label>
                  URL para tablets sem câmera
                  <textarea value={activationUrl} readOnly rows={4} onFocus={(event) => event.currentTarget.select()} />
                </label>
                <div className="tablet-activation-actions">
                  <button type="button" className="primary-button" onClick={() => void copyActivationUrl()}>
                    <Copy size={16} /> Copiar URL
                  </button>
                  <a className="secondary-button" href={activationUrl} target="_blank" rel="noreferrer">
                    <ExternalLink size={16} /> Testar link
                  </a>
                </div>
                <small>Por segurança, gerar um novo link invalida o anterior. A mesa precisa estar aberta para concluir a ativação.</small>
              </div>
            </div>
          ) : (
            <form onSubmit={(event) => void submitProvisioning(event)}>
              <div className="form-grid two-columns">
                {provisioningMode === 'create' && (
                  <>
                    <label>
                      Nome do tablet
                      <input
                        autoFocus
                        maxLength={100}
                        required
                        value={draft.name}
                        onChange={(event) => setDraft((current) => ({ ...current, name: event.target.value }))}
                        placeholder="Ex.: Tablet salão 03"
                      />
                    </label>
                    <label>
                      Sistema
                      <select value={draft.platform} onChange={(event) => setDraft((current) => ({ ...current, platform: event.target.value }))}>
                        <option value="iPadOS">iPadOS</option>
                        <option value="Android">Android</option>
                        <option value="Windows">Windows</option>
                        <option value="Web">Outro navegador</option>
                      </select>
                    </label>
                  </>
                )}
                <label className="wide">
                  Mesa vinculada
                  <select required value={draft.linkedTableId} onChange={(event) => setDraft((current) => ({ ...current, linkedTableId: event.target.value }))}>
                    <option value="">Selecione uma mesa</option>
                    {tables.map((table) => <option key={table.id} value={table.id}>{table.name} · {table.area}</option>)}
                  </select>
                </label>
              </div>
              <div className="modal-footer">
                <button type="button" className="secondary-button" onClick={closeProvisioning}>Cancelar</button>
                <button className="primary-button" disabled={busy === 'provisioning'} aria-busy={busy === 'provisioning'}>
                  <Link2 size={16} /> {busy === 'provisioning' ? 'Gerando...' : 'Vincular e gerar QR Code'}
                </button>
              </div>
            </form>
          )}
        </Modal>
      )}

      <ConfirmDialog
        open={Boolean(pendingDevice)}
        title="Bloquear dispositivo?"
        description={`O ${pendingDevice?.name ?? 'dispositivo'} perderá o acesso à operação até ser desbloqueado.`}
        confirmLabel="Bloquear"
        tone="danger"
        busy={Boolean(pendingDevice && busy === pendingDevice.id)}
        onOpenChange={(open) => !open && setPendingDevice(undefined)}
        onConfirm={() => {
          if (pendingDevice) void toggleLock(pendingDevice).finally(() => setPendingDevice(undefined))
        }}
      />
    </>
  )
}
