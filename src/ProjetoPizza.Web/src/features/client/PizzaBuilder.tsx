import {
  ArrowLeft,
  ArrowRight,
  Check,
  ChevronLeft,
  Minus,
  Pizza,
  Plus,
  RotateCcw,
  Sparkles,
  X,
} from 'lucide-react'
import { useEffect, useMemo, useRef, useState } from 'react'
import type {
  ClientPizzaCatalog,
  ClientPizzaCrust,
  ClientPizzaExtra,
  ClientPizzaFlavor,
  ClientPizzaSize,
  ClientProduct,
  PizzaCartConfiguration,
} from '../../types/client'
import { formatCurrency } from '../../utils/money'
import { getFlavorImage, getProductImage } from './clientPresentation'

export interface PizzaBuilderResult {
  productId: string
  name: string
  quantity: number
  unitPrice: number
  notes?: string
  imageUrl: string
  pizza: PizzaCartConfiguration
}

interface PizzaBuilderProps {
  product: ClientProduct
  catalog: ClientPizzaCatalog
  onCancel: () => void
  onAdd: (result: PizzaBuilderResult) => void
}

interface PizzaExtraSelection {
  ingredientId: string
  pizzaFlavorId?: string
  quantity: number
}

type CrustMode = 'whole' | 'split'

const stepLabels = ['Tamanho', 'Quantidade', 'Sabores', 'Personalizar', 'Borda', 'Revisão']

