import type { AdministrativeOrderCatalog, Customer } from '../types/admin'
import {
  mockCategories,
  mockPizzaFlavors,
  mockPizzaRules,
  mockPizzaSizes,
  mockProducts,
} from './adminData'
import { mockCrusts, mockOperationSettings } from './adminManagementData'

export const mockCustomers: Customer[] = [
  {
    id: '21000000-0000-0000-0000-000000000001',
    name: 'Cliente Delivery',
    phone: '11999990001',
    birthDate: '1990-05-15',
    isActive: true,
    loyaltyPoints: 248,
    lifetimeSpend: 1248.9,
    orderCount: 17,
    lastOrderAt: new Date().toISOString(),
    createdAt: new Date().toISOString(),
  },
]

export const mockAdministrativeOrderCatalog: AdministrativeOrderCatalog = {
  defaultDeliveryFee: mockOperationSettings.defaultDeliveryFee,
  catalog: {
    categories: mockCategories.map((category, index) => ({
      id: category.id,
      name: category.name,
      slug: category.slug,
      displayOrder: index,
    })),
    products: mockProducts.filter((product) => product.isActive && product.isAvailable).map((product) => ({
      id: product.id,
      categoryId: product.categoryId,
      name: product.name,
      productType: product.type,
      price: product.basePrice,
      isFeatured: product.isFeatured,
      isPopular: product.isFeatured,
      isAvailable: product.isAvailable,
      preparationTimeMinutes: 15,
      usesCustomExtras: product.usesCustomExtras,
      complements: product.complements.map((extra) => ({
        id: extra.ingredientId ?? `${product.id}-${extra.name}`,
        name: extra.name,
        price: extra.price,
        maxQuantity: extra.maxQuantity,
        isAllergen: false,
      })),
    })),
    pizza: {
      globalMaxFlavors: mockPizzaRules.globalMaxFlavors,
      pricingPolicy: mockPizzaRules.pricingPolicy,
      allowSweetAndSavoryMix: mockPizzaRules.allowSweetAndSavoryMix,
      allowExtrasPerFlavor: mockPizzaRules.allowExtrasPerFlavor,
      allowRepeatedFlavors: mockPizzaRules.allowRepeatedFlavors,
      sizes: mockPizzaSizes.map((size) => ({
        id: size.id,
        name: size.name,
        shortName: size.shortName,
        slices: size.slices,
        diameterCm: size.diameterCm,
        basePrice: size.basePrice,
        maxFlavors: size.maxFlavors,
      })),
      flavors: mockPizzaFlavors.map((flavor) => ({
        id: flavor.id,
        categoryId: flavor.categoryId,
        name: flavor.name,
        description: flavor.description,
        flavorType: flavor.type,
        isPremium: flavor.isPremium,
        isVegetarian: flavor.isVegetarian,
        isAvailable: flavor.isAvailable,
        soldOutReason: flavor.soldOutReason,
        prices: mockPizzaSizes.map((size) => ({
          pizzaSizeId: size.id,
          price: size.basePrice + (flavor.isPremium ? 5 : 0),
          additionalPrice: flavor.isPremium ? 5 : 0,
          isAvailable: flavor.isAvailable,
        })),
        ingredients: [],
        extras: flavor.extras.map((extra) => ({
          id: extra.ingredientId,
          name: extra.ingredientName,
          price: extra.price,
          maxQuantity: extra.maxQuantity,
          isAllergen: false,
        })),
      })),
      crusts: mockCrusts.map((crust) => ({
        id: crust.id,
        name: crust.name,
        description: crust.description,
        isAvailable: crust.isAvailable,
        prices: crust.prices.map((price) => ({
          pizzaSizeId: price.pizzaSizeId,
          fullPrice: price.fullPrice,
          halfPrice: price.halfPrice,
        })),
      })),
      extras: [],
    },
    serviceFeePercentage: 0,
  },
}
