import { AlertTriangle, Boxes, ChefHat, Pencil, Plus, Save, SlidersHorizontal, Trash2 } from 'lucide-react'
import { type FormEvent, useState } from 'react'
import { Modal } from '../components/ui/Modal'
import { CurrencyInput } from '../components/ui/CurrencyInput'
import { PageHeader } from '../components/ui/PageHeader'
import { useToast } from '../components/ui/toast'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { queryKeys } from '../lib/queryKeys'
import { adminService } from '../services/adminService'
import { hasPermission } from '../services/authSession'
import type { InventoryItem, InventoryRecipe, SaveInventoryRecipe } from '../types/admin'
import { getUserErrorMessage } from '../utils/errors'
import { formatCurrency } from '../utils/money'

interface RecipeDraft {
  id?: string
  targetType: 'product' | 'flavor'
  targetId: string
  pizzaSizeId: string
  yieldQuantity: number
  items: Array<{ inventoryItemId: string; quantity: number; unitOfMeasure: string }>
}

const emptyRecipe = (): RecipeDraft => ({ targetType: 'product', targetId: '', pizzaSizeId: '', yieldQuantity: 1, items: [{ inventoryItemId: '', quantity: 1, unitOfMeasure: 'kg' }] })

export function InventoryPage() {
  const itemsQuery = useAdminQuery(queryKeys.inventory, adminService.inventory)
  const recipesQuery = useAdminQuery(queryKeys.recipes, adminService.recipes)
  const productsQuery = useAdminQuery(queryKeys.products, adminService.products)
  const flavorsQuery = useAdminQuery(queryKeys.pizzaFlavors, adminService.pizzaFlavors)
  const sizesQuery = useAdminQuery(queryKeys.pizzaSizes, adminService.pizzaSizes)
  const [editing, setEditing] = useState<InventoryItem | 'new'>()
  const [editingUnitCost, setEditingUnitCost] = useState(0)
  const [adjusting, setAdjusting] = useState<InventoryItem>()
  const [recipeDraft, setRecipeDraft] = useState<RecipeDraft>()
  const [saving, setSaving] = useState(false)
  const toast = useToast()

  async function saveItem(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const data = new FormData(event.currentTarget)
    setSaving(true)
    try {
      await adminService.saveInventoryItem({
        id: editing === 'new' ? undefined : editing?.id,
        name: String(data.get('name') ?? ''),
        sku: String(data.get('sku') ?? ''),
        unitOfMeasure: String(data.get('unitOfMeasure') ?? ''),
        minimumStock: Number(data.get('minimumStock') ?? 0),
        unitCost: editingUnitCost,
        isActive: data.get('isActive') === 'on',
      })
      await itemsQuery.refresh()
      setEditing(undefined)
      toast.success('Item de estoque salvo', 'O cadastro está disponível para receitas, custos e alertas.')
    } catch (error) {
      toast.error('Não foi possível salvar o item', getUserErrorMessage(error))
    } finally { setSaving(false) }
  }

  async function adjust(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!adjusting) return
    const data = new FormData(event.currentTarget)
    setSaving(true)
    try {
      await adminService.adjustInventory(adjusting.id, Number(data.get('quantityDelta')), String(data.get('reason') ?? ''))
      await itemsQuery.refresh()
      setAdjusting(undefined)
      toast.success('Estoque ajustado', 'O saldo e a movimentação foram registrados.')
    } catch (error) {
      toast.error('Não foi possível ajustar o estoque', getUserErrorMessage(error))
    } finally { setSaving(false) }
  }

  function editRecipe(recipe: InventoryRecipe) {
    setRecipeDraft({
      id: recipe.id,
      targetType: recipe.pizzaFlavorId ? 'flavor' : 'product',
      targetId: recipe.pizzaFlavorId ?? recipe.productId ?? '',
      pizzaSizeId: recipe.pizzaSizeId ?? '',
      yieldQuantity: recipe.yieldQuantity,
      items: recipe.items.map((item) => ({ inventoryItemId: item.inventoryItemId, quantity: item.quantity, unitOfMeasure: item.unitOfMeasure })),
    })
  }

  function openItemEditor(item: InventoryItem | 'new') {
    setEditingUnitCost(item === 'new' ? 0 : item.unitCost)
    setEditing(item)
  }

  async function saveRecipe(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!recipeDraft) return
    const command: SaveInventoryRecipe = {
      id: recipeDraft.id,
      productId: recipeDraft.targetType === 'product' ? recipeDraft.targetId : undefined,
      pizzaFlavorId: recipeDraft.targetType === 'flavor' ? recipeDraft.targetId : undefined,
      pizzaSizeId: recipeDraft.pizzaSizeId || undefined,
      yieldQuantity: recipeDraft.yieldQuantity,
      items: recipeDraft.items,
    }
    setSaving(true)
    try {
      await adminService.saveRecipe(command)
      await recipesQuery.refresh()
      setRecipeDraft(undefined)
      toast.success('Receita salva', 'A baixa automática será aplicada aos próximos pedidos enviados.')
    } catch (error) {
      toast.error('Não foi possível salvar a receita', getUserErrorMessage(error))
    } finally { setSaving(false) }
  }

  function updateRecipeItem(index: number, patch: Partial<RecipeDraft['items'][number]>) {
    setRecipeDraft((current) => current ? { ...current, items: current.items.map((item, itemIndex) => itemIndex === index ? { ...item, ...patch } : item) } : current)
  }

  return <>
    <PageHeader title="Estoque e receitas" description="Saldos, custos, fichas técnicas e consumo automático por pedido." actions={hasPermission('admin:write') && <><button className="secondary-button" onClick={() => setRecipeDraft(emptyRecipe())}><ChefHat size={16} /> Nova receita</button><button className="primary-button" onClick={() => openItemEditor('new')}><Plus size={16} /> Novo item</button></>} />
    <section className="surface-card">
      <div className="responsive-table inventory-table-scroll"><table className="inventory-table"><thead><tr><th>Item</th><th>SKU</th><th>Disponível</th><th>Mínimo</th><th>Custo unitário</th><th>Valor em estoque</th><th>Status</th><th aria-label="Ações" /></tr></thead><tbody>
        {itemsQuery.data.map((item) => <tr key={item.id}><td><span className="inventory-item-summary"><strong>{item.name}</strong><small>{item.currentQuantity.toLocaleString('pt-BR')} atual · {item.reservedQuantity.toLocaleString('pt-BR')} reservado</small></span></td><td>{item.sku}</td><td>{item.availableQuantity.toLocaleString('pt-BR')} {item.unitOfMeasure}</td><td>{item.minimumStock.toLocaleString('pt-BR')} {item.unitOfMeasure}</td><td>{formatCurrency(item.unitCost)}</td><td>{formatCurrency(item.availableQuantity * item.unitCost)}</td><td><span className={`status-pill ${item.isLowStock ? 'danger' : item.isActive ? 'success' : 'neutral'}`}>{item.isLowStock ? 'Estoque baixo' : item.isActive ? 'Normal' : 'Inativo'}</span></td><td><div className="table-actions inventory-actions">{hasPermission('admin:write') && <><button className="icon-button" aria-label={`Editar ${item.name}`} onClick={() => openItemEditor(item)}><Pencil size={15} /></button><button className="secondary-button" onClick={() => setAdjusting(item)}><SlidersHorizontal size={15} /> Ajustar</button></>}</div></td></tr>)}
      </tbody></table></div>
      {!itemsQuery.data.length && <div className="empty-state compact"><Boxes size={30} /><h2>Nenhum item cadastrado</h2><p>Cadastre os insumos para ativar alertas e consumo automático.</p></div>}
    </section>

    <section className="surface-card inventory-recipes-section">
      <header className="card-heading"><div><h2>Fichas técnicas</h2><p>Ingredientes consumidos quando um produto ou sabor é enviado à produção.</p></div></header>
      <div className="recipe-list">{recipesQuery.data.map((recipe) => <article key={recipe.id}><span><strong>{recipe.productName ?? recipe.pizzaFlavorName}</strong><small>{recipe.pizzaSizeName ? `${recipe.pizzaSizeName}, ` : ''}rendimento ${recipe.yieldQuantity.toLocaleString('pt-BR')}</small></span><p>{recipe.items.map((item) => `${item.quantity.toLocaleString('pt-BR')} ${item.unitOfMeasure} ${item.inventoryItemName}`).join(', ')}</p>{hasPermission('admin:write') && <button className="icon-button" aria-label={`Editar receita de ${recipe.productName ?? recipe.pizzaFlavorName}`} onClick={() => editRecipe(recipe)}><Pencil size={15} /></button>}</article>)}</div>
      {!recipesQuery.data.length && <div className="empty-state compact"><ChefHat size={30} /><h2>Nenhuma receita cadastrada</h2><p>Crie uma ficha técnica para começar a baixar insumos automaticamente.</p></div>}
    </section>

    {editing && <Modal open title={editing === 'new' ? 'Novo item de estoque' : 'Editar item de estoque'} description="O custo unitário alimenta os indicadores de CMV e margem." isBusy={saving} onClose={() => setEditing(undefined)}><form onSubmit={saveItem}><div className="modal-body"><div className="form-grid two-columns">
      <label className="field-label wide">Nome<input name="name" defaultValue={editing === 'new' ? '' : editing.name} required autoFocus /></label>
      <label className="field-label">SKU<input name="sku" defaultValue={editing === 'new' ? '' : editing.sku} required /></label>
      <label className="field-label">Unidade<input name="unitOfMeasure" defaultValue={editing === 'new' ? 'kg' : editing.unitOfMeasure} required /></label>
      <label className="field-label">Estoque mínimo<input name="minimumStock" type="number" min="0" step="0.001" defaultValue={editing === 'new' ? 0 : editing.minimumStock} required /></label>
      <label className="field-label">Custo unitário<CurrencyInput value={editingUnitCost} onCurrencyValueChange={setEditingUnitCost} required /></label>
      <label className="check-label"><input name="isActive" type="checkbox" defaultChecked={editing === 'new' || editing.isActive} /> Item ativo</label>
    </div></div><div className="modal-footer"><button type="button" className="secondary-button" onClick={() => setEditing(undefined)}>Cancelar</button><button className="primary-button" disabled={saving}><Save size={16} /> Salvar</button></div></form></Modal>}

    {adjusting && <Modal open title={`Ajustar ${adjusting.name}`} description={`Saldo disponível atual: ${adjusting.availableQuantity.toLocaleString('pt-BR')} ${adjusting.unitOfMeasure}. Use valor negativo para baixa.`} isBusy={saving} onClose={() => setAdjusting(undefined)}><form onSubmit={adjust}><div className="modal-body"><aside className="cash-opening-note"><AlertTriangle size={20} /><span><strong>Ajuste auditado</strong>O saldo nunca poderá ficar negativo.</span></aside><div className="form-grid"><label className="field-label">Quantidade (+ entrada / - baixa)<input name="quantityDelta" type="number" step="0.001" required autoFocus /></label><label className="field-label">Motivo<input name="reason" maxLength={300} required /></label></div></div><div className="modal-footer"><button type="button" className="secondary-button" onClick={() => setAdjusting(undefined)}>Cancelar</button><button className="primary-button" disabled={saving}><SlidersHorizontal size={16} /> Registrar ajuste</button></div></form></Modal>}

    {recipeDraft && <Modal open title={recipeDraft.id ? 'Editar ficha técnica' : 'Nova ficha técnica'} description="O consumo respeita o rendimento e a fração de cada sabor da pizza." size="large" isBusy={saving} onClose={() => setRecipeDraft(undefined)}><form onSubmit={saveRecipe}><div className="modal-body"><div className="form-grid three-columns"><label className="field-label">Tipo<select value={recipeDraft.targetType} onChange={(event) => setRecipeDraft({ ...recipeDraft, targetType: event.target.value as RecipeDraft['targetType'], targetId: '' })}><option value="product">Produto</option><option value="flavor">Sabor de pizza</option></select></label><label className="field-label">Destino<select value={recipeDraft.targetId} onChange={(event) => setRecipeDraft({ ...recipeDraft, targetId: event.target.value })} required><option value="">Selecione</option>{(recipeDraft.targetType === 'product' ? productsQuery.data : flavorsQuery.data).map((target) => <option value={target.id} key={target.id}>{target.name}</option>)}</select></label><label className="field-label">Tamanho da pizza<select value={recipeDraft.pizzaSizeId} disabled={recipeDraft.targetType !== 'flavor'} onChange={(event) => setRecipeDraft({ ...recipeDraft, pizzaSizeId: event.target.value })}><option value="">Todos os tamanhos</option>{sizesQuery.data.map((size) => <option value={size.id} key={size.id}>{size.name}</option>)}</select></label><label className="field-label">Rendimento<input type="number" min="0.0001" step="0.0001" value={recipeDraft.yieldQuantity} onChange={(event) => setRecipeDraft({ ...recipeDraft, yieldQuantity: Number(event.target.value) })} required /></label></div><section className="recipe-editor"><header><h3>Ingredientes</h3><button type="button" className="secondary-button" onClick={() => setRecipeDraft({ ...recipeDraft, items: [...recipeDraft.items, { inventoryItemId: '', quantity: 1, unitOfMeasure: 'kg' }] })}><Plus size={15} /> Adicionar insumo</button></header>{recipeDraft.items.map((item, index) => <div key={index}><label className="field-label">Insumo<select value={item.inventoryItemId} onChange={(event) => { const inventoryItem = itemsQuery.data.find((candidate) => candidate.id === event.target.value); updateRecipeItem(index, { inventoryItemId: event.target.value, unitOfMeasure: inventoryItem?.unitOfMeasure ?? item.unitOfMeasure }) }} required><option value="">Selecione</option>{itemsQuery.data.filter((candidate) => candidate.isActive).map((inventoryItem) => <option value={inventoryItem.id} key={inventoryItem.id}>{inventoryItem.name}</option>)}</select></label><label className="field-label">Quantidade<input type="number" min="0.0001" step="0.0001" value={item.quantity} onChange={(event) => updateRecipeItem(index, { quantity: Number(event.target.value) })} required /></label><label className="field-label">Unidade<input value={item.unitOfMeasure} onChange={(event) => updateRecipeItem(index, { unitOfMeasure: event.target.value })} required /></label><button type="button" className="icon-button danger-text" aria-label="Remover ingrediente" disabled={recipeDraft.items.length === 1} onClick={() => setRecipeDraft({ ...recipeDraft, items: recipeDraft.items.filter((_, itemIndex) => itemIndex !== index) })}><Trash2 size={16} /></button></div>)}</section></div><div className="modal-footer"><button type="button" className="secondary-button" onClick={() => setRecipeDraft(undefined)}>Cancelar</button><button className="primary-button" disabled={saving || !recipeDraft.targetId || recipeDraft.items.some((item) => !item.inventoryItemId)}><Save size={16} /> Salvar receita</button></div></form></Modal>}
  </>
}
