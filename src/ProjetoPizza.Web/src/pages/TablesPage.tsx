import { Plus, Search, UsersRound } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Link, useLocation } from 'wouter'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { queryKeys } from '../lib/queryKeys'
import { PageHeader } from '../components/ui/PageHeader'
import { StatusBadge } from '../components/ui/StatusBadge'
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

  const visibleTables = useMemo(() => tables.filter((table) =>
    (activeFilter === 'Todas' || table.status === activeFilter) &&
    table.name.toLowerCase().includes(search.toLowerCase())), [activeFilter, search, tables])
  const firstAvailableTable = tables.find((table) => table.status === 'Livre')

  return (
    <>
      <PageHeader
        title="Mesas"
        description="Gerencie a ocupação e o atendimento do salão principal."
        actions={hasPermission('operations:write') &&
          <button
            className="primary-button"
            disabled={!firstAvailableTable}
            onClick={() => firstAvailableTable && navigate(`/admin/tables/${firstAvailableTable.id}`)}
          >
            <Plus size={16} /> Abrir mesa
          </button>
        }
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
