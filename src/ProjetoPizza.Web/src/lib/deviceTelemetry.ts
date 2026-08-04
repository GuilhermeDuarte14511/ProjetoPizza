import type { ClientTelemetry } from '../types/client'

export interface ClientBatteryManager extends EventTarget {
  charging: boolean
  level: number
}

interface NavigatorWithBattery extends Navigator {
  getBattery?: () => Promise<ClientBatteryManager>
}

export async function getClientBattery(): Promise<ClientBatteryManager | undefined> {
  const getBattery = (navigator as NavigatorWithBattery).getBattery
  if (typeof getBattery !== 'function') return undefined

  try {
    return await getBattery.call(navigator)
  } catch {
    return undefined
  }
}

export function createClientTelemetry(battery?: ClientBatteryManager): ClientTelemetry {
  return {
    batteryPercentage: battery ? Math.round(battery.level * 100) : undefined,
    isCharging: battery?.charging ?? false,
    networkStatus: navigator.onLine ? 'Online' : 'Offline',
    appVersion: 'Web',
  }
}
