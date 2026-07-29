import { zodResolver } from '@hookform/resolvers/zod'
import { CircleAlert, Edit3, Plus, Save } from 'lucide-react'
import { useState } from 'react'
import { Controller, useForm, useWatch } from 'react-hook-form'
import { CurrencyInput } from '../components/ui/CurrencyInput'
import { FieldError } from '../components/ui/FieldError'
import { Modal } from '../components/ui/Modal'
import { PageHeader } from '../components/ui/PageHeader'
import { useToast } from '../components/ui/toast'
import { ViewTransitionLink } from '../components/ui/ViewTransitionLink'
import { ingredientSchema, type IngredientFormData } from '../features/admin/formSchemas'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { queryKeys } from '../lib/queryKeys'
import { adminService } from '../services/adminService'
import { hasPermission } from '../services/authSession'
import type { Ingredient } from '../types/admin'
import { getUserErrorMessage } from '../utils/errors'
import { formatCurrency } from '../utils/money'

const emptyIngredient: IngredientFormData = {
  name: '',
  description: '',
  isActive: true,
  isAllergen: false,
  allergenDescription: '',
  isAvailableAsExtra: true,
  extraPrice: 0,
  maxExtraQuantity: 1,
}

export function IngredientsPage() {
  const { data: ingredients, setData: setIngredients } = useAdminQuery(queryKeys.ingredients, adminService.ingredients)
  const [editingId, setEditingId] = useState<string>()
  const [saving, setSaving] = useState(false)
  const toast = useToast()
  const form = useForm<IngredientFormData>({
    resolver: zodResolver(ingredientSchema),
    defaultValues: emptyIngredient,
  })
  const isAllergen = useWatch({ control: form.control, name: 'isAllergen' })
  const isAvailableAsExtra = useWatch({ control: form.control, name: 'isAvailableAsExtra' })

  function edit(ingredient?: Ingredient) {
    form.reset(ingredient ?? emptyIngredient)
    setEditingId(ingredient?.id ?? 'new')
  }

  async function save(draft: IngredientFormData) {
    setSaving(true)
    try {
      const result = await adminService.saveIngredient(draft)
      const saved = { ...draft, id: draft.id ?? (result as { id: string }).id } as Ingredient
      setIngredients((current) => draft.id
        ? current.map((item) => item.id === draft.id ? saved : item)
        : [...current, saved].sort((left, right) => left.name.localeCompare(right.name)))
      setEditingId(undefined)
      toast.success(draft.id ? 'Ingrediente atualizado' : 'Ingrediente adicionado', `${draft.name} já está disponível no catálogo.`)
    } catch (error) {
      toast.error('Não foi possível salvar o ingrediente', getUserErrorMessage(error))
    } finally {
      setSaving(false)
    }
  }

  return (
    <>
      <PageHeader
        title="Ingredientes e adicionais"
        description="Configure quais ingredientes podem ser acrescentados às pizzas, seus preços e limites."
        actions={hasPermission('admin:write') && <button className="primary-button" onClick={() => edit()}><Plus size={16} /> Novo ingrediente</button>}
      />
      <nav className="catalog-tabs" aria-label="Seções do cardápio" role="tablist">
        <ViewTransitionLink role="tab" aria-selected={false} href="/admin/catalog/products">Produtos</ViewTransitionLink>
        <ViewTransitionLink role="tab" aria-selected={false} href="/admin/catalog/categories">Categorias</ViewTransitionLink>
        <ViewTransitionLink role="tab" aria-selected={false} href="/admin/catalog/pizza-flavors">Sabores</ViewTransitionLink>
        <ViewTransitionLink role="tab" aria-selected={false} href="/admin/catalog/crusts">Bordas</ViewTransitionLink>
        <ViewTransitionLink role="tab" aria-selected className="active" href="/admin/catalog/ingredients">Ingredientes</ViewTransitionLink>
      </nav>

      {editingId && (
        <Modal
          open
          title={form.getValues('id') ? 'Editar ingrediente' : 'Novo ingrediente'}
          description="O preço informado será aplicado por porção e validado novamente pela API."
          isBusy={saving}
          onClose={() => setEditingId(undefined)}
        >
          <form onSubmit={form.handleSubmit(save)} noValidate>
            <div className="modal-body">
              <div className="form-grid">
                <label className="field-label">Nome<input autoFocus aria-invalid={Boolean(form.formState.errors.name)} {...form.register('name')} /><FieldError message={form.formState.errors.name?.message} /></label>
                <label className="field-label">Descrição<input {...form.register('description')} /><FieldError message={form.formState.errors.description?.message} /></label>
                <label className="check-label"><input type="checkbox" {...form.register('isActive')} /> Ativo no catálogo</label>
                <label className="check-label"><input type="checkbox" {...form.register('isAvailableAsExtra')} /> Disponível como adicional</label>
                <label className="field-label">Preço por porção
                  <Controller control={form.control} name="extraPrice" render={({ field }) => (
                    <CurrencyInput
                      name={field.name}
                      value={field.value}
                      disabled={!isAvailableAsExtra}
                      onBlur={field.onBlur}
                      getInputRef={field.ref}
                      aria-invalid={Boolean(form.formState.errors.extraPrice)}
                      onCurrencyValueChange={field.onChange}
                    />
                  )} />
                  <FieldError message={form.formState.errors.extraPrice?.message} />
                </label>
                <label className="field-label">Máximo por sabor<input type="number" min={1} max={10} disabled={!isAvailableAsExtra} {...form.register('maxExtraQuantity', { valueAsNumber: true })} /><FieldError message={form.formState.errors.maxExtraQuantity?.message} /></label>
                <label className="check-label"><input type="checkbox" {...form.register('isAllergen')} /> Possui alérgeno</label>
                {isAllergen && <label className="field-label">Aviso de alérgeno<input {...form.register('allergenDescription')} placeholder="Ex.: contém leite e derivados" /><FieldError message={form.formState.errors.allergenDescription?.message} /></label>}
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="secondary-button" disabled={saving} onClick={() => setEditingId(undefined)}>Cancelar</button>
              <button className="primary-button" disabled={saving} aria-busy={saving}><Save size={16} /> {saving ? 'Salvando...' : 'Salvar ingrediente'}</button>
            </div>
          </form>
        </Modal>
      )}

      <section className="management-grid">
        {ingredients.map((ingredient) => (
          <article className="surface-card management-card" key={ingredient.id}>
            <div>
              <span className={`status-pill ${ingredient.isActive && ingredient.isAvailableAsExtra ? 'success' : 'neutral'}`}>
                {ingredient.isActive && ingredient.isAvailableAsExtra ? 'Disponível como adicional' : 'Somente composição'}
              </span>
              <h2>{ingredient.name}</h2>
              <p>{ingredient.description || 'Sem descrição.'}</p>
              {ingredient.isAvailableAsExtra && <strong>{formatCurrency(ingredient.extraPrice)} · até {ingredient.maxExtraQuantity} por sabor</strong>}
              {ingredient.isAllergen && <p><CircleAlert size={14} aria-hidden="true" /> {ingredient.allergenDescription || 'Contém alérgeno'}</p>}
            </div>
            {hasPermission('admin:write') && <button className="secondary-button" onClick={() => edit(ingredient)}><Edit3 size={16} /> Editar</button>}
          </article>
        ))}
      </section>
    </>
  )
}
