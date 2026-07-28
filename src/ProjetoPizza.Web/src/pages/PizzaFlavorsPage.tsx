import { zodResolver } from '@hookform/resolvers/zod'
import { Edit3, Plus, Save, Search } from 'lucide-react'
import { useMemo, useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { FieldError } from '../components/ui/FieldError'
import { Modal } from '../components/ui/Modal'
import { PageHeader } from '../components/ui/PageHeader'
import { StatusBadge } from '../components/ui/StatusBadge'
import { useToast } from '../components/ui/toast'
import { ViewTransitionLink } from '../components/ui/ViewTransitionLink'
import { pizzaFlavorSchema, type PizzaFlavorFormData } from '../features/admin/formSchemas'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { queryKeys } from '../lib/queryKeys'
import { adminService } from '../services/adminService'
import { hasPermission } from '../services/authSession'
import type { PizzaFlavor } from '../types/admin'
import { getUserErrorMessage } from '../utils/errors'

const emptyFlavor: PizzaFlavorFormData = {
  categoryId: '',
  name: '',
  type: 'Savory',
  isPremium: false,
  isVegetarian: false,
  isActive: true,
  isAvailable: true,
}

export function PizzaFlavorsPage() {
  const { data: flavors, setData: setFlavors } = useAdminQuery(queryKeys.pizzaFlavors, adminService.pizzaFlavors)
  const { data: categories } = useAdminQuery(queryKeys.categories, adminService.categories)
  const [editingId, setEditingId] = useState<string>()
  const [search, setSearch] = useState('')
  const [saving, setSaving] = useState(false)
  const toast = useToast()
  const form = useForm<PizzaFlavorFormData>({
    resolver: zodResolver(pizzaFlavorSchema),
    defaultValues: emptyFlavor,
  })
  const isAvailable = useWatch({ control: form.control, name: 'isAvailable' })

  const visible = useMemo(
    () => flavors.filter((flavor) => `${flavor.name} ${flavor.description ?? ''}`.toLowerCase().includes(search.toLowerCase())),
    [flavors, search],
  )
  const categoryName = (id: string) => categories.find((category) => category.id === id)?.name ?? 'Sem categoria'

  function edit(flavor?: PizzaFlavor) {
    form.reset(flavor ? { ...flavor, type: flavor.type === 'Sweet' ? 'Sweet' : 'Savory' } : emptyFlavor)
    setEditingId(flavor?.id ?? 'new')
  }

  async function save(draft: PizzaFlavorFormData) {
    setSaving(true)
    try {
      const result = await adminService.savePizzaFlavor(draft) as { id: string }
      const savedFlavor = { ...draft, id: draft.id ?? result.id } as PizzaFlavor
      setFlavors((current) => draft.id
        ? current.map((item) => item.id === draft.id ? savedFlavor : item)
        : [...current, savedFlavor])
      setEditingId(undefined)
      toast.success(draft.id ? 'Sabor atualizado' : 'Sabor adicionado', `${draft.name} foi salvo com sucesso.`)
    } catch (error) {
      toast.error('Não foi possível salvar o sabor', getUserErrorMessage(error))
    } finally {
      setSaving(false)
    }
  }

  return (
    <>
      <PageHeader title="Sabores de pizza" description="Gerencie tipos, disponibilidade e classificação dos sabores." actions={hasPermission('admin:write') && <button className="primary-button" onClick={() => edit()}><Plus size={16} /> Novo sabor</button>} />
      <nav className="catalog-tabs" aria-label="Seções do cardápio" role="tablist"><ViewTransitionLink role="tab" aria-selected={false} href="/admin/catalog/products">Produtos</ViewTransitionLink><ViewTransitionLink role="tab" aria-selected={false} href="/admin/catalog/categories">Categorias</ViewTransitionLink><ViewTransitionLink role="tab" aria-selected className="active" href="/admin/catalog/pizza-flavors">Sabores</ViewTransitionLink><ViewTransitionLink role="tab" aria-selected={false} href="/admin/catalog/crusts">Bordas</ViewTransitionLink></nav>
      {editingId && <Modal open title={form.getValues('id') ? 'Editar sabor' : 'Novo sabor'} description="As regras de composição permanecem isoladas das informações do catálogo." size="large" isBusy={saving} onClose={() => setEditingId(undefined)}>
        <form onSubmit={form.handleSubmit(save)} noValidate>
          <div className="modal-body"><div className="form-grid three-columns">
            <label className="field-label">Nome<input autoFocus aria-invalid={Boolean(form.formState.errors.name)} {...form.register('name')} /><FieldError message={form.formState.errors.name?.message} /></label>
            <label className="field-label">Categoria<select aria-invalid={Boolean(form.formState.errors.categoryId)} {...form.register('categoryId')}><option value="">Selecione</option>{categories.map((category) => <option value={category.id} key={category.id}>{category.name}</option>)}</select><FieldError message={form.formState.errors.categoryId?.message} /></label>
            <label className="field-label">Tipo<select {...form.register('type')}><option value="Savory">Salgado</option><option value="Sweet">Doce</option></select></label>
            <label className="field-label wide">Descrição<input {...form.register('description')} /><FieldError message={form.formState.errors.description?.message} /></label>
            <div className="check-stack"><label className="check-label"><input type="checkbox" {...form.register('isPremium')} /> Premium</label><label className="check-label"><input type="checkbox" {...form.register('isVegetarian')} /> Vegetariano</label></div>
            <div className="check-stack"><label className="check-label"><input type="checkbox" {...form.register('isActive')} /> Ativo</label><label className="check-label"><input type="checkbox" {...form.register('isAvailable')} /> Disponível</label></div>
            {!isAvailable && <label className="field-label wide">Motivo da indisponibilidade<input {...form.register('soldOutReason')} /><FieldError message={form.formState.errors.soldOutReason?.message} /></label>}
          </div></div>
          <div className="modal-footer"><button type="button" className="secondary-button" disabled={saving} onClick={() => setEditingId(undefined)}>Cancelar</button><button className="primary-button" disabled={saving} aria-busy={saving}><Save size={16} /> {saving ? 'Salvando...' : 'Salvar sabor'}</button></div>
        </form>
      </Modal>}
      <article className="surface-card table-card-container">
        <div className="toolbar inner"><div className="toolbar-search grow"><Search size={17} /><input aria-label="Buscar sabor" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar sabor..." /></div></div>
        <div className="responsive-table">
          <table>
            <thead><tr><th>Sabor</th><th>Categoria</th><th>Tipo</th><th>Características</th><th>Status</th><th aria-label="Ações" /></tr></thead>
            <tbody>{visible.map((flavor) => <tr key={flavor.id}><td><strong>{flavor.name}</strong><small className="table-description">{flavor.description}</small></td><td>{categoryName(flavor.categoryId)}</td><td>{flavor.type === 'Sweet' ? 'Doce' : 'Salgado'}</td><td>{[flavor.isPremium && 'Premium', flavor.isVegetarian && 'Vegetariano'].filter(Boolean).join(' · ') || 'Tradicional'}</td><td><StatusBadge status={flavor.isAvailable ? 'Disponível' : 'Fora de estoque'} /></td><td>{hasPermission('admin:write') && <button className="icon-button" aria-label={`Editar ${flavor.name}`} onClick={() => edit(flavor)}><Edit3 size={17} /></button>}</td></tr>)}</tbody>
          </table>
        </div>
      </article>
    </>
  )
}
