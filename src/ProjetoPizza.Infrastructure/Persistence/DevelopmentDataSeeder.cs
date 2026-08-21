using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProjetoPizza.Domain.Billing;
using ProjetoPizza.Domain.Cashier;
using ProjetoPizza.Domain.Catalog;
using ProjetoPizza.Domain.Core;
using ProjetoPizza.Domain.Customers;
using ProjetoPizza.Domain.Devices;
using ProjetoPizza.Domain.Dining;
using ProjetoPizza.Domain.Identity;
using ProjetoPizza.Domain.Inventory;
using ProjetoPizza.Domain.Ordering;
using ProjetoPizza.Domain.Production;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Infrastructure.Persistence;

public sealed class DevelopmentDataSeeder(
    ProjetoPizzaDbContext context,
    UserManager<IdentityUser<Guid>> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IConfiguration configuration)
{
    private static readonly RestaurantUnitId UnitId = new(Guid.Parse("10000000-0000-0000-0000-000000000001"));
    private static readonly EmployeeId EmployeeId = new(Guid.Parse("20000000-0000-0000-0000-000000000001"));
    private static readonly DiningAreaId AreaId = new(Guid.Parse("30000000-0000-0000-0000-000000000001"));

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedIdentityAsync(cancellationToken);
        if (await context.RestaurantUnits.AnyAsync(unit => unit.Id == UnitId, cancellationToken))
        {
            await EnsureDevelopmentCatalogAsync(cancellationToken);
            await EnsureDevelopmentPizzaExtrasAsync(cancellationToken);
            await EnsureDevelopmentTabletLinksAsync(cancellationToken);
            return;
        }

        var unit = new RestaurantUnit(UnitId, "[DEV] Unidade Principal", "Projeto Pizza Desenvolvimento LTDA", "Forno 27", "00.000.000/0001-00");
        unit.UpdateContactInformation("(11) 99999-0000", "dev@projetopizza.local");
        context.AddRange(unit, new OperationSettings(UnitId), new PizzaSettings(UnitId),
            new LoyaltySettings(LoyaltySettingsId.New(), UnitId));

        var employee = new Employee(
            EmployeeId,
            UnitId,
            Guid.Parse("20000000-0000-0000-0000-000000000002"),
            "Administrador de Desenvolvimento",
            "admin@projetopizza.local",
            "DEV-ADMIN");
        context.Employees.Add(employee);
        var phoneCustomer = new Customer(
            new CustomerId(Guid.Parse("21000000-0000-0000-0000-000000000001")),
            UnitId,
            "Cliente Delivery",
            "11999990001",
            new DateOnly(1990, 5, 15));
        context.Customers.Add(phoneCustomer);

        var categories = CreateCategories();
        context.Categories.AddRange(categories);
        var products = CreateProducts(categories);
        context.Products.AddRange(products);

        var sizes = CreatePizzaSizes();
        context.PizzaSizes.AddRange(sizes);
        var flavors = CreatePizzaFlavors(categories);
        context.PizzaFlavors.AddRange(flavors);
        context.PizzaFlavorPrices.AddRange(CreateFlavorPrices(flavors, sizes));

        var crusts = CreateCrusts();
        context.PizzaCrusts.AddRange(crusts);
        context.PizzaCrustPrices.AddRange(CreateCrustPrices(crusts, sizes));

        var inventoryItems = CreateInventoryItems();
        context.InventoryItems.AddRange(inventoryItems);
        var ingredients = CreateIngredients(inventoryItems);
        context.Ingredients.AddRange(ingredients);
        context.PizzaFlavorIngredients.AddRange(
            new PizzaFlavorIngredient(flavors[0].Id, ingredients[0].Id, 120, "g"),
            new PizzaFlavorIngredient(flavors[0].Id, ingredients[1].Id, 80, "g"),
            new PizzaFlavorIngredient(flavors[1].Id, ingredients[0].Id, 120, "g"),
            new PizzaFlavorIngredient(flavors[1].Id, ingredients[2].Id, 90, "g"));
        context.PizzaFlavorExtras.AddRange(CreateFlavorExtras(flavors, ingredients));

        var area = new DiningArea(AreaId, UnitId, "Salão Principal");
        context.DiningAreas.Add(area);
        var tables = Enumerable.Range(1, 32)
            .Select(number => new RestaurantTable(
                new RestaurantTableId(Guid.Parse($"40000000-0000-0000-0000-{number:D12}")),
                UnitId,
                AreaId,
                number,
                number % 4 == 0 ? 6 : 4))
            .ToArray();
        context.RestaurantTables.AddRange(tables);

        var stations = CreateProductionStations();
        context.ProductionStations.AddRange(stations);
        var paymentMethods = CreatePaymentMethods();
        context.PaymentMethods.AddRange(paymentMethods);
        var callTypes = CreateServiceCallTypes();
        context.ServiceCallTypes.AddRange(callTypes);
        context.CashRegisters.Add(new CashRegister(
            new CashRegisterId(Guid.Parse("50000000-0000-0000-0000-000000000001")),
            UnitId,
            "Caixa Principal",
            "CX-01"));

        var devices = CreateDevices();
        context.Devices.AddRange(devices);
        AddOperationalSamples(tables, products, stations, callTypes, devices, phoneCustomer);

        await context.SaveChangesAsync(cancellationToken);
        await EnsureDevelopmentCatalogAsync(cancellationToken);
        await EnsureDevelopmentPizzaExtrasAsync(cancellationToken);
    }

    private async Task EnsureDevelopmentCatalogAsync(CancellationToken cancellationToken)
    {
        var categories = await context.Categories
            .Where(category => category.UnitId == UnitId)
            .ToDictionaryAsync(category => category.Slug, cancellationToken);
        var ingredients = await EnsureDevelopmentIngredientsAsync(cancellationToken);
        var products = await context.Products
            .Where(product => product.UnitId == UnitId)
            .ToDictionaryAsync(product => product.Id, cancellationToken);
        var flavors = await context.PizzaFlavors
            .Where(flavor => flavor.UnitId == UnitId)
            .ToDictionaryAsync(flavor => flavor.Id, cancellationToken);
        var productImages = await context.ProductImages
            .Where(image => products.Keys.Contains(image.ProductId))
            .ToDictionaryAsync(image => image.ProductId, cancellationToken);

        foreach (var definition in CreateDevelopmentProductDefinitions(categories))
        {
            if (!products.TryGetValue(definition.Id, out var product))
            {
                product = new Product(
                    definition.Id,
                    UnitId,
                    definition.CategoryId,
                    definition.Sku,
                    definition.Name,
                    definition.ProductType,
                    new Money(definition.BasePrice));
                context.Products.Add(product);
                products[definition.Id] = product;
            }

            product.ChangeCategory(definition.CategoryId);
            product.ChangePrice(new Money(definition.BasePrice));
            product.UpdateInformation(definition.Name, definition.Description, definition.PreparationTimeMinutes);
            product.SetActive(true);
            product.SetAvailable(true);
            if (definition.IsFeatured) product.MarkAsFeatured();

            if (productImages.TryGetValue(product.Id, out var image))
            {
                image.Update(definition.ImageUrl, $"Foto de {definition.Name}", isPrimary: true);
            }
            else
            {
                image = new ProductImage(
                    new ProductImageId(Guid.Parse(
                        $"6A000000-0000-0000-0000-{definition.Id.Value.ToString().Split('-').Last()}")),
                    product.Id,
                    definition.ImageUrl,
                    $"Foto de {definition.Name}");
                image.Update(definition.ImageUrl, $"Foto de {definition.Name}", isPrimary: true);
                context.ProductImages.Add(image);
                productImages[product.Id] = image;
            }
        }

        foreach (var definition in CreateDevelopmentFlavorDefinitions(categories))
        {
            if (!flavors.TryGetValue(definition.Id, out var flavor))
            {
                flavor = new PizzaFlavor(
                    definition.Id,
                    UnitId,
                    definition.CategoryId,
                    definition.Name,
                    definition.FlavorType);
                context.PizzaFlavors.Add(flavor);
                flavors[definition.Id] = flavor;
            }

            flavor.Update(
                definition.Name,
                definition.Description,
                definition.FlavorType,
                definition.IsPremium,
                definition.IsVegetarian,
                isActive: true,
                isAvailable: true,
                soldOutReason: null);
            flavor.SetImage(definition.ImageUrl);
        }

        await context.SaveChangesAsync(cancellationToken);

        var sizes = await context.PizzaSizes
            .Where(size => size.UnitId == UnitId && size.IsActive)
            .OrderBy(size => size.DisplayOrder)
            .ToArrayAsync(cancellationToken);
        var prices = (await context.PizzaFlavorPrices
            .Where(price => flavors.Keys.Contains(price.PizzaFlavorId))
            .ToListAsync(cancellationToken))
            .ToDictionary(price => (price.PizzaFlavorId, price.PizzaSizeId));
        var ingredientLinks = (await context.PizzaFlavorIngredients
                .Where(link => flavors.Keys.Contains(link.PizzaFlavorId))
                .Select(link => new { link.PizzaFlavorId, link.IngredientId })
                .ToListAsync(cancellationToken))
            .Select(link => (link.PizzaFlavorId, link.IngredientId))
            .ToHashSet();
        var extraLinks = (await context.PizzaFlavorExtras
                .Where(link => flavors.Keys.Contains(link.PizzaFlavorId))
                .Select(link => new { link.PizzaFlavorId, link.IngredientId })
                .ToListAsync(cancellationToken))
            .Select(link => (link.PizzaFlavorId, link.IngredientId))
            .ToHashSet();

        foreach (var definition in CreateDevelopmentFlavorDefinitions(categories))
        {
            var flavor = flavors[definition.Id];
            for (var sizeIndex = 0; sizeIndex < sizes.Length; sizeIndex++)
            {
                var size = sizes[sizeIndex];
                var key = (flavor.Id, size.Id);
                if (!prices.ContainsKey(key))
                {
                    var priceId = new PizzaFlavorPriceId(Guid.Parse(
                        $"64000000-0000-0000-0000-{(1000 + definition.Number * 10 + sizeIndex):D12}"));
                    var price = new PizzaFlavorPrice(
                        priceId,
                        flavor.Id,
                        size.Id,
                        size.BasePrice,
                        new Money(definition.AdditionalPrice));
                    context.PizzaFlavorPrices.Add(price);
                    prices[key] = price;
                }
            }

            for (var ingredientIndex = 0; ingredientIndex < definition.Ingredients.Length; ingredientIndex++)
            {
                var ingredientName = definition.Ingredients[ingredientIndex];
                if (!ingredients.TryGetValue(ingredientName, out var ingredient)) continue;
                var ingredientKey = (flavor.Id, ingredient.Id);
                if (ingredientLinks.Add(ingredientKey))
                {
                    context.PizzaFlavorIngredients.Add(new PizzaFlavorIngredient(
                        flavor.Id,
                        ingredient.Id,
                        quantity: ingredient.Name.Contains("Camarão", StringComparison.Ordinal) ? 100 : 80,
                        unitOfMeasure: "g",
                        displayOrder: ingredientIndex));
                }
            }

            var extraNames = definition.FlavorType == PizzaFlavorType.Sweet
                ? new[] { "Chocolate", "Morango", "Banana", "Coco", "Doce de Leite" }
                : new[] { "Mussarela", "Catupiry", "Bacon", "Calabresa Fatiada", "Tomate", "Cebola", "Azeitona", "Milho", "Champignon", "Palmito", "Pepperoni", "Frango Desfiado", "Presunto", "Cheddar" };
            foreach (var extraName in extraNames)
            {
                if (!ingredients.TryGetValue(extraName, out var ingredient)) continue;
                var extraKey = (flavor.Id, ingredient.Id);
                if (extraLinks.Add(extraKey))
                {
                    context.PizzaFlavorExtras.Add(new PizzaFlavorExtra(
                        flavor.Id,
                        ingredient.Id,
                        ingredient.ExtraPrice,
                        maxQuantity: ingredient.MaxExtraQuantity));
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<string, Ingredient>> EnsureDevelopmentIngredientsAsync(CancellationToken cancellationToken)
    {
        var definitions = CreateDevelopmentIngredientDefinitions();
        var inventoryItems = await context.InventoryItems
            .Where(item => item.UnitId == UnitId)
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var ingredients = await context.Ingredients
            .Where(ingredient => ingredient.UnitId == UnitId)
            .ToDictionaryAsync(ingredient => ingredient.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);

        foreach (var definition in definitions)
        {
            var inventoryId = new InventoryItemId(definition.InventoryId);
            if (!inventoryItems.TryGetValue(inventoryId, out var inventoryItem))
            {
                inventoryItem = new InventoryItem(
                    inventoryId,
                    UnitId,
                    definition.Name,
                    definition.Sku,
                    "g",
                    definition.MinimumStock);
                context.InventoryItems.Add(inventoryItem);
                inventoryItems[inventoryId] = inventoryItem;
            }

            inventoryItem.Update(
                definition.Name,
                definition.Sku,
                "g",
                definition.MinimumStock,
                Money.Zero(),
                isActive: true);

            var ingredientId = new IngredientId(definition.Id);
            if (!ingredients.TryGetValue(definition.Name, out var ingredient))
            {
                ingredient = new Ingredient(ingredientId, UnitId, definition.Name, inventoryId);
                context.Ingredients.Add(ingredient);
                ingredients[definition.Name] = ingredient;
            }

            ingredient.Update(
                definition.Name,
                definition.Description,
                isActive: true,
                definition.IsAllergen,
                definition.IsAllergen ? definition.AllergenDescription : null,
                isAvailableAsExtra: true,
                new Money(definition.ExtraPrice),
                definition.MaxExtraQuantity);
        }

        await context.SaveChangesAsync(cancellationToken);
        return ingredients;
    }

    private async Task EnsureDevelopmentTabletLinksAsync(CancellationToken cancellationToken)
    {
        var links = new[]
        {
            (
                DeviceId: new DeviceId(Guid.Parse("72000000-0000-0000-0000-000000000001")),
                TableId: new RestaurantTableId(Guid.Parse("40000000-0000-0000-0000-000000000002"))),
            (
                DeviceId: new DeviceId(Guid.Parse("72000000-0000-0000-0000-000000000002")),
                TableId: new RestaurantTableId(Guid.Parse("40000000-0000-0000-0000-000000000003")))
        };
        var changed = false;
        foreach (var link in links)
        {
            var device = await context.Devices.SingleOrDefaultAsync(
                candidate => candidate.Id == link.DeviceId,
                cancellationToken);
            if (device is not null && !device.LinkedTableId.HasValue)
            {
                device.LinkToTable(link.TableId);
                changed = true;
            }
        }

        if (changed)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task EnsureDevelopmentPizzaExtrasAsync(CancellationToken cancellationToken)
    {
        var inventoryDefinitions = new[]
        {
            (Id: Guid.Parse("67000000-0000-0000-0000-000000000001"), Name: "Mussarela", Sku: "INS-MUSS", MinimumStock: 5000m),
            (Id: Guid.Parse("67000000-0000-0000-0000-000000000002"), Name: "Tomate", Sku: "INS-TOMA", MinimumStock: 2000m),
            (Id: Guid.Parse("67000000-0000-0000-0000-000000000003"), Name: "Calabresa", Sku: "INS-CALA", MinimumStock: 3000m),
            (Id: Guid.Parse("67000000-0000-0000-0000-000000000004"), Name: "Bacon", Sku: "INS-BACO", MinimumStock: 2500m),
            (Id: Guid.Parse("67000000-0000-0000-0000-000000000005"), Name: "Cebola", Sku: "INS-CEBO", MinimumStock: 1500m),
            (Id: Guid.Parse("67000000-0000-0000-0000-000000000006"), Name: "Catupiry", Sku: "INS-CATU", MinimumStock: 3000m)
        };
        var existingInventoryIds = await context.InventoryItems
            .Where(item => item.UnitId == UnitId)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
        foreach (var definition in inventoryDefinitions.Where(definition =>
                     !existingInventoryIds.Contains(new InventoryItemId(definition.Id))))
        {
            context.InventoryItems.Add(new InventoryItem(
                new InventoryItemId(definition.Id),
                UnitId,
                definition.Name,
                definition.Sku,
                "g",
                definition.MinimumStock));
        }

        var extraDefinitions = new[]
        {
            (Id: Guid.Parse("68000000-0000-0000-0000-000000000001"), InventoryId: inventoryDefinitions[0].Id, Name: "Mussarela", Description: "Porção adicional de mussarela.", Price: 6m, Max: 3),
            (Id: Guid.Parse("68000000-0000-0000-0000-000000000002"), InventoryId: inventoryDefinitions[1].Id, Name: "Tomate", Description: "Porção adicional de tomate.", Price: 3m, Max: 3),
            (Id: Guid.Parse("68000000-0000-0000-0000-000000000003"), InventoryId: inventoryDefinitions[2].Id, Name: "Calabresa Fatiada", Description: "Porção adicional de calabresa fatiada.", Price: 7m, Max: 3),
            (Id: Guid.Parse("68000000-0000-0000-0000-000000000004"), InventoryId: inventoryDefinitions[3].Id, Name: "Bacon", Description: "Bacon crocante em cubos.", Price: 8m, Max: 3),
            (Id: Guid.Parse("68000000-0000-0000-0000-000000000005"), InventoryId: inventoryDefinitions[4].Id, Name: "Cebola", Description: "Cebola fatiada.", Price: 3m, Max: 3),
            (Id: Guid.Parse("68000000-0000-0000-0000-000000000006"), InventoryId: inventoryDefinitions[5].Id, Name: "Catupiry", Description: "Porção adicional de Catupiry.", Price: 8m, Max: 3)
        };
        var existingIngredients = await context.Ingredients
            .Where(ingredient => ingredient.UnitId == UnitId)
            .ToDictionaryAsync(ingredient => ingredient.Id, cancellationToken);
        foreach (var definition in extraDefinitions)
        {
            var ingredientId = new IngredientId(definition.Id);
            if (!existingIngredients.TryGetValue(ingredientId, out var ingredient))
            {
                ingredient = new Ingredient(
                    ingredientId,
                    UnitId,
                    definition.Name,
                    new InventoryItemId(definition.InventoryId));
                context.Ingredients.Add(ingredient);
            }

            ingredient.Update(
                definition.Name,
                definition.Description,
                isActive: true,
                isAllergen: definition.Name.Contains("Mussarela", StringComparison.Ordinal) ||
                            definition.Name.Contains("Catupiry", StringComparison.Ordinal),
                allergenDescription: definition.Name.Contains("Mussarela", StringComparison.Ordinal) ||
                                      definition.Name.Contains("Catupiry", StringComparison.Ordinal)
                    ? "Contém leite e derivados."
                    : null,
                isAvailableAsExtra: true,
                new Money(definition.Price),
                definition.Max);
        }

        var savoryFlavors = await context.PizzaFlavors
            .Where(flavor => flavor.UnitId == UnitId && flavor.FlavorType == PizzaFlavorType.Savory)
            .ToArrayAsync(cancellationToken);
        var existingFlavorExtras = await context.PizzaFlavorExtras
            .Where(extra => savoryFlavors.Select(flavor => flavor.Id).Contains(extra.PizzaFlavorId))
            .ToDictionaryAsync(
                extra => new { extra.PizzaFlavorId, extra.IngredientId },
                cancellationToken);
        foreach (var flavor in savoryFlavors)
        {
            foreach (var definition in extraDefinitions)
            {
                var ingredientId = new IngredientId(definition.Id);
                var key = new { PizzaFlavorId = flavor.Id, IngredientId = ingredientId };
                if (existingFlavorExtras.TryGetValue(key, out var flavorExtra))
                {
                    flavorExtra.Update(new Money(definition.Price), definition.Max, isActive: true);
                }
                else
                {
                    context.PizzaFlavorExtras.Add(new PizzaFlavorExtra(
                        flavor.Id,
                        ingredientId,
                        new Money(definition.Price),
                        definition.Max));
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static Category[] CreateCategories()
    {
        var names = new[]
        {
            ("Pizzas Tradicionais", "pizzas-tradicionais"),
            ("Pizzas Especiais", "pizzas-especiais"),
            ("Pizzas Doces", "pizzas-doces"),
            ("Porções", "porcoes"),
            ("Bebidas", "bebidas"),
            ("Sobremesas", "sobremesas"),
            ("Combos", "combos"),
            ("Opcionais e Bordas", "opcionais-bordas")
        };
        return names.Select((item, index) => new Category(
            new CategoryId(Guid.Parse($"60000000-0000-0000-0000-{index + 1:D12}")),
            UnitId,
            item.Item1,
            item.Item2,
            index)).ToArray();
    }

    private static DevelopmentFlavorDefinition[] CreateDevelopmentFlavorDefinitions(
        IReadOnlyDictionary<string, Category> categories) =>
    [
        new(1, new PizzaFlavorId(Guid.Parse("63000000-0000-0000-0000-000000000001")), new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000001")), categories["pizzas-tradicionais"].Id, "Margherita", PizzaFlavorType.Savory, false, true, 0, "Molho de tomate artesanal, mussarela, tomate fresco e manjericão.", "https://images.unsplash.com/photo-1574071318508-1cdbab80d002?auto=format&fit=crop&w=900&q=80", ["Mussarela", "Tomate", "Manjericão"]),
        new(2, new PizzaFlavorId(Guid.Parse("63000000-0000-0000-0000-000000000002")), new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000002")), categories["pizzas-tradicionais"].Id, "Calabresa", PizzaFlavorType.Savory, false, false, 2, "Mussarela derretida, calabresa fatiada, cebola roxa e azeitonas.", "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?auto=format&fit=crop&w=900&q=80", ["Mussarela", "Calabresa Fatiada", "Cebola", "Azeitona"]),
        new(3, new PizzaFlavorId(Guid.Parse("63000000-0000-0000-0000-000000000003")), new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000003")), categories["pizzas-especiais"].Id, "Quatro Queijos", PizzaFlavorType.Savory, true, false, 5, "Mussarela, Catupiry, cheddar e parmesão em uma combinação cremosa.", "https://images.unsplash.com/photo-1593504049359-74330189a345?auto=format&fit=crop&w=900&q=80", ["Mussarela", "Catupiry", "Cheddar", "Parmesão"]),
        new(4, new PizzaFlavorId(Guid.Parse("63000000-0000-0000-0000-000000000004")), new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000007")), categories["pizzas-doces"].Id, "Chocolate com Morango", PizzaFlavorType.Sweet, false, true, 6, "Chocolate cremoso, morangos frescos e um toque delicado de leite condensado.", "https://images.unsplash.com/photo-1579751626657-72bc17010498?auto=format&fit=crop&w=900&q=80", ["Chocolate", "Morango", "Leite Condensado"]),
        new(5, new PizzaFlavorId(Guid.Parse("63000000-0000-0000-0000-000000000005")), new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000008")), categories["pizzas-especiais"].Id, "Frango com Catupiry", PizzaFlavorType.Savory, false, false, 5, "Frango desfiado temperado, Catupiry cremoso, mussarela e milho.", "https://images.unsplash.com/photo-1513104890138-7c749659a591?auto=format&fit=crop&w=900&q=80", ["Mussarela", "Frango Desfiado", "Catupiry", "Milho"]),
        new(6, new PizzaFlavorId(Guid.Parse("63000000-0000-0000-0000-000000000006")), new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000009")), categories["pizzas-tradicionais"].Id, "Portuguesa", PizzaFlavorType.Savory, false, false, 5, "Presunto, ovo, ervilha, cebola, pimentão, azeitona e mussarela.", "https://images.unsplash.com/photo-1579751626657-72bc17010498?auto=format&fit=crop&w=900&q=80", ["Mussarela", "Presunto", "Ovo", "Ervilha", "Cebola", "Pimentão", "Azeitona"]),
        new(7, new PizzaFlavorId(Guid.Parse("63000000-0000-0000-0000-000000000007")), new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000010")), categories["pizzas-especiais"].Id, "Pepperoni", PizzaFlavorType.Savory, true, false, 7, "Pepperoni levemente picante, mussarela e molho de tomate artesanal.", "https://images.unsplash.com/photo-1574071318508-1cdbab80d002?auto=format&fit=crop&w=900&q=80", ["Mussarela", "Pepperoni", "Tomate"]),
        new(8, new PizzaFlavorId(Guid.Parse("63000000-0000-0000-0000-000000000008")), new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000011")), categories["pizzas-especiais"].Id, "Bacon e Milho", PizzaFlavorType.Savory, false, false, 5, "Bacon crocante, milho verde, cebola caramelizada e mussarela.", "https://images.unsplash.com/photo-1593504049359-74330189a345?auto=format&fit=crop&w=900&q=80", ["Mussarela", "Bacon", "Milho", "Cebola"]),
        new(9, new PizzaFlavorId(Guid.Parse("63000000-0000-0000-0000-000000000009")), new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000012")), categories["pizzas-tradicionais"].Id, "Napolitana", PizzaFlavorType.Savory, false, false, 3, "Mussarela, tomate, presunto, parmesão e orégano.", "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?auto=format&fit=crop&w=900&q=80", ["Mussarela", "Tomate", "Presunto", "Parmesão"]),
        new(10, new PizzaFlavorId(Guid.Parse("63000000-0000-0000-0000-000000000010")), new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000013")), categories["pizzas-especiais"].Id, "Vegetariana", PizzaFlavorType.Savory, false, true, 4, "Mussarela, tomate, milho, champignon, palmito, brócolis e pimentão.", "https://images.unsplash.com/photo-1513104890138-7c749659a591?auto=format&fit=crop&w=900&q=80", ["Mussarela", "Tomate", "Milho", "Champignon", "Palmito", "Brócolis", "Pimentão"]),
        new(11, new PizzaFlavorId(Guid.Parse("63000000-0000-0000-0000-000000000011")), new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000014")), categories["pizzas-especiais"].Id, "Carne Seca com Catupiry", PizzaFlavorType.Savory, true, false, 8, "Carne seca desfiada, Catupiry, mussarela e cebola dourada.", "https://images.unsplash.com/photo-1593504049359-74330189a345?auto=format&fit=crop&w=900&q=80", ["Mussarela", "Carne Seca", "Catupiry", "Cebola"]),
        new(12, new PizzaFlavorId(Guid.Parse("63000000-0000-0000-0000-000000000012")), new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000015")), categories["pizzas-especiais"].Id, "Lombo Canadense", PizzaFlavorType.Savory, false, false, 6, "Lombo canadense, mussarela, Catupiry e tomate fresco.", "https://images.unsplash.com/photo-1574071318508-1cdbab80d002?auto=format&fit=crop&w=900&q=80", ["Mussarela", "Lombo Canadense", "Catupiry", "Tomate"]),
        new(13, new PizzaFlavorId(Guid.Parse("63000000-0000-0000-0000-000000000013")), new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000016")), categories["pizzas-especiais"].Id, "Palmito com Alho-poró", PizzaFlavorType.Savory, false, true, 6, "Palmito macio, alho-poró, Catupiry e mussarela.", "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?auto=format&fit=crop&w=900&q=80", ["Mussarela", "Palmito", "Alho-poró", "Catupiry"]),
        new(14, new PizzaFlavorId(Guid.Parse("63000000-0000-0000-0000-000000000014")), new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000017")), categories["pizzas-especiais"].Id, "Camarão Cremoso", PizzaFlavorType.Savory, true, false, 12, "Camarões salteados, Catupiry, mussarela e tomate em molho cremoso.", "https://images.unsplash.com/photo-1513104890138-7c749659a591?auto=format&fit=crop&w=900&q=80", ["Mussarela", "Camarão", "Catupiry", "Tomate"]),
        new(15, new PizzaFlavorId(Guid.Parse("63000000-0000-0000-0000-000000000015")), new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000018")), categories["pizzas-especiais"].Id, "Mexicana", PizzaFlavorType.Savory, true, false, 7, "Calabresa, bacon, pimentão, cebola e pimenta jalapeño.", "https://images.unsplash.com/photo-1579751626657-72bc17010498?auto=format&fit=crop&w=900&q=80", ["Mussarela", "Calabresa Fatiada", "Bacon", "Pimentão", "Cebola", "Pimenta Jalapeño"]),
        new(16, new PizzaFlavorId(Guid.Parse("63000000-0000-0000-0000-000000000016")), new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000019")), categories["pizzas-especiais"].Id, "Brócolis com Bacon", PizzaFlavorType.Savory, false, false, 5, "Brócolis, bacon crocante, Catupiry e mussarela.", "https://images.unsplash.com/photo-1593504049359-74330189a345?auto=format&fit=crop&w=900&q=80", ["Mussarela", "Brócolis", "Bacon", "Catupiry"]),
        new(17, new PizzaFlavorId(Guid.Parse("63000000-0000-0000-0000-000000000017")), new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000020")), categories["pizzas-tradicionais"].Id, "Atum", PizzaFlavorType.Savory, false, false, 4, "Atum, cebola, azeitona, tomate e mussarela.", "https://images.unsplash.com/photo-1565299624946-b28f40a0ae38?auto=format&fit=crop&w=900&q=80", ["Mussarela", "Atum", "Cebola", "Azeitona", "Tomate"]),
        new(18, new PizzaFlavorId(Guid.Parse("63000000-0000-0000-0000-000000000018")), new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000021")), categories["pizzas-doces"].Id, "Romeu e Julieta", PizzaFlavorType.Sweet, false, true, 5, "Mussarela cremosa e goiabada artesanal em equilíbrio perfeito.", "https://images.unsplash.com/photo-1579751626657-72bc17010498?auto=format&fit=crop&w=900&q=80", ["Mussarela", "Goiabada"]),
        new(19, new PizzaFlavorId(Guid.Parse("63000000-0000-0000-0000-000000000019")), new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000022")), categories["pizzas-doces"].Id, "Banana com Canela", PizzaFlavorType.Sweet, false, true, 4, "Banana caramelizada, açúcar, canela e leite condensado.", "https://images.unsplash.com/photo-1579751626657-72bc17010498?auto=format&fit=crop&w=900&q=80", ["Banana", "Canela", "Leite Condensado"]),
        new(20, new PizzaFlavorId(Guid.Parse("63000000-0000-0000-0000-000000000020")), new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000023")), categories["pizzas-doces"].Id, "Prestígio", PizzaFlavorType.Sweet, false, true, 5, "Chocolate cremoso, coco ralado e leite condensado.", "https://images.unsplash.com/photo-1593504049359-74330189a345?auto=format&fit=crop&w=900&q=80", ["Chocolate", "Coco", "Leite Condensado"])
    ];

    private static DevelopmentProductDefinition[] CreateDevelopmentProductDefinitions(
        IReadOnlyDictionary<string, Category> categories)
    {
        var flavors = CreateDevelopmentFlavorDefinitions(categories);
        var products = flavors.Select(flavor => new DevelopmentProductDefinition(
            flavor.ProductId,
            flavor.CategoryId,
            $"PIZ-{NormalizeSku(flavor.Name)}",
            $"Pizza {flavor.Name}",
            ProductType.Pizza,
            32 + flavor.AdditionalPrice,
            flavor.Description,
            flavor.FlavorType == PizzaFlavorType.Sweet ? 25 : 30,
            flavor.ImageUrl,
            flavor.Number <= 3));

        return products.Concat([
            new(new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000004")), categories["bebidas"].Id, "BEB-COCA2", "Coca-Cola 2 L", ProductType.Beverage, 14, "Refrigerante cola de 2 litros, servido bem gelado.", 2, "https://images.unsplash.com/photo-1544145945-f90425340c7e?auto=format&fit=crop&w=900&q=80", false),
            new(new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000024")), categories["bebidas"].Id, "BEB-GUARA2", "Guaraná 2 L", ProductType.Beverage, 12, "Refrigerante de guaraná de 2 litros para compartilhar.", 2, "https://images.unsplash.com/photo-1625772299848-391b6a87d7b3?auto=format&fit=crop&w=900&q=80", false),
            new(new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000025")), categories["bebidas"].Id, "BEB-LARANJA", "Refrigerante de Laranja Lata", ProductType.Beverage, 6, "Lata de refrigerante de laranja, servida gelada.", 2, "https://images.unsplash.com/photo-1544145945-f90425340c7e?auto=format&fit=crop&w=900&q=80", false),
            new(new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000026")), categories["bebidas"].Id, "BEB-LIMAO", "Refrigerante de Limão Lata", ProductType.Beverage, 6, "Lata de refrigerante de limão, servida gelada.", 2, "https://images.unsplash.com/photo-1544145945-f90425340c7e?auto=format&fit=crop&w=900&q=80", false),
            new(new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000027")), categories["bebidas"].Id, "BEB-AGUA", "Água Mineral 500 ml", ProductType.Beverage, 4, "Água mineral sem gás para acompanhar a refeição.", 1, "https://images.unsplash.com/photo-1548839140-29a749e1cf4d?auto=format&fit=crop&w=900&q=80", false),
            new(new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000028")), categories["bebidas"].Id, "BEB-SUCO-LAR", "Suco Natural de Laranja", ProductType.Beverage, 9, "Suco natural de laranja, preparado na hora.", 5, "https://images.unsplash.com/photo-1600271886742-f049cd451bba?auto=format&fit=crop&w=900&q=80", false),
            new(new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000029")), categories["bebidas"].Id, "BEB-SUCO-UVA", "Suco Integral de Uva", ProductType.Beverage, 9, "Suco integral de uva servido gelado.", 3, "https://images.unsplash.com/photo-1600271886742-f049cd451bba?auto=format&fit=crop&w=900&q=80", false),
            new(new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000030")), categories["bebidas"].Id, "BEB-SUCO-MAR", "Suco de Maracujá", ProductType.Beverage, 9, "Suco refrescante de maracujá preparado na hora.", 5, "https://images.unsplash.com/photo-1600271886742-f049cd451bba?auto=format&fit=crop&w=900&q=80", false),
            new(new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000031")), categories["bebidas"].Id, "BEB-CHA", "Chá Gelado de Pêssego", ProductType.Beverage, 8, "Chá gelado de pêssego com toque frutado.", 3, "https://images.unsplash.com/photo-1544145945-f90425340c7e?auto=format&fit=crop&w=900&q=80", false),
            new(new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000005")), categories["porcoes"].Id, "POR-FRIT", "Batata Frita Especial", ProductType.Portion, 32, "Batata frita crocante com cheddar e bacon.", 15, "https://images.unsplash.com/photo-1573080496219-bb080dd4f877?auto=format&fit=crop&w=900&q=80", false),
            new(new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000032")), categories["porcoes"].Id, "POR-ALHO", "Pão de Alho com Queijo", ProductType.Portion, 18, "Pães de alho assados com cobertura de queijo cremoso.", 12, "https://images.unsplash.com/photo-1573140401552-3fab0b24306f?auto=format&fit=crop&w=900&q=80", false),
            new(new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000006")), categories["sobremesas"].Id, "SOB-TIRA", "Tiramisu", ProductType.Dessert, 24.90m, "Camadas delicadas de café, creme e cacau.", 8, "https://images.unsplash.com/photo-1571877227200-a0d98ea607e9?auto=format&fit=crop&w=900&q=80", false),
            new(new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000033")), categories["sobremesas"].Id, "SOB-BROWNIE", "Brownie com Calda de Chocolate", ProductType.Dessert, 22, "Brownie macio servido com calda cremosa de chocolate.", 8, "https://images.unsplash.com/photo-1606313564200-e75d5e30476c?auto=format&fit=crop&w=900&q=80", false)
        ]).ToArray();
    }

    private static DevelopmentIngredientDefinition[] CreateDevelopmentIngredientDefinitions() =>
    [
        new(Guid.Parse("68000000-0000-0000-0000-000000000001"), Guid.Parse("67000000-0000-0000-0000-000000000001"), "Mussarela", "INS-MUSS", "Porção adicional de mussarela.", 6, 5000, true, "Contém leite e derivados."),
        new(Guid.Parse("68000000-0000-0000-0000-000000000002"), Guid.Parse("67000000-0000-0000-0000-000000000002"), "Tomate", "INS-TOMA", "Tomate fresco fatiado.", 3, 2000, false, null),
        new(Guid.Parse("68000000-0000-0000-0000-000000000003"), Guid.Parse("67000000-0000-0000-0000-000000000003"), "Calabresa Fatiada", "INS-CALA", "Calabresa fatiada e temperada.", 7, 3000, false, null),
        new(Guid.Parse("68000000-0000-0000-0000-000000000004"), Guid.Parse("67000000-0000-0000-0000-000000000004"), "Bacon", "INS-BACO", "Bacon crocante em cubos.", 8, 2500, false, null),
        new(Guid.Parse("68000000-0000-0000-0000-000000000005"), Guid.Parse("67000000-0000-0000-0000-000000000005"), "Cebola", "INS-CEBO", "Cebola fatiada.", 3, 1500, false, null),
        new(Guid.Parse("68000000-0000-0000-0000-000000000006"), Guid.Parse("67000000-0000-0000-0000-000000000006"), "Catupiry", "INS-CATU", "Porção adicional de Catupiry.", 8, 3000, true, "Contém leite e derivados."),
        new(Guid.Parse("68000000-0000-0000-0000-000000000007"), Guid.Parse("67000000-0000-0000-0000-000000000007"), "Presunto", "INS-PRES", "Presunto cozido fatiado.", 6, 2500, false, null),
        new(Guid.Parse("68000000-0000-0000-0000-000000000008"), Guid.Parse("67000000-0000-0000-0000-000000000008"), "Milho", "INS-MILH", "Milho verde.", 3, 1800, false, null),
        new(Guid.Parse("68000000-0000-0000-0000-000000000009"), Guid.Parse("67000000-0000-0000-0000-000000000009"), "Ervilha", "INS-ERVI", "Ervilha tenra.", 3, 1200, false, null),
        new(Guid.Parse("68000000-0000-0000-0000-000000000010"), Guid.Parse("67000000-0000-0000-0000-000000000010"), "Azeitona", "INS-AZEI", "Azeitonas fatiadas.", 3, 1200, false, null),
        new(Guid.Parse("68000000-0000-0000-0000-000000000011"), Guid.Parse("67000000-0000-0000-0000-000000000011"), "Pimentão", "INS-PIME", "Pimentão colorido fatiado.", 3, 1200, false, null),
        new(Guid.Parse("68000000-0000-0000-0000-000000000012"), Guid.Parse("67000000-0000-0000-0000-000000000012"), "Champignon", "INS-CHAM", "Cogumelos champignon fatiados.", 5, 1000, false, null),
        new(Guid.Parse("68000000-0000-0000-0000-000000000013"), Guid.Parse("67000000-0000-0000-0000-000000000013"), "Frango Desfiado", "INS-FRAN", "Frango desfiado temperado.", 8, 2800, false, null),
        new(Guid.Parse("68000000-0000-0000-0000-000000000014"), Guid.Parse("67000000-0000-0000-0000-000000000014"), "Pepperoni", "INS-PEPP", "Pepperoni fatiado.", 9, 1800, false, null),
        new(Guid.Parse("68000000-0000-0000-0000-000000000015"), Guid.Parse("67000000-0000-0000-0000-000000000015"), "Palmito", "INS-PALM", "Palmito macio em rodelas.", 6, 1600, false, null),
        new(Guid.Parse("68000000-0000-0000-0000-000000000016"), Guid.Parse("67000000-0000-0000-0000-000000000016"), "Brócolis", "INS-BROC", "Brócolis cozido no vapor.", 5, 1400, false, null),
        new(Guid.Parse("68000000-0000-0000-0000-000000000017"), Guid.Parse("67000000-0000-0000-0000-000000000017"), "Carne Seca", "INS-CARS", "Carne seca desfiada.", 10, 2000, false, null),
        new(Guid.Parse("68000000-0000-0000-0000-000000000018"), Guid.Parse("67000000-0000-0000-0000-000000000018"), "Camarão", "INS-CAMA", "Camarão limpo e temperado.", 14, 1200, true, "Contém crustáceos."),
        new(Guid.Parse("68000000-0000-0000-0000-000000000019"), Guid.Parse("67000000-0000-0000-0000-000000000019"), "Chocolate", "INS-CHOC", "Chocolate cremoso.", 7, 1800, true, "Pode conter leite e derivados."),
        new(Guid.Parse("68000000-0000-0000-0000-000000000020"), Guid.Parse("67000000-0000-0000-0000-000000000020"), "Morango", "INS-MORA", "Morangos frescos.", 6, 1200, false, null),
        new(Guid.Parse("68000000-0000-0000-0000-000000000021"), Guid.Parse("67000000-0000-0000-0000-000000000021"), "Banana", "INS-BANA", "Banana em rodelas.", 4, 1200, false, null),
        new(Guid.Parse("68000000-0000-0000-0000-000000000022"), Guid.Parse("67000000-0000-0000-0000-000000000022"), "Coco", "INS-COCO", "Coco ralado.", 4, 900, false, null),
        new(Guid.Parse("68000000-0000-0000-0000-000000000023"), Guid.Parse("67000000-0000-0000-0000-000000000023"), "Doce de Leite", "INS-DL", "Doce de leite cremoso.", 7, 1000, true, "Contém leite e derivados."),
        new(Guid.Parse("68000000-0000-0000-0000-000000000024"), Guid.Parse("67000000-0000-0000-0000-000000000024"), "Cheddar", "INS-CHED", "Cheddar cremoso.", 8, 1600, true, "Contém leite e derivados."),
        new(Guid.Parse("68000000-0000-0000-0000-000000000025"), Guid.Parse("67000000-0000-0000-0000-000000000025"), "Alho-poró", "INS-ALHO", "Alho-poró fatiado.", 4, 900, false, null),
        new(Guid.Parse("68000000-0000-0000-0000-000000000026"), Guid.Parse("67000000-0000-0000-0000-000000000026"), "Manjericão", "INS-MANJ", "Folhas frescas de manjericão.", 2, 500, false, null),
        new(Guid.Parse("68000000-0000-0000-0000-000000000027"), Guid.Parse("67000000-0000-0000-0000-000000000027"), "Parmesão", "INS-PARM", "Parmesão ralado.", 7, 900, true, "Contém leite e derivados."),
        new(Guid.Parse("68000000-0000-0000-0000-000000000028"), Guid.Parse("67000000-0000-0000-0000-000000000028"), "Ovo", "INS-OVO", "Ovo cozido fatiado.", 3, 900, true, "Contém ovo."),
        new(Guid.Parse("68000000-0000-0000-0000-000000000029"), Guid.Parse("67000000-0000-0000-0000-000000000029"), "Lombo Canadense", "INS-LOMB", "Lombo canadense fatiado.", 7, 1200, false, null),
        new(Guid.Parse("68000000-0000-0000-0000-000000000030"), Guid.Parse("67000000-0000-0000-0000-000000000030"), "Pimenta Jalapeño", "INS-JALA", "Pimenta jalapeño fatiada.", 3, 500, false, null),
        new(Guid.Parse("68000000-0000-0000-0000-000000000031"), Guid.Parse("67000000-0000-0000-0000-000000000031"), "Atum", "INS-ATUM", "Atum em lascas.", 8, 1200, true, "Contém peixe."),
        new(Guid.Parse("68000000-0000-0000-0000-000000000032"), Guid.Parse("67000000-0000-0000-0000-000000000032"), "Goiabada", "INS-GOIA", "Goiabada cremosa.", 5, 900, false, null),
        new(Guid.Parse("68000000-0000-0000-0000-000000000033"), Guid.Parse("67000000-0000-0000-0000-000000000033"), "Canela", "INS-CANE", "Canela em pó.", 2, 400, false, null),
        new(Guid.Parse("68000000-0000-0000-0000-000000000034"), Guid.Parse("67000000-0000-0000-0000-000000000034"), "Leite Condensado", "INS-LCON", "Leite condensado cremoso.", 5, 1000, true, "Contém leite e derivados.")
    ];

    private static string NormalizeSku(string value) => new(value
        .ToUpperInvariant()
        .Normalize(System.Text.NormalizationForm.FormD)
        .Where(character => character is >= 'A' and <= 'Z' || character is >= '0' and <= '9')
        .ToArray());

    private sealed record DevelopmentFlavorDefinition(
        int Number,
        PizzaFlavorId Id,
        ProductId ProductId,
        CategoryId CategoryId,
        string Name,
        PizzaFlavorType FlavorType,
        bool IsPremium,
        bool IsVegetarian,
        decimal AdditionalPrice,
        string Description,
        string ImageUrl,
        string[] Ingredients);

    private sealed record DevelopmentProductDefinition(
        ProductId Id,
        CategoryId CategoryId,
        string Sku,
        string Name,
        ProductType ProductType,
        decimal BasePrice,
        string Description,
        int PreparationTimeMinutes,
        string ImageUrl,
        bool IsFeatured,
        int ImageOrder = 0);

    private sealed record DevelopmentIngredientDefinition(
        Guid Id,
        Guid InventoryId,
        string Name,
        string Sku,
        string Description,
        decimal ExtraPrice,
        decimal MinimumStock,
        bool IsAllergen,
        string? AllergenDescription,
        int MaxExtraQuantity = 3);

    private static Product[] CreateProducts(IReadOnlyList<Category> categories) =>
    [
        new(new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000001")), UnitId, categories[0].Id, "PIZ-MARG", "Pizza Margherita", ProductType.Pizza, new Money(49.90m)),
        new(new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000002")), UnitId, categories[0].Id, "PIZ-CALA", "Pizza Calabresa", ProductType.Pizza, new Money(54.90m)),
        new(new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000003")), UnitId, categories[1].Id, "PIZ-4QUE", "Pizza Quatro Queijos", ProductType.Pizza, new Money(59.90m)),
        new(new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000004")), UnitId, categories[4].Id, "BEB-COCA2", "Coca-Cola 2L", ProductType.Beverage, new Money(14.00m)),
        new(new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000005")), UnitId, categories[3].Id, "POR-FRIT", "Batata Frita Especial", ProductType.Portion, new Money(32.00m)),
        new(new ProductId(Guid.Parse("61000000-0000-0000-0000-000000000006")), UnitId, categories[5].Id, "SOB-TIRA", "Tiramisu", ProductType.Dessert, new Money(24.90m))
    ];

    private static PizzaSize[] CreatePizzaSizes() =>
    [
        new(new PizzaSizeId(Guid.Parse("62000000-0000-0000-0000-000000000001")), UnitId, "Broto", "B", 4, 20, new Money(32), 1, 1),
        new(new PizzaSizeId(Guid.Parse("62000000-0000-0000-0000-000000000002")), UnitId, "Média", "M", 6, 30, new Money(48), 2, 2),
        new(new PizzaSizeId(Guid.Parse("62000000-0000-0000-0000-000000000003")), UnitId, "Grande", "G", 8, 35, new Money(68), 3, 3),
        new(new PizzaSizeId(Guid.Parse("62000000-0000-0000-0000-000000000004")), UnitId, "Família", "F", 12, 45, new Money(84), 3, 4)
    ];

    private static PizzaFlavor[] CreatePizzaFlavors(IReadOnlyList<Category> categories) =>
    [
        new(new PizzaFlavorId(Guid.Parse("63000000-0000-0000-0000-000000000001")), UnitId, categories[0].Id, "Margherita", PizzaFlavorType.Savory),
        new(new PizzaFlavorId(Guid.Parse("63000000-0000-0000-0000-000000000002")), UnitId, categories[0].Id, "Calabresa", PizzaFlavorType.Savory),
        new(new PizzaFlavorId(Guid.Parse("63000000-0000-0000-0000-000000000003")), UnitId, categories[1].Id, "Quatro Queijos", PizzaFlavorType.Savory),
        new(new PizzaFlavorId(Guid.Parse("63000000-0000-0000-0000-000000000004")), UnitId, categories[2].Id, "Chocolate com Morango", PizzaFlavorType.Sweet)
    ];

    private static IEnumerable<PizzaFlavorPrice> CreateFlavorPrices(IReadOnlyList<PizzaFlavor> flavors, IReadOnlyList<PizzaSize> sizes)
    {
        var index = 1;
        foreach (var flavor in flavors)
        {
            foreach (var size in sizes)
            {
                yield return new PizzaFlavorPrice(
                    new PizzaFlavorPriceId(Guid.Parse($"64000000-0000-0000-0000-{index++:D12}")),
                    flavor.Id,
                    size.Id,
                    size.BasePrice,
                    new Money(flavor.Name == "Quatro Queijos" ? 5 : 0));
            }
        }
    }

    private static PizzaCrust[] CreateCrusts() =>
    [
        new(new PizzaCrustId(Guid.Parse("65000000-0000-0000-0000-000000000001")), UnitId, "Sem borda", "Massa tradicional"),
        new(new PizzaCrustId(Guid.Parse("65000000-0000-0000-0000-000000000002")), UnitId, "Catupiry", "Borda recheada"),
        new(new PizzaCrustId(Guid.Parse("65000000-0000-0000-0000-000000000003")), UnitId, "Cheddar", "Borda recheada"),
        new(new PizzaCrustId(Guid.Parse("65000000-0000-0000-0000-000000000004")), UnitId, "Cream Cheese", "Borda recheada")
    ];

    private static IEnumerable<PizzaCrustPrice> CreateCrustPrices(IReadOnlyList<PizzaCrust> crusts, IReadOnlyList<PizzaSize> sizes)
    {
        var index = 1;
        foreach (var crust in crusts)
        {
            foreach (var size in sizes)
            {
                var price = crust.Name switch { "Sem borda" => 0m, "Cream Cheese" => 14m, _ => 12m };
                yield return new PizzaCrustPrice(
                    new PizzaCrustPriceId(Guid.Parse($"66000000-0000-0000-0000-{index++:D12}")),
                    crust.Id,
                    size.Id,
                    new Money(price),
                    new Money(decimal.Round(price / 2m, 2, MidpointRounding.ToEven)));
            }
        }
    }

    private static InventoryItem[] CreateInventoryItems() =>
    [
        new(new InventoryItemId(Guid.Parse("67000000-0000-0000-0000-000000000001")), UnitId, "Mussarela", "INS-MUSS", "g", 5000),
        new(new InventoryItemId(Guid.Parse("67000000-0000-0000-0000-000000000002")), UnitId, "Tomate", "INS-TOMA", "g", 2000),
        new(new InventoryItemId(Guid.Parse("67000000-0000-0000-0000-000000000003")), UnitId, "Calabresa", "INS-CALA", "g", 3000),
        new(new InventoryItemId(Guid.Parse("67000000-0000-0000-0000-000000000004")), UnitId, "Bacon", "INS-BACO", "g", 2500),
        new(new InventoryItemId(Guid.Parse("67000000-0000-0000-0000-000000000005")), UnitId, "Cebola", "INS-CEBO", "g", 1500),
        new(new InventoryItemId(Guid.Parse("67000000-0000-0000-0000-000000000006")), UnitId, "Catupiry", "INS-CATU", "g", 3000)
    ];

    private static Ingredient[] CreateIngredients(IReadOnlyList<InventoryItem> items)
    {
        var definitions = new[]
        {
            (Name: "Mussarela", Description: "Porção adicional de mussarela.", Price: 6m, Max: 3),
            (Name: "Tomate", Description: "Porção adicional de tomate.", Price: 3m, Max: 3),
            (Name: "Calabresa Fatiada", Description: "Porção adicional de calabresa fatiada.", Price: 7m, Max: 3),
            (Name: "Bacon", Description: "Bacon crocante em cubos.", Price: 8m, Max: 3),
            (Name: "Cebola", Description: "Cebola fatiada.", Price: 3m, Max: 3),
            (Name: "Catupiry", Description: "Porção adicional de Catupiry.", Price: 8m, Max: 3)
        };
        return definitions.Select((definition, index) =>
        {
            var ingredient = new Ingredient(
                new IngredientId(Guid.Parse($"68000000-0000-0000-0000-{index + 1:D12}")),
                UnitId,
                definition.Name,
                items[index].Id);
            var isDairy = index is 0 or 5;
            ingredient.Update(
                definition.Name,
                definition.Description,
                isActive: true,
                isAllergen: isDairy,
                allergenDescription: isDairy ? "Contém leite e derivados." : null,
                isAvailableAsExtra: true,
                new Money(definition.Price),
                definition.Max);
            return ingredient;
        }).ToArray();
    }

    private static IEnumerable<PizzaFlavorExtra> CreateFlavorExtras(
        IReadOnlyList<PizzaFlavor> flavors,
        IReadOnlyList<Ingredient> ingredients)
    {
        var prices = new[] { 6m, 3m, 7m, 8m, 3m, 8m };
        foreach (var flavor in flavors.Where(flavor => flavor.FlavorType == PizzaFlavorType.Savory))
        {
            for (var index = 0; index < ingredients.Count; index++)
            {
                yield return new PizzaFlavorExtra(
                    flavor.Id,
                    ingredients[index].Id,
                    new Money(prices[index]),
                    maxQuantity: 3);
            }
        }
    }

    private static ProductionStation[] CreateProductionStations() =>
    [
        new(new ProductionStationId(Guid.Parse("69000000-0000-0000-0000-000000000001")), UnitId, "Pizzaria", "PIZZA", 20, 1),
        new(new ProductionStationId(Guid.Parse("69000000-0000-0000-0000-000000000002")), UnitId, "Cozinha Quente", "HOT", 18, 2),
        new(new ProductionStationId(Guid.Parse("69000000-0000-0000-0000-000000000003")), UnitId, "Bar", "BAR", 5, 3)
    ];

    private static PaymentMethod[] CreatePaymentMethods()
    {
        var data = new[]
        {
            ("CASH", "Dinheiro", false, true),
            ("PIX", "Pix", true, false),
            ("CREDIT", "Cartão de Crédito", true, false),
            ("DEBIT", "Cartão de Débito", true, false),
            ("MEAL", "Vale Refeição", true, false)
        };
        return data.Select((item, index) => new PaymentMethod(
            new PaymentMethodId(Guid.Parse($"70000000-0000-0000-0000-{index + 1:D12}")),
            UnitId,
            item.Item1,
            item.Item2,
            item.Item3,
            item.Item4,
            index + 1)).ToArray();
    }

    private static ServiceCallType[] CreateServiceCallTypes()
    {
        var data = new[]
        {
            ("CALL_WAITER", "Chamar garçom"),
            ("CUTLERY", "Pedir talheres"),
            ("NAPKINS", "Pedir guardanapos"),
            ("CLEANING", "Solicitar limpeza"),
            ("ORDER_PROBLEM", "Problema no pedido"),
            ("QUESTION", "Tirar uma dúvida"),
            ("REQUEST_BILL", "Solicitar a conta")
        };
        return data.Select((item, index) => new ServiceCallType(
            new ServiceCallTypeId(Guid.Parse($"71000000-0000-0000-0000-{index + 1:D12}")),
            item.Item1,
            item.Item2)).ToArray();
    }

    private static Device[] CreateDevices() =>
        CreateConfiguredDevices();

    private static Device[] CreateConfiguredDevices()
    {
        var devices = new[]
        {
            new Device(new DeviceId(Guid.Parse("72000000-0000-0000-0000-000000000001")), UnitId, "Tablet Mesa 02", "DEV-TABLET-002", DeviceType.CustomerTablet, "Android"),
            new Device(new DeviceId(Guid.Parse("72000000-0000-0000-0000-000000000002")), UnitId, "Tablet Mesa 03", "DEV-TABLET-003", DeviceType.CustomerTablet, "Android"),
            new Device(new DeviceId(Guid.Parse("72000000-0000-0000-0000-000000000003")), UnitId, "KDS Pizzaria", "DEV-KDS-001", DeviceType.KitchenDisplay, "Web"),
            new Device(new DeviceId(Guid.Parse("72000000-0000-0000-0000-000000000004")), UnitId, "Impressora Cozinha", "DEV-PRINTER-001", DeviceType.Printer, "Network"),
            new Device(new DeviceId(Guid.Parse("72000000-0000-0000-0000-000000000005")), UnitId, "Impressora Caixa", "DEV-PRINTER-002", DeviceType.Printer, "USB")
        };
        devices[0].UpdateStatus(DeviceStatus.Online, 82, false, "Wi-Fi", "192.168.10.22", "1.0.0");
        devices[1].UpdateStatus(DeviceStatus.Idle, 54, true, "Wi-Fi", "192.168.10.23", "1.0.0");
        devices[2].UpdateStatus(DeviceStatus.Online, null, false, "Ethernet", "192.168.10.10", "1.0.0");
        devices[3].UpdateStatus(DeviceStatus.Online, null, false, "Ethernet", "192.168.10.31", "Firmware 2.4");
        devices[4].UpdateStatus(DeviceStatus.Offline, null, false, "USB", null, "Firmware 2.4");
        return devices;
    }

    private async Task SeedIdentityAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        const string roleName = "Administrator";
        var role = await roleManager.FindByNameAsync(roleName);
        if (role is null)
        {
            role = new IdentityRole<Guid>(roleName)
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000003")
            };
            EnsureIdentitySucceeded(await roleManager.CreateAsync(role));
        }

        var existingClaims = await roleManager.GetClaimsAsync(role);
        foreach (var permission in new[] { "admin:read", "admin:write", "operations:read", "operations:write" })
        {
            if (!existingClaims.Any(claim => claim.Type == "permission" && claim.Value == permission))
            {
                EnsureIdentitySucceeded(await roleManager.AddClaimAsync(role, new Claim("permission", permission)));
            }
        }

        const string adminEmail = "admin@projetopizza.local";
        var user = await userManager.FindByEmailAsync(adminEmail);
        if (user is null)
        {
            var password = configuration["DevelopmentSeed:AdminPassword"];
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "DevelopmentSeed:AdminPassword is required to create the local administrator.");
            }

            user = new IdentityUser<Guid>
            {
                Id = Guid.Parse("20000000-0000-0000-0000-000000000002"),
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                LockoutEnabled = true
            };
            EnsureIdentitySucceeded(await userManager.CreateAsync(user, password));
        }

        if (!await userManager.IsInRoleAsync(user, roleName))
        {
            EnsureIdentitySucceeded(await userManager.AddToRoleAsync(user, roleName));
        }
    }

    private static void EnsureIdentitySucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(error => error.Description)));
        }
    }

    private void AddOperationalSamples(
        IReadOnlyList<RestaurantTable> tables,
        IReadOnlyList<Product> products,
        IReadOnlyList<ProductionStation> stations,
        IReadOnlyList<ServiceCallType> callTypes,
        IReadOnlyList<Device> devices,
        Customer phoneCustomer)
    {
        var session2 = TableSession.Open(
            new TableSessionId(Guid.Parse("73000000-0000-0000-0000-000000000002")),
            UnitId,
            1002,
            2,
            EmployeeId,
            new Percentage(10),
            [tables[1]]);
        session2.AssignWaiter(EmployeeId);
        var session3 = TableSession.Open(
            new TableSessionId(Guid.Parse("73000000-0000-0000-0000-000000000003")),
            UnitId,
            1003,
            4,
            EmployeeId,
            new Percentage(10),
            [tables[2]]);
        var session12 = TableSession.Open(
            new TableSessionId(Guid.Parse("73000000-0000-0000-0000-000000000012")),
            UnitId,
            1012,
            6,
            EmployeeId,
            new Percentage(10),
            [tables[11]]);
        session12.RequestBill();
        context.TableSessions.AddRange(session2, session3, session12);
        devices[0].LinkToTable(tables[1].Id);
        devices[1].LinkToTable(tables[2].Id);

        var order = new Order(
            new OrderId(Guid.Parse("74000000-0000-0000-0000-000000001024")),
            UnitId,
            1024,
            SalesChannel.DineIn,
            FulfillmentType.DineIn,
            EmployeeId,
            devices[0].Id,
            session2.Id);
        var orderItem = order.AddItem(
            new OrderItemId(Guid.Parse("75000000-0000-0000-0000-000000001024")),
            products[0].Id,
            products[0].Name,
            1,
            new Money(85.90m));
        order.Submit();
        order.Accept();
        order.StartProduction();
        context.Orders.Add(order);
        var ticket = new KitchenTicket(
            new KitchenTicketId(Guid.Parse("76000000-0000-0000-0000-000000001024")),
            UnitId,
            order.Id,
            stations[0].Id,
            1024);
        context.KitchenTickets.Add(ticket);
        context.KitchenTicketItems.Add(new KitchenTicketItem(
            new KitchenTicketItemId(Guid.Parse("77000000-0000-0000-0000-000000001024")),
            ticket.Id,
            orderItem.Id,
            1));

        var completed = new Order(
            new OrderId(Guid.Parse("74000000-0000-0000-0000-000000001023")),
            UnitId,
            1023,
            SalesChannel.Delivery,
            FulfillmentType.Delivery,
            EmployeeId);
        completed.AssignCustomer(phoneCustomer.Id, phoneCustomer.Name);
        completed.ConfigureDeliveryAddress("[DEV] Rua das Pizzas, 27 - Centro");
        completed.AddItem(
            new OrderItemId(Guid.Parse("75000000-0000-0000-0000-000000001023")),
            products[1].Id,
            products[1].Name,
            2,
            products[1].BasePrice);
        completed.Submit();
        completed.Accept();
        completed.StartProduction();
        completed.MarkReady();
        completed.Complete();
        context.Orders.Add(completed);

        context.ServiceCalls.Add(new ServiceCall(
            new ServiceCallId(Guid.Parse("78000000-0000-0000-0000-000000000001")),
            UnitId,
            session3.Id,
            callTypes[0].Id,
            devices[1].Id,
            "[DEV] Mesa solicita atendimento."));
        context.Bills.Add(new Bill(
            new BillId(Guid.Parse("79000000-0000-0000-0000-000000000001")),
            UnitId,
            session12.Id,
            new Money(145),
            new Percentage(10)));
        context.CashShifts.Add(new CashShift(
            new CashShiftId(Guid.Parse("80000000-0000-0000-0000-000000000001")),
            new CashRegisterId(Guid.Parse("50000000-0000-0000-0000-000000000001")),
            EmployeeId,
            new Money(200)));
    }
}
