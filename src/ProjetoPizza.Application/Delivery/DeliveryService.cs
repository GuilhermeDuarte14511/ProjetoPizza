using System.Security.Cryptography;
using System.Text;
using ProjetoPizza.Application.Abstractions.Persistence;
using ProjetoPizza.Application.Client;
using ProjetoPizza.Application.Customers;
using ProjetoPizza.Application.Inventory;
using ProjetoPizza.Domain.Audit;
using ProjetoPizza.Domain.Cashier;
using ProjetoPizza.Domain.Customers;
using ProjetoPizza.Domain.Ordering;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Application.Delivery;

public sealed class DeliveryService(
    IProjetoPizzaDbContext context,
    IOperationNumberGenerator numberGenerator) : IDeliveryService
{
    public Task<DeliveryCatalogDto> GetCatalogAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var unit = context.RestaurantUnits.Single(unit => unit.IsActive);
        var settings = context.OperationSettings.Single(candidate => candidate.UnitId == unit.Id);
        var catalog = new ClientService(context, numberGenerator).CreateAdministrativeCatalog(unit.Id);
        return Task.FromResult(new DeliveryCatalogDto(catalog, settings.DefaultDeliveryFee.Amount));
    }

    public async Task<DeliveryOrderPlacedDto> PlaceOrderAsync(
        PlaceDeliveryOrderCommand command,
        CancellationToken cancellationToken)
    {
        if (command.RequestId == Guid.Empty)
            throw new BusinessRuleException("delivery.request_id", "Delivery request identifier is required.");
        var items = command.Items?.ToArray() ?? [];
        if (items.Length is < 1 or > 30)
            throw new BusinessRuleException("delivery.items", "A delivery order must contain between one and thirty items.");

        var trackingToken = command.RequestId.ToString("N");
        var normalizedPhone = Customer.NormalizePhone(command.Phone);
        var orderId = new OrderId(command.RequestId);
        var existingOrder = context.Orders.SingleOrDefault(candidate => candidate.Id == orderId);
        if (existingOrder is not null)
        {
            var belongsToSameCheckout = existingOrder.SalesChannel == SalesChannel.Website &&
                existingOrder.FulfillmentType == FulfillmentType.Delivery &&
                existingOrder.CustomerId.HasValue &&
                context.Customers.Any(customer =>
                    customer.Id == existingOrder.CustomerId.Value && customer.Phone == normalizedPhone);
            if (!belongsToSameCheckout)
                throw new BusinessRuleException("delivery.request_id_conflict", "This delivery request identifier is already in use.");
            return new DeliveryOrderPlacedDto(
                existingOrder.Id.Value, existingOrder.OrderNumber, trackingToken,
                existingOrder.DeliveryStatus?.ToString() ?? existingOrder.Status.ToString(),
                existingOrder.Total.Amount);
        }

        var unit = context.RestaurantUnits.Single(unit => unit.IsActive);
        var settings = context.OperationSettings.Single(candidate => candidate.UnitId == unit.Id);
        if (!settings.AllowOrdersWithoutOpenCashShift &&
            !context.CashShifts.Any(shift => shift.Status == CashShiftStatus.Open))
        {
            throw new BusinessRuleException("delivery.cash_shift", "Delivery ordering is unavailable while the cash register is closed.");
        }

        var customer = context.Customers.SingleOrDefault(candidate =>
            candidate.UnitId == unit.Id && candidate.Phone == normalizedPhone);
        if (customer is not null && !customer.IsActive)
            throw new BusinessRuleException("delivery.customer", "This customer registration is unavailable.");
        if (customer is null)
        {
            customer = new Customer(CustomerId.New(), unit.Id, command.CustomerName, command.Phone, command.BirthDate);
            context.Add(customer);
        }
        else
        {
            if (customer.BirthDate != command.BirthDate)
                throw new BusinessRuleException("delivery.customer_identity", "Phone and birth date do not match the customer registration.");
            customer.Update(command.CustomerName, command.Phone, command.BirthDate, isActive: true);
        }

        var order = new Order(
            orderId,
            unit.Id,
            await numberGenerator.NextOrderNumberAsync(cancellationToken),
            SalesChannel.Website,
            FulfillmentType.Delivery);
        order.AssignCustomer(customer.Id, customer.Name);
        order.ConfigureDeliveryAddress(command.Address);
        order.ConfigureDeliveryTracking(HashTrackingToken(trackingToken));
        order.SetNotes(command.Notes);

        var stationItems = new Dictionary<string, List<OrderItem>>(StringComparer.OrdinalIgnoreCase);
        var composition = new ClientService(context, numberGenerator);
        foreach (var item in items)
        {
            composition.AddAdministrativeOrderItem(order, item, unit.Id, stationItems);
        }
        order.RecalculateTotals(deliveryFee: settings.DefaultDeliveryFee);
        LoyaltyProgramService.ApplyBenefits(context, order, customer, Money.Zero(), command.CouponCode, command.LoyaltyPoints);
        InventoryAllocation.Reserve(context, order, items);
        order.Submit();
        context.Add(order);
        await composition.CreateAdministrativeKitchenTicketsAsync(order, stationItems, cancellationToken);
        context.Add(new AuditLog(
            AuditLogId.New(), unit.Id, "Ordering", "CreateExternalDelivery", nameof(Order), order.Id.Value.ToString()));
        await context.SaveChangesAsync(cancellationToken);

        return new DeliveryOrderPlacedDto(
            order.Id.Value, order.OrderNumber, trackingToken,
            order.DeliveryStatus!.Value.ToString(), order.Total.Amount);
    }

    public Task<DeliveryTrackingDto?> TrackAsync(string trackingToken, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(trackingToken) || trackingToken.Length > 128) return Task.FromResult<DeliveryTrackingDto?>(null);
        var order = context.Orders.SingleOrDefault(candidate =>
            candidate.DeliveryTrackingTokenHash == HashTrackingToken(trackingToken));
        if (order is null || order.FulfillmentType != FulfillmentType.Delivery)
            return Task.FromResult<DeliveryTrackingDto?>(null);
        var items = context.OrderItems
            .Where(item => item.OrderId == order.Id)
            .Select(item => new DeliveryTrackingItemDto(item.ProductNameSnapshot, item.Quantity, item.Status.ToString()))
            .ToArray();
        return Task.FromResult<DeliveryTrackingDto?>(new DeliveryTrackingDto(
            order.OrderNumber,
            order.Status.ToString(),
            order.DeliveryStatus?.ToString() ?? "AwaitingPreparation",
            order.CustomerNameSnapshot ?? "Cliente",
            order.DeliveryAddressSnapshot ?? string.Empty,
            order.DeliveryDriverName,
            order.PlacedAt ?? order.CreatedAt,
            order.DispatchedAt,
            order.DeliveredAt,
            order.Total.Amount,
            items));
    }

    public async Task<LoyaltyLookupDto?> LookupLoyaltyAsync(LoyaltyLookupCommand command, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var unit = context.RestaurantUnits.Single(unit => unit.IsActive);
        var phone = Customer.NormalizePhone(command.Phone);
        var customer = context.Customers.SingleOrDefault(candidate => candidate.UnitId == unit.Id && candidate.Phone == phone &&
            candidate.BirthDate == command.BirthDate && candidate.IsActive);
        if (customer is null) return null;
        LoyaltyProgramService.ExpirePoints(context, customer);
        var eligible = new Money(command.OrderAmount);
        var couponDiscount = Money.Zero();
        if (!string.IsNullOrWhiteSpace(command.CouponCode))
        {
            var code = command.CouponCode.Trim().ToUpperInvariant();
            var coupon = context.PromotionCoupons.SingleOrDefault(candidate => candidate.UnitId == unit.Id && candidate.Code == code)
                ?? throw new BusinessRuleException("coupon.not_found", "Coupon was not found.");
            couponDiscount = coupon.CalculateDiscount(eligible, DateTimeOffset.UtcNow);
        }
        var loyaltyDiscount = Money.Zero();
        if (command.LoyaltyPoints > 0)
        {
            if (command.LoyaltyPoints > customer.LoyaltyPoints) throw new BusinessRuleException("loyalty.balance", "Insufficient loyalty point balance.");
            loyaltyDiscount = LoyaltyProgramService.GetOrCreateSettings(context, unit.Id)
                .CalculateRedemption(command.LoyaltyPoints, eligible - couponDiscount);
        }
        await context.SaveChangesAsync(cancellationToken);
        return new LoyaltyLookupDto(customer.Name, customer.LoyaltyPoints, customer.LoyaltyPointsExpireAt,
            couponDiscount.Amount, loyaltyDiscount.Amount, couponDiscount.Amount + loyaltyDiscount.Amount);
    }

    private static string HashTrackingToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