export function PizzaBuilder({ product, catalog, onCancel, onAdd }: PizzaBuilderProps) {
  const dialogRef = useRef<HTMLDivElement>(null)
  const [step, setStep] = useState(0)
  const [size, setSize] = useState<ClientPizzaSize>()
  const [flavorCount, setFlavorCount] = useState(1)
  const [flavorIds, setFlavorIds] = useState<string[]>([])
  const [removedIngredientIds, setRemovedIngredientIds] = useState<string[]>([])
  const [extraIngredients, setExtraIngredients] = useState<PizzaExtraSelection[]>([])
  const [customizingFlavorId, setCustomizingFlavorId] = useState<string>()
  const [crustMode, setCrustMode] = useState<CrustMode>('whole')
  const [crust, setCrust] = useState<ClientPizzaCrust>()
  const [secondCrust, setSecondCrust] = useState<ClientPizzaCrust>()
  const [quantity, setQuantity] = useState(1)
  const [notes, setNotes] = useState('')

  const selectedFlavors = catalog.flavors.filter((flavor) => flavorIds.includes(flavor.id))
  const pricing = useMemo(
    () => calculatePizzaPrice(product, catalog, size, selectedFlavors, crustMode, crust, secondCrust, extraIngredients),
    [catalog, crust, crustMode, extraIngredients, product, secondCrust, selectedFlavors, size],
  )
  const availableFlavorCount = size
    ? Math.min(size.maxFlavors, catalog.globalMaxFlavors)
    : catalog.globalMaxFlavors
  const customizingFlavor = selectedFlavors.find((flavor) => flavor.id === customizingFlavorId)
    ?? selectedFlavors[0]
  const availableExtras = resolveAvailableExtras(product, catalog, selectedFlavors, customizingFlavor)
  const removableIngredients = (customizingFlavor?.ingredients ?? [])
    .filter((ingredient, index, all) =>
      ingredient.isRemovable && all.findIndex((candidate) => candidate.id === ingredient.id) === index)

  useEffect(() => {
    const dialog = dialogRef.current
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    dialog?.querySelector<HTMLElement>('button')?.focus()

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        event.preventDefault()
        onCancel()
        return
      }

      if (event.key !== 'Tab' || !dialog) return
      const focusable = Array.from(dialog.querySelectorAll<HTMLElement>(
        'button:not([disabled]), input:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])',
      ))
      if (focusable.length === 0) return
      const first = focusable[0]
      const last = focusable[focusable.length - 1]
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault()
        last.focus()
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault()
        first.focus()
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => {
      document.removeEventListener('keydown', handleKeyDown)
      document.body.style.overflow = previousOverflow
    }
  }, [onCancel])

  function goBack() {
    if (step === 0) onCancel()
    else setStep((current) => current - 1)
  }

  function canContinue() {
    if (step === 0) return Boolean(size)
    if (step === 1) return flavorCount >= 1 && flavorCount <= availableFlavorCount
    if (step === 2) return flavorIds.length === flavorCount
    if (step === 4) return Boolean(crust && (crustMode === 'whole' || secondCrust))
    return true
  }

  function continueFlow() {
    if (!canContinue()) return
    if (step < stepLabels.length - 1) {
      setStep((current) => current + 1)
      return
    }

    if (!size || !crust || (crustMode === 'split' && !secondCrust) || selectedFlavors.length !== flavorCount) return
    onAdd({
      productId: product.id,
      name: `Pizza ${size.name} · ${flavorCount} ${flavorCount === 1 ? 'sabor' : 'sabores'}`,
      quantity,
      unitPrice: pricing.total,
      notes: notes.trim() || undefined,
      imageUrl: getProductImage(product),
      pizza: {
        sizeId: size.id,
        sizeName: size.name,
        flavorIds,
        flavorNames: selectedFlavors.map((flavor) => flavor.name),
        crustId: crust.id,
        crustName: crust.name,
        secondCrustId: crustMode === 'split' ? secondCrust?.id : undefined,
        secondCrustName: crustMode === 'split' ? secondCrust?.name : undefined,
        removedIngredientIds,
        extraIngredients: extraIngredients.map((extra) => {
          const ingredient = resolveExtraConfiguration(product, catalog, selectedFlavors, extra)
          const flavor = selectedFlavors.find((candidate) => candidate.id === extra.pizzaFlavorId)
          return {
            ...extra,
            ingredientName: ingredient?.name ?? 'Ingrediente adicional',
            pizzaFlavorName: flavor?.name,
            unitPrice: ingredient?.price ?? 0,
          }
        }),
      },
    })
  }

  function selectSize(nextSize: ClientPizzaSize) {
    setSize(nextSize)
    const nextMax = Math.min(nextSize.maxFlavors, catalog.globalMaxFlavors)
    if (flavorCount > nextMax) setFlavorCount(nextMax)
    setFlavorIds([])
    setRemovedIngredientIds([])
    setExtraIngredients([])
    setCustomizingFlavorId(undefined)
  }

  function toggleFlavor(flavor: ClientPizzaFlavor) {
    if (!flavor.isAvailable) return
    if (flavorIds.includes(flavor.id)) {
      setFlavorIds((current) => current.filter((id) => id !== flavor.id))
      setRemovedIngredientIds([])
      setExtraIngredients([])
      if (customizingFlavorId === flavor.id) setCustomizingFlavorId(undefined)
      return
    }
    if (flavorIds.length >= flavorCount) return
    if (!catalog.allowSweetAndSavoryMix && selectedFlavors.some((selected) => selected.flavorType !== flavor.flavorType)) return
    setExtraIngredients([])
    setFlavorIds((current) => [...current, flavor.id])
  }

  function changeExtraQuantity(extra: ClientPizzaExtra, delta: number) {
    const pizzaFlavorId = catalog.allowExtrasPerFlavor ? customizingFlavor?.id : undefined
    if (catalog.allowExtrasPerFlavor && !pizzaFlavorId) return
    setExtraIngredients((current) => {
      const index = current.findIndex((selection) =>
        selection.ingredientId === extra.id && selection.pizzaFlavorId === pizzaFlavorId)
      const currentQuantity = index >= 0 ? current[index].quantity : 0
      const nextQuantity = Math.max(0, Math.min(extra.maxQuantity, currentQuantity + delta))
      if (nextQuantity === 0) {
        return current.filter((_, selectionIndex) => selectionIndex !== index)
      }
      const selection = { ingredientId: extra.id, pizzaFlavorId, quantity: nextQuantity }
      if (index < 0) return [...current, selection]
      return current.map((item, selectionIndex) => selectionIndex === index ? selection : item)
    })
  }

  function getExtraQuantity(extraId: string) {
    const pizzaFlavorId = catalog.allowExtrasPerFlavor ? customizingFlavor?.id : undefined
    return extraIngredients.find((selection) =>
      selection.ingredientId === extraId && selection.pizzaFlavorId === pizzaFlavorId)?.quantity ?? 0
  }

  return (
    <div ref={dialogRef} className="pizza-builder" role="dialog" aria-modal="true" aria-labelledby="pizza-builder-title">
      <header className="pizza-builder-header">
        <button type="button" onClick={goBack} aria-label="Voltar"><ArrowLeft aria-hidden="true" /></button>
        <div>
          <Pizza aria-hidden="true" />
          <h1 id="pizza-builder-title">Monte sua pizza</h1>
        </div>
        <button type="button" onClick={onCancel} aria-label="Fechar montador"><X aria-hidden="true" /></button>
      </header>

      <ol className="pizza-builder-progress" aria-label="Etapas da montagem">
        {stepLabels.map((label, index) => (
          <li key={label} className={index === step ? 'active' : index < step ? 'complete' : ''} aria-current={index === step ? 'step' : undefined}>
            <span>{index < step ? <Check aria-hidden="true" /> : index + 1}</span>
            <small>{label}</small>
          </li>
        ))}
      </ol>

      <main className="pizza-builder-content">
        {step === 0 && (
          <BuilderStage title="Escolha o tamanho" subtitle="O tamanho define a quantidade de fatias e o limite de sabores.">
            <div className="pizza-option-grid size-options" role="radiogroup" aria-label="Tamanho da pizza">
              {catalog.sizes.map((option) => (
                <button
                  type="button"
                  role="radio"
                  aria-checked={size?.id === option.id}
                  className={size?.id === option.id ? 'selected' : ''}
                  key={option.id}
                  onClick={() => selectSize(option)}
                >
                  <span className="pizza-size-visual"><Pizza aria-hidden="true" /></span>
                  <strong>{option.name}</strong>
                  <small>{option.slices} fatias · {option.diameterCm} cm</small>
                  <span>Até {Math.min(option.maxFlavors, catalog.globalMaxFlavors)} sabores</span>
                  <b>{formatCurrency(option.basePrice)}</b>
                </button>
              ))}
            </div>
          </BuilderStage>
        )}

        {step === 1 && (
          <BuilderStage title="Quantos sabores?" subtitle={`A pizza ${size?.name} aceita até ${availableFlavorCount} sabores.`}>
            <div className="pizza-option-grid flavor-count-options" role="radiogroup" aria-label="Quantidade de sabores">
              {Array.from({ length: availableFlavorCount }, (_, index) => index + 1).map((count) => (
                <button
                  type="button"
                  role="radio"
                  aria-checked={flavorCount === count}
                  className={flavorCount === count ? 'selected' : ''}
                  key={count}
                  onClick={() => {
                    setFlavorCount(count)
                    setFlavorIds([])
                    setRemovedIngredientIds([])
                    setExtraIngredients([])
                    setCustomizingFlavorId(undefined)
                  }}
                >
                  <span className={`pizza-parts parts-${count}`}>
                    {Array.from({ length: count }, (_, part) => <i key={part} />)}
                  </span>
                  <strong>{count} {count === 1 ? 'sabor' : 'sabores'}</strong>
                  <small>{count === 1 ? 'Uma pizza inteira do seu sabor favorito' : `${count} partes iguais para compartilhar`}</small>
                </button>
              ))}
            </div>
          </BuilderStage>
        )}

        {step === 2 && (
          <BuilderStage
            title={`Escolha ${flavorCount === 1 ? 'o sabor' : `os ${flavorCount} sabores`}`}
            subtitle={`${flavorIds.length} de ${flavorCount} selecionado${flavorIds.length === 1 ? '' : 's'}.`}
          >
            <div className="pizza-flavor-layout">
              <aside className="pizza-selection-summary">
                <strong>Pizza {size?.name}</strong>
                <span className="pizza-diagram" aria-hidden="true">
                  {Array.from({ length: flavorCount }, (_, part) => (
                    <i key={part} className={part < flavorIds.length ? 'filled' : ''} />
                  ))}
                </span>
                {Array.from({ length: flavorCount }, (_, index) => (
                  <div key={index} className={flavorIds[index] ? 'filled' : ''}>
                    <span>{index + 1}</span>
                    {selectedFlavors[index]?.name ?? 'Aguardando escolha'}
                    {flavorIds[index] && (
                      <button type="button" onClick={() => toggleFlavor(selectedFlavors[index])} aria-label={`Remover ${selectedFlavors[index].name}`}>
                        <X aria-hidden="true" />
                      </button>
                    )}
                  </div>
                ))}
              </aside>
              <div className="pizza-flavor-grid">
                {catalog.flavors.map((flavor) => {
                  const flavorPrice = flavor.prices.find((price) => price.pizzaSizeId === size?.id)
                  const mixedBlocked = !catalog.allowSweetAndSavoryMix &&
                    selectedFlavors.some((selected) => selected.flavorType !== flavor.flavorType)
                  const disabled = !flavor.isAvailable || !flavorPrice?.isAvailable || mixedBlocked
                  const selected = flavorIds.includes(flavor.id)
                  return (
                    <button
                      type="button"
                      className={`pizza-flavor-card ${selected ? 'selected' : ''}`}
                      key={flavor.id}
                      onClick={() => toggleFlavor(flavor)}
                      disabled={disabled}
                      aria-pressed={selected}
                    >
                      <span className="pizza-flavor-image">
                        <img src={getFlavorImage(flavor)} alt="" />
                        {flavor.isPremium && <b><Sparkles aria-hidden="true" /> Premium</b>}
                        {!flavor.isAvailable && <em>{flavor.soldOutReason || 'Esgotado temporariamente'}</em>}
                      </span>
                      <span>
                        <strong>{flavor.name}</strong>
                        <small>{flavor.description || 'Sabor preparado com ingredientes selecionados.'}</small>
                        {flavor.isVegetarian && <i>Vegetariana</i>}
                      </span>
                      <span className="pizza-flavor-add">{selected ? <Check aria-hidden="true" /> : <Plus aria-hidden="true" />}</span>
                    </button>
                  )
                })}
              </div>
            </div>
          </BuilderStage>
        )}

        {step === 3 && (
          <BuilderStage title="Personalize os sabores" subtitle="Retire ingredientes ou acrescente adicionais. O valor é atualizado automaticamente.">
            {catalog.allowExtrasPerFlavor && selectedFlavors.length > 1 && (
              <div className="pizza-customize-tabs" role="tablist" aria-label="Sabor que será personalizado">
                {selectedFlavors.map((flavor) => (
                  <button
                    type="button"
                    role="tab"
                    aria-selected={customizingFlavor?.id === flavor.id}
                    className={customizingFlavor?.id === flavor.id ? 'active' : ''}
                    key={flavor.id}
                    onClick={() => setCustomizingFlavorId(flavor.id)}
                  >
                    {flavor.name}
                  </button>
                ))}
              </div>
            )}
            <div className="pizza-customize-layout">
              <div className="pizza-customize-photo">
                <img src={getFlavorImage(customizingFlavor)} alt="" />
                <div>
                  <strong>{catalog.allowExtrasPerFlavor ? customizingFlavor?.name : selectedFlavors.map((flavor) => flavor.name).join(' · ')}</strong>
                  <small>{catalog.allowExtrasPerFlavor ? 'Personalização aplicada somente a este sabor' : 'Personalização aplicada à pizza inteira'}</small>
                </div>
              </div>
              <div className="pizza-ingredient-list">
                <section className="pizza-customize-section">
                  <header><strong>Ingredientes da receita</strong><small>Desative o que não deseja.</small></header>
                  {removableIngredients.length === 0 ? (
                    <p>Nenhum ingrediente removível disponível para este sabor.</p>
                  ) : removableIngredients.map((ingredient) => {
                    const removed = removedIngredientIds.includes(ingredient.id)
                    return (
                      <label key={ingredient.id}>
                        <span>
                          <strong>{ingredient.name}</strong>
                          <small>{ingredient.isAllergen ? ingredient.allergenDescription || 'Contém alérgeno' : 'Ingrediente padrão'}</small>
                        </span>
                        <input
                          type="checkbox"
                          checked={!removed}
                          onChange={() => setRemovedIngredientIds((current) =>
                            removed ? current.filter((id) => id !== ingredient.id) : [...current, ingredient.id])}
                        />
                        <span>{removed ? 'Removido' : 'Incluir'}</span>
                      </label>
                    )
                  })}
                </section>
                <section className="pizza-customize-section pizza-extras-section">
                  <header><strong>Ingredientes adicionais</strong><small>Escolha até o limite indicado.</small></header>
                  {availableExtras.length === 0 ? <p>Nenhum adicional disponível no momento.</p> : availableExtras.map((extra) => {
                    const extraQuantity = getExtraQuantity(extra.id)
                    return (
                      <article className={extraQuantity > 0 ? 'pizza-extra-row selected' : 'pizza-extra-row'} key={extra.id}>
                        <span>
                          <strong>{extra.name}</strong>
                          <small>{extra.description}{extra.isAllergen ? ` · ${extra.allergenDescription || 'Contém alérgeno'}` : ''}</small>
                          <b>+ {formatCurrency(extra.price)} por porção</b>
                        </span>
                        <div className="pizza-extra-quantity" aria-label={`Quantidade de ${extra.name}`}>
                          <button type="button" onClick={() => changeExtraQuantity(extra, -1)} disabled={extraQuantity === 0} aria-label={`Diminuir ${extra.name}`}>
                            <Minus aria-hidden="true" />
                          </button>
                          <strong>{extraQuantity}</strong>
                          <button type="button" onClick={() => changeExtraQuantity(extra, 1)} disabled={extraQuantity >= extra.maxQuantity} aria-label={`Adicionar ${extra.name}`}>
                            <Plus aria-hidden="true" />
                          </button>
                        </div>
                      </article>
                    )
                  })}
                </section>
              </div>
            </div>
          </BuilderStage>
        )}

        {step === 4 && (
          <BuilderStage title="Escolha a borda" subtitle="Use uma borda inteira ou combine dois recheios, um em cada metade.">
            <div className="crust-mode-selector" role="tablist" aria-label="Formato da borda">
              <button
                type="button"
                role="tab"
                aria-selected={crustMode === 'whole'}
                className={crustMode === 'whole' ? 'selected' : ''}
                onClick={() => {
                  setCrustMode('whole')
                  setSecondCrust(undefined)
                }}
              >
                <strong>Borda inteira</strong>
                <small>Um recheio em todas as fatias</small>
              </button>
              <button
                type="button"
                role="tab"
                aria-selected={crustMode === 'split'}
                className={crustMode === 'split' ? 'selected' : ''}
                onClick={() => setCrustMode('split')}
              >
                <strong>Duas metades</strong>
                <small>Combine dois recheios diferentes</small>
              </button>
            </div>

            {crustMode === 'whole' ? (
              <div className="pizza-option-grid crust-options" role="radiogroup" aria-label="Borda inteira da pizza">
                {catalog.crusts.map((option) => {
                  const price = option.prices.find((candidate) => candidate.pizzaSizeId === size?.id)?.fullPrice
                  const unavailable = !option.isAvailable || price === undefined
                  return (
                    <CrustOption
                      key={option.id}
                      option={option}
                      price={price}
                      selected={crust?.id === option.id}
                      disabled={unavailable}
                      priceLabel="Borda inteira"
                      onSelect={() => setCrust(option)}
                    />
                  )
                })}
              </div>
            ) : (
              <div className="split-crust-layout">
                <CrustHalfSelector
                  title="Primeira metade"
                  detail={`${Math.ceil((size?.slices ?? 0) / 2)} fatias`}
                  options={catalog.crusts}
                  sizeId={size?.id}
                  selected={crust}
                  unavailableId={secondCrust?.id}
                  onSelect={(option) => {
                    setCrust(option)
                    if (secondCrust?.id === option.id) setSecondCrust(undefined)
                  }}
                />
                <CrustHalfSelector
                  title="Segunda metade"
                  detail={`${Math.floor((size?.slices ?? 0) / 2)} fatias`}
                  options={catalog.crusts}
                  sizeId={size?.id}
                  selected={secondCrust}
                  unavailableId={crust?.id}
                  onSelect={setSecondCrust}
                />
              </div>
            )}
          </BuilderStage>
        )}

        {step === 5 && size && crust && (
          <BuilderStage title="Revise sua pizza" subtitle="Confira os detalhes antes de adicionar ao carrinho.">
            <div className="pizza-review-layout">
              <div className="pizza-review-photo">
                <img src={getFlavorImage(selectedFlavors[0])} alt="" />
                <span>{flavorCount} {flavorCount === 1 ? 'sabor' : 'sabores'}</span>
              </div>
              <div className="pizza-review-card">
                <h2>Pizza {size.name}</h2>
                <ReviewLine label="Tamanho" value={`${size.name} · ${size.slices} fatias`} onEdit={() => setStep(0)} />
                <ReviewLine label="Sabores" value={selectedFlavors.map((flavor) => flavor.name).join(' · ')} onEdit={() => setStep(2)} />
                <ReviewLine
                  label="Borda"
                  value={crustMode === 'split'
                    ? `½ ${crust.name} + ½ ${secondCrust?.name}`
                    : crust.name}
                  onEdit={() => setStep(4)}
                />
                <ReviewLine
                  label="Personalização"
                  value={[
                    removedIngredientIds.length ? `${removedIngredientIds.length} removido(s)` : '',
                    extraIngredients.length ? `${extraIngredients.reduce((sum, extra) => sum + extra.quantity, 0)} adicional(is)` : '',
                  ].filter(Boolean).join(' · ') || 'Receita original'}
                  onEdit={() => setStep(3)}
                />
                <label className="pizza-notes">
                  <span>Observações para a cozinha</span>
                  <textarea
                    value={notes}
                    onChange={(event) => setNotes(event.target.value)}
                    maxLength={1000}
                    placeholder="Ex.: assar bem, cortar em mais pedaços..."
                  />
                </label>
                <div className="pizza-quantity">
                  <span>Quantidade</span>
                  <div>
                    <button type="button" onClick={() => setQuantity((current) => Math.max(1, current - 1))} aria-label="Diminuir quantidade">
                      <Minus aria-hidden="true" />
                    </button>
                    <strong>{quantity}</strong>
                    <button type="button" onClick={() => setQuantity((current) => Math.min(20, current + 1))} aria-label="Aumentar quantidade">
                      <Plus aria-hidden="true" />
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </BuilderStage>
        )}
      </main>

      <footer className="pizza-builder-footer">
        <div>
          <small>{step === 5 ? `Total para ${quantity} ${quantity === 1 ? 'pizza' : 'pizzas'}` : 'Subtotal da pizza'}</small>
          <strong>{formatCurrency(pricing.total * quantity)}</strong>
        </div>
        <button type="button" className="client-secondary-action" onClick={goBack}>
          <ChevronLeft aria-hidden="true" /> Voltar
        </button>
        <button type="button" className="client-primary-action" onClick={continueFlow} disabled={!canContinue()}>
          {step === stepLabels.length - 1 ? <><Plus aria-hidden="true" /> Adicionar ao carrinho</> : <>Continuar <ArrowRight aria-hidden="true" /></>}
        </button>
      </footer>
    </div>
  )
}

