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
            .RequireAuthorization("AdminOrOperationsAccess")
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
            service.ListCategoriesAsync(cancellationToken)).RequireAuthorization("AdminAccess");
        group.MapGet("/products", (IAdminQueryService service, CancellationToken cancellationToken) =>
            service.ListProductsAsync(cancellationToken)).RequireAuthorization("AdminAccess");
        group.MapGet("/pizza-sizes", (IAdminQueryService service, CancellationToken cancellationToken) =>
            service.ListPizzaSizesAsync(cancellationToken)).RequireAuthorization("AdminAccess");
        group.MapGet("/pizza-flavors", (IAdminQueryService service, CancellationToken cancellationToken) =>
            service.ListPizzaFlavorsAsync(cancellationToken)).RequireAuthorization("AdminAccess");
        group.MapGet("/service-calls", (IAdminQueryService service, CancellationToken cancellationToken) =>
            service.ListPendingServiceCallsAsync(cancellationToken));
        group.MapGet("/kitchen/tickets", (IAdminQueryService service, CancellationToken cancellationToken) =>
            service.ListKitchenTicketsAsync(cancellationToken));

        group.MapGet("/orders", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.ListOrdersAsync(cancellationToken));
        group.MapGet("/orders/{id:guid}/receipt", async (
            Guid id,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
        {
            var receipt = await service.GetOrderReceiptAsync(id, cancellationToken);
            return receipt is null ? Results.NotFound() : Results.Ok(receipt);
        });
        group.MapGet("/orders/catalog", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.GetOrderCatalogAsync(cancellationToken));
        group.MapGet("/customers", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.ListCustomersAsync(cancellationToken));
        group.MapGet("/pizza-crusts", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.ListPizzaCrustsAsync(cancellationToken)).RequireAuthorization("AdminAccess");
        group.MapGet("/ingredients", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.ListIngredientsAsync(cancellationToken)).RequireAuthorization("AdminAccess");
        group.MapGet("/settings/unit", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.GetUnitSettingsAsync(cancellationToken)).RequireAuthorization("AdminAccess");
        group.MapGet("/settings/operation", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.GetOperationSettingsAsync(cancellationToken));
        group.MapGet("/settings/pizza-rules", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.GetPizzaRulesAsync(cancellationToken)).RequireAuthorization("AdminAccess");
        group.MapGet("/cashier/registers", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.ListCashRegistersAsync(cancellationToken));
        group.MapGet("/cashier/current", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.GetCurrentCashShiftAsync(cancellationToken));
        group.MapGet("/cashier/history", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.ListCashShiftHistoryAsync(cancellationToken));
        group.MapGet("/payment-methods", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.ListPaymentMethodsAsync(cancellationToken));
        group.MapGet("/payments", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.ListPaymentsAsync(cancellationToken)).RequireAuthorization("AdminAccess");
        group.MapGet("/reports/financial", (
            DateTimeOffset? from,
            DateTimeOffset? to,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
        {
            var end = to ?? DateTimeOffset.UtcNow;
            var start = from ?? end.AddDays(-30);
            return service.GetFinancialReportAsync(start, end, cancellationToken);
        }).RequireAuthorization("AdminAccess");
        group.MapGet("/devices", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.ListDevicesAsync(cancellationToken)).RequireAuthorization("AdminAccess");
        group.MapGet("/print-jobs", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.ListPrintJobsAsync(cancellationToken)).RequireAuthorization("AdminAccess");
        group.MapGet("/audit", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.ListAuditLogsAsync(cancellationToken)).RequireAuthorization("AdminAccess");
        group.MapGet("/system/snapshot", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.CreateSystemSnapshotAsync(cancellationToken))
            .RequireAuthorization("AdminAccess");
        group.MapGet("/settings/dining-areas", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.ListDiningAreasAsync(cancellationToken)).RequireAuthorization("AdminAccess");
        group.MapGet("/settings/tables", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.ListRestaurantTableSettingsAsync(cancellationToken)).RequireAuthorization("AdminAccess");
        group.MapGet("/settings/production-stations", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.ListProductionStationsAsync(cancellationToken)).RequireAuthorization("AdminAccess");
        group.MapGet("/settings/service-call-types", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.ListServiceCallTypesAsync(cancellationToken)).RequireAuthorization("AdminAccess");
        group.MapGet("/inventory/items", (IAdminManagementService service, CancellationToken cancellationToken) =>
            service.ListInventoryItemsAsync(cancellationToken)).RequireAuthorization("AdminAccess");
        group.MapGet("/system/backups", (ISystemBackupService service, CancellationToken cancellationToken) =>
            service.ListAsync(cancellationToken)).RequireAuthorization("AdminAccess");
        group.MapGet("/system/backups/{fileName}", async (
            string fileName,
            ISystemBackupService service,
            CancellationToken cancellationToken) =>
        {
            var backup = await service.OpenReadAsync(fileName, cancellationToken);
            return backup is null
                ? Results.NotFound()
                : Results.File(backup.Stream, backup.ContentType, backup.FileName);
        }).RequireAuthorization("AdminAccess");
    }

    private static void MapWriteEndpoints(RouteGroupBuilder group)
    {
        group.MapPost("/orders", (
            CreateAdministrativeOrderCommand command,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.CreateOrderAsync(command, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");

        group.MapPost("/counter-orders/checkout", (
            CheckoutCounterOrderCommand command,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.CheckoutCounterOrderAsync(command, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");

        group.MapPost("/customers", (
            SaveCustomerCommand command,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.SaveCustomerAsync(command with { Id = null }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPut("/customers/{id:guid}", (
            Guid id,
            SaveCustomerCommand command,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
            service.SaveCustomerAsync(command with { Id = id }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");

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
        group.MapPost("/products/{id:guid}/image", async (
            Guid id,
            HttpRequest request,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
        {
            if (!request.HasFormContentType) return Results.BadRequest("Envie a imagem como multipart/form-data.");
            var form = await request.ReadFormAsync(cancellationToken);
            var image = form.Files.GetFile("image");
            if (image is null || image.Length == 0) return Results.BadRequest("Selecione uma imagem.");
            var altText = form["altText"].ToString();
            if (string.IsNullOrWhiteSpace(altText)) altText = "Foto do produto";
            await using var stream = image.OpenReadStream();
            var result = await service.SaveProductImageAsync(
                id, stream, image.ContentType, image.FileName, altText,
                GetIdentityUserId(user), cancellationToken);
            return Results.Ok(result);
        })
            .DisableAntiforgery()
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
        group.MapPost("/pizza-flavors/{id:guid}/image", async (
            Guid id,
            HttpRequest request,
            ClaimsPrincipal user,
            IAdminManagementService service,
            CancellationToken cancellationToken) =>
        {
            if (!request.HasFormContentType) return Results.BadRequest("Envie a imagem como multipart/form-data.");
            var form = await request.ReadFormAsync(cancellationToken);
            var image = form.Files.GetFile("image");
            if (image is null || image.Length == 0) return Results.BadRequest("Selecione uma imagem.");
            await using var stream = image.OpenReadStream();
            var result = await service.SavePizzaFlavorImageAsync(
                id, stream, image.ContentType, image.FileName,
                GetIdentityUserId(user), cancellationToken);
            return Results.Ok(result);
        })
            .DisableAntiforgery()
            .RequireAuthorization("AdminWrite");

        group.MapPost("/settings/dining-areas", (
            SaveDiningAreaCommand command, ClaimsPrincipal user, IAdminManagementService service, CancellationToken cancellationToken) =>
            service.SaveDiningAreaAsync(command with { Id = null }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPut("/settings/dining-areas/{id:guid}", (
            Guid id, SaveDiningAreaCommand command, ClaimsPrincipal user, IAdminManagementService service, CancellationToken cancellationToken) =>
            service.SaveDiningAreaAsync(command with { Id = id }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPost("/settings/tables", (
            SaveRestaurantTableCommand command, ClaimsPrincipal user, IAdminManagementService service, CancellationToken cancellationToken) =>
            service.SaveRestaurantTableAsync(command with { Id = null }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPut("/settings/tables/{id:guid}", (
            Guid id, SaveRestaurantTableCommand command, ClaimsPrincipal user, IAdminManagementService service, CancellationToken cancellationToken) =>
            service.SaveRestaurantTableAsync(command with { Id = id }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPost("/cashier/registers", (
            SaveCashRegisterCommand command, ClaimsPrincipal user, IAdminManagementService service, CancellationToken cancellationToken) =>
            service.SaveCashRegisterAsync(command with { Id = null }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPut("/cashier/registers/{id:guid}", (
            Guid id, SaveCashRegisterCommand command, ClaimsPrincipal user, IAdminManagementService service, CancellationToken cancellationToken) =>
            service.SaveCashRegisterAsync(command with { Id = id }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPost("/payment-methods", (
            SavePaymentMethodCommand command, ClaimsPrincipal user, IAdminManagementService service, CancellationToken cancellationToken) =>
            service.SavePaymentMethodAsync(command with { Id = null }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPut("/payment-methods/{id:guid}", (
            Guid id, SavePaymentMethodCommand command, ClaimsPrincipal user, IAdminManagementService service, CancellationToken cancellationToken) =>
            service.SavePaymentMethodAsync(command with { Id = id }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPost("/settings/production-stations", (
            SaveProductionStationCommand command, ClaimsPrincipal user, IAdminManagementService service, CancellationToken cancellationToken) =>
            service.SaveProductionStationAsync(command with { Id = null }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPut("/settings/production-stations/{id:guid}", (
            Guid id, SaveProductionStationCommand command, ClaimsPrincipal user, IAdminManagementService service, CancellationToken cancellationToken) =>
            service.SaveProductionStationAsync(command with { Id = id }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPost("/settings/service-call-types", (
            SaveServiceCallTypeCommand command, ClaimsPrincipal user, IAdminManagementService service, CancellationToken cancellationToken) =>
            service.SaveServiceCallTypeAsync(command with { Id = null }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPut("/settings/service-call-types/{id:guid}", (
            Guid id, SaveServiceCallTypeCommand command, ClaimsPrincipal user, IAdminManagementService service, CancellationToken cancellationToken) =>
            service.SaveServiceCallTypeAsync(command with { Id = id }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPost("/inventory/items", (
            SaveInventoryItemCommand command, ClaimsPrincipal user, IAdminManagementService service, CancellationToken cancellationToken) =>
            service.SaveInventoryItemAsync(command with { Id = null }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPut("/inventory/items/{id:guid}", (
            Guid id, SaveInventoryItemCommand command, ClaimsPrincipal user, IAdminManagementService service, CancellationToken cancellationToken) =>
            service.SaveInventoryItemAsync(command with { Id = id }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPost("/inventory/items/{id:guid}/adjustments", (
            Guid id, AdjustInventoryCommand command, ClaimsPrincipal user, IAdminManagementService service, CancellationToken cancellationToken) =>
            service.AdjustInventoryAsync(id, command, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPost("/system/backups", (
            ISystemBackupService service, CancellationToken cancellationToken) =>
            service.CreateAsync("manual", cancellationToken))
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
        group.MapPost("/orders/{id:guid}/print", (
            Guid id, ClaimsPrincipal user, IAdminManagementService service, CancellationToken cancellationToken) =>
            service.QueueOrderReceiptAsync(id, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("OperationsWrite");
        group.MapPost("/orders/{id:guid}/print/customer-receipt", (
            Guid id, ClaimsPrincipal user, IAdminManagementService service, CancellationToken cancellationToken) =>
            service.QueueOrderReceiptAsync(id, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("OperationsWrite");
        group.MapPost("/orders/{id:guid}/print/kitchen-command", (
            Guid id, ClaimsPrincipal user, IAdminManagementService service, CancellationToken cancellationToken) =>
            service.QueueKitchenCommandAsync(id, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("OperationsWrite");
        group.MapPost("/deliveries/{id:guid}/dispatch", (
            Guid id, DispatchDeliveryCommand command, ClaimsPrincipal user,
            IAdminManagementService service, CancellationToken cancellationToken) =>
            service.DispatchDeliveryAsync(id, command.DriverName, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("OperationsWrite");
        group.MapPost("/deliveries/{id:guid}/complete", (
            Guid id, ClaimsPrincipal user,
            IAdminManagementService service, CancellationToken cancellationToken) =>
            service.CompleteDeliveryAsync(id, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("OperationsWrite");
        group.MapPost("/deliveries/{id:guid}/fail", (
            Guid id, FailDeliveryCommand command, ClaimsPrincipal user,
            IAdminManagementService service, CancellationToken cancellationToken) =>
            service.FailDeliveryAsync(id, command.Reason, GetIdentityUserId(user), cancellationToken))
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
        group.MapPost("/printers", (
            SaveNetworkPrinterCommand command, ClaimsPrincipal user,
            IAdminManagementService service, CancellationToken cancellationToken) =>
            service.SaveNetworkPrinterAsync(command with { Id = null }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPut("/printers/{id:guid}", (
            Guid id, SaveNetworkPrinterCommand command, ClaimsPrincipal user,
            IAdminManagementService service, CancellationToken cancellationToken) =>
            service.SaveNetworkPrinterAsync(command with { Id = id }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPost("/printers/{id:guid}/test", (
            Guid id, ClaimsPrincipal user,
            IAdminManagementService service, CancellationToken cancellationToken) =>
            service.QueuePrinterTestAsync(id, GetIdentityUserId(user), cancellationToken))
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
            service.ListUsersAsync(cancellationToken)).RequireAuthorization("AdminAccess");
        group.MapGet("/roles", (IIdentityAccessService service, CancellationToken cancellationToken) =>
            service.ListRolesAsync(cancellationToken)).RequireAuthorization("AdminAccess");
        group.MapPost("/users", (
            SaveUserCommand command,
            ClaimsPrincipal user,
            IIdentityAccessService service,
            CancellationToken cancellationToken) =>
            service.SaveUserAsync(command with { Id = null }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPut("/users/{id:guid}", (
            Guid id,
            SaveUserCommand command,
            ClaimsPrincipal user,
            IIdentityAccessService service,
            CancellationToken cancellationToken) =>
            service.SaveUserAsync(command with { Id = id }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPost("/roles", (
            SaveRoleCommand command,
            ClaimsPrincipal user,
            IIdentityAccessService service,
            CancellationToken cancellationToken) =>
            service.SaveRoleAsync(command with { Id = null }, GetIdentityUserId(user), cancellationToken))
            .RequireAuthorization("AdminWrite");
        group.MapPut("/roles/{id:guid}", (
            Guid id,
            SaveRoleCommand command,
            ClaimsPrincipal user,
            IIdentityAccessService service,
            CancellationToken cancellationToken) =>
            service.SaveRoleAsync(command with { Id = id }, GetIdentityUserId(user), cancellationToken))
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
