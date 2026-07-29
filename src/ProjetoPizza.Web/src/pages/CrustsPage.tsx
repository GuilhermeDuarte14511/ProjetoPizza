import { zodResolver } from '@hookform/resolvers/zod'
import { Edit3, Plus, Save } from 'lucide-react'
import { useState } from 'react'
import { Controller, useFieldArray, useForm } from 'react-hook-form'
import { CurrencyInput } from '../components/ui/CurrencyInput'
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
import { formatCurrency } from '../utils/money'

export function CrustsPage() {
  const { data: crusts, setData: setCrusts } = useAdminQuery(queryKeys.crusts, adminService.crusts)
  const { data: sizes } = useAdminQuery(queryKeys.pizzaSizes, adminService.pizzaSizes)
  const [editingId, setEditingId] = useState<string>()
  const [saving, setSaving] = useState(false)
  const toast = useToast()
  const form = useForm<CrustFormData>({
    resolver: zodResolver(crustSchema),
    defaultValues: { name: '', description: '', isActive: true, isAvailable: true, prices: [] },
  })
  const priceFields = useFieldArray({ control: form.control, name: 'prices' })

  function edit(crust?: PizzaCrust) {
    if (!crust && sizes.length === 0) {
      toast.info('Tamanhos ainda carregando', 'Aguarde um instante para cadastrar os preços da borda.')
      return
    }

    form.reset(crust ?? {
      name: '',
      description: '',
      isActive: true,
      isAvailable: true,
      prices: sizes.map((size) => ({
        pizzaSizeId: size.id,
        pizzaSizeName: size.name,
        sliceCount: size.slices,
        fullPrice: 0,
        halfPrice: 0,
      })),
    })
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
      <nav className="catalog-tabs" aria-label="Seções do cardápio" role="tablist"><ViewTransitionLink role="tab" aria-selected={false} href="/admin/catalog/products">Produtos</ViewTransitionLink><ViewTransitionLink role="tab" aria-selected={false} href="/admin/catalog/categories">Categorias</ViewTransitionLink><ViewTransitionLink role="tab" aria-selected={false} href="/admin/catalog/pizza-flavors">Sabores</ViewTransitionLink><ViewTransitionLink role="tab" aria-selected className="active" href="/admin/catalog/crusts">Bordas</ViewTransitionLink><ViewTransitionLink role="tab" aria-selected={false} href="/admin/catalog/ingredients">Ingredientes</ViewTransitionLink></nav>
      {editingId && <Modal open title={form.getValues('id') ? 'Editar borda' : 'Nova borda'} description="Defina quanto custa a borda inteira e cada meia borda por tamanho." isBusy={saving} onClose={() => setEditingId(undefined)}>
        <form onSubmit={form.handleSubmit(save)} noValidate>
        <div className="modal-body"><div className="form-grid two-columns">
          <label className="field-label">Nome<input autoFocus aria-invalid={Boolean(form.formState.errors.name)} {...form.register('name')} /><FieldError message={form.formState.errors.name?.message} /></label>
          <label className="field-label">Descrição<input {...form.register('description')} /><FieldError message={form.formState.errors.description?.message} /></label>
          <label className="check-label"><input type="checkbox" {...form.register('isActive')} /> Ativa no catálogo</label>
          <label className="check-label"><input type="checkbox" {...form.register('isAvailable')} /> Disponível para venda</label>
          <section className="crust-price-editor wide" aria-labelledby="crust-price-title">
            <header>
              <div>
                <h3 id="crust-price-title">Preços por tamanho</h3>
                <p>Na opção dividida, o total será a soma dos preços das duas metades escolhidas.</p>
              </div>
            </header>
            <div className="crust-price-table">
              <div className="crust-price-table-heading" aria-hidden="true">
                <span>Tamanho</span><span>Borda inteira</span><span>Meia borda</span>
              </div>
              {priceFields.fields.map((field, index) => (
                <div className="crust-price-row" key={field.id}>
                  <div>
                    <strong>{field.pizzaSizeName}</strong>
                    <small>{field.sliceCount} fatias</small>
                  </div>
                  <label>
                    <span className="mobile-field-label">Borda inteira</span>
                    <Controller
                      control={form.control}
                      name={`prices.${index}.fullPrice`}
                      render={({ field: priceField }) => (
                        <CurrencyInput
                          name={priceField.name}
                          value={priceField.value}
                          onBlur={priceField.onBlur}
                          getInputRef={priceField.ref}
                          aria-label={`Preço da borda inteira para pizza ${field.pizzaSizeName}`}
                          aria-invalid={Boolean(form.formState.errors.prices?.[index]?.fullPrice)}
                          onCurrencyValueChange={priceField.onChange}
                        />
                      )}
                    />
                    <FieldError message={form.formState.errors.prices?.[index]?.fullPrice?.message} />
                  </label>
                  <label>
                    <span className="mobile-field-label">Meia borda</span>
                    <Controller
                      control={form.control}
                      name={`prices.${index}.halfPrice`}
                      render={({ field: priceField }) => (
                        <CurrencyInput
                          name={priceField.name}
                          value={priceField.value}
                          onBlur={priceField.onBlur}
                          getInputRef={priceField.ref}
                          aria-label={`Preço da meia borda para pizza ${field.pizzaSizeName}`}
                          aria-invalid={Boolean(form.formState.errors.prices?.[index]?.halfPrice)}
                          onCurrencyValueChange={priceField.onChange}
                        />
                      )}
                    />
                    <FieldError message={form.formState.errors.prices?.[index]?.halfPrice?.message} />
                  </label>
                </div>
              ))}
            </div>
            <FieldError message={form.formState.errors.prices?.root?.message} />
          </section>
        </div></div>
        <div className="modal-footer"><button type="button" className="secondary-button" disabled={saving} onClick={() => setEditingId(undefined)}>Cancelar</button><button className="primary-button" disabled={saving} aria-busy={saving}><Save size={16} /> {saving ? 'Salvando...' : 'Salvar borda'}</button></div>
        </form>
      </Modal>}
      <section className="management-grid">
        {crusts.map((crust) => <article className="surface-card management-card" key={crust.id}>
          <div>
            <span className={`status-pill ${crust.isAvailable ? 'success' : 'danger'}`}>{crust.isAvailable ? 'Disponível' : 'Indisponível'}</span>
            <h2>{crust.name}</h2>
            <p>{crust.description}</p>
            {crust.prices[0] && <p className="management-price-summary">
              {crust.prices[0].pizzaSizeName}: inteira {formatCurrency(crust.prices[0].fullPrice)} · meia {formatCurrency(crust.prices[0].halfPrice)}
            </p>}
          </div>
          {hasPermission('admin:write') && <button className="secondary-button" onClick={() => edit(crust)}><Edit3 size={16} /> Editar</button>}
        </article>)}
      </section>
    </>
  )
}
