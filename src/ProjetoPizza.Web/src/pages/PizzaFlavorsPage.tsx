import { zodResolver } from '@hookform/resolvers/zod'
import { Edit3, Plus, Save, Search } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Controller, useFieldArray, useForm, useWatch } from 'react-hook-form'
import { CurrencyInput } from '../components/ui/CurrencyInput'
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
  extras: [],
}

export function PizzaFlavorsPage() {
  const { data: flavors, setData: setFlavors } = useAdminQuery(queryKeys.pizzaFlavors, adminService.pizzaFlavors)
  const { data: categories } = useAdminQuery(queryKeys.categories, adminService.categories)
  const { data: ingredients } = useAdminQuery(queryKeys.ingredients, adminService.ingredients)
  const [editingId, setEditingId] = useState<string>()
  const [search, setSearch] = useState('')
  const [saving, setSaving] = useState(false)
  const toast = useToast()
  const form = useForm<PizzaFlavorFormData>({
    resolver: zodResolver(pizzaFlavorSchema),
    defaultValues: emptyFlavor,
  })
  const isAvailable = useWatch({ control: form.control, name: 'isAvailable' })
  const extraFields = useFieldArray({ control: form.control, name: 'extras' })
  const eligibleExtras = ingredients.filter((ingredient) => ingredient.isActive && ingredient.isAvailableAsExtra)

  const visible = useMemo(
    () => flavors.filter((flavor) => `${flavor.name} ${flavor.description ?? ''}`.toLowerCase().includes(search.toLowerCase())),
    [flavors, search],
  )
  const categoryName = (id: string) => categories.find((category) => category.id === id)?.name ?? 'Sem categoria'

  function edit(flavor?: PizzaFlavor) {
    form.reset(flavor ? {
      ...flavor,
      type: flavor.type === 'Sweet' ? 'Sweet' : 'Savory',
      extras: flavor.extras.map((extra) => ({
        ingredientId: extra.ingredientId,
        price: extra.price,
        maxQuantity: extra.maxQuantity,
      })),
    } : emptyFlavor)
    setEditingId(flavor?.id ?? 'new')
  }

  function toggleExtra(ingredientId: string) {
    const index = extraFields.fields.findIndex((extra) => extra.ingredientId === ingredientId)
    if (index >= 0) {
      extraFields.remove(index)
      return
    }

    const ingredient = eligibleExtras.find((candidate) => candidate.id === ingredientId)
    if (!ingredient) return
    extraFields.append({
      ingredientId,
      price: ingredient.extraPrice,
      maxQuantity: ingredient.maxExtraQuantity,
    })
  }

  async function save(draft: PizzaFlavorFormData) {
    setSaving(true)
    try {
      const result = await adminService.savePizzaFlavor(draft) as { id: string }
      const savedFlavor = {
        ...draft,
        id: draft.id ?? result.id,
        extras: draft.extras.map((extra) => ({
          ...extra,
          ingredientName: ingredients.find((ingredient) => ingredient.id === extra.ingredientId)?.name ?? 'Ingrediente',
        })),
      } as PizzaFlavor
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
      <nav className="catalog-tabs" aria-label="Seções do cardápio" role="tablist"><ViewTransitionLink role="tab" aria-selected={false} href="/admin/catalog/products">Produtos</ViewTransitionLink><ViewTransitionLink role="tab" aria-selected={false} href="/admin/catalog/categories">Categorias</ViewTransitionLink><ViewTransitionLink role="tab" aria-selected className="active" href="/admin/catalog/pizza-flavors">Sabores</ViewTransitionLink><ViewTransitionLink role="tab" aria-selected={false} href="/admin/catalog/crusts">Bordas</ViewTransitionLink><ViewTransitionLink role="tab" aria-selected={false} href="/admin/catalog/ingredients">Ingredientes</ViewTransitionLink></nav>
      {editingId && <Modal open title={form.getValues('id') ? 'Editar sabor' : 'Novo sabor'} description="Defina os dados do sabor e os ingredientes que podem ser adicionados em cada parte da pizza." size="large" isBusy={saving} onClose={() => setEditingId(undefined)}>
        <form onSubmit={form.handleSubmit(save)} noValidate>
          <div className="modal-body"><div className="form-grid three-columns">
            <label className="field-label">Nome<input autoFocus aria-invalid={Boolean(form.formState.errors.name)} {...form.register('name')} /><FieldError message={form.formState.errors.name?.message} /></label>
            <label className="field-label">Categoria<select aria-invalid={Boolean(form.formState.errors.categoryId)} {...form.register('categoryId')}><option value="">Selecione</option>{categories.map((category) => <option value={category.id} key={category.id}>{category.name}</option>)}</select><FieldError message={form.formState.errors.categoryId?.message} /></label>
            <label className="field-label">Tipo<select {...form.register('type')}><option value="Savory">Salgado</option><option value="Sweet">Doce</option></select></label>
            <label className="field-label wide">Descrição<input {...form.register('description')} /><FieldError message={form.formState.errors.description?.message} /></label>
            <div className="check-stack"><label className="check-label"><input type="checkbox" {...form.register('isPremium')} /> Premium</label><label className="check-label"><input type="checkbox" {...form.register('isVegetarian')} /> Vegetariano</label></div>
            <div className="check-stack"><label className="check-label"><input type="checkbox" {...form.register('isActive')} /> Ativo</label><label className="check-label"><input type="checkbox" {...form.register('isAvailable')} /> Disponível</label></div>
            {!isAvailable && <label className="field-label wide">Motivo da indisponibilidade<input {...form.register('soldOutReason')} /><FieldError message={form.formState.errors.soldOutReason?.message} /></label>}
            <section className="flavor-extras-editor wide" aria-labelledby="flavor-extras-title">
              <header>
                <div>
                  <h3 id="flavor-extras-title">Adicionais permitidos</h3>
                  <p>O valor é específico para este sabor e será aplicado em cada parte selecionada.</p>
                </div>
                <strong>{extraFields.fields.length} selecionado(s)</strong>
              </header>
              {eligibleExtras.length === 0 ? (
                <p className="empty-editor-message">Cadastre primeiro um ingrediente disponível como adicional.</p>
              ) : (
                <div className="flavor-extras-list">
                  {eligibleExtras.map((ingredient) => {
                    const fieldIndex = extraFields.fields.findIndex((extra) => extra.ingredientId === ingredient.id)
                    const selected = fieldIndex >= 0
                    return (
                      <article className={selected ? 'flavor-extra-option selected' : 'flavor-extra-option'} key={ingredient.id}>
                        <label className="flavor-extra-toggle">
                          <input
                            type="checkbox"
                            checked={selected}
                            onChange={() => toggleExtra(ingredient.id)}
                          />
                          <span>
                            <strong>{ingredient.name}</strong>
                            <small>{ingredient.description || 'Ingrediente adicional'}</small>
                          </span>
                        </label>
                        {selected && (
                          <div className="flavor-extra-fields">
                            <label className="field-label">Valor neste sabor
                              <Controller
                                control={form.control}
                                name={`extras.${fieldIndex}.price`}
                                render={({ field }) => (
                                  <CurrencyInput
                                    name={field.name}
                                    value={field.value}
                                    onBlur={field.onBlur}
                                    getInputRef={field.ref}
                                    aria-invalid={Boolean(form.formState.errors.extras?.[fieldIndex]?.price)}
                                    onCurrencyValueChange={field.onChange}
                                  />
                                )}
                              />
                              <FieldError message={form.formState.errors.extras?.[fieldIndex]?.price?.message} />
                            </label>
                            <label className="field-label">Máximo por parte
                              <input
                                type="number"
                                min={1}
                                max={10}
                                aria-invalid={Boolean(form.formState.errors.extras?.[fieldIndex]?.maxQuantity)}
                                {...form.register(`extras.${fieldIndex}.maxQuantity`, { valueAsNumber: true })}
                              />
                              <FieldError message={form.formState.errors.extras?.[fieldIndex]?.maxQuantity?.message} />
                            </label>
                          </div>
                        )}
                      </article>
                    )
                  })}
                </div>
              )}
            </section>
          </div></div>
          <div className="modal-footer"><button type="button" className="secondary-button" disabled={saving} onClick={() => setEditingId(undefined)}>Cancelar</button><button className="primary-button" disabled={saving} aria-busy={saving}><Save size={16} /> {saving ? 'Salvando...' : 'Salvar sabor'}</button></div>
        </form>
      </Modal>}
      <article className="surface-card table-card-container">
        <div className="toolbar inner"><div className="toolbar-search grow"><Search size={17} /><input aria-label="Buscar sabor" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar sabor..." /></div></div>
        <div className="responsive-table">
          <table>
            <thead><tr><th>Sabor</th><th>Categoria</th><th>Tipo</th><th>Características</th><th>Status</th><th aria-label="Ações" /></tr></thead>
            <tbody>{visible.map((flavor) => <tr key={flavor.id}><td><strong>{flavor.name}</strong><small className="table-description">{flavor.description}</small></td><td>{categoryName(flavor.categoryId)}</td><td>{flavor.type === 'Sweet' ? 'Doce' : 'Salgado'}</td><td>{[flavor.isPremium && 'Premium', flavor.isVegetarian && 'Vegetariano', `${flavor.extras.length} adicionais`].filter(Boolean).join(' · ') || 'Tradicional'}</td><td><StatusBadge status={flavor.isAvailable ? 'Disponível' : 'Fora de estoque'} /></td><td>{hasPermission('admin:write') && <button className="icon-button" aria-label={`Editar ${flavor.name}`} onClick={() => edit(flavor)}><Edit3 size={17} /></button>}</td></tr>)}</tbody>
          </table>
        </div>
      </article>
    </>
  )
}
