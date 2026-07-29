import { BellRing, CheckCheck, Clock3, Hand, RefreshCw, TableProperties } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Link } from 'wouter'
import { PageHeader } from '../components/ui/PageHeader'
import { StatusBadge } from '../components/ui/StatusBadge'
import { useToast } from '../components/ui/toast'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { queryKeys } from '../lib/queryKeys'
import { adminService } from '../services/adminService'
import { hasPermission } from '../services/authSession'
import type { ServiceCall } from '../types/admin'
import { getUserErrorMessage } from '../utils/errors'

export function ServiceCallsPage() {
  const { data: calls, setData: setCalls, refresh, isRefreshing } = useAdminQuery(queryKeys.serviceCalls, adminService.serviceCalls)
  const { data: settings } = useAdminQuery(queryKeys.operationSettings, adminService.operationSettings)
  const [busyId, setBusyId] = useState<string>()
  const toast = useToast()
  const [now] = useState(() => Date.now())
  const orderedCalls = useMemo(
    () => [...calls].sort((left, right) => new Date(left.createdAt).getTime() - new Date(right.createdAt).getTime()),
    [calls],
  )

  async function acknowledge(call: ServiceCall) {
    setBusyId(call.id)
    try {
      await adminService.acknowledgeServiceCall(call.id)
      setCalls((current) => current.map((item) => item.id === call.id
        ? { ...item, status: 'Acknowledged', acknowledgedAt: new Date().toISOString() }
        : item))
      toast.success('Chamado assumido', `${call.tableName} sabe que a equipe está a caminho.`)
    } catch (error) {
      toast.error('Não foi possível assumir o chamado', getUserErrorMessage(error))
    } finally {
      setBusyId(undefined)
    }
  }

  async function complete(call: ServiceCall) {
    setBusyId(call.id)
    try {
      await adminService.completeServiceCall(call.id)
      setCalls((current) => current.filter((item) => item.id !== call.id))
      toast.success('Chamado concluído', `Atendimento da ${call.tableName} finalizado.`)
    } catch (error) {
      toast.error('Não foi possível concluir o chamado', getUserErrorMessage(error))
    } finally {
      setBusyId(undefined)
    }
  }

  return (
    <>
      <PageHeader
        title="Chamados das mesas"
        description="Assuma e conclua as solicitações enviadas pelos tablets."
        actions={
          <button className="secondary-button" disabled={isRefreshing} onClick={() => void refresh()}>
            <RefreshCw className={isRefreshing ? 'spin' : ''} size={16} />
            {isRefreshing ? 'Atualizando...' : 'Atualizar'}
          </button>
        }
      />
      <section className="service-call-summary" aria-label="Resumo dos chamados">
        <div><BellRing aria-hidden="true" /><span><strong>{calls.length}</strong><small>Chamados ativos</small></span></div>
        <div><Clock3 aria-hidden="true" /><span><strong>{settings.tableCallToleranceMinutes} min</strong><small>Tolerância configurada</small></span></div>
      </section>
      {orderedCalls.length === 0 ? (
        <section className="surface-card service-call-empty">
          <CheckCheck aria-hidden="true" />
          <h2>Nenhum chamado pendente</h2>
          <p>As solicitações enviadas pelos tablets aparecerão aqui em tempo real.</p>
        </section>
      ) : (
        <section className="service-call-grid" aria-label="Fila de chamados">
          {orderedCalls.map((call) => {
            const elapsedMinutes = Math.max(0, Math.floor((now - new Date(call.createdAt).getTime()) / 60_000))
            const overdue = elapsedMinutes >= settings.tableCallToleranceMinutes
            const busy = busyId === call.id
            return (
              <article className={`surface-card service-call-card ${overdue ? 'overdue' : ''}`} key={call.id}>
                <header>
                  <span className="service-call-icon"><BellRing aria-hidden="true" /></span>
                  <div>
                    <small>{call.typeName}</small>
                    <h2>{call.tableName}</h2>
                  </div>
                  <StatusBadge status={call.status} />
                </header>
                <p>{call.details || 'Sem observações adicionais.'}</p>
                <dl>
                  <div><dt>Tempo</dt><dd>{formatElapsed(elapsedMinutes)}</dd></div>
                  <div><dt>Responsável</dt><dd>{call.assignedEmployee || 'Aguardando aceite'}</dd></div>
                </dl>
                <footer>
                  <Link className="secondary-button" href={`/admin/tables/${call.tableId}`}><TableProperties size={16} /> Ver mesa</Link>
                  {hasPermission('operations:write') && call.status === 'Pending' && (
                    <button className="primary-button" disabled={busy} onClick={() => void acknowledge(call)}>
                      <Hand size={16} /> {busy ? 'Assumindo...' : 'Assumir'}
                    </button>
                  )}
                  {hasPermission('operations:write') && call.status !== 'Pending' && (
                    <button className="primary-button" disabled={busy} onClick={() => void complete(call)}>
                      <CheckCheck size={16} /> {busy ? 'Concluindo...' : 'Concluir'}
                    </button>
                  )}
                </footer>
              </article>
            )
          })}
        </section>
      )}
    </>
  )
}

function formatElapsed(minutes: number) {
  if (minutes < 1) return 'Agora'
  if (minutes < 60) return `Há ${minutes} min`
  const hours = Math.floor(minutes / 60)
  const remaining = minutes % 60
  return remaining ? `Há ${hours}h ${remaining}min` : `Há ${hours}h`
}
