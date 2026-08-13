import { zodResolver } from '@hookform/resolvers/zod'
import { Cake, Pencil, Phone, Plus, Search, Star, UserRound, UsersRound } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { FieldError } from '../components/ui/FieldError'
import { Modal } from '../components/ui/Modal'
import { PageHeader } from '../components/ui/PageHeader'
import { PhoneInput } from '../components/ui/PhoneInput'
import { StatusBadge } from '../components/ui/StatusBadge'
import { useToast } from '../components/ui/toast'
import { customerSchema, type CustomerFormData } from '../features/admin/formSchemas'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { queryKeys } from '../lib/queryKeys'
import { adminService } from '../services/adminService'
import { hasPermission } from '../services/authSession'
import type { Customer } from '../types/admin'
import { getUserErrorMessage } from '../utils/errors'
import { formatPhone } from '../utils/phone'

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
        description="Cadastre contatos e acompanhe a fidelidade gerada automaticamente pelos pedidos."
        actions={hasPermission('admin:write') && <button className="primary-button" onClick={() => open()}><Plus size={16} /> Novo cliente</button>}
      />
      <section className="customer-workspace" aria-label="Lista de clientes">
        <header className="customer-toolbar">
          <div className="customer-count">
            <span><UsersRound size={18} /></span>
            <div><strong>{visible.length} {visible.length === 1 ? 'cliente' : 'clientes'}</strong><small>{search ? `de ${customers.length} cadastrados` : 'cadastrados na unidade'}</small></div>
          </div>
          <div className="toolbar-search customer-search"><Search size={17} /><input aria-label="Buscar cliente" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar por nome ou celular..." /></div>
        </header>
        {visible.length > 0 && <div className="customer-list-heading" aria-hidden="true"><span>Cliente</span><span>Celular</span><span>Nascimento</span><span>Fidelidade</span><span>Status</span><span /></div>}
        <div className="customer-list">
          {visible.map((customer) => (
            <article className="customer-row" key={customer.id}>
              <div className="customer-identity">
                <span className="customer-avatar"><UserRound aria-hidden="true" /></span>
                <div><h2>{customer.name}</h2><small>Cliente desde {formatCustomerSince(customer.createdAt)}</small></div>
              </div>
              <span className="customer-detail"><Phone size={15} /><span><small>Celular</small><strong>{formatPhone(customer.phone)}</strong></span></span>
              <span className="customer-detail"><Cake size={15} /><span><small>Nascimento</small><strong>{formatBirthDate(customer.birthDate)}</strong></span></span>
              <span className="customer-detail customer-loyalty"><Star size={15} /><span><small>Fidelidade</small><strong>{customer.loyaltyPoints} pontos</strong><small>{customer.orderCount} pedidos · {new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(customer.lifetimeSpend)}</small></span></span>
              <StatusBadge status={customer.isActive ? 'Ativo' : 'Inativo'} />
              {hasPermission('admin:write') && <button className="secondary-button customer-edit" aria-label={`Editar ${customer.name}`} onClick={() => open(customer)}><Pencil size={15} /> Editar</button>}
            </article>
          ))}
          {!visible.length && <div className="customer-empty"><span><UserRound size={28} /></span><h2>Nenhum cliente encontrado</h2><p>{search ? 'Tente outro nome ou celular.' : 'Cadastre o primeiro cliente para começar.'}</p>{hasPermission('admin:write') && !search && <button className="secondary-button" onClick={() => open()}><Plus size={15} /> Cadastrar cliente</button>}</div>}
        </div>
      </section>

      {editing && <Modal open title={form.getValues('id') ? 'Editar cliente' : 'Novo cliente'} description="O telefone identifica o cadastro durante o atendimento por ligação." isBusy={saving} onClose={() => setEditing(false)}>
        <form onSubmit={form.handleSubmit(save)} noValidate>
          <div className="modal-body">
            <div className="form-grid two-columns">
              <label className="field-label wide">Nome completo<input autoFocus aria-invalid={Boolean(form.formState.errors.name)} {...form.register('name')} /><FieldError message={form.formState.errors.name?.message} /></label>
              <label className="field-label">Celular<Controller control={form.control} name="phone" render={({ field }) => <PhoneInput getInputRef={field.ref} name={field.name} value={field.value} onBlur={field.onBlur} onPhoneValueChange={field.onChange} placeholder="(00) 00000-0000" aria-invalid={Boolean(form.formState.errors.phone)} />} /><FieldError message={form.formState.errors.phone?.message} /></label>
              <label className="field-label">Data de nascimento<input type="date" aria-invalid={Boolean(form.formState.errors.birthDate)} {...form.register('birthDate')} /><FieldError message={form.formState.errors.birthDate?.message} /></label>
              <label className="switch-field wide"><input type="checkbox" {...form.register('isActive')} /><span /><strong>Cliente ativo</strong></label>
            </div>
            <aside className="form-note"><Star size={19} /><span><strong>Fidelidade automática</strong>Cada R$ 1 em pedidos válidos gera 1 ponto. Cancelamentos estornam os pontos e o valor acumulado.</span></aside>
          </div>
          <div className="modal-footer"><button type="button" className="secondary-button" disabled={saving} onClick={() => setEditing(false)}>Cancelar</button><button className="primary-button" disabled={saving}>{saving ? 'Salvando...' : 'Salvar cliente'}</button></div>
        </form>
      </Modal>}
    </>
  )
}

function formatBirthDate(value: string) {
  const date = new Date(`${value}T00:00:00`)
  return Number.isNaN(date.getTime()) ? value : date.toLocaleDateString('pt-BR')
}

function formatCustomerSince(value: string) {
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? 'data não informada' : date.toLocaleDateString('pt-BR', { month: 'short', year: 'numeric' })
}
