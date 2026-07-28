import { Battery, BatteryCharging, LockKeyhole, MonitorSmartphone, RefreshCw, Search, UnlockKeyhole, Wifi } from 'lucide-react'
import { useMemo, useState } from 'react'
import { ConfirmDialog } from '../components/ui/ConfirmDialog'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { queryKeys } from '../lib/queryKeys'
import { PageHeader } from '../components/ui/PageHeader'
import { useToast } from '../components/ui/toast'
import { adminService } from '../services/adminService'
import { hasPermission } from '../services/authSession'
import type { Device } from '../types/admin'
import { getUserErrorMessage } from '../utils/errors'
import { translateEnum } from '../utils/presentation'

export function DevicesPage() {
  const { data: devices, setData: setDevices, refresh: refreshDevices, isRefreshing } = useAdminQuery(queryKeys.devices, adminService.devices)
  const [search, setSearch] = useState('')
  const [busy, setBusy] = useState<string>()
  const [pendingDevice, setPendingDevice] = useState<Device>()
  const toast = useToast()
  const visible = useMemo(() => devices.filter((device) => `${device.name} ${device.serialNumber} ${device.status}`.toLowerCase().includes(search.toLowerCase())), [devices, search])

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

  return (
    <>
      <PageHeader title="Tablets e dispositivos" description="Monitore conectividade, bateria e vínculo com mesas." actions={<button className="secondary-button" disabled={isRefreshing} onClick={() => void refresh()}><RefreshCw className={isRefreshing ? 'spin' : ''} size={16} /> {isRefreshing ? 'Atualizando...' : 'Atualizar'}</button>} />
      <section className="cash-metrics"><article><span>Total</span><strong>{devices.length}</strong></article><article><span>Conectados</span><strong>{devices.filter((item) => item.status === 'Online').length}</strong></article><article><span>Com bateria baixa</span><strong>{devices.filter((item) => (item.batteryPercentage ?? 100) < 25).length}</strong></article><article><span>Bloqueados</span><strong>{devices.filter((item) => item.isLocked).length}</strong></article></section>
      <div className="toolbar"><div className="toolbar-search"><Search size={17} /><input aria-label="Buscar dispositivo" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar dispositivo..." /></div></div>
      <section className="surface-card data-table-card">
        <div className="data-table-header device-grid"><span>Dispositivo</span><span>Status</span><span>Bateria</span><span>Rede</span><span>Ações</span></div>
        {visible.map((device) => <div className="data-table-row device-grid" key={device.id}>
          <span className="cell-title"><MonitorSmartphone size={18} /><span><strong>{device.name}</strong><small>{device.serialNumber} · {device.platform}</small></span></span>
          <span className={`status-pill ${device.status === 'Online' ? 'success' : device.status === 'Blocked' ? 'danger' : 'warning'}`}>{translateEnum(device.status)}</span>
          <span className="cell-title">{device.isCharging ? <BatteryCharging size={17} /> : <Battery size={17} />} {device.batteryPercentage == null ? '—' : `${device.batteryPercentage}%`}</span>
          <span><Wifi size={15} /> {device.networkStatus ?? 'Sem rede'}<small>{device.ipAddress}</small></span>
          {hasPermission('admin:write') && <button className="secondary-button" disabled={busy === device.id} onClick={() => device.isLocked ? void toggleLock(device) : setPendingDevice(device)}>{device.isLocked ? <UnlockKeyhole size={15} /> : <LockKeyhole size={15} />}{device.isLocked ? 'Desbloquear' : 'Bloquear'}</button>}
        </div>)}
      </section>
      <ConfirmDialog open={Boolean(pendingDevice)} title="Bloquear dispositivo?" description={`O ${pendingDevice?.name ?? 'dispositivo'} perderá o acesso à operação até ser desbloqueado.`} confirmLabel="Bloquear" tone="danger" busy={Boolean(pendingDevice && busy === pendingDevice.id)} onOpenChange={(open) => !open && setPendingDevice(undefined)} onConfirm={() => { if (pendingDevice) void toggleLock(pendingDevice).finally(() => setPendingDevice(undefined)) }} />
    </>
  )
}
