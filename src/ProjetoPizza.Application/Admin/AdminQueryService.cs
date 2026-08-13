using System.Globalization;
using ProjetoPizza.Application.Abstractions.Persistence;
using ProjetoPizza.Domain.Billing;
using ProjetoPizza.Domain.Dining;
using ProjetoPizza.Domain.Ordering;
using ProjetoPizza.Domain.Production;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Application.Admin;

public sealed class AdminQueryService(IProjetoPizzaDbContext context) : IAdminQueryService
{
    public Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (dayStart, dayEnd) = GetCurrentBusinessDayUtc();
        var orders = context.Orders
            .Where(order =>
                (order.PlacedAt ?? order.CreatedAt) >= dayStart &&
                (order.PlacedAt ?? order.CreatedAt) < dayEnd &&
                order.Status != OrderStatus.Cancelled)
            .ToArray();
        var tables = ListTablesCore();
        var recentOrders = orders
            .OrderByDescending(order => order.PlacedAt ?? order.CreatedAt)
            .Take(5)
            .Select(order => new DashboardOrderDto(order.OrderNumber, order.SalesChannel.ToString(), order.Status.ToString(), order.Total.Amount, order.PlacedAt))
            .ToArray();

        var completedOrders = context.Orders
            .Where(order =>
                order.Status == OrderStatus.Completed &&
                order.UpdatedAt >= dayStart &&
                order.UpdatedAt < dayEnd)
            .ToArray();
        var sales = completedOrders.Sum(order => order.Total.Amount);
        var completedCount = completedOrders.Length;
        var orderIds = orders.Select(order => order.Id).ToHashSet();
        var topProducts = context.OrderItems
            .Where(item => orderIds.Contains(item.OrderId) && item.Status != OrderItemStatus.Cancelled)
            .ToArray()
            .GroupBy(item => item.ProductNameSnapshot)
            .Select(group => new DashboardProductDto(group.Key, group.Sum(item => item.Quantity)))
            .OrderByDescending(product => product.Quantity)
            .ThenBy(product => product.Name)
            .Take(5)
            .ToArray();
        var paidPayments = context.Payments
            .Where(payment =>
                payment.Status == PaymentStatus.Paid &&
                payment.PaidAt >= dayStart &&
                payment.PaidAt < dayEnd)
            .ToArray();
        var paymentMethodNames = context.PaymentMethods.ToDictionary(method => method.Id, method => method.Name);
        var totalPaid = paidPayments.Sum(payment => payment.Amount.Amount);
        var paymentMethods = paidPayments
            .GroupBy(payment => payment.PaymentMethodId)
            .Select(group =>
            {
                var total = group.Sum(payment => payment.Amount.Amount);
                return new DashboardPaymentMethodDto(
                    paymentMethodNames.GetValueOrDefault(group.Key, "Desconhecido"),
                    total,
                    totalPaid == 0 ? 0 : decimal.Round(total / totalPaid * 100m, 2));
            })
            .OrderByDescending(method => method.Total)
            .ToArray();
        var balances = context.StockBalances.ToDictionary(balance => balance.InventoryItemId);
        var stockAlerts = context.InventoryItems
            .Where(item => item.IsActive)
            .ToArray()
            .Select(item =>
            {
                var available = balances.TryGetValue(item.Id, out var balance) ? balance.AvailableQuantity : 0m;
                return new DashboardStockAlertDto(
                    item.Id.Value,
                    item.Name,
                    available,
                    item.MinimumStock,
                    item.UnitOfMeasure);
            })
            .Where(item => item.AvailableQuantity <= item.MinimumStock)
            .OrderBy(item => item.AvailableQuantity - item.MinimumStock)
            .ThenBy(item => item.Name)
            .Take(5)
            .ToArray();
        var tableStatus = new DashboardTableStatusDto(
            tables.Count(table => table.Status == "Livre"),
            tables.Count(table => table.Status == "Ocupada"),
            tables.Count(table => table.Status == "Chamando"),
            tables.Count(table => table.Status is "Conta solicitada" or "Pagamento pendente"));
        var result = new DashboardDto(
            sales,
            orders.Length,
            completedCount == 0 ? 0 : decimal.Round(sales / completedCount, 2),
            tables.Count(table => table.Status != "Livre"),
            tables.Count,
            orders.Count(order => order.Status == OrderStatus.InProduction),
            context.ServiceCalls.Count(call => call.Status == ServiceCallStatus.Pending),
            recentOrders,
            tableStatus,
            topProducts,
            paymentMethods,
            stockAlerts);
        return Task.FromResult(result);
    }

    public Task<IReadOnlyCollection<TableSummaryDto>> ListTablesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyCollection<TableSummaryDto>>(ListTablesCore());
    }

    public Task<TableDetailDto?> GetTableAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var table = ListTablesCore().SingleOrDefault(candidate => candidate.Id == id);
        if (table is null)
        {
            return Task.FromResult<TableDetailDto?>(null);
        }

        var tableId = new RestaurantTableId(id);
        var link = context.TableSessionTables
            .Where(candidate => candidate.RestaurantTableId == tableId && candidate.UnlinkedAt == null)
            .ToArray()
            .LastOrDefault(candidate => context.TableSessions.Any(session =>
                session.Id == candidate.TableSessionId &&
                session.Status != TableSessionStatus.Closed &&
                session.Status != TableSessionStatus.Cancelled));

        if (link is null)
        {
            return Task.FromResult<TableDetailDto?>(new TableDetailDto(table, null, null, null, [], null, 0, 0, 0, 0, 0, null, [], [], GetWaiters()));
        }

        var session = context.TableSessions.Single(candidate => candidate.Id == link.TableSessionId);
        var employeeNames = context.Employees.ToDictionary(employee => employee.Id, employee => employee.DisplayName);
        var tableNames = context.RestaurantTables.ToDictionary(item => item.Id, item => item.Name);
        var linkedTableLinks = context.TableSessionTables
            .Where(candidate => candidate.TableSessionId == session.Id && candidate.UnlinkedAt == null)
            .ToArray();
        var linkedTables = linkedTableLinks
            .Select(candidate => new TableReferenceDto(
                candidate.RestaurantTableId.Value,
                tableNames.GetValueOrDefault(candidate.RestaurantTableId, "Mesa"),
                candidate.IsPrimary))
            .OrderBy(candidate => candidate.Name)
            .ToArray();
        var sessionOrders = context.Orders
            .Where(order => order.TableSessionId == session.Id)
            .OrderByDescending(order => order.CreatedAt)
            .ToArray();
        var allOrderIds = sessionOrders.Select(order => order.Id).ToHashSet();
        var allOrderItems = context.OrderItems.Where(item => allOrderIds.Contains(item.OrderId)).ToArray();
        var allItemIds = allOrderItems.Select(item => item.Id).ToHashSet();
        var pizzasByItem = context.OrderItemPizzas
            .Where(pizza => allItemIds.Contains(pizza.Id))
            .ToArray()
            .ToDictionary(pizza => pizza.Id);
        var flavorsByItem = context.OrderItemPizzaFlavors
            .Where(flavor => allItemIds.Contains(flavor.OrderItemId))
            .ToArray()
            .GroupBy(flavor => flavor.OrderItemId)
            .ToDictionary(group => group.Key, group => group.OrderBy(flavor => flavor.PartNumber).ToArray());
        var modifiersByItem = context.OrderItemModifiers
            .Where(modifier => allItemIds.Contains(modifier.OrderItemId))
            .ToArray()
            .GroupBy(modifier => modifier.OrderItemId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var itemsByOrder = allOrderItems.GroupBy(item => item.OrderId).ToDictionary(group => group.Key, group => group.ToArray());
        var ptBr = CultureInfo.GetCultureInfo("pt-BR");
        var orders = sessionOrders.Select(order =>
        {
            var items = itemsByOrder.GetValueOrDefault(order.Id, [])
                .Select(item =>
                {
                    var details = new List<string>();
                    if (pizzasByItem.TryGetValue(item.Id, out var pizza))
                    {
                        details.Add($"Tamanho: {pizza.SizeNameSnapshot}");
                        if (flavorsByItem.TryGetValue(item.Id, out var flavors))
                            details.Add($"Sabores: {string.Join(" / ", flavors.Select(flavor => flavor.FlavorNameSnapshot))}");
                        if (pizza.CrustSelectionMode == CrustSelectionMode.Split)
                            details.Add($"Borda: 1/2 {pizza.CrustNameSnapshot} + 1/2 {pizza.SecondCrustNameSnapshot}");
                        else if (pizza.CrustSelectionMode == CrustSelectionMode.Whole)
                            details.Add($"Borda: {pizza.CrustNameSnapshot}");
                    }

                    if (modifiersByItem.TryGetValue(item.Id, out var modifiers))
                    {
                        details.AddRange(modifiers.Select(modifier => modifier.ModifierType switch
                        {
                            ModifierType.Remove => $"Sem {modifier.NameSnapshot}",
                            ModifierType.Extra => $"Adicional: {modifier.Quantity:0.##}x {modifier.NameSnapshot} (+ {modifier.TotalPrice.Amount.ToString("C", ptBr)})",
                            _ => $"{modifier.Quantity:0.##}x {modifier.NameSnapshot}"
                        }));
                    }

                    return new TableOrderItemDto(
                        item.Id.Value, item.ProductNameSnapshot, item.Quantity, item.UnitPrice.Amount,
                        item.TotalPrice.Amount, item.Notes, details);
                })
                .ToArray();
            return new TableOrderDto(
                order.Id.Value, order.OrderNumber, order.SalesChannel.ToString(), order.Status.ToString(),
                order.Subtotal.Amount, order.Discount.Amount, order.ServiceFee.Amount, order.Total.Amount,
                order.PlacedAt, order.Notes, items);
        }).ToArray();
        var bill = context.Bills
            .Where(candidate => candidate.TableSessionId == session.Id && candidate.Status != BillStatus.Cancelled)
            .ToArray()
            .OrderByDescending(candidate => candidate.RequestedAt)
            .FirstOrDefault();
        var subtotal = bill?.Subtotal.Amount ?? table.CurrentTotal;
        var serviceFeePercentage = bill?.ServiceFeePercentage.Value ?? session.ServiceFeePercentageSnapshot.Value;
        var serviceFeeAmount = bill?.ServiceFeeAmount.Amount ??
            decimal.Round(subtotal * session.ServiceFeePercentageSnapshot.AsFactor, 2, MidpointRounding.ToEven);
        var total = bill?.TotalAmount.Amount ?? subtotal + serviceFeeAmount;
        var orderIds = sessionOrders.Where(order => order.Status != OrderStatus.Cancelled).Select(order => order.Id).ToHashSet();
        var rawItems = allOrderItems.Where(item => orderIds.Contains(item.OrderId)).ToArray();
        var rawTotal = rawItems.Sum(item => item.TotalPrice.Amount);
        var billItems = rawItems.Select((item, index) => new TableBillItemDto(
            item.Id.Value,
            item.ProductNameSnapshot,
            item.Quantity,
            index == rawItems.Length - 1
                ? total - rawItems.Take(index).Sum(previous => rawTotal == 0 ? 0 : decimal.Round(previous.TotalPrice.Amount / rawTotal * total, 2))
                : rawTotal == 0 ? 0 : decimal.Round(item.TotalPrice.Amount / rawTotal * total, 2)))
            .ToArray();
        return Task.FromResult<TableDetailDto?>(new TableDetailDto(
            table,
            session.Id.Value,
            session.SessionNumber,
            session.PrimaryWaiterId.HasValue ? employeeNames.GetValueOrDefault(session.PrimaryWaiterId.Value) : null,
            orders,
            bill?.Id.Value,
            subtotal,
            serviceFeePercentage,
            serviceFeeAmount,
            total,
            bill?.RemainingAmount.Amount ?? total,
            bill?.RequestedSplitCount,
            billItems,
            linkedTables,
            GetWaiters()));
    }

    private TableOperatorDto[] GetWaiters() => context.Employees
        .Where(employee => employee.IsActive)
        .OrderBy(employee => employee.DisplayName)
        .Select(employee => new TableOperatorDto(employee.Id.Value, employee.DisplayName))
        .ToArray();

    public Task<IReadOnlyCollection<CategoryDto>> ListCategoriesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = context.Categories
            .OrderBy(category => category.DisplayOrder)
            .Select(category => new CategoryDto(category.Id.Value, category.Name, category.Slug, category.Description, category.IsActive, category.IsVisibleOnTablet))
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<CategoryDto>>(result);
    }

    public Task<IReadOnlyCollection<ProductDto>> ListProductsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ingredients = context.Ingredients.ToDictionary(ingredient => ingredient.Id, ingredient => ingredient.Name);
        var complements = context.ProductExtras
            .Where(extra => extra.IsActive)
            .ToArray()
            .GroupBy(extra => extra.ProductId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<ProductExtraDto>)group
                    .OrderBy(extra => ingredients.GetValueOrDefault(extra.IngredientId))
                    .Select(extra => new ProductExtraDto(
                        extra.IngredientId.Value,
                        ingredients.GetValueOrDefault(extra.IngredientId, "Complemento"),
                        extra.Price.Amount,
                        extra.MaxQuantity))
                    .ToArray());
        var images = context.ProductImages
            .OrderByDescending(image => image.IsPrimary)
            .ThenBy(image => image.DisplayOrder)
            .ToArray()
            .GroupBy(image => image.ProductId)
            .ToDictionary(group => group.Key, group => group.First().Url);
        var result = context.Products
            .OrderBy(product => product.DisplayOrder)
            .ThenBy(product => product.Name)
            .ToArray()
            .Select(product => new ProductDto(
                product.Id.Value,
                product.CategoryId.Value,
                product.Sku,
                product.Name,
                product.Description,
                product.ProductType.ToString(),
                product.BasePrice.Amount,
                product.PreparationTimeMinutes,
                images.GetValueOrDefault(product.Id),
                product.IsActive,
                product.IsAvailable,
                product.IsFeatured,
                product.UsesCustomExtras,
                complements.GetValueOrDefault(product.Id, [])))
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<ProductDto>>(result);
    }

    public Task<IReadOnlyCollection<PizzaSizeDto>> ListPizzaSizesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = context.PizzaSizes
            .OrderBy(size => size.DisplayOrder)
            .ToArray()
            .Select(size => new PizzaSizeDto(size.Id.Value, size.Name, size.ShortName, size.Slices, size.DiameterCm, size.BasePrice.Amount, size.MaxFlavors, size.IsActive))
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<PizzaSizeDto>>(result);
    }

    public Task<IReadOnlyCollection<PizzaFlavorDto>> ListPizzaFlavorsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ingredientNames = context.Ingredients.ToDictionary(
            ingredient => ingredient.Id,
            ingredient => ingredient.Name);
        var extras = context.PizzaFlavorExtras
            .Where(extra => extra.IsActive)
            .ToArray()
            .GroupBy(extra => extra.PizzaFlavorId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<PizzaFlavorExtraDto>)group
                    .OrderBy(extra => ingredientNames.GetValueOrDefault(extra.IngredientId))
                    .Select(extra => new PizzaFlavorExtraDto(
                        extra.IngredientId.Value,
                        ingredientNames.GetValueOrDefault(extra.IngredientId, "Ingrediente"),
                        extra.Price.Amount,
                        extra.MaxQuantity))
                    .ToArray());
        var result = context.PizzaFlavors
            .OrderBy(flavor => flavor.DisplayOrder)
            .ToArray()
            .Select(flavor => new PizzaFlavorDto(
                flavor.Id.Value,
                flavor.CategoryId.Value,
                flavor.Name,
                flavor.Description,
                flavor.FlavorType.ToString(),
                flavor.IsPremium,
                flavor.IsVegetarian,
                flavor.IsActive,
                flavor.IsAvailable,
                flavor.SoldOutReason,
                flavor.ImageUrl,
                extras.GetValueOrDefault(flavor.Id, [])))
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<PizzaFlavorDto>>(result);
    }

    public Task<IReadOnlyCollection<ServiceCallDto>> ListPendingServiceCallsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var calls = context.ServiceCalls
            .Where(call => call.Status == ServiceCallStatus.Pending ||
                           call.Status == ServiceCallStatus.Acknowledged ||
                           call.Status == ServiceCallStatus.InProgress)
            .OrderBy(call => call.CreatedAt)
            .ToArray();
        var links = context.TableSessionTables
            .Where(link => link.UnlinkedAt == null)
            .ToArray()
            .GroupBy(link => link.TableSessionId)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(link => link.IsPrimary).ThenByDescending(link => link.LinkedAt).First());
        var tables = context.RestaurantTables.ToDictionary(table => table.Id);
        var callTypes = context.ServiceCallTypes.ToDictionary(callType => callType.Id);
        var employees = context.Employees.ToDictionary(employee => employee.Id, employee => employee.DisplayName);
        var result = calls.Select(call =>
        {
            if (!links.TryGetValue(call.TableSessionId, out var link) ||
                !tables.TryGetValue(link.RestaurantTableId, out var table) ||
                !callTypes.TryGetValue(call.ServiceCallTypeId, out var callType))
            {
                throw new BusinessRuleException(
                    "service_call.projection",
                    "Service call references an unavailable table or call type.");
            }

            return new ServiceCallDto(
                call.Id.Value,
                call.TableSessionId.Value,
                table.Id.Value,
                table.Number,
                table.Name,
                callType.Code,
                callType.Name,
                call.Status.ToString(),
                call.Details,
                call.AssignedEmployeeId.HasValue
                    ? employees.GetValueOrDefault(call.AssignedEmployeeId.Value)
                    : null,
                call.CreatedAt,
                call.AcknowledgedAt);
        }).ToArray();
        return Task.FromResult<IReadOnlyCollection<ServiceCallDto>>(result);
    }

    public Task<IReadOnlyCollection<KitchenTicketDto>> ListKitchenTicketsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var tickets = context.KitchenTickets
            .Where(ticket => ticket.Status != KitchenTicketStatus.Dispatched && ticket.Status != KitchenTicketStatus.Cancelled)
            .OrderBy(ticket => ticket.CreatedAt)
            .ToArray();
        var orders = context.Orders.ToDictionary(order => order.Id);
        var stations = context.ProductionStations.ToDictionary(station => station.Id);
        var ticketItems = context.KitchenTicketItems
            .ToArray()
            .GroupBy(item => item.KitchenTicketId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var orderItems = context.OrderItems.ToDictionary(item => item.Id);
        var pizzas = context.OrderItemPizzas.ToDictionary(pizza => pizza.Id);
        var modifiers = context.OrderItemModifiers
            .ToArray()
            .GroupBy(modifier => modifier.OrderItemId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var flavorNames = context.PizzaFlavors.ToDictionary(flavor => flavor.Id, flavor => flavor.Name);
        var result = tickets.Select(ticket => new KitchenTicketDto(
                ticket.Id.Value,
                ticket.TicketNumber,
                orders[ticket.OrderId].OrderNumber,
                stations[ticket.ProductionStationId].Name,
                stations[ticket.ProductionStationId].Code,
                ticket.Status.ToString(),
                ticket.CreatedAt,
                ticket.StartedAt,
                stations[ticket.ProductionStationId].TargetPreparationMinutes,
                ticketItems.GetValueOrDefault(ticket.Id, []).Length,
                string.Join(" · ", ticketItems.GetValueOrDefault(ticket.Id, []).Select(ticketItem =>
                {
                    var orderItem = orderItems[ticketItem.OrderItemId];
                    var itemModifiers = modifiers.GetValueOrDefault(ticketItem.OrderItemId, []);
                    var instructions = itemModifiers.Select(modifier =>
                    {
                        var flavor = modifier.PizzaFlavorId.HasValue
                            ? $" em {flavorNames.GetValueOrDefault(modifier.PizzaFlavorId.Value, "sabor selecionado")}"
                            : string.Empty;
                        return modifier.ModifierType == ModifierType.Remove
                            ? $"sem {modifier.NameSnapshot}{flavor}"
                            : $"+{modifier.Quantity:0.##}× {modifier.NameSnapshot}{flavor}";
                    });
                    var pizzaInstruction = pizzas.TryGetValue(ticketItem.OrderItemId, out var pizza)
                        ? pizza.CrustSelectionMode switch
                        {
                            CrustSelectionMode.Split =>
                                $"borda ½ {pizza.CrustNameSnapshot} + ½ {pizza.SecondCrustNameSnapshot}",
                            CrustSelectionMode.Whole => $"borda {pizza.CrustNameSnapshot}",
                            _ => null
                        }
                        : null;
                    var instructionText = string.Join(", ", instructions
                        .Prepend(pizzaInstruction)
                        .Where(instruction => !string.IsNullOrWhiteSpace(instruction)));
                    return $"{ticketItem.Quantity}× {orderItem.ProductNameSnapshot}" +
                           (string.IsNullOrWhiteSpace(instructionText) ? string.Empty : $" ({instructionText})");
                }))))
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<KitchenTicketDto>>(result);
    }

    private List<TableSummaryDto> ListTablesCore()
    {
        var areas = context.DiningAreas.ToDictionary(area => area.Id, area => area.Name);
        var sessions = context.TableSessions
            .Where(session => session.Status != TableSessionStatus.Closed && session.Status != TableSessionStatus.Cancelled)
            .ToDictionary(session => session.Id);
        var links = context.TableSessionTables
            .Where(link => link.UnlinkedAt == null)
            .ToArray()
            .Where(link => sessions.ContainsKey(link.TableSessionId))
            .GroupBy(link => link.RestaurantTableId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(link => link.LinkedAt).First());
        var calls = context.ServiceCalls
            .Where(call => call.Status == ServiceCallStatus.Pending)
            .Select(call => call.TableSessionId)
            .ToHashSet();
        var bills = context.Bills
            .Where(bill => bill.TableSessionId != null && bill.Status != BillStatus.Paid && bill.Status != BillStatus.Cancelled)
            .ToArray()
            .GroupBy(bill => bill.TableSessionId!.Value)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(bill => bill.RequestedAt).First());
        var orders = context.Orders
            .Where(order => order.TableSessionId != null && order.Status != OrderStatus.Cancelled)
            .ToArray()
            .GroupBy(order => order.TableSessionId!.Value)
            .ToDictionary(group => group.Key, group => group.Sum(order => order.Total.Amount));

        return context.RestaurantTables
            .Where(table => table.IsActive)
            .OrderBy(table => table.DisplayOrder)
            .ThenBy(table => table.Number)
            .ToArray()
            .Select(table =>
            {
                links.TryGetValue(table.Id, out var link);
                var session = link is null ? null : sessions[link.TableSessionId];
                var status = ResolveTableStatus(session, calls, bills);
                return new TableSummaryDto(
                    table.Id.Value,
                    table.Number,
                    table.Name,
                    table.Capacity,
                    areas.GetValueOrDefault(table.DiningAreaId, "Sem área"),
                    status,
                    session?.GuestCount,
                    session?.OpenedAt,
                    session is null ? 0 : orders.GetValueOrDefault(session.Id),
                    session is not null && calls.Contains(session.Id));
            })
            .ToList();
    }

    private static string ResolveTableStatus(
        TableSession? session,
        IReadOnlySet<ProjetoPizza.Domain.SharedKernel.TableSessionId> calls,
        IReadOnlyDictionary<ProjetoPizza.Domain.SharedKernel.TableSessionId, Bill> bills)
    {
        if (session is null)
        {
            return "Livre";
        }

        if (bills.TryGetValue(session.Id, out var bill) &&
            bill.Status == BillStatus.PaymentInProgress &&
            bill.RemainingAmount.Amount > 0)
        {
            return "Pagamento pendente";
        }

        if (session.Status == TableSessionStatus.BillRequested ||
            (bills.TryGetValue(session.Id, out bill) && bill.Status == BillStatus.Requested))
        {
            return "Conta solicitada";
        }

        return calls.Contains(session.Id) ? "Chamando" : "Ocupada";
    }

    private (DateTimeOffset Start, DateTimeOffset End) GetCurrentBusinessDayUtc()
    {
        var timezoneId = context.RestaurantUnits.Single().Timezone;
        TimeZoneInfo timezone;
        try
        {
            timezone = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new BusinessRuleException("restaurant_unit.timezone", "The configured restaurant timezone is invalid.");
        }
        catch (InvalidTimeZoneException)
        {
            throw new BusinessRuleException("restaurant_unit.timezone", "The configured restaurant timezone is invalid.");
        }

        var localNow = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timezone);
        var localStart = DateTime.SpecifyKind(localNow.Date, DateTimeKind.Unspecified);
        var localEnd = localStart.AddDays(1);
        return (
            new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localStart, timezone), TimeSpan.Zero),
            new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localEnd, timezone), TimeSpan.Zero));
    }
}