function CrustHalfSelector({
  title,
  detail,
  options,
  sizeId,
  selected,
  unavailableId,
  onSelect,
}: {
  title: string
  detail: string
  options: ClientPizzaCrust[]
  sizeId?: string
  selected?: ClientPizzaCrust
  unavailableId?: string
  onSelect: (option: ClientPizzaCrust) => void
}) {
  return (
    <section className="crust-half-panel">
      <header>
        <span><strong>{title}</strong><small>{detail}</small></span>
        {selected && <b><Check aria-hidden="true" /> {selected.name}</b>}
      </header>
      <div className="crust-half-options" role="radiogroup" aria-label={title}>
        {options.map((option) => {
          const price = option.prices.find((candidate) => candidate.pizzaSizeId === sizeId)?.halfPrice
          const unavailable = !option.isAvailable || price === undefined || option.id === unavailableId
          return (
            <CrustOption
              key={option.id}
              option={option}
              price={price}
              selected={selected?.id === option.id}
              disabled={unavailable}
              priceLabel="Meia borda"
              compact
              onSelect={() => onSelect(option)}
            />
          )
        })}
      </div>
    </section>
  )
}

function CrustOption({
  option,
  price,
  selected,
  disabled,
  priceLabel,
  compact = false,
  onSelect,
}: {
  option: ClientPizzaCrust
  price?: number
  selected: boolean
  disabled: boolean
  priceLabel: string
  compact?: boolean
  onSelect: () => void
}) {
  return (
    <button
      type="button"
      role="radio"
      aria-checked={selected}
      className={`${selected ? 'selected' : ''}${compact ? ' compact' : ''}`}
      disabled={disabled}
      onClick={onSelect}
    >
      <span className="crust-visual"><Pizza aria-hidden="true" /></span>
      <span className="crust-option-copy">
        <strong>{option.name}</strong>
        {!compact && <small>{option.description || 'Borda artesanal Forno 27'}</small>}
        <small className="crust-price-kind">{priceLabel}</small>
        <b>{price ? `+ ${formatCurrency(price)}` : 'Sem acréscimo'}</b>
      </span>
    </button>
  )
}

