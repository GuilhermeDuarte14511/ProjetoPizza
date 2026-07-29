import type { ClientPizzaFlavor, ClientProduct } from '../../types/client'
import heroImage from '../../assets/hero.png'

const images = {
  hero: heroImage,
  margherita: heroImage,
  calabresa: heroImage,
  cheese: heroImage,
  generic: heroImage,
} as const

export const clientHeroImage = images.hero

export function getProductImage(product: ClientProduct) {
  if (product.imageUrl?.startsWith('/')) return product.imageUrl
  const name = product.name.toLocaleLowerCase('pt-BR')
  if (name.includes('margherita')) return images.margherita
  if (name.includes('calabresa')) return images.calabresa
  if (name.includes('queijo')) return images.cheese
  return images.generic
}

export function getFlavorImage(flavor: ClientPizzaFlavor) {
  if (flavor.imageUrl?.startsWith('/')) return flavor.imageUrl
  const name = flavor.name.toLocaleLowerCase('pt-BR')
  if (name.includes('margherita')) return images.margherita
  if (name.includes('calabresa')) return images.calabresa
  if (name.includes('queijo')) return images.cheese
  return images.generic
}
