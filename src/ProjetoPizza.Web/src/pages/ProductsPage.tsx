import { zodResolver } from '@hookform/resolvers/zod'
import { MoreHorizontal, Plus, Save, Search } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
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

const currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })

export function ProductsPage() {
  const { data: products, setData: setProducts } = useAdminQuery(queryKeys.products, adminService.products)
  const { data: categories } = useAdminQuery(queryKeys.categories, adminService.categories)
  const [search, setSearch] = useState(() => new URLSearchParams(window.location.search).get('search') ?? '')
  const [editingId, setEditingId] = useState<string>()
  const [confirmDiscard, setConfirmDiscard] = useState(false)
  const [saving, setSaving] = useState(false)
  const toast = useToast()
  const form = useForm<ProductFormData>({
    resolver: zodResolver(productSchema),
    defaultValues: {
      categoryId: '',
      sku: '',
      name: '',
      type: 'Standard',
      basePrice: 0,
      isActive: true,
      isAvailable: true,
      isFeatured: false,
    },
  })

  const visibleProducts = useMemo(() => products.filter((product) => product.name.toLowerCase().includes(search.toLowerCase())), [products, search])
  const categoryName = (id: string) => categories.find((category) => category.id === id)?.name ?? 'Sem categoria'

  function edit(product?: Product) {
    const draft: ProductFormData = product ?? {
      categoryId: '',
      sku: '',
      name: '',
      type: 'Standard',
      basePrice: 0,
      isActive: true,
      isAvailable: true,
      isFeatured: false,
    }
    form.reset(draft)
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
      const result = await adminService.saveProduct(draft) as { id: string }
      const saved = { ...draft, id: draft.id ?? result.id } as Product
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

  return (
    <>
      <PageHeader title="Produtos" description="Gerencie itens, preços e disponibilidade do cardápio." actions={hasPermission('admin:write') && <button className="primary-button" onClick={() => edit()}><Plus size={16} /> Adicionar produto</button>} />
      <nav className="catalog-tabs" aria-label="Seções do cardápio" role="tablist"><ViewTransitionLink role="tab" aria-selected className="active" href="/admin/catalog/products">Produtos</ViewTransitionLink><ViewTransitionLink role="tab" aria-selected={false} href="/admin/catalog/categories">Categorias</ViewTransitionLink><ViewTransitionLink role="tab" aria-selected={false} href="/admin/catalog/pizza-flavors">Sabores</ViewTransitionLink><ViewTransitionLink role="tab" aria-selected={false} href="/admin/catalog/crusts">Bordas</ViewTransitionLink></nav>
      {editingId && <Modal open title={form.getValues('id') ? 'Editar produto' : 'Novo produto'} description="Informe os dados comerciais e a disponibilidade no cardápio." size="large" isBusy={saving} onClose={requestClose}>
        <form onSubmit={form.handleSubmit(save)} noValidate>
          <div className="modal-body"><div className="form-grid three-columns">
            <label className="field-label">Nome<input autoFocus aria-invalid={Boolean(form.formState.errors.name)} {...form.register('name')} /><FieldError message={form.formState.errors.name?.message} /></label>
            <label className="field-label">SKU<input disabled={Boolean(form.getValues('id'))} aria-invalid={Boolean(form.formState.errors.sku)} {...form.register('sku')} /><FieldError message={form.formState.errors.sku?.message} /></label>
            <label className="field-label">Categoria<select aria-invalid={Boolean(form.formState.errors.categoryId)} {...form.register('categoryId')}><option value="">Selecione</option>{categories.map((category) => <option value={category.id} key={category.id}>{category.name}</option>)}</select><FieldError message={form.formState.errors.categoryId?.message} /></label>
            <label className="field-label">Tipo<select {...form.register('type')}><option value="Pizza">Pizza</option><option value="Beverage">Bebida</option><option value="Portion">Porção</option><option value="Dessert">Sobremesa</option><option value="Standard">Padrão</option></select></label>
            <label className="field-label">Preço base<Controller control={form.control} name="basePrice" render={({ field }) => <CurrencyInput name={field.name} value={field.value} onBlur={field.onBlur} getInputRef={field.ref} aria-invalid={Boolean(form.formState.errors.basePrice)} onCurrencyValueChange={field.onChange} />} /><FieldError message={form.formState.errors.basePrice?.message} /></label>
            <div className="check-stack"><label className="check-label"><input type="checkbox" {...form.register('isAvailable')} /> Disponível</label><label className="check-label"><input type="checkbox" {...form.register('isFeatured')} /> Destaque</label></div>
          </div></div>
          <div className="modal-footer"><button type="button" className="secondary-button" disabled={saving} onClick={requestClose}>Cancelar</button><button className="primary-button" disabled={saving} aria-busy={saving}><Save size={16} /> {saving ? 'Salvando...' : 'Salvar produto'}</button></div>
        </form>
      </Modal>}
      <ConfirmDialog open={confirmDiscard} title="Descartar alterações?" description="As informações ainda não salvas serão perdidas." confirmLabel="Descartar" tone="danger" onOpenChange={setConfirmDiscard} onConfirm={() => { setConfirmDiscard(false); setEditingId(undefined) }} />
      <article className="surface-card table-card-container">
        <div className="toolbar inner"><div className="toolbar-search grow"><Search size={17} /><input aria-label="Buscar produto" value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Buscar produto..." /></div></div>
        <div className="responsive-table">
          <table>
            <thead><tr><th>Produto</th><th>Categoria</th><th>Preço base</th><th>Status</th><th aria-label="Ações" /></tr></thead>
            <tbody>{visibleProducts.map((product) => <tr key={product.id}><td><div className="product-cell"><span className="product-thumb">{product.name.slice(0, 1)}</span><span><strong>{product.name}</strong><small>{product.sku}</small></span></div></td><td>{categoryName(product.categoryId)}</td><td><strong>{currency.format(product.basePrice)}</strong></td><td><StatusBadge status={product.isAvailable ? 'Disponível' : 'Fora de estoque'} /></td><td>{hasPermission('admin:write') && <button className="icon-button" aria-label={`Editar ${product.name}`} onClick={() => edit(product)}><MoreHorizontal size={18} /></button>}</td></tr>)}</tbody>
          </table>
        </div>
      </article>
    </>
  )
}
