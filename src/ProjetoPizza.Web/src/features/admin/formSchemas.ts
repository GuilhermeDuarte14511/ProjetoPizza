import { z } from 'zod'

const requiredText = (label: string) => z.string().trim().min(1, `${label} é obrigatório.`)
const nonNegativeMoney = z.number({ error: 'Informe um valor válido.' }).min(0, 'O valor não pode ser negativo.')

export const productSchema = z.object({
  id: z.string().optional(),
  categoryId: requiredText('A categoria'),
  sku: requiredText('O SKU').max(50, 'O SKU deve ter no máximo 50 caracteres.'),
  name: requiredText('O nome').max(150, 'O nome deve ter no máximo 150 caracteres.'),
  type: requiredText('O tipo'),
  basePrice: nonNegativeMoney,
  isActive: z.boolean(),
  isAvailable: z.boolean(),
  isFeatured: z.boolean(),
})

export const categorySchema = z.object({
  id: z.string().optional(),
  name: requiredText('O nome').max(100, 'O nome deve ter no máximo 100 caracteres.'),
  slug: requiredText('O slug')
    .regex(/^[a-z0-9]+(?:-[a-z0-9]+)*$/, 'Use apenas letras minúsculas, números e hífens.'),
  description: z.string().trim().max(300, 'A descrição deve ter no máximo 300 caracteres.').optional(),
  isActive: z.boolean(),
  isVisibleOnTablet: z.boolean(),
})

export const crustSchema = z.object({
  id: z.string().optional(),
  name: requiredText('O nome').max(100, 'O nome deve ter no máximo 100 caracteres.'),
  description: z.string().trim().max(300, 'A descrição deve ter no máximo 300 caracteres.').optional(),
  isActive: z.boolean(),
  isAvailable: z.boolean(),
})

export const pizzaFlavorSchema = z.object({
  id: z.string().optional(),
  categoryId: requiredText('A categoria'),
  name: requiredText('O nome').max(120, 'O nome deve ter no máximo 120 caracteres.'),
  description: z.string().trim().max(500, 'A descrição deve ter no máximo 500 caracteres.').optional(),
  type: z.enum(['Savory', 'Sweet'], { error: 'Selecione o tipo do sabor.' }),
  isPremium: z.boolean(),
  isVegetarian: z.boolean(),
  isActive: z.boolean(),
  isAvailable: z.boolean(),
  soldOutReason: z.string().trim().max(200, 'O motivo deve ter no máximo 200 caracteres.').optional(),
})

export const pizzaSizeSchema = z.object({
  id: z.string().optional(),
  name: requiredText('O nome').max(80, 'O nome deve ter no máximo 80 caracteres.'),
  shortName: requiredText('O nome curto').max(20, 'O nome curto deve ter no máximo 20 caracteres.'),
  slices: z.number({ error: 'Informe a quantidade de fatias.' }).int().min(1, 'Informe ao menos uma fatia.'),
  diameterCm: z.number({ error: 'Informe o diâmetro.' }).min(1, 'O diâmetro deve ser maior que zero.'),
  basePrice: nonNegativeMoney,
  maxFlavors: z.number({ error: 'Informe o limite de sabores.' }).int().min(1, 'O mínimo é um sabor.').max(3, 'O máximo é três sabores.'),
  isActive: z.boolean(),
})

export const openTableSchema = z.object({
  partySize: z.number({ error: 'Informe a quantidade de pessoas.' }).int().min(1, 'Informe ao menos uma pessoa.').max(50, 'O limite é de 50 pessoas.'),
})

export const cashMovementSchema = z.object({
  type: z.enum(['Supply', 'Withdrawal']),
  amount: z.number({ error: 'Informe o valor.' }).positive('O valor deve ser maior que zero.'),
  description: requiredText('A descrição').max(150, 'A descrição deve ter no máximo 150 caracteres.'),
  reason: requiredText('O motivo').max(200, 'O motivo deve ter no máximo 200 caracteres.'),
})

export const cashCloseSchema = z.object({
  countedCashAmount: nonNegativeMoney,
  notes: z.string().trim().max(500, 'A observação deve ter no máximo 500 caracteres.').optional(),
})

export const paymentSchema = z.object({
  paymentMethodId: requiredText('A forma de pagamento'),
  amount: z.number({ error: 'Informe o valor.' }).positive('O valor deve ser maior que zero.'),
  receivedAmount: z.number({ error: 'Informe o valor recebido.' }).min(0, 'O valor recebido não pode ser negativo.'),
  externalReference: z.string().trim().max(100, 'A referência deve ter no máximo 100 caracteres.').optional(),
})

export const userSchema = z.object({
  id: z.string().optional(),
  displayName: requiredText('O nome').max(150, 'O nome deve ter no máximo 150 caracteres.'),
  email: z.email('Informe um e-mail válido.'),
  employeeCode: requiredText('O código').max(30, 'O código deve ter no máximo 30 caracteres.'),
  password: z.string().min(8, 'A senha deve ter ao menos 8 caracteres.').optional().or(z.literal('')),
  phone: z.string().trim().max(30, 'O telefone deve ter no máximo 30 caracteres.').optional(),
  isActive: z.boolean(),
  roles: z.array(z.string()),
})

export const roleSchema = z.object({
  id: z.string().optional(),
  name: requiredText('O nome').max(80, 'O nome deve ter no máximo 80 caracteres.'),
  permissions: z.array(z.string()).min(1, 'Selecione ao menos uma permissão.'),
  userCount: z.number(),
})

export type ProductFormData = z.infer<typeof productSchema>
export type CategoryFormData = z.infer<typeof categorySchema>
export type CrustFormData = z.infer<typeof crustSchema>
export type PizzaFlavorFormData = z.infer<typeof pizzaFlavorSchema>
export type PizzaSizeFormData = z.infer<typeof pizzaSizeSchema>
export type OpenTableFormData = z.infer<typeof openTableSchema>
export type CashMovementFormData = z.infer<typeof cashMovementSchema>
export type CashCloseFormData = z.infer<typeof cashCloseSchema>
export type PaymentFormData = z.infer<typeof paymentSchema>
export type UserFormData = z.infer<typeof userSchema>
export type RoleFormData = z.infer<typeof roleSchema>
