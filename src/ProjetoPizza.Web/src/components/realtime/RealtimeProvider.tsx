import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr'
import { useQueryClient } from '@tanstack/react-query'
import { WifiOff } from 'lucide-react'
import { useEffect, useState, type ReactNode } from 'react'
import { apiBaseUrl, getAccessToken, isApiConfigured } from '../../api/httpClient'
import { queryKeys } from '../../lib/queryKeys'
import { useToast } from '../ui/toast'

type ConnectionStatus = 'connected' | 'reconnecting' | 'offline'

export function RealtimeProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const toast = useToast()
  const [status, setStatus] = useState<ConnectionStatus>(navigator.onLine ? 'connected' : 'offline')

  useEffect(() => {
    function handleOffline() {
      setStatus('offline')
    }

    function handleOnline() {
      setStatus('reconnecting')
      void queryClient.invalidateQueries({ queryKey: queryKeys.all })
    }

    window.addEventListener('offline', handleOffline)
    window.addEventListener('online', handleOnline)
    return () => {
      window.removeEventListener('offline', handleOffline)
      window.removeEventListener('online', handleOnline)
    }
  }, [queryClient])

  useEffect(() => {
    if (!isApiConfigured || !getAccessToken()) return

    const connection = new HubConnectionBuilder()
      .withUrl(`${apiBaseUrl}/hubs/admin`, { accessTokenFactory: () => getAccessToken() ?? '' })
      .withAutomaticReconnect([0, 2_000, 5_000, 10_000, 30_000])
      .configureLogging(import.meta.env.DEV ? LogLevel.Warning : LogLevel.Error)
      .build()

    connection.on('admin:changed', () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.all })
    })
    connection.onreconnecting(() => setStatus('reconnecting'))
    connection.onreconnected(() => {
      setStatus('connected')
      void queryClient.invalidateQueries({ queryKey: queryKeys.all })
      toast.success('Conexão restabelecida', 'Os dados administrativos foram sincronizados.')
    })
    connection.onclose(() => setStatus(navigator.onLine ? 'reconnecting' : 'offline'))

    void connection.start()
      .then(() => setStatus('connected'))
      .catch(() => setStatus(navigator.onLine ? 'reconnecting' : 'offline'))

    return () => {
      connection.off('admin:changed')
      if (connection.state !== HubConnectionState.Disconnected) void connection.stop()
    }
  }, [queryClient, toast])

  return (
    <>
      {children}
      {status !== 'connected' && (
        <div className="connection-banner" role="status" aria-live="polite">
          <WifiOff size={15} />
          {status === 'offline' ? 'Sem conexão. Suas ações serão retomadas quando a internet voltar.' : 'Reconectando e sincronizando dados...'}
        </div>
      )}
    </>
  )
}
