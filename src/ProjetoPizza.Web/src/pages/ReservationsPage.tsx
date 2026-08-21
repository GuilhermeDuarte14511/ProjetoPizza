import { BellRing, CalendarClock, Check, Clock3, Plus, Search, TableProperties, UserCheck, UserPlus, UserRoundCheck, UsersRound, X } from 'lucide-react'
import { type KeyboardEvent, useId, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { Modal } from '../components/ui/Modal'
import { PageHeader } from '../components/ui/PageHeader'
import { PhoneInput } from '../components/ui/PhoneInput'
import { StatusBadge } from '../components/ui/StatusBadge'
import { useToast } from '../components/ui/toast'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { queryKeys } from '../lib/queryKeys'
import { adminService } from '../services/adminService'
import { hasPermission } from '../services/authSession'
import type { Customer, Reservation, WaitlistEntry } from '../types/admin'
import { getUserErrorMessage } from '../utils/errors'
import { formatPhone } from '../utils/phone'

type Tab = 'reservations' | 'waitlist'
type NewEntry = 'reservation' | 'waitlist'
type SeatingTarget = { kind: 'reservation'; item: Reservation } | { kind: 'waitlist'; item: WaitlistEntry }

const initialDraft = {
  customerId: undefined as string | undefined, customerName: '', phone: '', birthDate: '', partySize: 2, scheduledAt: '', durationMinutes: 90,
  estimatedWaitMinutes: 30, notes: '',
}

export function ReservationsPage() {
  const reservationsQuery = useAdminQuery(queryKeys.reservations, adminService.reservations)
  const waitlistQuery = useAdminQuery(queryKeys.waitlist, adminService.waitlist)
  const customersQuery = useAdminQuery(queryKeys.customers, adminService.customers)
  const tablesQuery = useAdminQuery(queryKeys.tables, adminService.tables)
  const [tab, setTab] = useState<Tab>('reservations')
  const [creating, setCreating] = useState<NewEntry>()
  const [draft, setDraft] = useState(initialDraft)
  const [customerSearchOpen, setCustomerSearchOpen] = useState(false)
  const [activeSuggestion, setActiveSuggestion] = useState(0)
  const [busy, setBusy] = useState(false)
  const [seating, setSeating] = useState<SeatingTarget>()
  const [selectedTableIds, setSelectedTableIds] = useState<string[]>([])
  const customerListId = useId()
  const toast = useToast()
  const reservations = useMemo(() => [...reservationsQuery.data].sort((a, b) => a.scheduledAt.localeCompare(b.scheduledAt)), [reservationsQuery.data])
  const waiting = waitlistQuery.data.filter((entry) => entry.status === 'Waiting' || entry.status === 'Notified')
  const customerMatches = useMemo(() => {
    const term = normalizeSearch(draft.customerName)
    if (term.length < 2 || draft.customerId) return []
    return customersQuery.data
      .filter((customer) => customer.isActive && (normalizeSearch(customer.name).includes(term) || customer.phone.includes(term.replace(/\D/g, ''))))
      .slice(0, 6)
  }, [customersQuery.data, draft.customerId, draft.customerName])
  const selectedCustomer = draft.customerId
    ? customersQuery.data.find((customer) => customer.id === draft.customerId)
    : undefined
  const freeTables = tablesQuery.data.filter((table) => table.status === 'Livre')
  const selectedCapacity = freeTables
    .filter((table) => selectedTableIds.includes(table.id))
    .reduce((total, table) => total + table.capacity, 0)

  function openCreate(kind: NewEntry) {
    const nextHour = new Date(Date.now() + 60 * 60_000)
    nextHour.setMinutes(0, 0, 0)
    setDraft({ ...initialDraft, scheduledAt: toLocalDateTime(nextHour) })
    setCustomerSearchOpen(false)
    setActiveSuggestion(0)
    setCreating(kind)
  }

  function updateCustomerName(customerName: string) {
    setDraft((current) => ({
      ...current,
      customerId: customerName === current.customerName ? current.customerId : undefined,
      birthDate: customerName === current.customerName ? current.birthDate : '',
      customerName,
    }))
    setActiveSuggestion(0)
    setCustomerSearchOpen(true)
  }

  function selectCustomer(customer: Customer) {
    setDraft((current) => ({
      ...current,
      customerId: customer.id,
      customerName: customer.name,
      phone: customer.phone,
      birthDate: customer.birthDate,
    }))
    setCustomerSearchOpen(false)
  }

  function updatePhone(phone: string) {
    setDraft((current) => ({
      ...current,
      customerId: current.phone === phone ? current.customerId : undefined,
      birthDate: current.phone === phone ? current.birthDate : '',
      phone,
    }))
  }

  function handleCustomerKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (!customerMatches.length) return
    if (event.key === 'ArrowDown') {
      event.preventDefault()
      setCustomerSearchOpen(true)
      setActiveSuggestion((current) => (current + 1) % customerMatches.length)
    } else if (event.key === 'ArrowUp') {
      event.preventDefault()
      setCustomerSearchOpen(true)
      setActiveSuggestion((current) => (current - 1 + customerMatches.length) % customerMatches.length)
    } else if (event.key === 'Enter' && customerSearchOpen) {
      event.preventDefault()
      selectCustomer(customerMatches[activeSuggestion] ?? customerMatches[0])
    } else if (event.key === 'Escape') {
      setCustomerSearchOpen(false)
    }
  }

  async function create() {
    if (!draft.customerName.trim() || draft.phone.replace(/\D/g, '').length < 8 || (creating === 'reservation' && !draft.customerId && !draft.birthDate)) {
      toast.error('Dados incompletos', creating === 'reservation' && !draft.customerId && !draft.birthDate
        ? 'Informe a data de nascimento para cadastrar o novo cliente.'
        : 'Informe o nome e um telefone válido.')
      return
    }
    setBusy(true)
    try {
      if (creating === 'reservation') {
        const saved = await adminService.createReservation({
          customerId: draft.customerId, customerName: draft.customerName, phone: draft.phone, partySize: draft.partySize,
          scheduledAt: new Date(draft.scheduledAt).toISOString(), durationMinutes: draft.durationMinutes,
          notes: draft.notes || undefined, customerBirthDate: draft.customerId ? undefined : draft.birthDate,
        })
        reservationsQuery.setData((current) => [...current, saved])
        if (!draft.customerId) await customersQuery.refresh()
        toast.success('Reserva criada', draft.customerId ? 'O cliente foi vinculado e o horário incluído na agenda.' : 'O cliente foi cadastrado e o horário incluído na agenda.')
      } else {
        const saved = await adminService.createWaitlistEntry({
          customerId: draft.customerId, customerName: draft.customerName, phone: draft.phone, partySize: draft.partySize,
          estimatedWaitMinutes: draft.estimatedWaitMinutes, notes: draft.notes || undefined,
        })
        waitlistQuery.setData((current) => [...current, saved])
        toast.success('Cliente na fila', 'A posição já aparece para a equipe.')
      }
      setCreating(undefined)
    } catch (error) {
      toast.error('Não foi possível salvar', getUserErrorMessage(error))
    } finally {
      setBusy(false)
    }
  }

  async function transitionReservation(item: Reservation, transition: string) {
    setBusy(true)
    try {
      await adminService.transitionReservation(item.id, transition)
      reservationsQuery.setData((current) => current.map((reservation) => reservation.id === item.id ? { ...reservation, status: transition } : reservation))
    } catch (error) { toast.error('Não foi possível atualizar a reserva', getUserErrorMessage(error)) } finally { setBusy(false) }
  }

  async function transitionWaitlist(item: WaitlistEntry, transition: string) {
    setBusy(true)
    try {
      await adminService.transitionWaitlistEntry(item.id, transition)
      waitlistQuery.setData((current) => current.map((entry) => entry.id === item.id ? { ...entry, status: transition, notifiedAt: transition === 'Notified' ? new Date().toISOString() : entry.notifiedAt } : entry))
    } catch (error) { toast.error('Não foi possível atualizar a fila', getUserErrorMessage(error)) } finally { setBusy(false) }
  }

  function openSeating(target: SeatingTarget) {
    setSelectedTableIds([])
    setSeating(target)
  }

  function toggleTable(tableId: string) {
    setSelectedTableIds((current) => current.includes(tableId)
      ? current.filter((id) => id !== tableId)
      : [...current, tableId])
  }

  async function confirmSeating() {
    if (!seating || selectedCapacity < seating.item.partySize) return
    setBusy(true)
    try {
      const result = seating.kind === 'reservation'
        ? await adminService.seatReservation(seating.item.id, selectedTableIds)
        : await adminService.seatWaitlistEntry(seating.item.id, selectedTableIds)
      if (seating.kind === 'reservation') {
        reservationsQuery.setData((current) => current.map((item) => item.id === seating.item.id
          ? { ...item, status: 'Seated', tableSessionId: result.id, seatedAt: new Date().toISOString() }
          : item))
      } else {
        waitlistQuery.setData((current) => current.map((item) => item.id === seating.item.id
          ? { ...item, status: 'Seated', tableSessionId: result.id, seatedAt: new Date().toISOString() }
          : item))
      }
      await tablesQuery.refresh()
      toast.success('Cliente acomodado', 'A comanda foi aberta e as mesas selecionadas ficaram vinculadas ao atendimento.')
      setSeating(undefined)
    } catch (error) {
      toast.error('Não foi possível acomodar', getUserErrorMessage(error))
    } finally {
      setBusy(false)
    }
  }

  return (
    <>
      <PageHeader title="Reservas e lista de espera" description="Organize chegadas, previsões e ocupação do salão em um só fluxo." actions={hasPermission('operations:write') && <button className="primary-button" onClick={() => openCreate(tab === 'reservations' ? 'reservation' : 'waitlist')}><Plus size={16} /> {tab === 'reservations' ? 'Nova reserva' : 'Adicionar à fila'}</button>} />
      <section className="reservation-metrics" aria-label="Resumo do salão">
        <article><CalendarClock /><span><small>Reservas futuras</small><strong>{reservations.filter((item) => item.status === 'Pending' || item.status === 'Confirmed').length}</strong></span></article>
        <article><UsersRound /><span><small>Pessoas aguardando</small><strong>{waiting.reduce((total, item) => total + item.partySize, 0)}</strong></span></article>
        <article><Clock3 /><span><small>Espera média prevista</small><strong>{waiting.length ? Math.round(waiting.reduce((total, item) => total + item.estimatedWaitMinutes, 0) / waiting.length) : 0} min</strong></span></article>
      </section>
      <div className="section-tabs reservation-tabs" role="tablist" aria-label="Agenda do salão"><button id="reservations-tab" role="tab" aria-controls="reservations-panel" aria-selected={tab === 'reservations'} className={tab === 'reservations' ? 'active' : ''} onClick={() => setTab('reservations')}>Reservas</button><button id="waitlist-tab" role="tab" aria-controls="waitlist-panel" aria-selected={tab === 'waitlist'} className={tab === 'waitlist' ? 'active' : ''} onClick={() => setTab('waitlist')}>Lista de espera <span>{waiting.length}</span></button></div>
      {tab === 'reservations' ? <section id="reservations-panel" role="tabpanel" aria-labelledby="reservations-tab" className="surface-card reservation-list">
        {reservations.map((item) => <article className="reservation-row" key={item.id}>
          <time dateTime={item.scheduledAt}><strong>{new Date(item.scheduledAt).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })}</strong><small>{new Date(item.scheduledAt).toLocaleDateString('pt-BR', { day: '2-digit', month: 'short' })}</small></time>
          <div><h2>{item.customerName}</h2><span>{formatPhone(item.phone)} · {item.partySize} pessoas · {item.durationMinutes} min</span>{item.notes && <small>{item.notes}</small>}</div>
          <StatusBadge status={item.status} />
          {hasPermission('operations:write') && <div className="reservation-actions">{item.status === 'Pending' && <button disabled={busy} onClick={() => void transitionReservation(item, 'Confirmed')}><Check size={15} /> Confirmar</button>}{item.status === 'Confirmed' && <button disabled={busy} onClick={() => openSeating({ kind: 'reservation', item })}><UserRoundCheck size={15} /> Acomodar</button>}{item.status === 'Seated' && <button disabled={busy} onClick={() => void transitionReservation(item, 'Completed')}><Check size={15} /> Concluir</button>}{(item.status === 'Pending' || item.status === 'Confirmed') && <button className="danger-text" disabled={busy} aria-label={`Cancelar reserva de ${item.customerName}`} onClick={() => void transitionReservation(item, 'Cancelled')}><X size={15} /></button>}</div>}
        </article>)}
        {!reservations.length && <EmptyState icon={<CalendarClock />} title="Agenda livre" description="As novas reservas aparecerão aqui em ordem de horário." />}
      </section> : <section id="waitlist-panel" role="tabpanel" aria-labelledby="waitlist-tab" className="surface-card reservation-list waitlist-list">
        {waiting.map((item, index) => <article className="reservation-row" key={item.id}>
          <span className="queue-position">{index + 1}</span>
          <div><h2>{item.customerName}</h2><span>{formatPhone(item.phone)} · {item.partySize} pessoas</span>{item.notes && <small>{item.notes}</small>}</div>
          <span className="wait-estimate"><small>Previsão</small><strong>{item.estimatedWaitMinutes} min</strong></span>
          <StatusBadge status={item.status} />
          {hasPermission('operations:write') && <div className="reservation-actions">{item.status === 'Waiting' && <button disabled={busy} onClick={() => void transitionWaitlist(item, 'Notified')}><BellRing size={15} /> Avisar</button>}<button disabled={busy} onClick={() => openSeating({ kind: 'waitlist', item })}><UserRoundCheck size={15} /> Acomodar</button><button className="danger-text" disabled={busy} aria-label={`Remover ${item.customerName} da fila`} onClick={() => void transitionWaitlist(item, 'Cancelled')}><X size={15} /></button></div>}
        </article>)}
        {!waiting.length && <EmptyState icon={<UsersRound />} title="Ninguém aguardando" description="A fila está livre neste momento." />}
      </section>}
      {creating && <Modal open title={creating === 'reservation' ? 'Nova reserva' : 'Adicionar à lista de espera'} description="Busque um cliente cadastrado ou informe os dados para um novo atendimento." isBusy={busy} onClose={() => setCreating(undefined)}>
        <div className="modal-body"><div className="form-grid two-columns">
          <div className="customer-autocomplete wide" onBlur={(event) => { if (!event.currentTarget.contains(event.relatedTarget as Node)) setCustomerSearchOpen(false) }}>
            <label className="field-label">Nome do cliente<span className="input-with-icon"><Search size={16} /><input autoFocus role="combobox" aria-autocomplete="list" aria-expanded={customerSearchOpen && customerMatches.length > 0} aria-controls={customerListId} aria-activedescendant={customerSearchOpen && customerMatches.length ? `${customerListId}-${activeSuggestion}` : undefined} value={draft.customerName} maxLength={120} onFocus={() => setCustomerSearchOpen(true)} onKeyDown={handleCustomerKeyDown} onChange={(event) => updateCustomerName(event.target.value)} placeholder="Digite ao menos 2 letras para buscar" /></span></label>
            {customerSearchOpen && customerMatches.length > 0 && <div className="customer-suggestions" id={customerListId} role="listbox" aria-label="Clientes encontrados">
              {customerMatches.map((customer, index) => <button id={`${customerListId}-${index}`} type="button" role="option" aria-selected={index === activeSuggestion} className={index === activeSuggestion ? 'active' : ''} key={customer.id} onMouseEnter={() => setActiveSuggestion(index)} onClick={() => selectCustomer(customer)}><span className="customer-suggestion-icon"><UserCheck size={17} /></span><span><strong>{customer.name}</strong><small>{formatPhone(customer.phone)} · Nasc. {formatBirthDate(customer.birthDate)}</small></span></button>)}
            </div>}
          </div>
          <label className="field-label">Telefone<PhoneInput value={draft.phone} onPhoneValueChange={updatePhone} placeholder="(00) 00000-0000" /></label>
          <label className="field-label">Pessoas<input type="number" min="1" max="50" value={draft.partySize} onChange={(event) => setDraft({ ...draft, partySize: Number(event.target.value) })} /></label>
          {selectedCustomer && <aside className="customer-match-note wide"><UserCheck size={19} /><span><strong>Cliente encontrado</strong>Cadastro vinculado à reserva. Os dados de contato foram preenchidos automaticamente.</span></aside>}
          {creating === 'reservation' && !selectedCustomer && <><label className="field-label">Data de nascimento<input type="date" max={new Date().toISOString().slice(0, 10)} value={draft.birthDate} onChange={(event) => setDraft({ ...draft, birthDate: event.target.value })} required /></label><aside className="customer-new-note"><UserPlus size={18} /><span><strong>Novo cliente</strong>Será salvo ao confirmar a reserva.</span></aside></>}
          {creating === 'reservation' ? <><label className="field-label">Data e horário<input type="datetime-local" value={draft.scheduledAt} onChange={(event) => setDraft({ ...draft, scheduledAt: event.target.value })} /></label><label className="field-label">Duração prevista<input type="number" min="30" max="360" step="15" value={draft.durationMinutes} onChange={(event) => setDraft({ ...draft, durationMinutes: Number(event.target.value) })} /></label></> : <label className="field-label wide">Espera estimada em minutos<input type="number" min="0" max="360" step="5" value={draft.estimatedWaitMinutes} onChange={(event) => setDraft({ ...draft, estimatedWaitMinutes: Number(event.target.value) })} /></label>}
          <label className="field-label wide">Observações<textarea value={draft.notes} maxLength={500} onChange={(event) => setDraft({ ...draft, notes: event.target.value })} placeholder="Preferência de mesa, acessibilidade ou ocasião especial" /></label>
        </div></div>
        <div className="modal-footer"><button type="button" className="secondary-button" disabled={busy} onClick={() => setCreating(undefined)}>Cancelar</button><button type="button" className="primary-button" disabled={busy || !draft.customerName || !draft.phone || (creating === 'reservation' && (!draft.scheduledAt || (!draft.customerId && !draft.birthDate)))} onClick={() => void create()}>{busy ? 'Salvando...' : creating === 'reservation' && !draft.customerId ? 'Cadastrar e reservar' : 'Salvar'}</button></div>
      </Modal>}
      {seating && <Modal open title={`Acomodar ${seating.item.customerName}`} description="Selecione uma ou mais mesas livres. A comanda será aberta na mesma operação." isBusy={busy} onClose={() => setSeating(undefined)}>
        <div className="modal-body seating-dialog-body">
          <div className="seating-summary"><UsersRound size={20} /><span><strong>{seating.item.partySize} pessoas</strong><small>Capacidade selecionada: {selectedCapacity}</small></span></div>
          <div className="seating-table-grid" role="group" aria-label="Mesas livres">
            {freeTables.map((table) => <label className={`seating-table-option ${selectedTableIds.includes(table.id) ? 'selected' : ''}`} key={table.id}>
              <input type="checkbox" checked={selectedTableIds.includes(table.id)} onChange={() => toggleTable(table.id)} />
              <TableProperties aria-hidden="true" /><span><strong>{table.name}</strong><small>{table.area} · {table.capacity} lugares</small></span>
            </label>)}
            {!freeTables.length && <div className="reservation-empty compact"><TableProperties /><h2>Nenhuma mesa livre</h2><p>Finalize ou transfira um atendimento antes de acomodar este cliente.</p></div>}
          </div>
          {selectedTableIds.length > 0 && selectedCapacity < seating.item.partySize && <p className="field-error" role="alert">Selecione capacidade para pelo menos {seating.item.partySize} pessoas.</p>}
        </div>
        <div className="modal-footer"><button type="button" className="secondary-button" disabled={busy} onClick={() => setSeating(undefined)}>Cancelar</button><button type="button" className="primary-button" disabled={busy || selectedCapacity < seating.item.partySize} onClick={() => void confirmSeating()}>{busy ? 'Abrindo comanda...' : 'Acomodar e abrir comanda'}</button></div>
      </Modal>}
    </>
  )
}

function EmptyState({ icon, title, description }: { icon: ReactNode; title: string; description: string }) {
  return <div className="reservation-empty"><span>{icon}</span><h2>{title}</h2><p>{description}</p></div>
}

function toLocalDateTime(date: Date) {
  const offset = date.getTimezoneOffset() * 60_000
  return new Date(date.getTime() - offset).toISOString().slice(0, 16)
}

function normalizeSearch(value: string) {
  return value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').trim().toLowerCase()
}

function formatBirthDate(value: string) {
  const [year, month, day] = value.slice(0, 10).split('-')
  return year && month && day ? `${day}/${month}/${year}` : value
}