function BuilderStage({ title, subtitle, children }: { title: string; subtitle: string; children: React.ReactNode }) {
  return (
    <section className="pizza-builder-stage">
      <header>
        <h2>{title}</h2>
        <p>{subtitle}</p>
      </header>
      {children}
    </section>
  )
}

function ReviewLine({ label, value, onEdit }: { label: string; value: string; onEdit: () => void }) {
  return (
    <div className="pizza-review-line">
      <span><small>{label}</small><strong>{value}</strong></span>
      <button type="button" onClick={onEdit}><RotateCcw aria-hidden="true" /> Editar</button>
    </div>
  )
}

function calculatePizzaPrice(
  product: ClientProduct,
  catalog: ClientPizzaCatalog,
  size: ClientPizzaSize | undefined,
  flavors: ClientPizzaFlavor[],
  crustMode: CrustMode,
  crust: ClientPizzaCrust | undefined,
  secondCrust: ClientPizzaCrust | undefined,
  extraIngredients: PizzaExtraSelection[],
) {
  if (!size) return { base: 0, crust: 0, extras: 0, total: 0 }
  const prices = flavors
    .map((flavor) => flavor.prices.find((price) => price.pizzaSizeId === size.id)?.price)
    .filter((price): price is number => price !== undefined)
  let base = size.basePrice
  if (prices.length > 0) {
    base = catalog.pricingPolicy === 'HighestFlavorPrice'
      ? Math.max(...prices)
      : prices.reduce((sum, price) => sum + price, 0) / prices.length
  }
  const firstCrustPrice = crust?.prices.find((price) => price.pizzaSizeId === size.id)
  const secondCrustPrice = secondCrust?.prices.find((price) => price.pizzaSizeId === size.id)
  const crustPrice = crustMode === 'split'
    ? (firstCrustPrice?.halfPrice ?? 0) + (secondCrustPrice?.halfPrice ?? 0)
    : firstCrustPrice?.fullPrice ?? 0
  const extrasPrice = extraIngredients.reduce((total, selection) => {
    const extra = resolveExtraConfiguration(product, catalog, flavors, selection)
    return total + (extra?.price ?? 0) * selection.quantity
  }, 0)
  return {
    base,
    crust: crustPrice,
    extras: extrasPrice,
    total: Math.round((base + crustPrice + extrasPrice) * 100) / 100,
  }
}

