import { zodResolver } from '@hookform/resolvers/zod'
import { Edit3, Plus, Save } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { FieldError } from '../components/ui/FieldError'
import { Modal } from '../components/ui/Modal'
import { PageHeader } from '../components/ui/PageHeader'
import { useToast } from '../components/ui/toast'
import { ViewTransitionLink } from '../components/ui/ViewTransitionLink'
import { crustSchema, type CrustFormData } from '../features/admin/formSchemas'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { queryKeys } from '../lib/queryKeys'
import { adminService } from '../services/adminService'
import { hasPermission } from '../services/authSession'
import type { PizzaCrust } from '../types/admin'
import { getUserErrorMessage } from '../utils/errors'

export function CrustsPage() {
  const { data: crusts, setData: setCrusts } = useAdminQuery(queryKeys.crusts, adminService.crusts)
  const [editingId, setEditingId] = useState<string>()
  const [saving, setSaving] = useState(false)
  const toast = useToast()
  const form = useForm<CrustFormData>({
    resolver: zodResolver(crustSchema),
    defaultValues: { name: '', description: '', isActive: true, isAvailable: true },
  })

  function edit(crust?: PizzaCrust) {
    form.reset(crust ?? { name: '', description: '', isActive: true, isAvailable: true })
    setEditingId(crust?.id ?? 'new')
  }

  async function save(draft: CrustFormData) {
    setSaving(true)
    try {
      const result = await adminService.saveCrust(draft)
      const saved = { ...draft, id: draft.id ?? (result as { id: string }).id } as PizzaCrust
      setCrusts((current) => draft.id ? current.map((item) => item.id === draft.id ? saved : item) : [...current, saved])
      setEditingId(undefined)
      toast.success(draft.id ? 'Borda atualizada' : 'Borda adicionada', `${draft.name} foi salva com sucesso.`)
    } catch (error) {
      toast.error('Não foi possível salvar a borda', getUserErrorMessage(error))
    } finally {
      setSaving(false)
    }
  }

  return (
    <>
      <PageHeader title="Bordas" description="Gerencie recheios e disponibilidade das bordas." actions={hasPermission('admin:write') && <button className="primary-button" onClick={() => edit()}><Plus size={16} /> Nova borda</button>} />
      <nav className="catalog-tabs" aria-label="Seções do cardápio" role="tablist"><ViewTransitionLink role="tab" aria-selected={false} href="/admin/catalog/products">Produtos</ViewTransitionLink><ViewTransitionLink role="tab" aria-selected={false} href="/admin/catalog/categories">Categorias</ViewTransitionLink><ViewTransitionLink role="tab" aria-selected={false} href="/admin/catalog/pizza-flavors">Sabores</ViewTransitionLink><ViewTransitionLink role="tab" aria-selected className="active" href="/admin/catalog/crusts">Bordas</ViewTransitionLink></nav>
      {editingId && <Modal open title={form.getValues('id') ? 'Editar borda' : 'Nova borda'} description="Os preços por tamanho permanecem no catálogo de pizza." isBusy={saving} onClose={() => setEditingId(undefined)}>
        <form onSubmit={form.handleSubmit(save)} noValidate>
        <div className="modal-body"><div className="form-grid">
          <label className="field-label">Nome<input autoFocus aria-invalid={Boolean(form.formState.errors.name)} {...form.register('name')} /><FieldError message={form.formState.errors.name?.message} /></label>
          <label className="field-label">Descrição<input {...form.register('description')} /><FieldError message={form.formState.errors.description?.message} /></label>
          <label className="check-label"><input type="checkbox" {...form.register('isActive')} /> Ativa no catálogo</label>
          <label className="check-label"><input type="checkbox" {...form.register('isAvailable')} /> Disponível para venda</label>
        </div></div>
        <div className="modal-footer"><button type="button" className="secondary-button" disabled={saving} onClick={() => setEditingId(undefined)}>Cancelar</button><button className="primary-button" disabled={saving} aria-busy={saving}><Save size={16} /> {saving ? 'Salvando...' : 'Salvar borda'}</button></div>
        </form>
      </Modal>}
      <section className="management-grid">
        {crusts.map((crust) => <article className="surface-card management-card" key={crust.id}>
          <div><span className={`status-pill ${crust.isAvailable ? 'success' : 'danger'}`}>{crust.isAvailable ? 'Disponível' : 'Indisponível'}</span><h2>{crust.name}</h2><p>{crust.description}</p></div>
          {hasPermission('admin:write') && <button className="secondary-button" onClick={() => edit(crust)}><Edit3 size={16} /> Editar</button>}
        </article>)}
      </section>
    </>
  )
}
