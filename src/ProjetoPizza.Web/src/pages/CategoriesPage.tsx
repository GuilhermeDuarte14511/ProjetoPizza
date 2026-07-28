import { zodResolver } from '@hookform/resolvers/zod'
import { GripVertical, MoreHorizontal, Plus, Save, TabletSmartphone } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { FieldError } from '../components/ui/FieldError'
import { Modal } from '../components/ui/Modal'
import { PageHeader } from '../components/ui/PageHeader'
import { StatusBadge } from '../components/ui/StatusBadge'
import { useToast } from '../components/ui/toast'
import { ViewTransitionLink } from '../components/ui/ViewTransitionLink'
import { categorySchema, type CategoryFormData } from '../features/admin/formSchemas'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { queryKeys } from '../lib/queryKeys'
import { adminService } from '../services/adminService'
import { hasPermission } from '../services/authSession'
import type { Category } from '../types/admin'
import { getUserErrorMessage } from '../utils/errors'

export function CategoriesPage() {
  const { data: categories, setData: setCategories } = useAdminQuery(queryKeys.categories, adminService.categories)
  const [editingId, setEditingId] = useState<string>()
  const [saving, setSaving] = useState(false)
  const toast = useToast()
  const form = useForm<CategoryFormData>({
    resolver: zodResolver(categorySchema),
    defaultValues: { name: '', slug: '', description: '', isActive: true, isVisibleOnTablet: true },
  })

  function edit(category?: Category) {
    form.reset(category ?? { name: '', slug: '', description: '', isActive: true, isVisibleOnTablet: true })
    setEditingId(category?.id ?? 'new')
  }

  async function save(draft: CategoryFormData) {
    setSaving(true)
    try {
      const result = await adminService.saveCategory(draft) as { id: string }
      const saved = { ...draft, id: draft.id ?? result.id } as Category
      setCategories((current) => draft.id ? current.map((item) => item.id === draft.id ? saved : item) : [...current, saved])
      setEditingId(undefined)
      toast.success(draft.id ? 'Categoria atualizada' : 'Categoria adicionada', `${draft.name} foi salva com sucesso.`)
    } catch (error) {
      toast.error('Não foi possível salvar a categoria', getUserErrorMessage(error))
    } finally {
      setSaving(false)
    }
  }

  return (
    <>
      <PageHeader title="Categorias" description="Organize a navegação e a exibição no tablet." actions={hasPermission('admin:write') && <button className="primary-button" onClick={() => edit()}><Plus size={16} /> Nova categoria</button>} />
      <nav className="catalog-tabs" aria-label="Seções do cardápio" role="tablist"><ViewTransitionLink role="tab" aria-selected={false} href="/admin/catalog/products">Produtos</ViewTransitionLink><ViewTransitionLink role="tab" aria-selected className="active" href="/admin/catalog/categories">Categorias</ViewTransitionLink><ViewTransitionLink role="tab" aria-selected={false} href="/admin/catalog/pizza-flavors">Sabores</ViewTransitionLink><ViewTransitionLink role="tab" aria-selected={false} href="/admin/catalog/crusts">Bordas</ViewTransitionLink></nav>
      {editingId && <Modal open title={form.getValues('id') ? 'Editar categoria' : 'Nova categoria'} description="Organize a categoria e defina sua visibilidade no cardápio." isBusy={saving} onClose={() => setEditingId(undefined)}>
        <form onSubmit={form.handleSubmit(save)} noValidate>
          <div className="modal-body"><div className="form-grid two-columns">
            <label className="field-label">Nome<input autoFocus aria-invalid={Boolean(form.formState.errors.name)} {...form.register('name')} /><FieldError message={form.formState.errors.name?.message} /></label>
            <label className="field-label">Slug<input aria-invalid={Boolean(form.formState.errors.slug)} {...form.register('slug')} /><FieldError message={form.formState.errors.slug?.message} /></label>
            <label className="field-label wide">Descrição<input {...form.register('description')} /><FieldError message={form.formState.errors.description?.message} /></label>
            <label className="check-label"><input type="checkbox" {...form.register('isVisibleOnTablet')} /> Visível no tablet</label>
            <label className="check-label"><input type="checkbox" {...form.register('isActive')} /> Categoria ativa</label>
          </div></div>
          <div className="modal-footer"><button type="button" className="secondary-button" disabled={saving} onClick={() => setEditingId(undefined)}>Cancelar</button><button className="primary-button" disabled={saving} aria-busy={saving}><Save size={16} /> {saving ? 'Salvando...' : 'Salvar categoria'}</button></div>
        </form>
      </Modal>}
      <article className="surface-card category-list">
        <div className="category-list-header"><span>Ordem e categoria</span><span>Tablet</span><span>Status</span><span /></div>
        {categories.map((category) => (
          <div className="category-row" key={category.id}>
            <div><GripVertical size={18} /><span className="category-icon">{category.name.slice(0, 1)}</span><span><strong>{category.name}</strong><small>/{category.slug}</small></span></div>
            <span className="tablet-visibility"><TabletSmartphone size={16} /> {category.isVisibleOnTablet ? 'Visível' : 'Oculta'}</span>
            <StatusBadge status={category.isActive ? 'Ativa' : 'Inativa'} />
            {hasPermission('admin:write') && <button className="icon-button" aria-label={`Editar ${category.name}`} onClick={() => edit(category)}><MoreHorizontal size={18} /></button>}
          </div>
        ))}
      </article>
    </>
  )
}
