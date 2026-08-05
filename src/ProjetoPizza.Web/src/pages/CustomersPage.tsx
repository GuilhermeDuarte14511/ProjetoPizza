import { zodResolver } from '@hookform/resolvers/zod'
import { Cake, Pencil, Phone, Plus, Search, UserRound } from 'lucide-react'
import { useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { FieldError } from '../components/ui/FieldError'
import { Modal } from '../components/ui/Modal'
import { PageHeader } from '../components/ui/PageHeader'
import { StatusBadge } from '../components/ui/StatusBadge'
import { useToast } from '../components/ui/toast'
import { customerSchema, type CustomerFormData } from '../features/admin/formSchemas'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { queryKeys } from '../lib/queryKeys'
import { adminService } from '../services/adminService'
import { hasPermission } from '../services/authSession'
import type { Customer } from '../types/admin'
import { getUserErrorMessage } from '../utils/errors'

const emptyCustomer: CustomerFormData = {
  name: '',
  phone: '',
  birthDate: '',
  isActive: true,
}

export function CustomersPage() {
  const { data: customers, setData: setCustomers } = useAdminQuery(queryKeys.customers, adminService.customers)
  const [search, setSearch] = useState('')
  const [editing, setEditing] = useState(false)
  const [saving, setSaving] = useState(false)
  const toast = useToast()
  const form = useForm<CustomerFormData>({
    resolver: zodResolver(customerSchema),
    defaultValues: emptyCustomer,
  })
  const normalizedSearch = search.replace(/\D/g, '')
  const visible = useMemo(() => customers.filter((customer) =>
    customer.name.toLocaleLowerCase('pt-BR').includes(search.toLocaleLowerCase('pt-BR')) ||
    (normalizedSearch && customer.phone.includes(normalizedSearch))), [customers, normalizedSearch, search])

  function open(customer?: Customer) {
    form.reset(customer ? {
      id: customer.id,
      name: customer.name,
      phone: customer.phone,
      birthDate: customer.birthDate,
      isActive: customer.isActive,
    } : emptyCustomer)
    setEditing(true)
  }

  async function save(draft: CustomerFormData) {
    setSaving(true)
    try {
      const saved = await adminService.saveCustomer(draft)
      setCustomers((current) => draft.id
        ? current.map((customer) => customer.id === draft.id ? saved : customer)
        : [...current, saved].sort((a, b) => a.name.localeCompare(b.name, 'pt-BR')))
      setEditing(false)
      toast.success(draft.id ? 'Cliente atualizado' : 'Cliente cadastrado', `${draft.name} está disponível para novos pedidos.`)
    } catch (error) {
      toast.error('Não foi possível salvar o cliente', getUserErrorMessage(error))
    } finally {
      setSaving(false)
    }
  }

  return (
    <>
      <PageHeader
        title="Clientes"
        description="Cadastre contatos para pedidos de retirada, entrega e futuras ações de fidelidade."
        actions={hasPermission('admin:write') && <button className="primary-button" onClick={() => open()}><Plus size={16} /> Novo cliente</button>}
      />
      <div className="toolbar customer-toolbar">
        <div className="toolbar-search"><Search size={17} /><input aria-label="Buscar cliente" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar por nome ou telefone..." /></div>
      </div>
      <section className="management-grid customer-grid">
        {visible.map((customer) => (
          <article className="management-card customer-card" key={customer.id}>
            <header>
              <span className="customer-avatar"><UserRound aria-hidden="true" /></span>
              <div><h2>{customer.name}</h2><StatusBadge status={customer.isActive ? 'Ativo' : 'Inativo'} /></div>
            </header>
            <dl>
              <div><dt><Phone size={14} /> Telefone</dt><dd>{formatPhone(customer.phone)}</dd></div>
              <div><dt><Cake size={14} /> Nascimento</dt><dd>{formatBirthDate(customer.birthDate)}</dd></div>
            </dl>
            {hasPermission('admin:write') && <button className="secondary-button" onClick={() => open(customer)}><Pencil size={15} /> Editar</button>}
          </article>
        ))}
        {!visible.length && <div className="empty-state"><UserRound size={34} /><h2>Nenhum cliente encontrado</h2><p>Revise a busca ou cadastre um novo cliente.</p></div>}
      </section>

      {editing && <Modal open title={form.getValues('id') ? 'Editar cliente' : 'Novo cliente'} description="O telefone identifica o cadastro durante o atendimento por ligação." isBusy={saving} onClose={() => setEditing(false)}>
        <form onSubmit={form.handleSubmit(save)} noValidate>
          <div className="modal-body">
            <div className="form-grid two-columns">
              <label className="field-label wide">Nome completo<input autoFocus aria-invalid={Boolean(form.formState.errors.name)} {...form.register('name')} /><FieldError message={form.formState.errors.name?.message} /></label>
              <label className="field-label">Telefone<input inputMode="tel" aria-invalid={Boolean(form.formState.errors.phone)} {...form.register('phone')} /><FieldError message={form.formState.errors.phone?.message} /></label>
              <label className="field-label">Data de nascimento<input type="date" aria-invalid={Boolean(form.formState.errors.birthDate)} {...form.register('birthDate')} /><FieldError message={form.formState.errors.birthDate?.message} /></label>
              <label className="switch-field wide"><input type="checkbox" {...form.register('isActive')} /><span /><strong>Cliente ativo</strong></label>
            </div>
            <aside className="form-note"><Cake size={19} /><span><strong>Base para fidelidade</strong>A data de nascimento ficará disponível para futuras campanhas, sem gerar descontos ou cashback automaticamente.</span></aside>
          </div>
          <div className="modal-footer"><button type="button" className="secondary-button" disabled={saving} onClick={() => setEditing(false)}>Cancelar</button><button className="primary-button" disabled={saving}>{saving ? 'Salvando...' : 'Salvar cliente'}</button></div>
        </form>
      </Modal>}
    </>
  )
}

function formatPhone(phone: string) {
  if (phone.length === 11) return `(${phone.slice(0, 2)}) ${phone.slice(2, 7)}-${phone.slice(7)}`
  if (phone.length === 10) return `(${phone.slice(0, 2)}) ${phone.slice(2, 6)}-${phone.slice(6)}`
  return phone
}

function formatBirthDate(value: string) {
  const date = new Date(`${value}T00:00:00`)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleDateString('pt-BR')
}
