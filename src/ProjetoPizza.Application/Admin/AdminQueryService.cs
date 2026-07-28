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
        var today = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var orders = context.Orders.Where(order => order.CreatedAt >= today && order.Status != OrderStatus.Cancelled).ToArray();
        var tables = ListTablesCore();
        var recentOrders = orders
            .OrderByDescending(order => order.PlacedAt)
            .Take(5)
            .Select(order => new DashboardOrderDto(order.OrderNumber, order.SalesChannel.ToString(), order.Status.ToString(), order.Total.Amount, order.PlacedAt))
            .ToArray();

        var sales = orders.Where(order => order.Status == OrderStatus.Completed).Sum(order => order.Total.Amount);
        var completedCount = orders.Count(order => order.Status == OrderStatus.Completed);
        var result = new DashboardDto(
            sales,
            orders.Length,
            completedCount == 0 ? 0 : decimal.Round(sales / completedCount, 2),
            tables.Count(table => table.Status != "Livre"),
            tables.Count,
            orders.Count(order => order.Status == OrderStatus.InProduction),
            context.ServiceCalls.Count(call => call.Status == ServiceCallStatus.Pending),
            recentOrders);
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
            return Task.FromResult<TableDetailDto?>(new TableDetailDto(table, null, null, null, [], null, 0));
        }

        var session = context.TableSessions.Single(candidate => candidate.Id == link.TableSessionId);
        var orders = context.Orders
            .Where(order => order.TableSessionId == session.Id)
            .OrderByDescending(order => order.CreatedAt)
            .Select(order => new TableOrderDto(order.OrderNumber, order.SalesChannel.ToString(), order.Status.ToString(), order.Total.Amount, order.PlacedAt))
            .ToArray();
        var bill = context.Bills
            .Where(candidate => candidate.TableSessionId == session.Id && candidate.Status != BillStatus.Cancelled)
            .ToArray()
            .OrderByDescending(candidate => candidate.RequestedAt)
            .FirstOrDefault();
        return Task.FromResult<TableDetailDto?>(new TableDetailDto(
            table,
            session.Id.Value,
            session.SessionNumber,
            null,
            orders,
            bill?.Id.Value,
            bill?.RemainingAmount.Amount ?? table.CurrentTotal));
    }

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
        var result = context.Products
            .OrderBy(product => product.DisplayOrder)
            .ThenBy(product => product.Name)
            .ToArray()
            .Select(product => new ProductDto(
                product.Id.Value,
                product.CategoryId.Value,
                product.Sku,
                product.Name,
                product.ProductType.ToString(),
                product.BasePrice.Amount,
                product.IsActive,
                product.IsAvailable,
                product.IsFeatured))
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
                flavor.SoldOutReason))
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<PizzaFlavorDto>>(result);
    }

    public Task<IReadOnlyCollection<ServiceCallDto>> ListPendingServiceCallsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = context.ServiceCalls
            .Where(call => call.Status == ServiceCallStatus.Pending)
            .OrderBy(call => call.CreatedAt)
            .Select(call => new ServiceCallDto(call.Id.Value, call.TableSessionId.Value, call.Status.ToString(), call.Details, call.CreatedAt))
            .ToArray();
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
        var itemCounts = context.KitchenTicketItems.GroupBy(item => item.KitchenTicketId).ToDictionary(group => group.Key, group => group.Count());
        var result = tickets.Select(ticket => new KitchenTicketDto(
                ticket.Id.Value,
                ticket.TicketNumber,
                orders[ticket.OrderId].OrderNumber,
                stations[ticket.ProductionStationId].Name,
                ticket.Status.ToString(),
                ticket.CreatedAt,
                itemCounts.GetValueOrDefault(ticket.Id)))
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
            .Where(bill => bill.Status != BillStatus.Paid && bill.Status != BillStatus.Cancelled)
            .ToArray()
            .GroupBy(bill => bill.TableSessionId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(bill => bill.RequestedAt).First());
        var orders = context.Orders
            .Where(order => order.TableSessionId != null && order.Status != OrderStatus.Cancelled)
            .ToArray()
            .GroupBy(order => order.TableSessionId!.Value)
            .ToDictionary(group => group.Key, group => group.Sum(order => order.Total.Amount));

        return context.RestaurantTables
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
}
