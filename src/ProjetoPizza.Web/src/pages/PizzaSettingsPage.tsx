import { zodResolver } from '@hookform/resolvers/zod'
import { Edit3, Plus, Save } from 'lucide-react'
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { CurrencyInput } from '../components/ui/CurrencyInput'
import { FieldError } from '../components/ui/FieldError'
import { Modal } from '../components/ui/Modal'
import { PageHeader } from '../components/ui/PageHeader'
import { useToast } from '../components/ui/toast'
import { ViewTransitionLink } from '../components/ui/ViewTransitionLink'
import { pizzaSizeSchema, type PizzaSizeFormData } from '../features/admin/formSchemas'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { queryKeys } from '../lib/queryKeys'
import { adminService } from '../services/adminService'
import { hasPermission } from '../services/authSession'
import type { PizzaSize } from '../types/admin'
import { getUserErrorMessage } from '../utils/errors'

interface PizzaSettingsPageProps {
  initialTab: 'rules' | 'sizes'
}

export function PizzaSettingsPage({ initialTab }: PizzaSettingsPageProps) {
  const { data: sizes, setData: setSizes } = useAdminQuery(queryKeys.pizzaSizes, adminService.pizzaSizes)
  const { data: rules, setData: setRules } = useAdminQuery(queryKeys.pizzaRules, adminService.pizzaRules)
  const [editingSizeId, setEditingSizeId] = useState<string>()
  const [savingRules, setSavingRules] = useState(false)
  const [savingSize, setSavingSize] = useState(false)
  const toast = useToast()
  const sizeForm = useForm<PizzaSizeFormData>({
    resolver: zodResolver(pizzaSizeSchema),
    defaultValues: { name: '', shortName: '', slices: 4, diameterCm: 20, basePrice: 0, maxFlavors: 1, isActive: true },
  })

  async function saveRules() {
    setSavingRules(true)
    try {
      await adminService.savePizzaRules(rules)
      toast.success('Regras atualizadas', 'As configurações de pizzas foram salvas.')
    } catch (error) {
      toast.error('Não foi possível salvar as regras', getUserErrorMessage(error))
    } finally {
      setSavingRules(false)
    }
  }

  function editSize(size?: PizzaSize) {
    sizeForm.reset(size ?? { name: '', shortName: '', slices: 4, diameterCm: 20, basePrice: 0, maxFlavors: 1, isActive: true })
    setEditingSizeId(size?.id ?? 'new')
  }

  async function saveSize(command: PizzaSizeFormData) {
    setSavingSize(true)
    try {
      const result = await adminService.savePizzaSize(command as PizzaSize | Omit<PizzaSize, 'id'>) as { id: string }
      const savedSize = { ...command, id: command.id ?? result.id } as PizzaSize
      setSizes((current) => command.id ? current.map((item) => item.id === command.id ? savedSize : item) : [...current, savedSize])
      setEditingSizeId(undefined)
      toast.success(command.id ? 'Tamanho atualizado' : 'Tamanho adicionado', `${command.name} foi salvo com sucesso.`)
    } catch (error) {
      toast.error('Não foi possível salvar o tamanho', getUserErrorMessage(error))
    } finally {
      setSavingSize(false)
    }
  }

  return (
    <>
      <PageHeader title="Regras de Pizzas" description="Configure composição, preços e limites por tamanho." actions={hasPermission('admin:write') && <button className="primary-button" disabled={savingRules} aria-busy={savingRules} onClick={() => void saveRules()}><Save size={16} /> {savingRules ? 'Salvando...' : 'Salvar alterações'}</button>} />
      <nav className="settings-tabs" aria-label="Seções de configurações" role="tablist"><ViewTransitionLink role="tab" aria-selected={false} href="/admin/settings/general">Dados da pizzaria</ViewTransitionLink><ViewTransitionLink role="tab" aria-selected={false} href="/admin/settings/operation">Operação</ViewTransitionLink><ViewTransitionLink role="tab" aria-selected={initialTab === 'rules'} className={initialTab === 'rules' ? 'active' : ''} href="/admin/settings/pizza-rules">Regras de pizzas</ViewTransitionLink><ViewTransitionLink role="tab" aria-selected={initialTab === 'sizes'} className={initialTab === 'sizes' ? 'active' : ''} href="/admin/catalog/pizza-sizes">Tamanhos</ViewTransitionLink></nav>
      <section className="settings-grid">
        <article className="surface-card">
          <div className="card-heading"><div><h2>Composição de sabores</h2><p>Regras globais aplicadas ao montador.</p></div></div>
          <label className="field-label">Máximo de sabores<input type="number" min="1" max="3" value={rules.globalMaxFlavors} onChange={(event) => setRules({ ...rules, globalMaxFlavors: Number(event.target.value) })} /></label>
          <label className="field-label">Política de precificação<select value={rules.pricingPolicy} onChange={(event) => setRules({ ...rules, pricingPolicy: event.target.value })}><option value="HighestFlavorPrice">Maior valor entre os sabores</option><option value="AverageFlavorPrice">Média dos sabores</option><option value="ProportionalFlavorPrice">Valor proporcional</option></select></label>
          <Toggle label="Permitir mistura doce e salgada" checked={rules.allowSweetAndSavoryMix} onChange={(checked) => setRules({ ...rules, allowSweetAndSavoryMix: checked })} />
          <Toggle label="Permitir adicionais por sabor" checked={rules.allowExtrasPerFlavor} onChange={(checked) => setRules({ ...rules, allowExtrasPerFlavor: checked })} />
          <Toggle label="Permitir sabores repetidos" checked={rules.allowRepeatedFlavors} onChange={(checked) => setRules({ ...rules, allowRepeatedFlavors: checked })} />
        </article>
        <article className="surface-card">
          <div className="card-heading"><div><h2>Limites por tamanho</h2><p>Fatias, dimensões e composição.</p></div>{hasPermission('admin:write') && <button className="secondary-button" onClick={() => editSize()}><Plus size={16} /> Adicionar</button>}</div>
          <div className="size-list">
            {sizes.map((size) => <div className="size-row" key={size.id}><span className="size-mark">{size.shortName}</span><span><strong>{size.name}</strong><small>{size.slices} fatias · {size.diameterCm} cm</small></span><span><small>Máx. sabores</small><strong>{size.maxFlavors}</strong></span>{hasPermission('admin:write') && <button className="icon-button" aria-label={`Editar ${size.name}`} onClick={() => editSize(size)}><Edit3 size={17} /></button>}</div>)}
          </div>
        </article>
      </section>
      {editingSizeId && <Modal open title={sizeForm.getValues('id') ? 'Editar tamanho' : 'Novo tamanho'} description="Os limites serão validados pelas regras do domínio." size="large" isBusy={savingSize} onClose={() => setEditingSizeId(undefined)}>
        <form onSubmit={sizeForm.handleSubmit(saveSize)} noValidate>
          <div className="modal-body"><div className="form-grid three-columns">
            <label className="field-label">Nome<input autoFocus aria-invalid={Boolean(sizeForm.formState.errors.name)} {...sizeForm.register('name')} /><FieldError message={sizeForm.formState.errors.name?.message} /></label>
            <label className="field-label">Sigla<input aria-invalid={Boolean(sizeForm.formState.errors.shortName)} {...sizeForm.register('shortName')} /><FieldError message={sizeForm.formState.errors.shortName?.message} /></label>
            <label className="field-label">Fatias<input type="number" min="1" aria-invalid={Boolean(sizeForm.formState.errors.slices)} {...sizeForm.register('slices', { valueAsNumber: true })} /><FieldError message={sizeForm.formState.errors.slices?.message} /></label>
            <label className="field-label">Diâmetro (cm)<input type="number" min="1" aria-invalid={Boolean(sizeForm.formState.errors.diameterCm)} {...sizeForm.register('diameterCm', { valueAsNumber: true })} /><FieldError message={sizeForm.formState.errors.diameterCm?.message} /></label>
            <label className="field-label">Preço base<Controller control={sizeForm.control} name="basePrice" render={({ field }) => <CurrencyInput name={field.name} value={field.value} onBlur={field.onBlur} getInputRef={field.ref} aria-invalid={Boolean(sizeForm.formState.errors.basePrice)} onCurrencyValueChange={field.onChange} />} /><FieldError message={sizeForm.formState.errors.basePrice?.message} /></label>
            <label className="field-label">Máx. sabores<input type="number" min="1" max="3" aria-invalid={Boolean(sizeForm.formState.errors.maxFlavors)} {...sizeForm.register('maxFlavors', { valueAsNumber: true })} /><FieldError message={sizeForm.formState.errors.maxFlavors?.message} /></label>
          </div></div>
          <div className="modal-footer"><button type="button" className="secondary-button" disabled={savingSize} onClick={() => setEditingSizeId(undefined)}>Cancelar</button><button className="primary-button" disabled={savingSize} aria-busy={savingSize}><Save size={16} /> {savingSize ? 'Salvando...' : 'Salvar tamanho'}</button></div>
        </form>
      </Modal>}
    </>
  )
}

function Toggle({ label, checked, onChange }: { label: string; checked: boolean; onChange: (checked: boolean) => void }) {
  return <label className="toggle-row"><span>{label}</span><input type="checkbox" checked={checked} onChange={(event) => onChange(event.target.checked)} /></label>
}
