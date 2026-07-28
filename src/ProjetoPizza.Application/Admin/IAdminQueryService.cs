namespace ProjetoPizza.Application.Admin;

public interface IAdminQueryService
{
    Task<DashboardDto> GetDashboardAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<TableSummaryDto>> ListTablesAsync(CancellationToken cancellationToken);
    Task<TableDetailDto?> GetTableAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<CategoryDto>> ListCategoriesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ProductDto>> ListProductsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PizzaSizeDto>> ListPizzaSizesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<PizzaFlavorDto>> ListPizzaFlavorsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ServiceCallDto>> ListPendingServiceCallsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<KitchenTicketDto>> ListKitchenTicketsAsync(CancellationToken cancellationToken);
}
