using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProjetoPizza.Domain.Billing;
using ProjetoPizza.Domain.Cashier;
using ProjetoPizza.Domain.Catalog;
using ProjetoPizza.Domain.Core;
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
            return;
        }

        var unit = new RestaurantUnit(UnitId, "[DEV] Unidade Principal", "Projeto Pizza Desenvolvimento LTDA", "Forno 27", "00.000.000/0001-00");
        unit.UpdateContactInformation("(11) 99999-0000", "dev@projetopizza.local");
        context.AddRange(unit, new OperationSettings(UnitId), new PizzaSettings(UnitId));

        var employee = new Employee(
            EmployeeId,
            UnitId,
            Guid.Parse("20000000-0000-0000-0000-000000000002"),
            "Administrador de Desenvolvimento",
            "admin@projetopizza.local",
            "DEV-ADMIN");
        context.Employees.Add(employee);

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
        AddOperationalSamples(tables, products, stations, callTypes, devices);

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
                    new Money(price));
            }
        }
    }

    private static InventoryItem[] CreateInventoryItems() =>
    [
        new(new InventoryItemId(Guid.Parse("67000000-0000-0000-0000-000000000001")), UnitId, "Mussarela", "INS-MUSS", "g", 5000),
        new(new InventoryItemId(Guid.Parse("67000000-0000-0000-0000-000000000002")), UnitId, "Tomate", "INS-TOMA", "g", 2000),
        new(new InventoryItemId(Guid.Parse("67000000-0000-0000-0000-000000000003")), UnitId, "Calabresa", "INS-CALA", "g", 3000)
    ];

    private static Ingredient[] CreateIngredients(IReadOnlyList<InventoryItem> items) =>
    [
        new(new IngredientId(Guid.Parse("68000000-0000-0000-0000-000000000001")), UnitId, "Mussarela", items[0].Id),
        new(new IngredientId(Guid.Parse("68000000-0000-0000-0000-000000000002")), UnitId, "Tomate", items[1].Id),
        new(new IngredientId(Guid.Parse("68000000-0000-0000-0000-000000000003")), UnitId, "Calabresa Fatiada", items[2].Id)
    ];

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
        IReadOnlyList<Device> devices)
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