function resolveAvailableExtras(
  product: ClientProduct,
  catalog: ClientPizzaCatalog,
  flavors: ClientPizzaFlavor[],
  activeFlavor?: ClientPizzaFlavor,
): ClientPizzaExtra[] {
  if (product.usesCustomExtras) {
    return product.complements ?? []
  }

  if (catalog.allowExtrasPerFlavor) {
    return activeFlavor?.extras ?? []
  }

  if (flavors.length === 0) return []
  const firstFlavorExtras = flavors[0].extras ?? []
  return firstFlavorExtras
    .filter((extra) => flavors.every((flavor) =>
      (flavor.extras ?? []).some((candidate) => candidate.id === extra.id)))
    .map((extra) => {
      const configurations = flavors.map((flavor) =>
        (flavor.extras ?? []).find((candidate) => candidate.id === extra.id)!)
      return {
        ...extra,
        price: Math.max(...configurations.map((configuration) => configuration.price)),
        maxQuantity: Math.min(...configurations.map((configuration) => configuration.maxQuantity)),
      }
    })
}

function resolveExtraConfiguration(
  product: ClientProduct,
  catalog: ClientPizzaCatalog,
  flavors: ClientPizzaFlavor[],
  selection: { ingredientId: string; pizzaFlavorId?: string },
) {
  if (product.usesCustomExtras) {
    return product.complements.find((extra) => extra.id === selection.ingredientId)
  }

  if (selection.pizzaFlavorId) {
    return flavors
      .find((flavor) => flavor.id === selection.pizzaFlavorId)
      ?.extras.find((extra) => extra.id === selection.ingredientId)
  }

  return resolveAvailableExtras(product, catalog, flavors, flavors[0])
    .find((extra) => extra.id === selection.ingredientId)
}
