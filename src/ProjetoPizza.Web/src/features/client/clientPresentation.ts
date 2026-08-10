import type { ClientPizzaFlavor, ClientProduct } from '../../types/client'
import heroImage from '../../assets/hero.png'
import idleImage from '../../assets/tablet-idle-pizzeria.jpg'
import { resolveApiMediaUrl } from '../../api/httpClient'

const images = {
  hero: heroImage,
  margherita: heroImage,
  calabresa: heroImage,
  cheese: heroImage,
  generic: heroImage,
} as const

export const clientHeroImage = images.hero
export const clientIdleImage = idleImage

export function getProductImage(product: ClientProduct) {
  if (product.imageUrl) return resolveApiMediaUrl(product.imageUrl) ?? product.imageUrl
  const name = product.name.toLocaleLowerCase('pt-BR')
  if (name.includes('margherita')) return images.margherita
  if (name.includes('calabresa')) return images.calabresa
  if (name.includes('queijo')) return images.cheese
  return images.generic
}

export function getFlavorImage(flavor: ClientPizzaFlavor) {
  if (flavor.imageUrl) return resolveApiMediaUrl(flavor.imageUrl) ?? flavor.imageUrl
  const name = flavor.name.toLocaleLowerCase('pt-BR')
  if (name.includes('margherita')) return images.margherita
  if (name.includes('calabresa')) return images.calabresa
  if (name.includes('queijo')) return images.cheese
  return images.generic
}
