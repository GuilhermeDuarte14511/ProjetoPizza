import { Plus, Search, Settings2, UsersRound } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Link, useLocation } from 'wouter'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { usePdfTableExport } from '../hooks/usePdfTableExport'
import { queryKeys } from '../lib/queryKeys'
import { PageHeader } from '../components/ui/PageHeader'
import { PdfExportButton } from '../components/ui/PdfExportButton'
import { StatusBadge } from '../components/ui/StatusBadge'
import { ViewTransitionLink } from '../components/ui/ViewTransitionLink'
import { adminService } from '../services/adminService'
import { hasPermission } from '../services/authSession'
import type { TableVisualStatus } from '../types/admin'

const currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })
const filters: Array<'Todas' | TableVisualStatus> = ['Todas', 'Ocupada', 'Livre', 'Chamando', 'Conta solicitada', 'Pagamento pendente']

export function TablesPage() {
  const [, navigate] = useLocation()
  const { data: tables } = useAdminQuery(queryKeys.tables, adminService.tables)
  const [activeFilter, setActiveFilter] = useState<(typeof filters)[number]>('Todas')
  const [search, setSearch] = useState(() => new URLSearchParams(window.location.search).get('search') ?? '')
  const { exportPdf, exporting } = usePdfTableExport()

  const visibleTables = useMemo(() => tables.filter((table) =>
    (activeFilter === 'Todas' || table.status === activeFilter) &&
    table.name.toLowerCase().includes(search.toLowerCase())), [activeFilter, search, tables])
  const firstAvailableTable = tables.find((table) => table.status === 'Livre')

  function exportTables() {
    const activeTables = visibleTables.filter((table) => table.status !== 'Livre')
    const guests = visibleTables.reduce((sum, table) => sum + (table.guestCount ?? 0), 0)
    const total = visibleTables.reduce((sum, table) => sum + table.currentTotal, 0)
    void exportPdf({
      title: 'Relatório de mesas',
      subtitle: `Status: ${activeFilter}${search ? ` · Busca: ${search}` : ''}`,
      fileName: `mesas-${new Date().toISOString().slice(0, 10)}.pdf`,
      orientation: 'landscape',
      columns: ['Mesa', 'Área', 'Status', 'Pessoas', 'Capacidade', 'Abertura', 'Total atual', 'Chamado'],
      rows: visibleTables.map((table) => [
        `${table.number.toString().padStart(2, '0')} · ${table.name}`,
        table.area,
        table.status,
        String(table.guestCount ?? 0),
        String(table.capacity),
        table.openedAt ? new Date(table.openedAt).toLocaleString('pt-BR') : '—',
        currency.format(table.currentTotal),
        table.hasPendingCall ? 'Pendente' : 'Não',
      ]),
      metrics: [
        { label: 'Mesas listadas', value: String(visibleTables.length) },
        { label: 'Em atendimento', value: String(activeTables.length) },
        { label: 'Pessoas', value: String(guests) },
        { label: 'Total em aberto', value: currency.format(total) },
      ],
      rightAlignedColumns: [3, 4, 6],
    })
  }

  return (
    <>
      <PageHeader
        title="Mesas"
        description="Gerencie a ocupação e o atendimento do salão principal."
        actions={<>
          <PdfExportButton exporting={exporting} onClick={exportTables} label="Exportar mesas em PDF" />
          {hasPermission('admin:write') && <ViewTransitionLink className="secondary-button" href="/admin/settings/structure"><Settings2 size={16} /> Adicionar ou excluir</ViewTransitionLink>}
          {hasPermission('operations:write') &&
            <button
              className="primary-button"
              disabled={!firstAvailableTable}
              onClick={() => firstAvailableTable && navigate(`/admin/tables/${firstAvailableTable.id}`)}
            >
              <Plus size={16} /> Abrir mesa
            </button>
          }
        </>}
      />
      <div className="toolbar">
        <div className="filter-tabs" role="group" aria-label="Filtrar mesas por status">{filters.map((filter) => <button aria-pressed={filter === activeFilter} className={filter === activeFilter ? 'active' : ''} key={filter} onClick={() => setActiveFilter(filter)}>{filter}</button>)}</div>
        <div className="toolbar-search"><Search size={17} /><input aria-label="Buscar mesa" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar mesa..." /></div>
      </div>
      <section className="table-map">
        {visibleTables.map((table) => (
          <Link href={`/admin/tables/${table.id}`} key={table.id} className={`table-card table-${table.status.toLowerCase().replaceAll(' ', '-')}`}>
            <div className="table-card-top"><span className="table-number">{table.number.toString().padStart(2, '0')}</span><StatusBadge status={table.status} /></div>
            <div className="table-meta"><UsersRound size={16} /> {table.guestCount ? `${table.guestCount} pessoas` : `${table.capacity} lugares`}</div>
            {table.status !== 'Livre' ? <><strong className="table-total">{currency.format(table.currentTotal)}</strong><span className="table-action">Ver comanda</span></> : <span className="table-action">Abrir mesa</span>}
          </Link>
        ))}
      </section>
    </>
  )
}
