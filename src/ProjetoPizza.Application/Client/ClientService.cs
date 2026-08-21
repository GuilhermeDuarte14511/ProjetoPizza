using System.Security.Cryptography;
using System.Text;
using ProjetoPizza.Application.Abstractions.Persistence;
using ProjetoPizza.Application.Devices;
using ProjetoPizza.Application.Inventory;
using ProjetoPizza.Application.Customers;
using ProjetoPizza.Domain.Audit;
using ProjetoPizza.Domain.Billing;
using ProjetoPizza.Domain.Cashier;
using ProjetoPizza.Domain.Catalog;
using ProjetoPizza.Domain.Core;
using ProjetoPizza.Domain.Customers;
using ProjetoPizza.Domain.Devices;
using ProjetoPizza.Domain.Dining;
using ProjetoPizza.Domain.Ordering;
using ProjetoPizza.Domain.Production;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Application.Client;

public sealed class ClientService(
    IProjetoPizzaDbContext context,
    IOperationNumberGenerator? numberGenerator = null) : IClientService
{
    public async Task<ClientActivationDto> ActivateAsync(
        ActivateClientSessionCommand command,
        CancellationToken cancellationToken)
    {
        DeviceProvisioning? provisioning = null;
        Device? device;
        if (!string.IsNullOrWhiteSpace(command.ProvisioningToken))
        {
            var provisioningToken = Guard.Required(
                command.ProvisioningToken,
                nameof(command.ProvisioningToken),
                128);
            var tokenHash = DeviceProvisioningTokens.Hash(provisioningToken);
            provisioning = context.DeviceProvisionings.SingleOrDefault(candidate =>
                candidate.TokenHash == tokenHash &&
                candidate.ConsumedAt == null &&
                candidate.RevokedAt == null &&
                candidate.ExpiresAt > DateTimeOffset.UtcNow);
            device = provisioning is null
                ? null
                : context.Devices.SingleOrDefault(candidate =>
                    candidate.Id == provisioning.DeviceId &&
                    candidate.DeviceType == DeviceType.CustomerTablet);
        }
        else
        {
            var deviceCode = Guard.Required(command.DeviceCode, nameof(command.DeviceCode), 100);
            device = context.Devices.SingleOrDefault(candidate =>
                candidate.SerialNumber == deviceCode &&
                candidate.DeviceType == DeviceType.CustomerTablet);
        }

        if (device is null || device.IsLocked)
        {
            throw new BusinessRuleException("client.device_unavailable", "Customer tablet is unavailable.");
        }

        if (!device.LinkedTableId.HasValue)
        {
            throw new BusinessRuleException("client.device_not_linked", "Customer tablet is not linked to a table.");
        }

        var tableSessionId = FindActiveTableSessionId(device.LinkedTableId.Value);

        foreach (var activeDeviceSession in context.DeviceSessions
                     .Where(candidate => candidate.DeviceId == device.Id && candidate.EndedAt == null)
                     .ToArray())
        {
            activeDeviceSession.End("Replaced by a new tablet activation.");
        }

        var token = CreateToken();
        var deviceSession = new DeviceSession(
            DeviceSessionId.New(),
            device.Id,
            HashToken(token),
            tableSessionId);
        provisioning?.Consume();
        context.Add(deviceSession);
        await context.SaveChangesAsync(cancellationToken);

        var session = await ValidateSessionAsync(token, cancellationToken)
            ?? throw new BusinessRuleException("client.session_activation", "Tablet session could not be activated.");
        return new ClientActivationDto(token, await GetBootstrapAsync(session, cancellationToken));
    }

    public Task<ClientSessionContext?> ValidateSessionAsync(
        string token,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(token) || token.Length > 512)
        {
            return Task.FromResult<ClientSessionContext?>(null);
        }

        var tokenHash = HashToken(token);
        var deviceSession = context.DeviceSessions.SingleOrDefault(candidate =>
            candidate.SessionTokenHash == tokenHash);
        if (deviceSession is null || !deviceSession.IsAvailableAt(DateTimeOffset.UtcNow))
        {
            return Task.FromResult<ClientSessionContext?>(null);
        }

        var device = context.Devices.SingleOrDefault(candidate =>
            candidate.Id == deviceSession.DeviceId &&
            candidate.DeviceType == DeviceType.CustomerTablet &&
            !candidate.IsLocked &&
            candidate.LinkedTableId.HasValue);
        if (device is null)
        {
            return Task.FromResult<ClientSessionContext?>(null);
        }

        if (!device.LinkedTableId.HasValue)
        {
            return Task.FromResult<ClientSessionContext?>(null);
        }

        var table = context.RestaurantTables.SingleOrDefault(candidate =>
            candidate.Id == device.LinkedTableId.Value &&
            candidate.UnitId == device.UnitId);
        if (table is null)
        {
            return Task.FromResult<ClientSessionContext?>(null);
        }

        var tableSessionId = ResolveReadableTableSessionId(deviceSession.TableSessionId, table.Id)
            ?? FindActiveTableSessionId(table.Id);
        return Task.FromResult<ClientSessionContext?>(new ClientSessionContext(
            deviceSession.Id.Value,
            device.Id.Value,
            tableSessionId?.Value,
            device.UnitId.Value,
            table.Id.Value,
            table.Number));
    }

    public Task<ClientBootstrapDto> GetBootstrapAsync(
        ClientSessionContext session,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(CreateBootstrap(session));
    }

    public Task<ClientStateDto> GetStateAsync(
        ClientSessionContext session,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(CreateState(session));
    }

    public async Task<ClientLoyaltyLookupDto?> LookupLoyaltyAsync(ClientSessionContext session,
        ClientLoyaltyLookupCommand command, CancellationToken cancellationToken)
    {
        var phone = Customer.NormalizePhone(command.Phone);
        var customer = context.Customers.SingleOrDefault(candidate => candidate.UnitId == new RestaurantUnitId(session.RestaurantUnitId) &&
            candidate.Phone == phone && candidate.BirthDate == command.BirthDate && candidate.IsActive);
        if (customer is null) return null;
        LoyaltyProgramService.ExpirePoints(context, customer);
        var eligible = new Money(command.OrderAmount);
        var couponDiscount = Money.Zero();
        if (!string.IsNullOrWhiteSpace(command.CouponCode))
        {
            var code = command.CouponCode.Trim().ToUpperInvariant();
            var coupon = context.PromotionCoupons.SingleOrDefault(candidate => candidate.UnitId == customer.UnitId && candidate.Code == code)
                ?? throw new BusinessRuleException("coupon.not_found", "Coupon was not found.");
            couponDiscount = coupon.CalculateDiscount(eligible, DateTimeOffset.UtcNow);
        }
        var loyaltyDiscount = Money.Zero();
        if (command.LoyaltyPoints > 0)
        {
            if (command.LoyaltyPoints > customer.LoyaltyPoints) throw new BusinessRuleException("loyalty.balance", "Insufficient loyalty point balance.");
            loyaltyDiscount = LoyaltyProgramService.GetOrCreateSettings(context, customer.UnitId)
                .CalculateRedemption(command.LoyaltyPoints, eligible - couponDiscount);
        }
        await context.SaveChangesAsync(cancellationToken);
        return new ClientLoyaltyLookupDto(customer.Name, customer.LoyaltyPoints, customer.LoyaltyPointsExpireAt,
            couponDiscount.Amount, loyaltyDiscount.Amount, couponDiscount.Amount + loyaltyDiscount.Amount);
    }

    public async Task<ClientBootstrapDto> StartTableSessionAsync(
        ClientSessionContext session,
        StartClientTableSessionCommand command,
        CancellationToken cancellationToken)
    {
        if (command.GuestCount is < 1 or > 50)
        {
            throw new BusinessRuleException("client.guest_count", "Guest count must be between one and fifty.");
        }

        var deviceSession = GetAvailableDeviceSession(session.DeviceSessionId);
        var tableId = new RestaurantTableId(session.TableId);
        var table = context.RestaurantTables.Single(candidate => candidate.Id == tableId);
        table.EnsureCanOpenSession();

        var existingSessionId = FindActiveTableSessionId(tableId);
        if (existingSessionId.HasValue)
        {
            deviceSession.BindToTableSession(existingSessionId.Value);
            await context.SaveChangesAsync(cancellationToken);
            return CreateBootstrap(session with { TableSessionId = existingSessionId.Value.Value });
        }

        var unitId = new RestaurantUnitId(session.RestaurantUnitId);
        var settings = context.OperationSettings
            .ToArray()
            .Single(candidate => candidate.UnitId == unitId);
        var sessionNumber = numberGenerator is null
            ? context.TableSessions.Any() ? context.TableSessions.Max(candidate => candidate.SessionNumber) + 1 : 1
            : await numberGenerator.NextTableSessionNumberAsync(cancellationToken);
        var tableSession = TableSession.OpenFromDevice(
            TableSessionId.New(),
            unitId,
            sessionNumber,
            command.GuestCount,
            new DeviceId(session.DeviceId),
            settings.ServiceFeePercentage,
            [table]);

        context.Add(tableSession);
        deviceSession.BindToTableSession(tableSession.Id);
        context.Add(new AuditLog(
            AuditLogId.New(),
            unitId,
            "Dining",
            "OpenFromTablet",
            nameof(TableSession),
            tableSession.Id.Value.ToString()));
        await context.SaveChangesAsync(cancellationToken);
        return CreateBootstrap(session with { TableSessionId = tableSession.Id.Value });
    }

    public async Task<ClientBootstrapDto> CompleteTableSessionAsync(
        ClientSessionContext session,
        CancellationToken cancellationToken)
    {
        var tableSessionId = RequireTableSessionId(session);
        var tableSession = context.TableSessions.Single(candidate => candidate.Id == tableSessionId);
        var isPaidAndClosed = tableSession.Status == TableSessionStatus.Closed &&
            context.Bills.Any(candidate => candidate.TableSessionId == tableSessionId && candidate.Status == BillStatus.Paid);
        if (!isPaidAndClosed)
        {
            throw new BusinessRuleException(
                "client.table_session_not_completed",
                "Only a paid and closed table session can return the tablet to standby.");
        }

        GetAvailableDeviceSession(session.DeviceSessionId).ClearTableSession();
        await context.SaveChangesAsync(cancellationToken);
        return CreateBootstrap(session with { TableSessionId = null });
    }

    public async Task UpdateTelemetryAsync(
        ClientSessionContext session,
        UpdateClientTelemetryCommand command,
        CancellationToken cancellationToken)
    {
        var deviceId = new DeviceId(session.DeviceId);
        var device = context.Devices.Single(candidate =>
            candidate.Id == deviceId &&
            candidate.DeviceType == DeviceType.CustomerTablet);

        device.UpdateStatus(
            DeviceStatus.Online,
            command.BatteryPercentage,
            command.IsCharging,
            command.NetworkStatus,
            command.IpAddress,
            command.AppVersion);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task LogoutAsync(ClientSessionContext session, CancellationToken cancellationToken)
    {
        GetAvailableDeviceSession(session.DeviceSessionId).End("Logged out from the tablet.");
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ClientOrderDto> SubmitOrderAsync(
        ClientSessionContext session,
        SubmitClientOrderCommand command,
        CancellationToken cancellationToken)
    {
        var tableSessionId = RequireTableSessionId(session);
        var tableSession = context.TableSessions.Single(candidate => candidate.Id == tableSessionId);
        if (tableSession.Status != TableSessionStatus.Open)
        {
            throw new BusinessRuleException("client.order_session", "Orders can only be submitted while the table session is open.");
        }

        if (command.RequestId == Guid.Empty)
        {
            throw new BusinessRuleException("client.order_request_id", "Order request identifier is required.");
        }

        var requestedOrderId = new OrderId(command.RequestId);
        var existing = context.Orders.SingleOrDefault(candidate => candidate.Id == requestedOrderId);
        if (existing is not null)
        {
            if (existing.TableSessionId != tableSessionId)
            {
                throw new BusinessRuleException("client.order_request_conflict", "Order request identifier is already in use.");
            }

            return CreateOrder(existing);
        }

        var requestedItems = command.Items?.ToArray() ?? [];
        if (requestedItems.Length is < 1 or > 30)
        {
            throw new BusinessRuleException("client.order_items", "An order must contain between one and thirty items.");
        }

        var operationSettings = context.OperationSettings
            .ToArray()
            .Single(candidate => candidate.UnitId == tableSession.UnitId);
        if (!operationSettings.AllowOrdersWithoutOpenCashShift &&
            !context.CashShifts.Any(shift => shift.Status == CashShiftStatus.Open))
        {
            throw new BusinessRuleException("client.cash_shift", "Orders are unavailable while the cash register is closed.");
        }

        var orderNumber = numberGenerator is null
            ? context.Orders.Any() ? context.Orders.Max(candidate => candidate.OrderNumber) + 1 : 1
            : await numberGenerator.NextOrderNumberAsync(cancellationToken);
        var order = new Order(
            requestedOrderId,
            tableSession.UnitId,
            orderNumber,
            SalesChannel.DineIn,
            FulfillmentType.DineIn,
            createdByDeviceId: new DeviceId(session.DeviceId),
            tableSessionId: tableSession.Id);

        var stationItems = new Dictionary<string, List<OrderItem>>();
        foreach (var requestedItem in requestedItems)
        {
            AddOrderItem(order, requestedItem, tableSession.UnitId, stationItems);
        }

        Customer? loyaltyCustomer = null;
        if (!string.IsNullOrWhiteSpace(command.CustomerPhone) || command.CustomerBirthDate.HasValue || command.LoyaltyPoints > 0)
        {
            if (string.IsNullOrWhiteSpace(command.CustomerPhone) || !command.CustomerBirthDate.HasValue)
                throw new BusinessRuleException("loyalty.identification", "Phone and birth date are required to identify the loyalty customer.");
            var phone = Customer.NormalizePhone(command.CustomerPhone);
            loyaltyCustomer = context.Customers.SingleOrDefault(candidate =>
                candidate.UnitId == tableSession.UnitId && candidate.Phone == phone &&
                candidate.BirthDate == command.CustomerBirthDate.Value && candidate.IsActive)
                ?? throw new BusinessRuleException("loyalty.customer_not_found", "Loyalty customer was not found.");
            order.AssignCustomer(loyaltyCustomer.Id, loyaltyCustomer.Name);
        }

        LoyaltyProgramService.ApplyBenefits(context, order, loyaltyCustomer, Money.Zero(), command.CouponCode, command.LoyaltyPoints);

        InventoryAllocation.Reserve(context, order, requestedItems);
        order.Submit();
        context.Add(order);
        await CreateKitchenTicketsAsync(order, stationItems, cancellationToken);
        context.Add(new AuditLog(
            AuditLogId.New(),
            order.UnitId,
            "Ordering",
            "SubmitFromTablet",
            nameof(Order),
            order.Id.Value.ToString()));
        await context.SaveChangesAsync(cancellationToken);
        return CreateOrder(order);
    }

    public async Task<ClientCommandResultDto> CreateServiceCallAsync(
        ClientSessionContext session,
        CreateClientServiceCallCommand command,
        CancellationToken cancellationToken)
    {
        var tableSessionId = RequireTableSessionId(session);
        var tableSession = context.TableSessions.Single(candidate => candidate.Id == tableSessionId);
        if (!IsActiveTableSession(tableSession))
        {
            throw new BusinessRuleException("client.service_call_session", "Service calls require an active table session.");
        }

        var serviceCallTypeId = new ServiceCallTypeId(command.ServiceCallTypeId);
        var callType = context.ServiceCallTypes.SingleOrDefault(candidate =>
            candidate.Id == serviceCallTypeId && candidate.IsActive);
        if (callType is null)
        {
            throw new BusinessRuleException("client.service_call_type", "The selected service call type is unavailable.");
        }

        var hasOpenDuplicate = context.ServiceCalls.Any(candidate =>
            candidate.TableSessionId == tableSession.Id &&
            candidate.ServiceCallTypeId == callType.Id &&
            candidate.Status != ServiceCallStatus.Completed &&
            candidate.Status != ServiceCallStatus.Cancelled);
        if (hasOpenDuplicate)
        {
            throw new BusinessRuleException("client.service_call_duplicate", "There is already an open call for this reason.");
        }

        var serviceCall = new ServiceCall(
            ServiceCallId.New(),
            tableSession.UnitId,
            tableSession.Id,
            callType.Id,
            new DeviceId(session.DeviceId),
            NormalizeOptionalText(command.Details, 500, nameof(command.Details)));
        context.Add(serviceCall);
        context.Add(new AuditLog(
            AuditLogId.New(),
            tableSession.UnitId,
            "Dining",
            "CallFromTablet",
            nameof(ServiceCall),
            serviceCall.Id.Value.ToString()));
        await context.SaveChangesAsync(cancellationToken);
        return new ClientCommandResultDto(serviceCall.Id.Value, serviceCall.Status.ToString());
    }

    public async Task<ClientBillDto> RequestBillAsync(
        ClientSessionContext session,
        RequestClientBillCommand command,
        CancellationToken cancellationToken)
    {
        var sessionId = RequireTableSessionId(session);
        var tableSession = context.TableSessions.Single(candidate => candidate.Id == sessionId);
        var bill = context.Bills
            .Where(candidate => candidate.TableSessionId == sessionId && candidate.Status != BillStatus.Cancelled)
            .ToArray()
            .OrderByDescending(candidate => candidate.RequestedAt)
            .FirstOrDefault();
        if (bill is null)
        {
            var subtotal = context.Orders
                .Where(order => order.TableSessionId == sessionId && order.Status != OrderStatus.Cancelled)
                .ToArray()
                .Sum(order => order.Total.Amount);
            if (subtotal <= 0)
            {
                throw new BusinessRuleException("bill.empty", "A bill requires at least one valid order.");
            }

            bill = new Bill(
                BillId.New(),
                tableSession.UnitId,
                tableSession.Id,
                new Money(subtotal),
                tableSession.ServiceFeePercentageSnapshot);
            context.Add(bill);
        }

        bill.Request(command.SplitCount);

        if (tableSession.Status == TableSessionStatus.Open)
        {
            tableSession.RequestBill();
        }

        context.Add(new AuditLog(
            AuditLogId.New(),
            tableSession.UnitId,
            "Billing",
            "RequestFromTablet",
            nameof(Bill),
            bill.Id.Value.ToString()));
        await context.SaveChangesAsync(cancellationToken);
        return ToBillDto(bill);
    }

    private ClientBootstrapDto CreateBootstrap(ClientSessionContext session)
    {
        var unitId = new RestaurantUnitId(session.RestaurantUnitId);
        var table = context.RestaurantTables.Single(candidate => candidate.Id == new RestaurantTableId(session.TableId));
        var unit = context.RestaurantUnits.Single(candidate => candidate.Id == unitId);
        var tableSession = session.TableSessionId.HasValue
            ? context.TableSessions.SingleOrDefault(candidate => candidate.Id == new TableSessionId(session.TableSessionId.Value))
            : null;

        return new ClientBootstrapDto(
            CreateSessionDto(session, unit.TradeName, table, tableSession),
            CreateCatalog(unitId),
            context.ServiceCallTypes
                .Where(callType => callType.IsActive)
                .OrderBy(callType => callType.Name)
                .Select(callType => new ClientServiceCallTypeDto(callType.Id.Value, callType.Code, callType.Name))
                .ToArray(),
            tableSession is null ? [] : CreateServiceCalls(tableSession.Id),
            tableSession is null ? [] : CreateOrders(tableSession.Id),
            tableSession is null ? EmptyBill() : CreateBill(tableSession));
    }

    private ClientStateDto CreateState(ClientSessionContext session)
    {
        var unitId = new RestaurantUnitId(session.RestaurantUnitId);
        var table = context.RestaurantTables.Single(candidate => candidate.Id == new RestaurantTableId(session.TableId));
        var unit = context.RestaurantUnits.Single(candidate => candidate.Id == unitId);
        var tableSession = session.TableSessionId.HasValue
            ? context.TableSessions.SingleOrDefault(candidate => candidate.Id == new TableSessionId(session.TableSessionId.Value))
            : null;

        return new ClientStateDto(
            CreateSessionDto(session, unit.TradeName, table, tableSession),
            tableSession is null ? [] : CreateServiceCalls(tableSession.Id),
            tableSession is null ? [] : CreateOrders(tableSession.Id),
            tableSession is null ? EmptyBill() : CreateBill(tableSession));
    }

    private ClientSessionDto CreateSessionDto(
        ClientSessionContext session,
        string restaurantName,
        RestaurantTable table,
        TableSession? tableSession)
    {
        var waiterName = tableSession?.PrimaryWaiterId.HasValue == true
            ? context.Employees
                .Where(employee => employee.Id == tableSession.PrimaryWaiterId.Value)
                .Select(employee => employee.DisplayName)
                .SingleOrDefault()
            : null;
        var clearAfterClose = context.OperationSettings
            .ToArray()
            .Single(candidate => candidate.UnitId == new RestaurantUnitId(session.RestaurantUnitId))
            .ClearTabletAfterTableClose;

        return new ClientSessionDto(
            session.DeviceId,
            tableSession?.Id.Value,
            restaurantName,
            table.Number,
            table.Name,
            tableSession?.GuestCount ?? 0,
            tableSession?.Status.ToString() ?? "Idle",
            waiterName,
            clearAfterClose);
    }

    private TableSessionId? ResolveReadableTableSessionId(
        TableSessionId? tableSessionId,
        RestaurantTableId tableId)
    {
        if (!tableSessionId.HasValue)
        {
            return null;
        }

        var tableSession = context.TableSessions.SingleOrDefault(candidate => candidate.Id == tableSessionId.Value);
        var belongsToTable = tableSession is not null && context.TableSessionTables.Any(link =>
            link.TableSessionId == tableSession.Id &&
            link.RestaurantTableId == tableId &&
            link.UnlinkedAt == null);
        if (!belongsToTable || tableSession is null)
        {
            return null;
        }

        if (IsActiveTableSession(tableSession))
        {
            return tableSession.Id;
        }

        var isRecentlyPaid = tableSession.Status == TableSessionStatus.Closed &&
            tableSession.ClosedAt >= DateTimeOffset.UtcNow.AddHours(-2) &&
            context.Bills.Any(candidate =>
                candidate.TableSessionId == tableSession.Id &&
                candidate.Status == BillStatus.Paid);
        return isRecentlyPaid ? tableSession.Id : null;
    }

    private TableSessionId? FindActiveTableSessionId(RestaurantTableId tableId)
    {
        var activeSessionIds = context.TableSessions
            .Where(IsActiveTableSession)
            .Select(candidate => candidate.Id)
            .ToHashSet();
        var tableSessionId = context.TableSessionTables
            .Where(link =>
                link.RestaurantTableId == tableId &&
                link.UnlinkedAt == null &&
                activeSessionIds.Contains(link.TableSessionId))
            .Select(link => link.TableSessionId)
            .SingleOrDefault();
        return tableSessionId == default ? null : tableSessionId;
    }

    private DeviceSession GetAvailableDeviceSession(Guid id)
    {
        var deviceSession = context.DeviceSessions.SingleOrDefault(candidate =>
            candidate.Id == new DeviceSessionId(id));
        if (deviceSession is null || !deviceSession.IsAvailableAt(DateTimeOffset.UtcNow))
        {
            throw new BusinessRuleException("client.session_unavailable", "Tablet access is no longer available.");
        }

        return deviceSession;
    }

    private static TableSessionId RequireTableSessionId(ClientSessionContext session) =>
        session.TableSessionId.HasValue
            ? new TableSessionId(session.TableSessionId.Value)
            : throw new BusinessRuleException("client.table_session_required", "Start a table session before using this feature.");

    private static ClientBillDto EmptyBill() =>
        new(null, "Idle", 0, 0, 0, 0, 0, 0, null, null);

    private ClientCatalogDto CreateCatalog(RestaurantUnitId unitId)
    {
        var categories = context.Categories
            .Where(category => category.UnitId == unitId && category.IsActive && category.IsVisibleOnTablet)
            .OrderBy(category => category.DisplayOrder)
            .ToArray();
        var productImages = context.ProductImages
            .OrderBy(image => image.DisplayOrder)
            .GroupBy(image => image.ProductId)
            .ToDictionary(group => group.Key, group => group.First().Url);
        var ingredients = context.Ingredients
            .Where(ingredient => ingredient.UnitId == unitId && ingredient.IsActive)
            .ToDictionary(ingredient => ingredient.Id);
        var productExtras = context.ProductExtras
            .Where(extra => extra.IsActive)
            .ToArray();
        var products = context.Products
            .Where(product => product.UnitId == unitId && product.IsActive && product.IsAvailable)
            .OrderBy(product => product.DisplayOrder)
            .ThenBy(product => product.Name)
            .ToArray()
            .Select(product => new ClientProductDto(
                product.Id.Value,
                product.CategoryId.Value,
                product.Name,
                product.Description,
                product.ProductType.ToString(),
                product.BasePrice.Amount,
                productImages.GetValueOrDefault(product.Id),
                product.IsFeatured,
                product.IsPopular,
                product.PreparationTimeMinutes,
                product.UsesCustomExtras,
                productExtras
                    .Where(link =>
                        link.ProductId == product.Id &&
                        ingredients.TryGetValue(link.IngredientId, out var ingredient) &&
                        ingredient.IsAvailableAsExtra)
                    .OrderBy(link => ingredients[link.IngredientId].Name)
                    .Select(link =>
                    {
                        var ingredient = ingredients[link.IngredientId];
                        return new ClientPizzaExtraDto(
                            ingredient.Id.Value,
                            ingredient.Name,
                            ingredient.Description,
                            link.Price.Amount,
                            link.MaxQuantity,
                            ingredient.IsAllergen,
                            ingredient.AllergenDescription);
                    })
                    .ToArray()))
            .ToArray();

        var settings = context.PizzaSettings
            .ToArray()
            .Single(candidate => candidate.UnitId == unitId);
        var sizes = context.PizzaSizes
            .Where(size => size.UnitId == unitId && size.IsActive)
            .OrderBy(size => size.DisplayOrder)
            .ToArray();
        var flavorPrices = context.PizzaFlavorPrices.ToArray();
        var flavorIngredients = context.PizzaFlavorIngredients.ToArray();
        var flavorExtras = context.PizzaFlavorExtras
            .Where(extra => extra.IsActive)
            .ToArray();
        var flavors = context.PizzaFlavors
            .Where(flavor => flavor.UnitId == unitId && flavor.IsActive)
            .OrderBy(flavor => flavor.DisplayOrder)
            .ThenBy(flavor => flavor.Name)
            .ToArray()
            .Select(flavor => new ClientPizzaFlavorDto(
                flavor.Id.Value,
                flavor.CategoryId.Value,
                flavor.Name,
                flavor.Description,
                flavor.FlavorType.ToString(),
                flavor.IsPremium,
                flavor.IsVegetarian,
                flavor.IsAvailable,
                flavor.SoldOutReason,
                flavor.ImageUrl,
                flavorPrices
                    .Where(price => price.PizzaFlavorId == flavor.Id)
                    .Select(price => new ClientPizzaFlavorPriceDto(
                        price.PizzaSizeId.Value,
                        price.Price.Amount,
                        price.AdditionalPrice.Amount,
                        price.IsAvailable))
                    .ToArray(),
                flavorIngredients
                    .Where(link => link.PizzaFlavorId == flavor.Id && ingredients.ContainsKey(link.IngredientId))
                    .OrderBy(link => link.DisplayOrder)
                    .Select(link =>
                    {
                        var ingredient = ingredients[link.IngredientId];
                        return new ClientIngredientDto(
                            ingredient.Id.Value,
                            ingredient.Name,
                            link.IsRemovable,
                            ingredient.IsAllergen,
                            ingredient.AllergenDescription);
                    })
                    .ToArray(),
                flavorExtras
                    .Where(link =>
                        link.PizzaFlavorId == flavor.Id &&
                        ingredients.TryGetValue(link.IngredientId, out var ingredient) &&
                        ingredient.IsAvailableAsExtra)
                    .OrderBy(link => ingredients[link.IngredientId].Name)
                    .Select(link =>
                    {
                        var ingredient = ingredients[link.IngredientId];
                        return new ClientPizzaExtraDto(
                            ingredient.Id.Value,
                            ingredient.Name,
                            ingredient.Description,
                            link.Price.Amount,
                            link.MaxQuantity,
                            ingredient.IsAllergen,
                            ingredient.AllergenDescription);
                    })
                    .ToArray()))
            .ToArray();
        var crustPrices = context.PizzaCrustPrices.ToArray();
        var crusts = context.PizzaCrusts
            .Where(crust => crust.UnitId == unitId && crust.IsActive)
            .OrderBy(crust => crust.DisplayOrder)
            .ToArray()
            .Select(crust => new ClientPizzaCrustDto(
                crust.Id.Value,
                crust.Name,
                crust.Description,
                crust.IsAvailable,
                crustPrices
                    .Where(price => price.PizzaCrustId == crust.Id)
                    .Select(price => new ClientPizzaCrustPriceDto(
                        price.PizzaSizeId.Value,
                        price.AdditionalPrice.Amount,
                        price.HalfAdditionalPrice.Amount))
                    .ToArray()))
            .ToArray();
        var operationSettings = context.OperationSettings
            .ToArray()
            .Single(candidate => candidate.UnitId == unitId);

        return new ClientCatalogDto(
            categories.Select(category => new ClientCategoryDto(
                category.Id.Value,
                category.Name,
                category.Slug,
                category.Icon,
                category.DisplayOrder)).ToArray(),
            products,
            new ClientPizzaCatalogDto(
                settings.GlobalMaxFlavors,
                settings.PricingPolicy.ToString(),
                settings.AllowSweetAndSavoryMix,
                settings.AllowExtrasPerFlavor,
                settings.AllowRepeatedFlavors,
                sizes.Select(size => new ClientPizzaSizeDto(
                    size.Id.Value,
                    size.Name,
                    size.ShortName,
                    size.Slices,
                    size.DiameterCm,
                    size.BasePrice.Amount,
                    size.MaxFlavors)).ToArray(),
                flavors,
                crusts,
                ingredients.Values
                    .Where(ingredient => ingredient.IsAvailableAsExtra)
                    .OrderBy(ingredient => ingredient.Name)
                    .Select(ingredient => new ClientPizzaExtraDto(
                        ingredient.Id.Value,
                        ingredient.Name,
                        ingredient.Description,
                        ingredient.ExtraPrice.Amount,
                        ingredient.MaxExtraQuantity,
                        ingredient.IsAllergen,
                        ingredient.AllergenDescription))
                    .ToArray()),
            operationSettings.ServiceFeePercentage.Value);
    }

    internal ClientCatalogDto CreateAdministrativeCatalog(RestaurantUnitId unitId) => CreateCatalog(unitId);

    internal void AddAdministrativeOrderItem(
        Order order,
        SubmitClientOrderItemCommand requestedItem,
        RestaurantUnitId unitId,
        IDictionary<string, List<OrderItem>> stationItems) =>
        AddOrderItem(order, requestedItem, unitId, stationItems);

    internal Task CreateAdministrativeKitchenTicketsAsync(
        Order order,
        IReadOnlyDictionary<string, List<OrderItem>> stationItems,
        CancellationToken cancellationToken) =>
        CreateKitchenTicketsAsync(order, stationItems, cancellationToken);

    private void AddOrderItem(
        Order order,
        SubmitClientOrderItemCommand requestedItem,
        RestaurantUnitId unitId,
        IDictionary<string, List<OrderItem>> stationItems)
    {
        if (requestedItem.Quantity is < 1 or > 20)
        {
            throw new BusinessRuleException("client.order_item_quantity", "Item quantity must be between one and twenty.");
        }

        var productId = new ProductId(requestedItem.ProductId);
        var product = context.Products.SingleOrDefault(candidate =>
            candidate.Id == productId &&
            candidate.UnitId == unitId &&
            candidate.IsActive &&
            candidate.IsAvailable);
        if (product is null)
        {
            throw new BusinessRuleException("client.product_unavailable", "The selected product is unavailable.");
        }

        var notes = NormalizeOptionalText(requestedItem.Notes, 1000, nameof(requestedItem.Notes));
        var itemId = OrderItemId.New();
        if (requestedItem.Pizza is null)
        {
            if (product.ProductType == ProductType.Pizza)
            {
                throw new BusinessRuleException("client.pizza_configuration", "Pizza products require a valid configuration.");
            }

            var item = order.AddItem(itemId, product.Id, product.Name, requestedItem.Quantity, product.BasePrice, notes: notes);
            AddStationItem(stationItems, ResolveStationCode(product.ProductType), item);
            return;
        }

        if (product.ProductType != ProductType.Pizza)
        {
            throw new BusinessRuleException("client.product_not_pizza", "Only pizza products accept pizza configuration.");
        }

        var (pizza, extras) = CreatePizza(itemId, requestedItem.Pizza, unitId, product);
        var unitPrice = pizza.BasePrice + pizza.CrustPrice + pizza.ExtrasPrice;
        var pizzaItem = order.AddItem(
            itemId,
            product.Id,
            $"Pizza {pizza.SizeNameSnapshot} · {pizza.FlavorCount} sabor(es)",
            requestedItem.Quantity,
            unitPrice,
            notes: notes);
        context.Add(pizza);
        AddRemovedIngredientModifiers(pizzaItem.Id, requestedItem.Pizza, pizza);
        AddExtraIngredientModifiers(pizzaItem.Id, extras);
        AddStationItem(stationItems, "PIZZA", pizzaItem);
    }

    private (OrderItemPizza Pizza, IReadOnlyList<ResolvedPizzaExtra> Extras) CreatePizza(
        OrderItemId orderItemId,
        SubmitClientPizzaCommand command,
        RestaurantUnitId unitId,
        Product product)
    {
        var sizeId = new PizzaSizeId(command.SizeId);
        var size = context.PizzaSizes.SingleOrDefault(candidate =>
            candidate.Id == sizeId && candidate.UnitId == unitId && candidate.IsActive);
        if (size is null)
        {
            throw new BusinessRuleException("client.pizza_size", "The selected pizza size is unavailable.");
        }

        var flavorIds = (command.FlavorIds ?? []).Select(id => new PizzaFlavorId(id)).ToArray();
        var settings = context.PizzaSettings
            .ToArray()
            .Single(candidate => candidate.UnitId == unitId);
        var maxFlavors = Math.Min(size.MaxFlavors, settings.GlobalMaxFlavors);
        if (flavorIds.Length is < 1 || flavorIds.Length > maxFlavors)
        {
            throw new BusinessRuleException("client.pizza_flavors", $"This pizza accepts between one and {maxFlavors} flavors.");
        }

        if (!settings.AllowRepeatedFlavors && flavorIds.Distinct().Count() != flavorIds.Length)
        {
            throw new BusinessRuleException("client.pizza_repeated_flavor", "Repeated pizza flavors are not allowed.");
        }

        var flavors = flavorIds
            .Select(id => context.PizzaFlavors.SingleOrDefault(candidate =>
                candidate.Id == id &&
                candidate.UnitId == unitId &&
                candidate.IsActive &&
                candidate.IsAvailable)
                ?? throw new BusinessRuleException("client.pizza_flavor_unavailable", "A selected pizza flavor is unavailable."))
            .ToArray();
        if (!settings.AllowSweetAndSavoryMix && flavors.Select(flavor => flavor.FlavorType).Distinct().Count() > 1)
        {
            throw new BusinessRuleException("client.pizza_mixed_flavors", "Sweet and savory flavors cannot be combined.");
        }

        var extras = ResolveExtraIngredients(command, flavors, settings, unitId, product);
        var extrasPrice = extras.Aggregate(
            Money.Zero(),
            (total, extra) => total + (extra.Price * extra.Quantity));

        var prices = flavors.Select(flavor =>
            context.PizzaFlavorPrices.SingleOrDefault(candidate =>
                candidate.PizzaFlavorId == flavor.Id &&
                candidate.PizzaSizeId == size.Id &&
                candidate.IsAvailable)
            ?? throw new BusinessRuleException("client.pizza_flavor_price", "A selected flavor is unavailable for this size."))
            .ToArray();
        var calculatedBasePrice = PizzaPricingPolicies.Calculate(
            settings.PricingPolicy,
            prices.Select(price => price.Price).ToArray());

        PizzaCrust? crust = null;
        PizzaCrust? secondCrust = null;
        var crustPrice = Money.Zero();
        if (command.CrustId.HasValue)
        {
            var crustId = new PizzaCrustId(command.CrustId.Value);
            crust = context.PizzaCrusts.SingleOrDefault(candidate =>
                candidate.Id == crustId &&
                candidate.UnitId == unitId &&
                candidate.IsActive &&
                candidate.IsAvailable)
                ?? throw new BusinessRuleException("client.pizza_crust", "The selected pizza crust is unavailable.");
            var price = context.PizzaCrustPrices.SingleOrDefault(candidate =>
                candidate.PizzaCrustId == crust.Id && candidate.PizzaSizeId == size.Id)
                ?? throw new BusinessRuleException("client.pizza_crust_price", "The selected crust is unavailable for this size.");
            crustPrice = price.AdditionalPrice;

            if (command.SecondCrustId.HasValue)
            {
                var secondCrustId = new PizzaCrustId(command.SecondCrustId.Value);
                if (secondCrustId == crustId)
                {
                    throw new BusinessRuleException("client.pizza_crust_duplicate_half", "Split crust halves must be different.");
                }

                secondCrust = context.PizzaCrusts.SingleOrDefault(candidate =>
                    candidate.Id == secondCrustId &&
                    candidate.UnitId == unitId &&
                    candidate.IsActive &&
                    candidate.IsAvailable)
                    ?? throw new BusinessRuleException("client.pizza_crust", "The selected pizza crust is unavailable.");
                var secondPrice = context.PizzaCrustPrices.SingleOrDefault(candidate =>
                    candidate.PizzaCrustId == secondCrust.Id && candidate.PizzaSizeId == size.Id)
                    ?? throw new BusinessRuleException("client.pizza_crust_price", "The selected crust is unavailable for this size.");
                crustPrice = price.HalfAdditionalPrice + secondPrice.HalfAdditionalPrice;
            }
        }
        else if (command.SecondCrustId.HasValue)
        {
            throw new BusinessRuleException("client.pizza_crust_first_half", "The first crust half is required.");
        }

        var pizza = new OrderItemPizza(
            orderItemId,
            size.Id,
            size.Name,
            size.Slices,
            size.MaxFlavors,
            settings.PricingPolicy,
            calculatedBasePrice,
            crust?.Id,
            crust?.Name,
            secondCrust?.Id,
            secondCrust?.Name,
            crustPrice,
            extrasPrice);
        for (var index = 0; index < flavors.Length; index++)
        {
            pizza.AddFlavor(
                OrderItemPizzaFlavorId.New(),
                flavors[index].Id,
                flavors[index].Name,
                prices[index].Price,
                settings.AllowRepeatedFlavors);
        }

        pizza.EnsureValidComposition();
        return (pizza, extras);
    }

    private IReadOnlyList<ResolvedPizzaExtra> ResolveExtraIngredients(
        SubmitClientPizzaCommand command,
        IReadOnlyCollection<PizzaFlavor> selectedFlavors,
        PizzaSettings settings,
        RestaurantUnitId unitId,
        Product product)
    {
        var requestedExtras = command.ExtraIngredients ?? [];
        if (requestedExtras.Count > 30)
        {
            throw new BusinessRuleException(
                "client.pizza_extra_limit",
                "A pizza accepts at most thirty additional ingredient selections.");
        }

        var selectedFlavorIds = selectedFlavors.Select(flavor => flavor.Id).ToHashSet();
        var allowedFlavorExtras = context.PizzaFlavorExtras
            .Where(link => selectedFlavorIds.Contains(link.PizzaFlavorId) && link.IsActive)
            .ToArray();
        var allowedProductExtras = product.UsesCustomExtras
            ? context.ProductExtras
                .Where(link => link.ProductId == product.Id && link.IsActive)
                .ToArray()
            : [];
        var duplicatedSelection = requestedExtras
            .GroupBy(extra => new { extra.IngredientId, extra.PizzaFlavorId })
            .Any(group => group.Count() > 1);
        if (duplicatedSelection)
        {
            throw new BusinessRuleException(
                "client.pizza_extra_duplicate",
                "The same additional ingredient cannot be repeated for the same flavor.");
        }

        return requestedExtras.Select(requested =>
        {
            var ingredientId = new IngredientId(requested.IngredientId);
            var ingredient = context.Ingredients.SingleOrDefault(candidate =>
                candidate.Id == ingredientId &&
                candidate.UnitId == unitId &&
                candidate.IsActive &&
                candidate.IsAvailableAsExtra)
                ?? throw new BusinessRuleException(
                    "client.pizza_extra_unavailable",
                    "A selected additional ingredient is unavailable.");

            PizzaFlavorId? flavorId = null;
            Money price;
            int maxQuantity;
            if (product.UsesCustomExtras)
            {
                var allowedExtra = allowedProductExtras.SingleOrDefault(link =>
                    link.IngredientId == ingredientId)
                    ?? throw new BusinessRuleException(
                        "client.pizza_extra_not_allowed",
                        $"{ingredient.Name} is not available for the selected pizza.");
                price = allowedExtra.Price;
                maxQuantity = allowedExtra.MaxQuantity;

                if (settings.AllowExtrasPerFlavor)
                {
                    if (!requested.PizzaFlavorId.HasValue)
                    {
                        throw new BusinessRuleException(
                            "client.pizza_extra_flavor_required",
                            "Each additional ingredient must be assigned to a selected flavor.");
                    }

                    flavorId = new PizzaFlavorId(requested.PizzaFlavorId.Value);
                    if (!selectedFlavorIds.Contains(flavorId.Value))
                    {
                        throw new BusinessRuleException(
                            "client.pizza_extra_flavor",
                            "An additional ingredient was assigned to a flavor that is not part of the pizza.");
                    }
                }
                else if (requested.PizzaFlavorId.HasValue)
                {
                    throw new BusinessRuleException(
                        "client.pizza_extra_whole_pizza",
                        "Additional ingredients must apply to the whole pizza.");
                }
            }
            else if (settings.AllowExtrasPerFlavor)
            {
                if (!requested.PizzaFlavorId.HasValue)
                {
                    throw new BusinessRuleException(
                        "client.pizza_extra_flavor_required",
                        "Each additional ingredient must be assigned to a selected flavor.");
                }

                flavorId = new PizzaFlavorId(requested.PizzaFlavorId.Value);
                if (!selectedFlavorIds.Contains(flavorId.Value))
                {
                    throw new BusinessRuleException(
                        "client.pizza_extra_flavor",
                        "An additional ingredient was assigned to a flavor that is not part of the pizza.");
                }

                var allowedExtra = allowedFlavorExtras.SingleOrDefault(link =>
                    link.PizzaFlavorId == flavorId.Value &&
                    link.IngredientId == ingredientId)
                    ?? throw new BusinessRuleException(
                        "client.pizza_extra_not_allowed",
                        $"{ingredient.Name} is not available for the selected flavor.");
                price = allowedExtra.Price;
                maxQuantity = allowedExtra.MaxQuantity;
            }
            else if (requested.PizzaFlavorId.HasValue)
            {
                throw new BusinessRuleException(
                    "client.pizza_extra_whole_pizza",
                    "Additional ingredients must apply to the whole pizza.");
            }
            else
            {
                var links = allowedFlavorExtras
                    .Where(link => link.IngredientId == ingredientId)
                    .GroupBy(link => link.PizzaFlavorId)
                    .Select(group => group.Single())
                    .ToArray();
                if (links.Length != selectedFlavorIds.Count)
                {
                    throw new BusinessRuleException(
                        "client.pizza_extra_not_allowed_for_all_flavors",
                        $"{ingredient.Name} is not available for every selected flavor.");
                }

                price = links
                    .OrderByDescending(link => link.Price.Amount)
                    .First()
                    .Price;
                maxQuantity = links.Min(link => link.MaxQuantity);
            }

            if (requested.Quantity is < 1 || requested.Quantity > maxQuantity)
            {
                throw new BusinessRuleException(
                    "client.pizza_extra_quantity",
                    $"The quantity of {ingredient.Name} must be between one and {maxQuantity}.");
            }

            return new ResolvedPizzaExtra(ingredient, flavorId, requested.Quantity, price);
        }).ToArray();
    }

    private void AddRemovedIngredientModifiers(
        OrderItemId itemId,
        SubmitClientPizzaCommand command,
        OrderItemPizza pizza)
    {
        var selectedFlavorIds = pizza.Flavors.Select(flavor => flavor.PizzaFlavorId).ToHashSet();
        var removableIngredients = context.PizzaFlavorIngredients
            .Where(link => selectedFlavorIds.Contains(link.PizzaFlavorId) && link.IsRemovable)
            .ToArray()
            .GroupBy(link => link.IngredientId)
            .ToDictionary(group => group.Key, group => group.First());
        foreach (var ingredientIdValue in (command.RemovedIngredientIds ?? []).Distinct())
        {
            var ingredientId = new IngredientId(ingredientIdValue);
            if (!removableIngredients.ContainsKey(ingredientId))
            {
                throw new BusinessRuleException("client.pizza_ingredient", "A selected ingredient cannot be removed.");
            }

            var ingredient = context.Ingredients.Single(candidate => candidate.Id == ingredientId);
            context.Add(new OrderItemModifier(
                OrderItemModifierId.New(),
                itemId,
                ModifierType.Remove,
                ingredient.Name,
                1,
                Money.Zero(),
                ingredientId: ingredient.Id));
        }
    }

    private void AddExtraIngredientModifiers(
        OrderItemId itemId,
        IReadOnlyCollection<ResolvedPizzaExtra> extras)
    {
        foreach (var extra in extras)
        {
            context.Add(new OrderItemModifier(
                OrderItemModifierId.New(),
                itemId,
                ModifierType.Extra,
                extra.Ingredient.Name,
                extra.Quantity,
                extra.Price,
                extra.PizzaFlavorId,
                extra.Ingredient.Id));
        }
    }

    private async Task CreateKitchenTicketsAsync(
        Order order,
        IReadOnlyDictionary<string, List<OrderItem>> stationItems,
        CancellationToken cancellationToken)
    {
        var fallbackTicketNumber = context.KitchenTickets.Any()
            ? context.KitchenTickets.Max(candidate => candidate.TicketNumber)
            : 0;
        foreach (var (stationCode, items) in stationItems)
        {
            var station = context.ProductionStations.SingleOrDefault(candidate =>
                candidate.UnitId == order.UnitId &&
                candidate.Code == stationCode &&
                candidate.IsActive)
                ?? throw new BusinessRuleException("client.production_station", "A production station is unavailable.");
            var ticketNumber = numberGenerator is null
                ? ++fallbackTicketNumber
                : await numberGenerator.NextKitchenTicketNumberAsync(cancellationToken);
            var ticket = new KitchenTicket(
                KitchenTicketId.New(),
                order.UnitId,
                order.Id,
                station.Id,
                ticketNumber);
            context.Add(ticket);
            foreach (var item in items)
            {
                context.Add(new KitchenTicketItem(
                    KitchenTicketItemId.New(),
                    ticket.Id,
                    item.Id,
                    item.Quantity));
            }
        }
    }

    private IReadOnlyList<ClientServiceCallDto> CreateServiceCalls(TableSessionId sessionId)
    {
        var typeNames = context.ServiceCallTypes.ToDictionary(type => type.Id, type => type.Name);
        return context.ServiceCalls
            .Where(call => call.TableSessionId == sessionId && call.Status != ServiceCallStatus.Cancelled)
            .ToArray()
            .OrderByDescending(call => call.CreatedAt)
            .Take(5)
            .Select(call => new ClientServiceCallDto(
                call.Id.Value,
                call.ServiceCallTypeId.Value,
                typeNames.GetValueOrDefault(call.ServiceCallTypeId, "Atendimento"),
                call.Status.ToString(),
                call.CreatedAt,
                call.AcknowledgedAt,
                call.CompletedAt))
            .ToArray();
    }

    private IReadOnlyList<ClientOrderDto> CreateOrders(TableSessionId sessionId) =>
        context.Orders
            .Where(order => order.TableSessionId == sessionId)
            .ToArray()
            .OrderByDescending(order => order.PlacedAt ?? order.CreatedAt)
            .Select(CreateOrder)
            .ToArray();

    private ClientOrderDto CreateOrder(Order order)
    {
        var items = context.OrderItems.Where(item => item.OrderId == order.Id).ToArray();
        if (items.Length == 0 && order.Items.Count > 0)
        {
            items = order.Items.ToArray();
        }

        var pizzas = context.OrderItemPizzas
            .Where(pizza => items.Select(item => item.Id).Contains(pizza.Id))
            .ToArray()
            .ToDictionary(pizza => pizza.Id);
        var pizzaFlavors = context.OrderItemPizzaFlavors
            .Where(flavor => items.Select(item => item.Id).Contains(flavor.OrderItemId))
            .ToArray()
            .GroupBy(flavor => flavor.OrderItemId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ClientOrderPizzaFlavorDto>)group.OrderBy(flavor => flavor.PartNumber)
                    .Select(flavor => new ClientOrderPizzaFlavorDto(flavor.PizzaFlavorId.Value, flavor.FlavorNameSnapshot))
                    .ToArray());
        var modifiers = context.OrderItemModifiers
            .Where(modifier => items.Select(item => item.Id).Contains(modifier.OrderItemId))
            .ToArray()
            .GroupBy(modifier => modifier.OrderItemId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<ClientOrderModifierDto>)group
                    .Select(modifier => new ClientOrderModifierDto(
                        modifier.ModifierType.ToString(),
                        modifier.NameSnapshot,
                        modifier.Quantity,
                        modifier.UnitPrice.Amount,
                        modifier.TotalPrice.Amount,
                        modifier.PizzaFlavorId?.Value,
                        modifier.IngredientId?.Value))
                    .ToArray());

        return new ClientOrderDto(
            order.Id.Value,
            order.OrderNumber,
            order.Status.ToString(),
            order.PlacedAt,
            order.Subtotal.Amount,
            order.Total.Amount,
            items.Select(item =>
            {
                pizzas.TryGetValue(item.Id, out var pizza);
                return new ClientOrderItemDto(
                    item.Id.Value,
                    item.ProductId.Value,
                    item.ProductNameSnapshot,
                    item.Quantity,
                    item.UnitPrice.Amount,
                    item.TotalPrice.Amount,
                    item.Status.ToString(),
                    item.Notes,
                    pizza is null
                        ? null
                        : new ClientOrderPizzaDto(
                            pizza.PizzaSizeId.Value,
                            pizza.SizeNameSnapshot,
                            pizzaFlavors.GetValueOrDefault(item.Id, []),
                            pizza.PizzaCrustId?.Value,
                            pizza.CrustNameSnapshot,
                            pizza.SecondPizzaCrustId?.Value,
                            pizza.SecondCrustNameSnapshot),
                    modifiers.GetValueOrDefault(item.Id, []));
            }).ToArray());
    }

    private static string? FormatCrustDescription(OrderItemPizza pizza) =>
        pizza.CrustSelectionMode switch
        {
            CrustSelectionMode.Split =>
                $"½ {pizza.CrustNameSnapshot} + ½ {pizza.SecondCrustNameSnapshot}",
            CrustSelectionMode.Whole => pizza.CrustNameSnapshot,
            _ => null
        };

    private sealed record ResolvedPizzaExtra(
        Ingredient Ingredient,
        PizzaFlavorId? PizzaFlavorId,
        int Quantity,
        Money Price);

    private ClientBillDto CreateBill(TableSession tableSession)
    {
        var bill = context.Bills
            .Where(candidate => candidate.TableSessionId == tableSession.Id && candidate.Status != BillStatus.Cancelled)
            .ToArray()
            .OrderByDescending(candidate => candidate.RequestedAt)
            .FirstOrDefault();
        if (bill is not null)
        {
            return ToBillDto(bill);
        }

        var subtotal = context.Orders
            .Where(order => order.TableSessionId == tableSession.Id && order.Status != OrderStatus.Cancelled)
            .ToArray()
            .Sum(order => order.Total.Amount);
        var fee = decimal.Round(
            subtotal * tableSession.ServiceFeePercentageSnapshot.AsFactor,
            2,
            MidpointRounding.ToEven);
        return new ClientBillDto(
            null,
            "Open",
            subtotal,
            tableSession.ServiceFeePercentageSnapshot.Value,
            fee,
            subtotal + fee,
            0,
            subtotal + fee,
            null,
            null);
    }

    private static ClientBillDto ToBillDto(Bill bill) => new(
        bill.Id.Value,
        bill.Status.ToString(),
        bill.Subtotal.Amount,
        bill.ServiceFeePercentage.Value,
        bill.ServiceFeeAmount.Amount,
        bill.TotalAmount.Amount,
        bill.PaidAmount.Amount,
        bill.RemainingAmount.Amount,
        bill.RequestedAt,
        bill.RequestedSplitCount);

    private static bool IsActiveTableSession(TableSession session) =>
        session.Status is TableSessionStatus.Open or TableSessionStatus.BillRequested or TableSessionStatus.PaymentPending;

    private static string ResolveStationCode(ProductType productType) => productType switch
    {
        ProductType.Beverage => "BAR",
        ProductType.Pizza or ProductType.PizzaFlavor => "PIZZA",
        _ => "HOT"
    };

    private static void AddStationItem(
        IDictionary<string, List<OrderItem>> stationItems,
        string stationCode,
        OrderItem item)
    {
        if (!stationItems.TryGetValue(stationCode, out var items))
        {
            items = [];
            stationItems[stationCode] = items;
        }

        items.Add(item);
    }

    private static string? NormalizeOptionalText(string? value, int maxLength, string parameterName) =>
        string.IsNullOrWhiteSpace(value) ? null : Guard.Required(value, parameterName, maxLength);

    private static string CreateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
