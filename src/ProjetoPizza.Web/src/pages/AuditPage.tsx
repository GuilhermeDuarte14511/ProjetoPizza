import { Download, History, Search } from 'lucide-react'
import { useMemo, useState } from 'react'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { queryKeys } from '../lib/queryKeys'
import { PageHeader } from '../components/ui/PageHeader'
import { adminService } from '../services/adminService'
import { translateEnum } from '../utils/presentation'

export function AuditPage() {
  const { data: logs } = useAdminQuery(queryKeys.audit, adminService.audit)
  const [search, setSearch] = useState('')
  const visible = useMemo(() => logs.filter((log) => `${log.module} ${log.action} ${log.entityType} ${log.entityDescription ?? ''} ${log.employee ?? ''}`.toLowerCase().includes(search.toLowerCase())), [logs, search])

  function exportCsv() {
    const rows = [['Data', 'Módulo', 'Ação', 'Entidade', 'Identificador', 'Usuário'], ...visible.map((log) => [log.occurredAt, log.module, log.action, log.entityType, log.entityId, log.employee ?? 'Sistema'])]
    const url = URL.createObjectURL(new Blob([rows.map((row) => row.join(';')).join('\n')], { type: 'text/csv' }))
    const anchor = document.createElement('a'); anchor.href = url; anchor.download = 'auditoria.csv'; anchor.click(); URL.revokeObjectURL(url)
  }

  return (
    <>
      <PageHeader title="Auditoria e histórico" description="Rastro imutável das ações administrativas." actions={<button className="secondary-button" onClick={exportCsv}><Download size={16} /> Exportar</button>} />
      <div className="toolbar"><div className="toolbar-search"><Search size={17} /><input aria-label="Buscar no histórico" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar módulo, ação ou usuário..." /></div></div>
      <section className="surface-card timeline-list">{visible.map((log) => <article key={log.id}><span className="timeline-icon"><History size={16} /></span><div><strong>{translateEnum(log.action)} · {translateEnum(log.entityType)}</strong><p>{translateEnum(log.module)} — {log.entityDescription ?? log.entityId}</p><small>{log.employee ?? 'Sistema'} · {new Date(log.occurredAt).toLocaleString('pt-BR')}</small></div></article>)}</section>
    </>
  )
}
