import { Award, CalendarClock, Gift, Plus, Settings2, Sparkles, Ticket, UsersRound } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { Modal } from '../components/ui/Modal'
import { PageHeader } from '../components/ui/PageHeader'
import { useToast } from '../components/ui/toast'
import { useAdminQuery } from '../hooks/useAdminQuery'
import { queryKeys } from '../lib/queryKeys'
import { adminService } from '../services/adminService'
import { hasPermission } from '../services/authSession'
import type { LoyaltySettings, PromotionCoupon } from '../types/admin'
import { getUserErrorMessage } from '../utils/errors'

const currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })
const transactionLabels = { OpeningBalance: 'Saldo inicial', Earned: 'Crédito', Redeemed: 'Resgate', Restored: 'Estorno', Expired: 'Expiração', ManualAdjustment: 'Ajuste manual' } as const

function emptyCoupon(): Omit<PromotionCoupon, 'timesRedeemed'> {
  const now = new Date()
  const end = new Date(now); end.setMonth(end.getMonth() + 1)
  return { id: '', code: '', name: '', discountType: 'Percentage', value: 10, minimumOrderAmount: 0,
    startsAt: toInputDate(now), endsAt: toInputDate(end), isActive: true }
}

export function LoyaltyPage() {
  const { data, setData } = useAdminQuery(queryKeys.loyalty, adminService.loyalty)
  const [settingsOpen, setSettingsOpen] = useState(false)
  const [couponOpen, setCouponOpen] = useState(false)
  const [settings, setSettings] = useState<LoyaltySettings>(data.settings)
  const [coupon, setCoupon] = useState(emptyCoupon)
  const [saving, setSaving] = useState(false)
  const toast = useToast()

  function editCoupon(current?: PromotionCoupon) {
    setCoupon(current ? { ...current, startsAt: toInputDate(new Date(current.startsAt)), endsAt: toInputDate(new Date(current.endsAt)) } : emptyCoupon())
    setCouponOpen(true)
  }

  async function saveSettings(event: FormEvent) {
    event.preventDefault(); setSaving(true)
    try {
      await adminService.saveLoyaltySettings(settings)
      setData((current) => ({ ...current, settings }))
      setSettingsOpen(false); toast.success('Programa atualizado', 'As novas regras valem para os próximos pedidos.')
    } catch (error) { toast.error('Não foi possível salvar', getUserErrorMessage(error)) } finally { setSaving(false) }
  }

  async function saveCoupon(event: FormEvent) {
    event.preventDefault(); setSaving(true)
    try {
      const payload = { ...coupon, id: coupon.id || undefined, code: coupon.code.trim().toUpperCase(), startsAt: new Date(coupon.startsAt).toISOString(), endsAt: new Date(coupon.endsAt).toISOString() }
      const result = await adminService.savePromotionCoupon(payload)
      const id = coupon.id || (result as { id: string }).id
      setData((current) => ({ ...current, coupons: coupon.id
        ? current.coupons.map((item) => item.id === coupon.id ? { ...item, ...payload, id } : item)
        : [...current.coupons, { ...payload, id, timesRedeemed: 0 }] }))
      setCouponOpen(false); toast.success(coupon.id ? 'Cupom atualizado' : 'Cupom criado', `${payload.code} está pronto para uso.`)
    } catch (error) { toast.error('Não foi possível salvar o cupom', getUserErrorMessage(error)) } finally { setSaving(false) }
  }

  return <>
    <PageHeader title="Fidelidade" description="Transforme pedidos concluídos em retorno, com regras transparentes e benefícios rastreáveis."
      actions={hasPermission('admin:write') && <div className="page-actions"><button className="secondary-button" onClick={() => { setSettings(data.settings); setSettingsOpen(true) }}><Settings2 size={16} /> Regras</button><button className="primary-button" onClick={() => editCoupon()}><Plus size={16} /> Novo cupom</button></div>} />

    <section className="loyalty-hero" aria-label="Resumo do programa">
      <div className="loyalty-ticket">
        <span className="loyalty-ticket-icon"><Award size={24} /></span>
        <div><small>PONTOS EM CIRCULAÇÃO</small><strong>{data.pointsInCirculation.toLocaleString('pt-BR')}</strong><p>{data.settings.isEnabled ? `${data.settings.pointsPerCurrencyUnit} ponto por real · validade de ${data.settings.pointsValidityDays} dias` : 'Programa pausado'}</p></div>
        <span className={`loyalty-state ${data.settings.isEnabled ? 'active' : ''}`}>{data.settings.isEnabled ? 'Ativo' : 'Pausado'}</span>
      </div>
      <div className="loyalty-side-metrics"><article><UsersRound /><span><small>CLIENTES ATIVOS</small><strong>{data.activeCustomers}</strong></span></article><article><Gift /><span><small>BENEFÍCIOS CONCEDIDOS</small><strong>{currency.format(data.grantedDiscount)}</strong></span></article></div>
    </section>

    <div className="loyalty-grid">
      <section className="surface-card loyalty-coupons"><header className="card-heading"><div><h2>Cupons promocionais</h2><p>Campanhas controladas por período, valor mínimo e limite de uso.</p></div><Ticket size={19} /></header>
        <div className="coupon-stack">{data.coupons.map((item) => <button type="button" className="coupon-row" key={item.id} onClick={() => editCoupon(item)}>
          <span className="coupon-code">{item.code}</span><span className="coupon-copy"><strong>{item.name}</strong><small>{item.discountType === 'Percentage' ? `${item.value}% de desconto` : currency.format(item.value)} · mínimo {currency.format(item.minimumOrderAmount)}</small></span>
          <span className="coupon-usage"><strong>{item.timesRedeemed}{item.usageLimit ? `/${item.usageLimit}` : ''}</strong><small>usos</small></span><span className={`loyalty-state ${item.isActive ? 'active' : ''}`}>{item.isActive ? 'Ativo' : 'Inativo'}</span>
        </button>)}{!data.coupons.length && <div className="loyalty-empty"><Ticket size={26} /><strong>Nenhum cupom criado</strong><p>Crie uma campanha curta e mensurável para começar.</p></div>}</div>
      </section>
      <section className="surface-card loyalty-ledger"><header className="card-heading"><div><h2>Razão de pontos</h2><p>Histórico imutável dos últimos movimentos.</p></div><Sparkles size={19} /></header>
        <div className="ledger-list">{data.transactions.map((item) => <article key={item.id}><span className={`ledger-mark ${item.points > 0 ? 'positive' : ''}`} /><div><strong>{item.customerName}</strong><small>{transactionLabels[item.type]} · {new Date(item.occurredAt).toLocaleString('pt-BR')}</small></div><span className="ledger-points"><strong>{item.points > 0 ? '+' : ''}{item.points}</strong><small>saldo {item.balanceAfter}</small></span></article>)}{!data.transactions.length && <div className="loyalty-empty"><CalendarClock size={26} /><strong>Ainda não há movimentos</strong><p>Créditos e resgates aparecerão aqui.</p></div>}</div>
      </section>
    </div>

    {settingsOpen && <Modal open title="Regras do programa" description="Mudanças afetam apenas pedidos futuros." isBusy={saving} onClose={() => setSettingsOpen(false)}><form onSubmit={saveSettings}><div className="modal-body"><div className="form-grid two-columns">
      <label className="switch-field wide"><input type="checkbox" checked={settings.isEnabled} onChange={(e) => setSettings({ ...settings, isEnabled: e.target.checked })} /><span /><strong>Programa ativo</strong></label>
      <NumberField label="Pontos por R$ 1" value={settings.pointsPerCurrencyUnit} step="0.1" onChange={(value) => setSettings({ ...settings, pointsPerCurrencyUnit: value })} />
      <NumberField label="Valor de cada ponto (R$)" value={settings.redemptionValuePerPoint} step="0.01" onChange={(value) => setSettings({ ...settings, redemptionValuePerPoint: value })} />
      <NumberField label="Mínimo para resgate" value={settings.minimumRedemptionPoints} onChange={(value) => setSettings({ ...settings, minimumRedemptionPoints: value })} />
      <NumberField label="Máximo do pedido (%)" value={settings.maximumRedemptionPercentage} onChange={(value) => setSettings({ ...settings, maximumRedemptionPercentage: value })} />
      <NumberField label="Validade dos pontos (dias)" value={settings.pointsValidityDays} onChange={(value) => setSettings({ ...settings, pointsValidityDays: value })} />
    </div></div><div className="modal-footer"><button type="button" className="secondary-button" onClick={() => setSettingsOpen(false)}>Cancelar</button><button className="primary-button" disabled={saving}>{saving ? 'Salvando…' : 'Salvar regras'}</button></div></form></Modal>}

    {couponOpen && <Modal open title={coupon.id ? 'Editar cupom' : 'Novo cupom'} description="O servidor confirma todas as regras antes de aplicar o desconto." isBusy={saving} onClose={() => setCouponOpen(false)}><form onSubmit={saveCoupon}><div className="modal-body"><div className="form-grid two-columns">
      <label className="field-label">Código<input required maxLength={40} value={coupon.code} onChange={(e) => setCoupon({ ...coupon, code: e.target.value.toUpperCase() })} placeholder="VOLTE10" /></label>
      <label className="field-label">Nome da campanha<input required maxLength={120} value={coupon.name} onChange={(e) => setCoupon({ ...coupon, name: e.target.value })} /></label>
      <label className="field-label">Tipo<select value={coupon.discountType} onChange={(e) => setCoupon({ ...coupon, discountType: e.target.value as PromotionCoupon['discountType'] })}><option value="Percentage">Percentual</option><option value="FixedAmount">Valor fixo</option></select></label>
      <NumberField label={coupon.discountType === 'Percentage' ? 'Desconto (%)' : 'Desconto (R$)'} value={coupon.value} step="0.01" onChange={(value) => setCoupon({ ...coupon, value })} />
      <NumberField label="Pedido mínimo (R$)" value={coupon.minimumOrderAmount} step="0.01" onChange={(value) => setCoupon({ ...coupon, minimumOrderAmount: value })} />
      <NumberField label="Limite de usos (opcional)" value={coupon.usageLimit ?? 0} onChange={(value) => setCoupon({ ...coupon, usageLimit: value || undefined })} />
      <label className="field-label">Início<input required type="datetime-local" value={coupon.startsAt} onChange={(e) => setCoupon({ ...coupon, startsAt: e.target.value })} /></label>
      <label className="field-label">Término<input required type="datetime-local" value={coupon.endsAt} onChange={(e) => setCoupon({ ...coupon, endsAt: e.target.value })} /></label>
      <label className="switch-field wide"><input type="checkbox" checked={coupon.isActive} onChange={(e) => setCoupon({ ...coupon, isActive: e.target.checked })} /><span /><strong>Cupom ativo</strong></label>
    </div></div><div className="modal-footer"><button type="button" className="secondary-button" onClick={() => setCouponOpen(false)}>Cancelar</button><button className="primary-button" disabled={saving}>{saving ? 'Salvando…' : 'Salvar cupom'}</button></div></form></Modal>}
  </>
}

function NumberField({ label, value, onChange, step = '1' }: { label: string; value: number; onChange: (value: number) => void; step?: string }) {
  return <label className="field-label">{label}<input required min="0" step={step} type="number" value={value} onChange={(event) => onChange(event.target.valueAsNumber || 0)} /></label>
}
function toInputDate(value: Date) { const local = new Date(value.getTime() - value.getTimezoneOffset() * 60_000); return local.toISOString().slice(0, 16) }
