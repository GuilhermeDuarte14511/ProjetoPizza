import { zodResolver } from '@hookform/resolvers/zod'
import { ImagePlus, MoreHorizontal, PackagePlus, Plus, Save, Search, Trash2 } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Controller, useFieldArray, useForm, useWatch } from 'react-hook-form'
import { CurrencyInput } from '../components/ui/CurrencyInput'
import { ConfirmDialog } from '../components/ui/ConfirmDialog'
import { FieldError } from '../components/ui/FieldError'
import { Modal } from '../components/ui/Modal'
import { PageHeader } from '../components/ui/PageHeader'
import { StatusBadge } from '../components/ui/StatusBadge'
import { useToast } from '../components/ui/toast'
import { ViewTransitionLink } from '../components/ui/ViewTransitionLink'
import { productSchema, type ProductFormData } from '../features/admin/formSchemas'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { queryKeys } from '../lib/queryKeys'
import { adminService } from '../services/adminService'
import { hasPermission } from '../services/authSession'
import type { Product } from '../types/admin'
import { getUserErrorMessage } from '../utils/errors'
import { resolveApiMediaUrl } from '../api/httpClient'

const currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })

export function ProductsPage() {
  const { data: products, setData: setProducts } = useAdminQuery(queryKeys.products, adminService.products)
  const { data: categories } = useAdminQuery(queryKeys.categories, adminService.categories)
  const { data: ingredients } = useAdminQuery(queryKeys.ingredients, adminService.ingredients)
  const [search, setSearch] = useState(() => new URLSearchParams(window.location.search).get('search') ?? '')
  const [editingId, setEditingId] = useState<string>()
  const [confirmDiscard, setConfirmDiscard] = useState(false)
  const [saving, setSaving] = useState(false)
  const [activeModalTab, setActiveModalTab] = useState<'details' | 'complements'>('details')
  const [complementsTouched, setComplementsTouched] = useState(false)
  const [newComplement, setNewComplement] = useState({ name: '', price: 0, maxQuantity: 3 })
  const [imageFile, setImageFile] = useState<File>()
  const [imagePreview, setImagePreview] = useState<string>()
  const toast = useToast()
  const form = useForm<ProductFormData>({
    resolver: zodResolver(productSchema),
    defaultValues: {
      categoryId: '',
      sku: '',
      name: '',
      description: '',
      type: 'Standard',
      basePrice: 0,
      preparationTimeMinutes: 15,
      isActive: true,
      isAvailable: true,
      isFeatured: false,
      usesCustomExtras: false,
      complements: [],
    },
  })
  const complements = useFieldArray({ control: form.control, name: 'complements' })
  const productType = useWatch({ control: form.control, name: 'type' })

  const visibleProducts = useMemo(() => products.filter((product) => product.name.toLowerCase().includes(search.toLowerCase())), [products, search])
  const categoryName = (id: string) => categories.find((category) => category.id === id)?.name ?? 'Sem categoria'

  function edit(product?: Product) {
    const draft: ProductFormData = product ?? {
      categoryId: '',
      sku: '',
      name: '',
      description: '',
      type: 'Standard',
      basePrice: 0,
      preparationTimeMinutes: 15,
      isActive: true,
      isAvailable: true,
      isFeatured: false,
      usesCustomExtras: false,
      complements: [],
    }
    form.reset(draft)
    setImageFile(undefined)
    setImagePreview(resolveApiMediaUrl(product?.imageUrl))
    setActiveModalTab('details')
    setComplementsTouched(false)
    setNewComplement({ name: '', price: 0, maxQuantity: 3 })
    setEditingId(product?.id ?? 'new')
  }

  function requestClose() {
    if (form.formState.isDirty) {
      setConfirmDiscard(true)
      return
    }
    setEditingId(undefined)
  }

  async function save(draft: ProductFormData) {
    setSaving(true)
    try {
      const savesCustomComplements = draft.type === 'Pizza' &&
        (!draft.id || draft.usesCustomExtras || complementsTouched)
      const command = {
        ...draft,
        usesCustomExtras: savesCustomComplements,
        complements: savesCustomComplements ? draft.complements : undefined,
      }
      const result = await adminService.saveProduct(command) as { id: string }
      const productId = draft.id ?? result.id
      const uploaded = imageFile
        ? await adminService.uploadProductImage(productId, imageFile, `Foto de ${draft.name}`)
        : undefined
      const saved = {
        ...draft,
        id: productId,
        imageUrl: uploaded?.status ?? products.find((item) => item.id === productId)?.imageUrl,
        usesCustomExtras: savesCustomComplements,
        complements: savesCustomComplements ? draft.complements : [],
      } as Product
      setProducts((current) => draft.id ? current.map((item) => item.id === draft.id ? saved : item) : [...current, saved])
      form.reset(draft)
      setEditingId(undefined)
      toast.success(draft.id ? 'Produto atualizado' : 'Produto adicionado', `${draft.name} foi salvo com sucesso.`)
    } catch (error) {
      toast.error('Não foi possível salvar o produto', getUserErrorMessage(error))
    } finally {
      setSaving(false)
    }
  }

  function markComplementsChanged() {
    setComplementsTouched(true)
    form.setValue('usesCustomExtras', true, { shouldDirty: true })
  }

  function addExistingComplement(ingredientId: string) {
    const ingredient = ingredients.find((item) => item.id === ingredientId)
    if (!ingredient) return
    complements.append({
      ingredientId: ingredient.id,
      name: ingredient.name,
      price: ingredient.extraPrice,
      maxQuantity: ingredient.maxExtraQuantity,
    })
    markComplementsChanged()
  }

  function addNewComplement() {
    const name = newComplement.name.trim()
    if (!name) {
      toast.error('Informe o complemento', 'Digite um nome antes de adicionar.')
      return
    }
    if (form.getValues('complements').some((item) => item.name.toLocaleLowerCase('pt-BR') === name.toLocaleLowerCase('pt-BR'))) {
      toast.error('Complemento duplicado', `${name} já foi incluído nesta pizza.`)
      return
    }
    complements.append({ ...newComplement, name })
    setNewComplement({ name: '', price: 0, maxQuantity: 3 })
    markComplementsChanged()
  }

  const selectedIngredientIds = new Set(complements.fields.map((item) => item.ingredientId).filter(Boolean))
  const availableComplements = ingredients.filter((ingredient) =>
    ingredient.isActive &&
    ingredient.isAvailableAsExtra &&
    !selectedIngredientIds.has(ingredient.id))

  return (
    <>
      <PageHeader title="Produtos" description="Gerencie itens, preços e disponibilidade do cardápio." actions={hasPermission('admin:write') && <button className="primary-button" onClick={() => edit()}><Plus size={16} /> Adicionar produto</button>} />
      <nav className="catalog-tabs" aria-label="Seções do cardápio" role="tablist"><ViewTransitionLink role="tab" aria-selected className="active" href="/admin/catalog/products">Produtos</ViewTransitionLink><ViewTransitionLink role="tab" aria-selected={false} href="/admin/catalog/categories">Categorias</ViewTransitionLink><ViewTransitionLink role="tab" aria-selected={false} href="/admin/catalog/pizza-flavors">Sabores</ViewTransitionLink><ViewTransitionLink role="tab" aria-selected={false} href="/admin/catalog/crusts">Bordas</ViewTransitionLink><ViewTransitionLink role="tab" aria-selected={false} href="/admin/catalog/ingredients">Ingredientes</ViewTransitionLink></nav>
      {editingId && <Modal open title={form.getValues('id') ? 'Editar produto' : 'Novo produto'} description="Configure os dados comerciais e os complementos disponíveis no cardápio." size="large" isBusy={saving} onClose={requestClose}>
        <form onSubmit={form.handleSubmit(save)} noValidate>
          <div className="product-modal-tabs" role="tablist" aria-label="Configuração do produto">
            <button type="button" role="tab" aria-selected={activeModalTab === 'details'} className={activeModalTab === 'details' ? 'active' : ''} onClick={() => setActiveModalTab('details')}>Dados do produto</button>
            <button type="button" role="tab" aria-selected={activeModalTab === 'complements'} className={activeModalTab === 'complements' ? 'active' : ''} onClick={() => setActiveModalTab('complements')}>Complementos <span>{complements.fields.length}</span></button>
          </div>
          <div className="modal-body">
            {activeModalTab === 'details' ? (
              <div className="form-grid three-columns" role="tabpanel">
                <label className="field-label">Nome<input autoFocus aria-invalid={Boolean(form.formState.errors.name)} {...form.register('name')} /><FieldError message={form.formState.errors.name?.message} /></label>
                <label className="field-label">SKU<input disabled={Boolean(form.getValues('id'))} aria-invalid={Boolean(form.formState.errors.sku)} {...form.register('sku')} /><FieldError message={form.formState.errors.sku?.message} /></label>
                <label className="field-label">Categoria<select aria-invalid={Boolean(form.formState.errors.categoryId)} {...form.register('categoryId')}><option value="">Selecione</option>{categories.map((category) => <option value={category.id} key={category.id}>{category.name}</option>)}</select><FieldError message={form.formState.errors.categoryId?.message} /></label>
                <label className="field-label">Tipo<select {...form.register('type')}><option value="Pizza">Pizza</option><option value="Beverage">Bebida</option><option value="Portion">Porção</option><option value="Dessert">Sobremesa</option><option value="Standard">Padrão</option></select></label>
                <label className="field-label">Preço base<Controller control={form.control} name="basePrice" render={({ field }) => <CurrencyInput name={field.name} value={field.value} onBlur={field.onBlur} getInputRef={field.ref} aria-invalid={Boolean(form.formState.errors.basePrice)} onCurrencyValueChange={field.onChange} />} /><FieldError message={form.formState.errors.basePrice?.message} /></label>
                <label className="field-label">Tempo de preparo (min)<input type="number" min={0} max={240} {...form.register('preparationTimeMinutes', { valueAsNumber: true })} /><FieldError message={form.formState.errors.preparationTimeMinutes?.message} /></label>
                <label className="field-label span-2">Descrição<textarea rows={3} {...form.register('description')} placeholder="Ingredientes, características e informações comerciais" /><FieldError message={form.formState.errors.description?.message} /></label>
                <label className="menu-image-field">
                  <span>Imagem do cardápio</span>
                  <span className="menu-image-preview">{imagePreview ? <img src={imagePreview} alt="Prévia do produto" /> : <ImagePlus aria-hidden="true" />}</span>
                  <input type="file" accept="image/jpeg,image/png,image/webp" onChange={(event) => {
                    const file = event.target.files?.[0]
                    setImageFile(file)
                    if (file) setImagePreview(URL.createObjectURL(file))
                  }} />
                  <small>JPEG, PNG ou WebP · até 5 MB</small>
                </label>
                <div className="check-stack"><label className="check-label"><input type="checkbox" {...form.register('isAvailable')} /> Disponível</label><label className="check-label"><input type="checkbox" {...form.register('isFeatured')} /> Destaque</label></div>
              </div>
            ) : productType !== 'Pizza' ? (
              <div className="product-complements-empty" role="tabpanel">
                <PackagePlus size={32} />
                <h3>Complementos são configurados em pizzas</h3>
                <p>Altere o tipo do produto para Pizza para adicionar ingredientes opcionais e seus valores.</p>
              </div>
            ) : (
              <div className="product-complements-panel" role="tabpanel">
                <section className="new-complement-card" aria-labelledby="new-complement-title">
                  <div><span className="eyebrow">Cadastro rápido</span><h3 id="new-complement-title">Adicionar novo complemento</h3><p>O complemento também ficará disponível no catálogo de ingredientes.</p></div>
                  <div className="new-complement-fields">
                    <label className="field-label">Nome<input value={newComplement.name} maxLength={120} onChange={(event) => setNewComplement((current) => ({ ...current, name: event.target.value }))} placeholder="Ex.: Bacon extra" /></label>
                    <label className="field-label">Valor<CurrencyInput value={newComplement.price} onCurrencyValueChange={(price) => setNewComplement((current) => ({ ...current, price }))} /></label>
                    <label className="field-label">Limite por parte<input type="number" min={1} max={10} value={newComplement.maxQuantity} onChange={(event) => setNewComplement((current) => ({ ...current, maxQuantity: Number(event.target.value) }))} /></label>
                    <button type="button" className="secondary-button" onClick={addNewComplement}><Plus size={16} /> Adicionar</button>
                  </div>
                </section>

                {availableComplements.length > 0 && (
                  <section className="available-complements">
                    <header><div><h3>Complementos já cadastrados</h3><p>Adicione rapidamente e ajuste o valor somente para esta pizza.</p></div></header>
                    <div className="available-complements-list">
                      {availableComplements.map((ingredient) => (
                        <button type="button" key={ingredient.id} onClick={() => addExistingComplement(ingredient.id)}>
                          <Plus size={15} /><span><strong>{ingredient.name}</strong><small>{currency.format(ingredient.extraPrice)} · até {ingredient.maxExtraQuantity}</small></span>
                        </button>
                      ))}
                    </div>
                  </section>
                )}

                <section className="selected-complements">
                  <header><div><h3>Disponíveis nesta pizza</h3><p>Remova ou personalize preço e limite de cada complemento.</p></div><span>{complements.fields.length} selecionado(s)</span></header>
                  {complements.fields.length === 0 ? (
                    <div className="product-complements-empty compact"><PackagePlus size={28} /><p>Nenhum complemento ficará disponível para esta pizza.</p></div>
                  ) : complements.fields.map((complement, index) => (
                    <article className="selected-complement-row" key={complement.id}>
                      <div><strong>{complement.name}</strong><small>Aplicável a cada sabor selecionado</small></div>
                      <label className="field-label">Valor<Controller control={form.control} name={`complements.${index}.price`} render={({ field }) => <CurrencyInput name={field.name} value={field.value} onBlur={field.onBlur} getInputRef={field.ref} onCurrencyValueChange={(value) => { field.onChange(value); markComplementsChanged() }} />} /></label>
                      <label className="field-label">Limite<input type="number" min={1} max={10} {...form.register(`complements.${index}.maxQuantity`, { valueAsNumber: true, onChange: markComplementsChanged })} /></label>
                      <button type="button" className="icon-button danger" aria-label={`Remover ${complement.name}`} onClick={() => { complements.remove(index); markComplementsChanged() }}><Trash2 size={17} /></button>
                    </article>
                  ))}
                </section>
              </div>
            )}
          </div>
          <div className="modal-footer"><button type="button" className="secondary-button" disabled={saving} onClick={requestClose}>Cancelar</button><button className="primary-button" disabled={saving} aria-busy={saving}><Save size={16} /> {saving ? 'Salvando...' : 'Salvar produto'}</button></div>
        </form>
      </Modal>}
      <ConfirmDialog open={confirmDiscard} title="Descartar alterações?" description="As informações ainda não salvas serão perdidas." confirmLabel="Descartar" tone="danger" onOpenChange={setConfirmDiscard} onConfirm={() => { setConfirmDiscard(false); setEditingId(undefined) }} />
      <article className="surface-card table-card-container">
        <div className="toolbar inner"><div className="toolbar-search grow"><Search size={17} /><input aria-label="Buscar produto" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar produto..." /></div></div>
        <div className="responsive-table">
          <table>
            <thead><tr><th>Produto</th><th>Categoria</th><th>Preço base</th><th>Status</th><th aria-label="Ações" /></tr></thead>
            <tbody>{visibleProducts.map((product) => <tr key={product.id}><td><div className="product-cell"><span className="product-thumb">{product.imageUrl ? <img src={resolveApiMediaUrl(product.imageUrl)} alt="" /> : product.name.slice(0, 1)}</span><span><strong>{product.name}</strong><small>{product.sku}</small></span></div></td><td>{categoryName(product.categoryId)}</td><td><strong>{currency.format(product.basePrice)}</strong></td><td><StatusBadge status={product.isAvailable ? 'Disponível' : 'Fora de estoque'} /></td><td>{hasPermission('admin:write') && <button className="icon-button" aria-label={`Editar ${product.name}`} onClick={() => edit(product)}><MoreHorizontal size={18} /></button>}</td></tr>)}</tbody>
          </table>
        </div>
      </article>
    </>
  )
}
