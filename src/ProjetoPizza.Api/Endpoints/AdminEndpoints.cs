using System.Security.Claims;
using ProjetoPizza.Application.Admin;
using ProjetoPizza.Application.Identity;
using ProjetoPizza.Api.Realtime;

namespace ProjetoPizza.Api.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/admin")
            .WithTags("Admin")
            .RequireAuthorization("AdminAccess")
            .AddEndpointFilter<AdminRealtimeFilter>();

        MapReadEndpoints(group);
        MapWriteEndpoints(group);
        MapIdentityEndpoints(group);
        return endpoints;
    }

    private static void MapReadEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/dashboard", (IAdminQueryService service, CancellationToken cancellationToken) =>
            service.GetDashboardAsync(cancellationToken));
        group.MapGet("/tables", (IAdminQueryService service, CancellationToken cancellationToken) =>
            service.ListTablesAsync(cancellationToken));
        group.MapGet("/tables/{id:guid}", async (
            Guid id,
            IAdminQueryService service,
            CancellationToken cancellationToken) =>
        {
            var table = await service.GetTableAsync(id, cancellationToken);
            return table is null ? Results.NotFound() : Results.Ok(table);
        });
        group.MapGet("/categories", (IAdminQueryService service, CancellationToken cancellationToken) =>
            service.ListCategoriesAsync(cancellationToken));
        group.MapGet("/products", (IAdminQueryService service, CancellationToken cancellationToken) =>
            service.ListProductsAsync(cancellationToken));
        group.MapGet("/pizza-sizes", (IAdminQueryService service, CancellationToken cancellationToken) =>
            service.ListPizzaSizesAsync(cancellationToken));
        group.MapGet("/pizza-flavors", (IAdminQueryService service, CancellationToken cancellationToken) =>
            service.ListPizzaFlavorsAsync(cancellationToken));
        group.MapGet("/service-calls", (IAdminQueryService service, CancellationToken cancellationToken) =>
            service.ListPendingServiceCallsAsync(cancellationToken));
        group.MapGet("/kitchen/tickets", (IAdminQueryService service, CancellationToken cancellationToken) =>
            service.ListKitchenTicketsAsync(cancellationToken));

        group.MapGet("/orders", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.ListOrdersAsync(cancellationToken));
        group.MapGet("/pizza-crusts", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.ListPizzaCrustsAsync(cancellationToken));
        group.MapGet("/ingredients", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.ListIngredientsAsync(cancellationToken));
        group.MapGet("/settings/unit", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.GetUnitSettingsAsync(cancellationToken));
        group.MapGet("/settings/operation", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.GetOperationSettingsAsync(cancellationToken));
        group.MapGet("/settings/pizza-rules", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.GetPizzaRulesAsync(cancellationToken));
        group.MapGet("/cashier/registers", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.ListCashRegistersAsync(cancellationToken));
        group.MapGet("/cashier/current", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.GetCurrentCashShiftAsync(cancellationToken));
        group.MapGet("/payment-methods", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.ListPaymentMethodsAsync(cancellationToken));
        group.MapGet("/payments", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.ListPaymentsAsync(cancellationToken));
        group.MapGet("/reports/financial", (
            DateTimeOffset? from,
            DateTimeOffset? to,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
        {
            var end = to ?? DateTimeOffset.UtcNow;
            var start = from ?? end.AddDays(-30);
            return service.GetFinancialReportAsync(start, end, cancellationToken);
        });
        group.MapGet("/devices", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.ListDevicesAsync(cancellationToken));
        group.MapGet("/audit", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.ListAuditLogsAsync(cancellationToken));
        group.MapGet("/system/snapshot", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.CreateSystemSnapshotAsync(cancellationToken));
    }

    private static void MapWriteEndpoints(RouteGroupBuilder group)
    {
        group.MapPut("/settings/unit", async (
            UpdateUnitCommand command,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
        {
            await service.UpdateUnitAsync(command, GetIdentityUserId(user), cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization("AdminWrite");

        group.MapPut("/settings/operation", async (
            UpdateOperationSettingsCommand command,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
        {
            await service.UpdateOperationSettingsAsync(command, GetIdentityUserId(user), cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization("AdminWrite");

        group.MapPut("/settings/pizza-rules", async (
            UpdatePizzaRulesCommand command,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
        {
            await service.UpdatePizzaRulesAsync(command, GetIdentityUserId(user), cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization("AdminWrite");

        group.MapPost("/categories", (
            SaveCategoryCommand command,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.SaveCategoryAsync(command with { Id = null }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPut("/categories/{id:guid}", (
            Guid id,
            SaveCategoryCommand command,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.SaveCategoryAsync(command with { Id = id }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");

        group.MapPost("/products", (
            SaveProductCommand command,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.SaveProductAsync(command with { Id = null }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPut("/products/{id:guid}", (
            Guid id,
            SaveProductCommand command,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.SaveProductAsync(command with { Id = id }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");

        group.MapPost("/pizza-sizes", (
            SavePizzaSizeCommand command,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.SavePizzaSizeAsync(command with { Id = null }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPut("/pizza-sizes/{id:guid}", (
            Guid id,
            SavePizzaSizeCommand command,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.SavePizzaSizeAsync(command with { Id = id }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");

        group.MapPost("/pizza-crusts", (
            SavePizzaCrustCommand command,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.SavePizzaCrustAsync(command with { Id = null }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPut("/pizza-crusts/{id:guid}", (
            Guid id,
            SavePizzaCrustCommand command,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.SavePizzaCrustAsync(command with { Id = id }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");

        group.MapPost("/ingredients", (
            SaveIngredientCommand command,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.SaveIngredientAsync(command with { Id = null }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPut("/ingredients/{id:guid}", (
            Guid id,
            SaveIngredientCommand command,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.SaveIngredientAsync(command with { Id = id }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");

        group.MapPost("/pizza-flavors", (
            SavePizzaFlavorCommand command,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.SavePizzaFlavorAsync(command with { Id = null }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPut("/pizza-flavors/{id:guid}", (
            Guid id,
            SavePizzaFlavorCommand command,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.SavePizzaFlavorAsync(command with { Id = id }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");

        group.MapPost("/tables/{id:guid}/open", (
            Guid id,
            OpenTableCommand command,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.OpenTableAsync(command with { TableId = id }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("OperationsWrite");
        group.MapPost("/table-sessions/{id:guid}/request-bill", (
            Guid id,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.RequestBillAsync(id, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("OperationsWrite");
        group.MapPost("/orders/{id:guid}/transitions/{transition}", (
            Guid id,
            string transition,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.TransitionOrderAsync(id, transition, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("OperationsWrite");
        group.MapPost("/kitchen/tickets/{id:guid}/transitions/{transition}", (
            Guid id,
            string transition,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.TransitionKitchenTicketAsync(id, transition, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("OperationsWrite");
        group.MapPost("/service-calls/{id:guid}/acknowledge", (
            Guid id,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.AcknowledgeServiceCallAsync(id, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("OperationsWrite");
        group.MapPost("/service-calls/{id:guid}/complete", (
            Guid id,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.CompleteServiceCallAsync(id, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("OperationsWrite");
        group.MapPost("/payments", (
            RecordPaymentCommand command,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.RecordPaymentAsync(command, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("OperationsWrite");
        group.MapPost("/payments/split", (
            RecordSplitPaymentCommand command,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.RecordSplitPaymentAsync(command, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("OperationsWrite");
        group.MapPost("/cashier/open", (
            OpenCashShiftCommand command,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.OpenCashShiftAsync(command, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("OperationsWrite");
        group.MapPost("/cashier/movements", (
            RegisterCashMovementCommand command,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.RegisterCashMovementAsync(command, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("OperationsWrite");
        group.MapPost("/cashier/close", (
            CloseCashShiftCommand command,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.CloseCashShiftAsync(command, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("OperationsWrite");
        group.MapPut("/devices/{id:guid}", (
            Guid id,
            UpdateDeviceCommand command,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.UpdateDeviceAsync(id, command, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPost("/devices/tablets", (
            CreateCustomerTabletCommand command,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.CreateCustomerTabletAsync(command, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPost("/devices/{id:guid}/provision", (
            Guid id,
            ProvisionCustomerTabletCommand command,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.ProvisionCustomerTabletAsync(id, command, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
    }

    private static void MapIdentityEndpoints(RouteGroupBuilder group)
    {
        group.MapGet("/users", (IIdentityAccessService service, CancellationToken cancellationToken) =>
            service.ListUsersAsync(cancellationToken));
        group.MapGet("/roles", (IIdentityAccessService service, CancellationToken cancellationToken) =>
            service.ListRolesAsync(cancellationToken));
        group.MapPost("/users", (
            SaveUserCommand command,
            IIdentityAccessService service,
            CancellationToken cancellationToken) =>
            service.SaveUserAsync(command with { Id = null }, cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPut("/users/{id:guid}", (
            Guid id,
            SaveUserCommand command,
            IIdentityAccessService service,
            CancellationToken cancellationToken) =>
            service.SaveUserAsync(command with { Id = id }, cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPost("/roles", (
            SaveRoleCommand command,
            IIdentityAccessService service,
            CancellationToken cancellationToken) =>
            service.SaveRoleAsync(command with { Id = null }, cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPut("/roles/{id:guid}", (
            Guid id,
            SaveRoleCommand command,
            IIdentityAccessService service,
            CancellationToken cancellationToken) =>
            service.SaveRoleAsync(command with { Id = id }, cancellationToken))
            .RequireAuthorization("AdminWrite");
    }

    private static Guid GetIdentityUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id)
            ? id
            : throw new UnauthorizedAccessException("Authenticated user identifier is missing.");
    }
}
