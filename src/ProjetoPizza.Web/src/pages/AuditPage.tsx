import { History, Search } from 'lucide-react'
import { useMemo, useState } from 'react'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { usePdfTableExport } from '../hooks/usePdfTableExport'
import { queryKeys } from '../lib/queryKeys'
import { PageHeader } from '../components/ui/PageHeader'
import { PdfExportButton } from '../components/ui/PdfExportButton'
import { adminService } from '../services/adminService'
import { translateEnum } from '../utils/presentation'

export function AuditPage() {
  const { data: logs } = useAdminQuery(queryKeys.audit, adminService.audit)
  const [search, setSearch] = useState('')
  const { exportPdf, exporting } = usePdfTableExport()
  const visible = useMemo(() => logs.filter((log) => `${log.module} ${translateEnum(log.module)} ${log.action} ${translateEnum(log.action)} ${log.entityType} ${translateEnum(log.entityType)} ${log.entityDescription ?? ''} ${log.employee ?? ''}`.toLocaleLowerCase('pt-BR').includes(search.toLocaleLowerCase('pt-BR'))), [logs, search])

  function exportAudit() {
    void exportPdf({
      title: 'Relatório de auditoria',
      subtitle: search ? `Busca aplicada: ${search}` : 'Histórico administrativo completo',
      fileName: `auditoria-${new Date().toISOString().slice(0, 10)}.pdf`,
      orientation: 'landscape',
      columns: ['Data', 'Módulo', 'Ação', 'Entidade', 'Identificador', 'Usuário'],
      rows: visible.map((log) => [
        new Date(log.occurredAt).toLocaleString('pt-BR'),
        translateEnum(log.module),
        translateEnum(log.action),
        `${translateEnum(log.entityType)}${log.entityDescription ? ` · ${log.entityDescription}` : ''}`,
        log.entityId,
        log.employee ?? 'Sistema',
      ]),
      metrics: [
        { label: 'Registros', value: String(visible.length) },
        { label: 'Usuários', value: String(new Set(visible.map((log) => log.employee ?? 'Sistema')).size) },
        { label: 'Módulos', value: String(new Set(visible.map((log) => log.module)).size) },
        { label: 'Exportado em', value: new Date().toLocaleDateString('pt-BR') },
      ],
    })
  }

  return (
    <>
      <PageHeader title="Auditoria e histórico" description="Rastro imutável das ações administrativas." actions={<PdfExportButton exporting={exporting} onClick={exportAudit} label="Exportar auditoria em PDF" />} />
      <div className="toolbar"><div className="toolbar-search"><Search size={17} /><input aria-label="Buscar no histórico" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar módulo, ação ou usuário..." /></div></div>
      <section className="surface-card timeline-list">{visible.map((log) => <article key={log.id}><span className="timeline-icon"><History size={16} /></span><div><strong>{translateEnum(log.action)} · {translateEnum(log.entityType)}</strong><p>{translateEnum(log.module)} — {log.entityDescription ?? log.entityId}</p><small>{log.employee ?? 'Sistema'} · {new Date(log.occurredAt).toLocaleString('pt-BR')}</small></div></article>)}</section>
    </>
  )
}
