using System.Globalization;
using ProjetoPizza.Application.Abstractions.Persistence;
using ProjetoPizza.Application.Client;
using ProjetoPizza.Application.Catalog;
using ProjetoPizza.Application.Devices;
using ProjetoPizza.Application.Inventory;
using ProjetoPizza.Domain.Audit;
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

namespace ProjetoPizza.Application.Admin;

public sealed class AdminManagementService(
    IProjetoPizzaDbContext context,
    IOperationNumberGenerator? numberGenerator = null,
    IMenuImageStorage? menuImageStorage = null) : IAdminManagementService
{
    public Task<IReadOnlyCollection<OrderManagementDto>> ListOrdersAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var lines = context.OrderItems
            .ToArray()
            .GroupBy(item => item.OrderId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<OrderLineDto>)group
                    .Select(item => new OrderLineDto(
                        item.Id.Value,
                        item.ProductNameSnapshot,
                        item.Quantity,
                        item.UnitPrice.Amount,
                        item.TotalPrice.Amount,
                        item.Status.ToString()))
                    .ToArray());
        var result = context.Orders
            .OrderByDescending(order => order.CreatedAt)
            .ToArray()
            .Select(order => new OrderManagementDto(
                order.Id.Value,
                order.OrderNumber,
                order.SalesChannel.ToString(),
                order.FulfillmentType.ToString(),
                order.Status.ToString(),
                order.CustomerId?.Value,
                order.CustomerNameSnapshot,
                order.DeliveryAddressSnapshot,
                order.DeliveryStatus?.ToString(),
                order.DeliveryDriverName,
                order.DispatchedAt,
                order.DeliveredAt,
                order.Notes,
                order.CancellationReason,
                order.Subtotal.Amount,
                order.Discount.Amount,
                order.Total.Amount,
                order.CreatedAt,
                order.PlacedAt,
                lines.GetValueOrDefault(order.Id, [])))
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<OrderManagementDto>>(result);
    }

    public Task<AdministrativeOrderCatalogDto> GetOrderCatalogAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var unit = GetUnit();
        var catalog = new ClientService(context, numberGenerator).CreateAdministrativeCatalog(unit.Id);
        var settings = context.OperationSettings.Single();
        return Task.FromResult(new AdministrativeOrderCatalogDto(catalog, settings.DefaultDeliveryFee.Amount));
    }

    public Task<IReadOnlyCollection<CustomerDto>> ListCustomersAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = context.Customers
            .OrderBy(customer => customer.Name)
            .ToArray()
            .Select(ToCustomerDto)
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<CustomerDto>>(result);
    }

    public Task<IReadOnlyCollection<ReservationDto>> ListReservationsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyCollection<ReservationDto>>(context.Reservations
            .OrderBy(reservation => reservation.ScheduledAt)
            .ToArray().Select(ToReservationDto).ToArray());
    }

    public Task<IReadOnlyCollection<WaitlistEntryDto>> ListWaitlistAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyCollection<WaitlistEntryDto>>(context.WaitlistEntries
            .OrderBy(entry => entry.EnteredAt)
            .ToArray().Select(ToWaitlistEntryDto).ToArray());
    }

    public Task<OrderReceiptDto?> GetOrderReceiptAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var order = context.Orders.SingleOrDefault(candidate => candidate.Id == new OrderId(id));
        return Task.FromResult(order is null ? null : CreateOrderReceipt(order));
    }

    public async Task<CustomerDto> SaveCustomerAsync(
        SaveCustomerCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var unit = GetUnit();
        var normalizedPhone = Customer.NormalizePhone(command.Phone);
        var customerId = command.Id.HasValue ? new CustomerId(command.Id.Value) : CustomerId.New();
        if (context.Customers.Any(customer =>
                customer.UnitId == unit.Id &&
                customer.Phone == normalizedPhone &&
                customer.Id != customerId))
        {
            throw new BusinessRuleException("customer.phone_duplicate", "A customer with this phone already exists.");
        }

        var customer = command.Id.HasValue
            ? context.Customers.Single(candidate => candidate.Id == customerId && candidate.UnitId == unit.Id)
            : new Customer(customerId, unit.Id, command.Name, command.Phone, command.BirthDate);
        var action = command.Id.HasValue ? "Update" : "Create";
        if (command.Id.HasValue)
        {
            customer.Update(command.Name, command.Phone, command.BirthDate, command.IsActive);
        }
        else
        {
            if (!command.IsActive)
            {
                customer.Update(command.Name, command.Phone, command.BirthDate, isActive: false);
            }

            context.Add(customer);
        }

        AddAudit(unit.Id, employee.Id, "Customers", action, nameof(Customer), customer.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return ToCustomerDto(customer);
    }

    public async Task<ReservationDto> CreateReservationAsync(
        CreateReservationCommand command, Guid identityUserId, CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var unit = GetUnit();
        CustomerId? customerId = command.CustomerId.HasValue ? new CustomerId(command.CustomerId.Value) : null;
        var phone = Customer.NormalizePhone(command.Phone);
        var customerName = command.CustomerName;
        if (customerId.HasValue)
        {
            var selectedCustomer = context.Customers.SingleOrDefault(customer =>
                customer.Id == customerId.Value && customer.UnitId == unit.Id && customer.IsActive);
            if (selectedCustomer is null)
                throw new BusinessRuleException("reservation.customer", "The selected customer is unavailable.");

            customerName = selectedCustomer.Name;
            phone = selectedCustomer.Phone;
        }
        else
        {
            var existingCustomer = context.Customers.SingleOrDefault(customer =>
                customer.UnitId == unit.Id && customer.Phone == phone && customer.IsActive);
            if (existingCustomer is not null)
            {
                customerId = existingCustomer.Id;
                customerName = existingCustomer.Name;
                phone = existingCustomer.Phone;
            }
            else
            {
                if (!command.CustomerBirthDate.HasValue)
                    throw new BusinessRuleException("reservation.customer_birth_date", "Birth date is required for a new customer.");

                var newCustomer = new Customer(
                    CustomerId.New(), unit.Id, command.CustomerName, command.Phone, command.CustomerBirthDate.Value);
                context.Add(newCustomer);
                customerId = newCustomer.Id;
                customerName = newCustomer.Name;
                phone = newCustomer.Phone;
                AddAudit(unit.Id, employee.Id, "Customers", "CreateFromReservation", nameof(Customer), newCustomer.Id.Value);
            }
        }

        var end = command.ScheduledAt.AddMinutes(command.DurationMinutes);
        if (context.Reservations.Any(reservation => reservation.Phone == phone &&
            reservation.Status != ReservationStatus.Cancelled && reservation.Status != ReservationStatus.NoShow &&
            reservation.ScheduledAt < end && reservation.ScheduledAt.AddMinutes(reservation.DurationMinutes) > command.ScheduledAt))
            throw new BusinessRuleException("reservation.duplicate", "This customer already has an overlapping reservation.");
        var reservation = new Reservation(
            ReservationId.New(), unit.Id, customerName, phone, command.PartySize,
            command.ScheduledAt, command.DurationMinutes, command.Notes, customerId);
        context.Add(reservation);
        AddAudit(unit.Id, employee.Id, "Dining", "CreateReservation", nameof(Reservation), reservation.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return ToReservationDto(reservation);
    }

    public async Task<CommandResultDto> TransitionReservationAsync(
        Guid id, string transition, Guid identityUserId, CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var reservation = context.Reservations.Single(item => item.Id == new ReservationId(id));
        if (!Enum.TryParse<ReservationStatus>(transition, true, out var status))
            throw new BusinessRuleException("reservation.transition", "Unknown reservation transition.");
        reservation.Transition(status);
        AddAudit(reservation.UnitId, employee.Id, "Dining", status.ToString(), nameof(Reservation), reservation.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(reservation.Id.Value, reservation.Status.ToString());
    }

    public async Task<WaitlistEntryDto> CreateWaitlistEntryAsync(
        CreateWaitlistEntryCommand command, Guid identityUserId, CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var unit = GetUnit();
        CustomerId? customerId = command.CustomerId.HasValue ? new CustomerId(command.CustomerId.Value) : null;
        var phone = Customer.NormalizePhone(command.Phone);
        if (context.WaitlistEntries.Any(entry => entry.Phone == phone &&
            entry.Status != WaitlistStatus.Seated && entry.Status != WaitlistStatus.Cancelled))
            throw new BusinessRuleException("waitlist.duplicate", "This customer is already on the waitlist.");
        var entry = new WaitlistEntry(
            WaitlistEntryId.New(), unit.Id, command.CustomerName, command.Phone, command.PartySize,
            command.EstimatedWaitMinutes, command.Notes, customerId);
        context.Add(entry);
        AddAudit(unit.Id, employee.Id, "Dining", "JoinWaitlist", nameof(WaitlistEntry), entry.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return ToWaitlistEntryDto(entry);
    }

    public async Task<CommandResultDto> TransitionWaitlistEntryAsync(
        Guid id, string transition, Guid identityUserId, CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var entry = context.WaitlistEntries.Single(item => item.Id == new WaitlistEntryId(id));
        if (!Enum.TryParse<WaitlistStatus>(transition, true, out var status))
            throw new BusinessRuleException("waitlist.transition", "Unknown waitlist transition.");
        entry.Transition(status);
        AddAudit(entry.UnitId, employee.Id, "Dining", status.ToString(), nameof(WaitlistEntry), entry.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(entry.Id.Value, entry.Status.ToString());
    }

    public async Task<CreatedOrderDto> CreateOrderAsync(
        CreateAdministrativeOrderCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        if (command.RequestId == Guid.Empty)
        {
            throw new BusinessRuleException("order.request_id", "Order request identifier is required.");
        }

        var requestedOrderId = new OrderId(command.RequestId);
        var existing = context.Orders.SingleOrDefault(candidate => candidate.Id == requestedOrderId);
        if (existing is not null)
        {
            return new CreatedOrderDto(existing.Id.Value, existing.OrderNumber, existing.Status.ToString(), existing.Total.Amount, CreateOrderReceipt(existing));
        }

        var employee = GetEmployee(identityUserId);
        var unit = GetUnit();
        var order = await BuildAdministrativeOrderAsync(command, employee, unit, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return new CreatedOrderDto(order.Id.Value, order.OrderNumber, order.Status.ToString(), order.Total.Amount, CreateOrderReceipt(order));
    }

    public async Task<CounterCheckoutResultDto> CheckoutCounterOrderAsync(
        CheckoutCounterOrderCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        if (!command.Order.Fulfillment.Equals("Pickup", StringComparison.OrdinalIgnoreCase) &&
            !command.Order.Fulfillment.Equals("Takeaway", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException("counter_checkout.fulfillment", "Counter checkout supports pickup orders only.");
        }

        if (command.Order.RequestId == Guid.Empty)
        {
            throw new BusinessRuleException("order.request_id", "Order request identifier is required.");
        }

        var requestedOrderId = new OrderId(command.Order.RequestId);
        var existing = context.Orders.SingleOrDefault(candidate => candidate.Id == requestedOrderId);
        if (existing is not null)
        {
            var existingBill = context.Bills.SingleOrDefault(candidate => candidate.OrderId == existing.Id);
            if (existingBill?.Status != BillStatus.Paid)
            {
                throw new BusinessRuleException(
                    "counter_checkout.request_conflict",
                    "The request identifier already belongs to an order that was not completed by counter checkout.");
            }

            return new CounterCheckoutResultDto(
                existing.Id.Value,
                existing.OrderNumber,
                existing.Status.ToString(),
                existing.Total.Amount,
                CreateOrderReceipt(existing));
        }

        var employee = GetEmployee(identityUserId);
        var unit = GetUnit();
        var methodId = new PaymentMethodId(command.Payment.PaymentMethodId);
        var paymentMethod = context.PaymentMethods.SingleOrDefault(method =>
            method.Id == methodId && method.UnitId == unit.Id && method.IsActive)
            ?? throw new BusinessRuleException("payment.method", "The selected payment method is unavailable.");
        var cashShift = context.CashShifts
            .Where(shift => shift.Status == CashShiftStatus.Open)
            .OrderByDescending(shift => shift.OpenedAt)
            .ToArray()
            .FirstOrDefault();
        if (paymentMethod.Code == "CASH" && cashShift is null)
        {
            throw new BusinessRuleException("payment.cash_shift", "Cash payments require an open cash shift.");
        }

        var order = await BuildAdministrativeOrderAsync(command.Order, employee, unit, cancellationToken);
        if (order.Total.Amount <= 0)
        {
            throw new BusinessRuleException("counter_checkout.total", "Counter checkout requires a positive total.");
        }

        var bill = new Bill(BillId.New(), unit.Id, order.Id, order.Subtotal, order.Discount);
        bill.Request();
        AddCounterBillItems(bill, order);

        var payment = new Payment(
            PaymentId.New(),
            unit.Id,
            bill.Id,
            paymentMethod,
            bill.TotalAmount,
            new Money(command.Payment.ReceivedAmount),
            employee.Id,
            cashShiftId: cashShift?.Id,
            externalReference: command.Payment.ExternalReference);
        bill.RegisterPayment(bill.TotalAmount);
        context.Add(bill);
        context.Add(payment);

        if (paymentMethod.Code == "CASH" && cashShift is not null)
        {
            _ = context.CashMovements.Where(movement => movement.CashShiftId == cashShift.Id).ToArray();
            cashShift.RegisterMovement(
                CashMovementId.New(),
                CashMovementType.Sale,
                bill.TotalAmount,
                $"Pagamento do pedido de balcão #{order.OrderNumber}",
                "Venda no balcão",
                employee.Id,
                paymentId: payment.Id);
        }

        AddAudit(unit.Id, employee.Id, "Billing", "CounterCheckout", nameof(Bill), bill.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CounterCheckoutResultDto(
            order.Id.Value,
            order.OrderNumber,
            order.Status.ToString(),
            order.Total.Amount,
            CreateOrderReceipt(order));
    }

    private async Task<Order> BuildAdministrativeOrderAsync(
        CreateAdministrativeOrderCommand command,
        Employee employee,
        RestaurantUnit unit,
        CancellationToken cancellationToken)
    {
        if (command.RequestId == Guid.Empty)
        {
            throw new BusinessRuleException("order.request_id", "Order request identifier is required.");
        }

        var requestedItems = command.Items?.ToArray() ?? [];
        if (requestedItems.Length is < 1 or > 30)
        {
            throw new BusinessRuleException("order.items", "An order must contain between one and thirty items.");
        }
        if (command.DiscountAmount < 0)
        {
            throw new BusinessRuleException("order.discount", "Discount cannot be negative.");
        }

        var fulfillment = command.Fulfillment.Equals("Delivery", StringComparison.OrdinalIgnoreCase)
            ? FulfillmentType.Delivery
            : command.Fulfillment.Equals("Pickup", StringComparison.OrdinalIgnoreCase) ||
              command.Fulfillment.Equals("Takeaway", StringComparison.OrdinalIgnoreCase)
                ? FulfillmentType.Pickup
                : throw new BusinessRuleException("order.fulfillment", "Administrative orders support pickup or delivery only.");
        var customerId = new CustomerId(command.CustomerId);
        var customer = context.Customers.SingleOrDefault(candidate =>
            candidate.Id == customerId && candidate.UnitId == unit.Id && candidate.IsActive)
            ?? throw new BusinessRuleException("order.customer", "The selected customer is unavailable.");
        var settings = context.OperationSettings.Single();
        if (!settings.AllowOrdersWithoutOpenCashShift &&
            !context.CashShifts.Any(shift => shift.Status == CashShiftStatus.Open))
        {
            throw new BusinessRuleException("order.cash_shift", "Orders are unavailable while the cash register is closed.");
        }

        var orderNumber = numberGenerator is null
            ? context.Orders.Any() ? context.Orders.Max(order => order.OrderNumber) + 1 : 1
            : await numberGenerator.NextOrderNumberAsync(cancellationToken);
        var order = new Order(
            new OrderId(command.RequestId),
            unit.Id,
            orderNumber,
            fulfillment == FulfillmentType.Delivery ? SalesChannel.Delivery : SalesChannel.Pickup,
            fulfillment,
            createdByEmployeeId: employee.Id);
        order.AssignCustomer(customer.Id, customer.Name);
        if (fulfillment == FulfillmentType.Delivery)
        {
            order.ConfigureDeliveryAddress(command.DeliveryAddress ?? string.Empty);
            order.ConfigureDeliveryTracking(Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(command.RequestId.ToByteArray())));
        }
        order.SetNotes(command.Notes);

        var stationItems = new Dictionary<string, List<OrderItem>>(StringComparer.OrdinalIgnoreCase);
        var composition = new ClientService(context, numberGenerator);
        foreach (var requestedItem in requestedItems)
        {
            composition.AddAdministrativeOrderItem(order, requestedItem, unit.Id, stationItems);
        }

        order.RecalculateTotals(
            deliveryFee: fulfillment == FulfillmentType.Delivery ? settings.DefaultDeliveryFee : Money.Zero(),
            discount: new Money(command.DiscountAmount));
        InventoryConsumption.Apply(context, order, requestedItems, employee.Id);
        order.Submit();
        customer.RegisterPurchase(order.Total);
        context.Add(order);
        await composition.CreateAdministrativeKitchenTicketsAsync(order, stationItems, cancellationToken);
        AddAudit(unit.Id, employee.Id, "Ordering", "CreateAdministrative", nameof(Order), order.Id.Value);
        return order;
    }

    private void AddCounterBillItems(Bill bill, Order order)
    {
        var items = order.Items.ToArray();
        var remainingDiscount = order.Discount.Amount;
        for (var index = 0; index < items.Length; index++)
        {
            var item = items[index];
            var allocatedDiscount = index == items.Length - 1
                ? remainingDiscount
                : decimal.Round(
                    order.Discount.Amount * item.TotalPrice.Amount / order.Subtotal.Amount,
                    2,
                    MidpointRounding.ToEven);
            remainingDiscount -= allocatedDiscount;
            context.Add(new BillItem(
                BillItemId.New(),
                bill.Id,
                item.Id,
                item.Quantity,
                item.TotalPrice,
                Money.Zero(),
                new Money(allocatedDiscount)));
        }
    }

    public Task<IReadOnlyCollection<PizzaCrustDto>> ListPizzaCrustsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sizes = context.PizzaSizes
            .OrderBy(size => size.Slices)
            .ToArray();
        var prices = context.PizzaCrustPrices.ToArray();
        var result = context.PizzaCrusts
            .OrderBy(crust => crust.DisplayOrder)
            .ToArray()
            .Select(crust => new PizzaCrustDto(
                crust.Id.Value,
                crust.Name,
                crust.Description,
                crust.IsActive,
                crust.IsAvailable,
                sizes
                    .Where(size => size.UnitId == crust.UnitId)
                    .Select(size =>
                    {
                        var price = prices.SingleOrDefault(candidate =>
                            candidate.PizzaCrustId == crust.Id &&
                            candidate.PizzaSizeId == size.Id);
                        return new PizzaCrustPriceDto(
                            size.Id.Value,
                            size.Name,
                            size.Slices,
                            price?.AdditionalPrice.Amount ?? 0m,
                            price?.HalfAdditionalPrice.Amount ?? 0m);
                    })
                    .ToArray()))
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<PizzaCrustDto>>(result);
    }

    public Task<IReadOnlyCollection<IngredientDto>> ListIngredientsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = context.Ingredients
            .OrderBy(ingredient => ingredient.Name)
            .ToArray()
            .Select(ingredient => new IngredientDto(
                ingredient.Id.Value,
                ingredient.Name,
                ingredient.Description,
                ingredient.IsActive,
                ingredient.IsAllergen,
                ingredient.AllergenDescription,
                ingredient.IsAvailableAsExtra,
                ingredient.ExtraPrice.Amount,
                ingredient.MaxExtraQuantity))
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<IngredientDto>>(result);
    }

    public Task<UnitSettingsDto> GetUnitSettingsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ToDto(GetUnit()));
    }

    public Task<OperationSettingsDto> GetOperationSettingsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var settings = context.OperationSettings.Single();
        return Task.FromResult(new OperationSettingsDto(
            settings.AllowTableWithoutWaiter,
            settings.AllowOrdersWithoutOpenCashShift,
            settings.ClearTabletAfterTableClose,
            settings.ServiceFeePercentage.Value,
            settings.DefaultDeliveryFee.Amount,
            settings.DeliveryOrderSoundEnabled,
            settings.TableCallSoundEnabled,
            settings.TableCallToleranceMinutes));
    }

    public Task<PizzaRulesDto> GetPizzaRulesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var settings = context.PizzaSettings.Single();
        return Task.FromResult(new PizzaRulesDto(
            settings.GlobalMaxFlavors,
            settings.PricingPolicy.ToString(),
            settings.AllowSweetAndSavoryMix,
            settings.AllowExtrasPerFlavor,
            settings.AllowRepeatedFlavors));
    }

    public Task<IReadOnlyCollection<CashRegisterDto>> ListCashRegistersAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = context.CashRegisters
            .OrderBy(register => register.Name)
            .ToArray()
            .Select(register => new CashRegisterDto(register.Id.Value, register.Name, register.Code, register.IsActive))
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<CashRegisterDto>>(result);
    }

    public Task<CashShiftDto?> GetCurrentCashShiftAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var shift = context.CashShifts
            .Where(candidate => candidate.Status == CashShiftStatus.Open || candidate.Status == CashShiftStatus.Closing)
            .OrderByDescending(candidate => candidate.OpenedAt)
            .ToArray()
            .FirstOrDefault();
        if (shift is null)
        {
            return Task.FromResult<CashShiftDto?>(null);
        }

        var movements = context.CashMovements
            .Where(movement => movement.CashShiftId == shift.Id)
            .OrderByDescending(movement => movement.CreatedAt)
            .ToArray()
            .Select(movement => new CashMovementDto(
                movement.Id.Value,
                movement.MovementType.ToString(),
                movement.Amount.Amount,
                movement.Description,
                movement.Reason,
                movement.CreatedAt))
            .ToArray();
        var register = context.CashRegisters.Single(item => item.Id == shift.CashRegisterId);
        var employee = context.Employees.Single(item => item.Id == shift.OperatorEmployeeId);
        return Task.FromResult<CashShiftDto?>(new CashShiftDto(
            shift.Id.Value,
            register.Name,
            employee.DisplayName,
            shift.Status.ToString(),
            shift.OpenedAt,
            shift.OpeningAmount.Amount,
            shift.ExpectedCashAmount.Amount,
            shift.CountedCashAmount?.Amount,
            shift.DifferenceAmount,
            movements));
    }

    public Task<IReadOnlyCollection<CashShiftHistoryDto>> ListCashShiftHistoryAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var registers = context.CashRegisters.ToDictionary(register => register.Id, register => register.Name);
        var employees = context.Employees.ToDictionary(employee => employee.Id, employee => employee.DisplayName);
        var movements = context.CashMovements
            .OrderBy(movement => movement.CreatedAt)
            .ToArray()
            .GroupBy(movement => movement.CashShiftId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<CashMovementDto>)group.Select(ToCashMovementDto).ToArray());
        var result = context.CashShifts
            .Where(shift => shift.Status == CashShiftStatus.Closed || shift.Status == CashShiftStatus.Cancelled)
            .OrderByDescending(shift => shift.ClosedAt ?? shift.OpenedAt)
            .Take(200)
            .ToArray()
            .Select(shift => new CashShiftHistoryDto(
                shift.Id.Value,
                registers.GetValueOrDefault(shift.CashRegisterId, "Caixa removido"),
                employees.GetValueOrDefault(shift.OperatorEmployeeId, "Operador removido"),
                shift.ClosedByEmployeeId.HasValue
                    ? employees.GetValueOrDefault(shift.ClosedByEmployeeId.Value, "Operador removido")
                    : null,
                shift.Status.ToString(),
                shift.OpenedAt,
                shift.ClosedAt,
                shift.OpeningAmount.Amount,
                shift.ExpectedCashAmount.Amount,
                shift.CountedCashAmount?.Amount,
                shift.DifferenceAmount,
                shift.ClosingNotes,
                movements.GetValueOrDefault(shift.Id, [])))
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<CashShiftHistoryDto>>(result);
    }

    public Task<IReadOnlyCollection<PaymentMethodDto>> ListPaymentMethodsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = context.PaymentMethods
            .OrderBy(method => method.DisplayOrder)
            .ToArray()
            .Select(method => new PaymentMethodDto(
                method.Id.Value,
                method.Code,
                method.Name,
                method.RequiresExternalReference,
                method.AllowsChange,
                method.DisplayOrder,
                method.IsActive))
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<PaymentMethodDto>>(result);
    }

    public Task<IReadOnlyCollection<PaymentDto>> ListPaymentsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var methods = context.PaymentMethods.ToDictionary(method => method.Id, method => method.Name);
        var payers = context.BillSplits.ToDictionary(split => split.Id, split => split.Name);
        var result = context.Payments
            .OrderByDescending(payment => payment.PaidAt)
            .ToArray()
            .Select(payment => new PaymentDto(
                payment.Id.Value,
                payment.BillId.Value,
                payment.BillSplitId.HasValue ? payers.GetValueOrDefault(payment.BillSplitId.Value) : null,
                methods.GetValueOrDefault(payment.PaymentMethodId, "Desconhecido"),
                payment.Status.ToString(),
                payment.Amount.Amount,
                payment.ReceivedAmount.Amount,
                payment.ChangeAmount.Amount,
                payment.RefundedAmount.Amount,
                payment.ExternalReference,
                payment.PaidAt,
                payment.RefundedAt,
                payment.RefundReason))
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<PaymentDto>>(result);
    }

    public Task<FinancialReportDto> GetFinancialReportAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (to < from)
        {
            throw new BusinessRuleException("financial_report.period", "End date must not be earlier than start date.");
        }

        var orders = context.Orders
            .Where(order => order.CreatedAt >= from && order.CreatedAt <= to && order.Status != OrderStatus.Cancelled)
            .ToArray();
        var payments = context.Payments
            .Where(payment => payment.PaidAt >= from && payment.PaidAt <= to &&
                payment.Status != PaymentStatus.Pending && payment.Status != PaymentStatus.Authorized &&
                payment.Status != PaymentStatus.Failed && payment.Status != PaymentStatus.Cancelled)
            .ToArray();
        var methods = context.PaymentMethods.ToDictionary(method => method.Id, method => method.Name);
        var stockMovements = context.StockMovements
            .Where(movement => movement.CreatedAt >= from && movement.CreatedAt <= to &&
                movement.MovementType == StockMovementType.Consumption)
            .ToArray();
        var stations = context.ProductionStations.ToDictionary(station => station.Id);
        var completedTickets = context.KitchenTickets
            .Where(ticket => ticket.ReadyAt >= from && ticket.ReadyAt <= to && ticket.StartedAt.HasValue)
            .ToArray();
        var grossSales = orders.Sum(order => order.Total.Amount);
        var paidAmount = payments.Sum(payment => payment.Amount.Amount - payment.RefundedAmount.Amount);
        var foodCost = stockMovements.Sum(movement => movement.Quantity * movement.UnitCost.Amount);
        var contributionMargin = grossSales - foodCost;
        var performance = completedTickets
            .GroupBy(ticket => ticket.ProductionStationId)
            .Select(group =>
            {
                var station = stations.GetValueOrDefault(group.Key);
                var durations = group.Select(ticket => (decimal)(ticket.ReadyAt!.Value - ticket.StartedAt!.Value).TotalMinutes).ToArray();
                var onTime = station is null ? 0 : group.Count(ticket =>
                    ticket.ReadyAt!.Value - ticket.StartedAt!.Value <= TimeSpan.FromMinutes(station.TargetPreparationMinutes));
                return new ProductionPerformanceDto(
                    station?.Name ?? "Praça desconhecida",
                    group.Count(),
                    decimal.Round(durations.Average(), 1),
                    decimal.Round(onTime * 100m / group.Count(), 1));
            })
            .OrderBy(item => item.Station)
            .ToArray();
        var result = new FinancialReportDto(
            from,
            to,
            grossSales,
            paidAmount,
            foodCost,
            contributionMargin,
            grossSales == 0 ? 0 : decimal.Round(contributionMargin * 100m / grossSales, 1),
            orders.Length == 0 ? 0 : decimal.Round(grossSales / orders.Length, 2),
            orders.Length,
            completedTickets.Length,
            completedTickets.Length == 0 ? 0 : decimal.Round(
                (decimal)completedTickets.Average(ticket => (ticket.ReadyAt!.Value - ticket.StartedAt!.Value).TotalMinutes), 1),
            completedTickets.Length == 0 ? 0 : decimal.Round(
                completedTickets.Count(ticket => stations.TryGetValue(ticket.ProductionStationId, out var station) &&
                    ticket.ReadyAt!.Value - ticket.StartedAt!.Value <= TimeSpan.FromMinutes(station.TargetPreparationMinutes)) * 100m /
                completedTickets.Length, 1),
            orders.GroupBy(order => order.SalesChannel)
                .Select(group => new FinancialChannelDto(group.Key.ToString(), group.Count(), group.Sum(order => order.Total.Amount)))
                .OrderByDescending(channel => channel.Total)
                .ToArray(),
            payments.GroupBy(payment => payment.PaymentMethodId)
                .Select(group => new FinancialMethodDto(
                    methods.GetValueOrDefault(group.Key, "Desconhecido"),
                    group.Count(),
                    group.Sum(payment => payment.Amount.Amount - payment.RefundedAmount.Amount)))
                .OrderByDescending(method => method.Total)
                .ToArray(),
            performance);
        return Task.FromResult(result);
    }

    public Task<IReadOnlyCollection<DeviceDto>> ListDevicesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = context.Devices
            .OrderBy(device => device.Name)
            .ToArray()
            .Select(device => new DeviceDto(
                device.Id.Value,
                device.Name,
                device.SerialNumber,
                device.DeviceType.ToString(),
                device.Platform,
                device.Status.ToString(),
                device.BatteryPercentage,
                device.IsCharging,
                device.NetworkStatus,
                device.IpAddress,
                device.AppVersion,
                device.LastSeenAt,
                device.LinkedTableId?.Value,
                device.IsLocked,
                device.PrinterPort,
                device.PaperWidthMm,
                device.AutoPrintKitchenTickets,
                device.AutoPrintCustomerReceipts,
                device.AutoPrintFiscalDocuments))
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<DeviceDto>>(result);
    }

    public Task<IReadOnlyCollection<PrintJobDto>> ListPrintJobsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var printers = context.Devices.ToDictionary(device => device.Id, device => device.Name);
        var result = context.PrintJobs
            .OrderByDescending(job => job.CreatedAt)
            .Take(100)
            .ToArray()
            .Select(job => new PrintJobDto(
                job.Id.Value, job.PrinterId.Value,
                printers.GetValueOrDefault(job.PrinterId, "Impressora removida"),
                job.DocumentType.ToString(), job.Status.ToString(), job.Attempts,
                job.LastError, job.CreatedAt, job.CompletedAt))
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<PrintJobDto>>(result);
    }

    public Task<IReadOnlyCollection<AuditLogDto>> ListAuditLogsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var employees = context.Employees.ToDictionary(employee => employee.Id, employee => employee.DisplayName);
        var kitchenTickets = context.KitchenTickets.ToDictionary(ticket => ticket.Id.Value, ticket => $"Ticket #{ticket.TicketNumber}");
        var result = context.AuditLogs
            .OrderByDescending(log => log.OccurredAt)
            .Take(250)
            .ToArray()
            .Select(log => new AuditLogDto(
                log.Id.Value,
                log.Module,
                log.Action,
                log.EntityType,
                log.EntityId,
                DescribeAuditEntity(log.EntityType, log.EntityId, kitchenTickets),
                log.EmployeeId.HasValue ? employees.GetValueOrDefault(log.EmployeeId.Value) : null,
                log.OccurredAt))
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<AuditLogDto>>(result);
    }

    public Task<SystemSnapshotDto> CreateSystemSnapshotAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new SystemSnapshotDto(
            DateTimeOffset.UtcNow,
            ToDto(GetUnit()),
            context.Categories.Count(),
            context.Products.Count(),
            context.RestaurantTables.Count(),
            context.Orders.Count(),
            context.Payments.Count(),
            context.Devices.Count()));
    }

    public Task<IReadOnlyCollection<DiningAreaAdminDto>> ListDiningAreasAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = context.DiningAreas
            .OrderBy(area => area.DisplayOrder)
            .ThenBy(area => area.Name)
            .Select(area => new DiningAreaAdminDto(area.Id.Value, area.Name, area.DisplayOrder, area.IsActive))
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<DiningAreaAdminDto>>(result);
    }

    public Task<IReadOnlyCollection<RestaurantTableAdminDto>> ListRestaurantTableSettingsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var areas = context.DiningAreas.ToDictionary(area => area.Id, area => area.Name);
        var result = context.RestaurantTables
            .OrderBy(table => table.DisplayOrder)
            .ThenBy(table => table.Number)
            .ToArray()
            .Select(table => new RestaurantTableAdminDto(
                table.Id.Value,
                table.DiningAreaId.Value,
                areas.GetValueOrDefault(table.DiningAreaId, "Área removida"),
                table.Number,
                table.Name,
                table.Capacity,
                table.DisplayOrder,
                table.IsActive))
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<RestaurantTableAdminDto>>(result);
    }

    public Task<IReadOnlyCollection<ProductionStationAdminDto>> ListProductionStationsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = context.ProductionStations
            .OrderBy(station => station.DisplayOrder)
            .Select(station => new ProductionStationAdminDto(
                station.Id.Value,
                station.Name,
                station.Code,
                station.TargetPreparationMinutes,
                station.DisplayOrder,
                station.IsActive))
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<ProductionStationAdminDto>>(result);
    }

    public Task<IReadOnlyCollection<ServiceCallTypeAdminDto>> ListServiceCallTypesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = context.ServiceCallTypes
            .OrderBy(type => type.Name)
            .Select(type => new ServiceCallTypeAdminDto(type.Id.Value, type.Code, type.Name, type.IsActive))
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<ServiceCallTypeAdminDto>>(result);
    }

    public Task<IReadOnlyCollection<InventoryItemAdminDto>> ListInventoryItemsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var balances = context.StockBalances.ToDictionary(balance => balance.InventoryItemId);
        var result = context.InventoryItems
            .OrderBy(item => item.Name)
            .ToArray()
            .Select(item =>
            {
                var balance = balances.GetValueOrDefault(item.Id);
                var current = balance?.CurrentQuantity ?? 0;
                var reserved = balance?.ReservedQuantity ?? 0;
                var available = current - reserved;
                return new InventoryItemAdminDto(
                    item.Id.Value,
                    item.Name,
                    item.Sku,
                    item.UnitOfMeasure,
                    item.MinimumStock,
                    item.UnitCost.Amount,
                    current,
                    reserved,
                    available,
                    item.IsActive && available <= item.MinimumStock,
                    item.IsActive);
            })
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<InventoryItemAdminDto>>(result);
    }

    public Task<IReadOnlyCollection<RecipeAdminDto>> ListRecipesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var products = context.Products.ToDictionary(product => product.Id, product => product.Name);
        var flavors = context.PizzaFlavors.ToDictionary(flavor => flavor.Id, flavor => flavor.Name);
        var sizes = context.PizzaSizes.ToDictionary(size => size.Id, size => size.Name);
        var inventoryItems = context.InventoryItems.ToDictionary(item => item.Id, item => item.Name);
        var ingredients = context.RecipeItems.ToArray().GroupBy(item => item.RecipeId).ToDictionary(group => group.Key, group => group.ToArray());
        var result = context.Recipes.OrderBy(recipe => recipe.Id).ToArray().Select(recipe => new RecipeAdminDto(
            recipe.Id.Value,
            recipe.ProductId?.Value,
            recipe.ProductId.HasValue ? products.GetValueOrDefault(recipe.ProductId.Value) : null,
            recipe.PizzaFlavorId?.Value,
            recipe.PizzaFlavorId.HasValue ? flavors.GetValueOrDefault(recipe.PizzaFlavorId.Value) : null,
            recipe.PizzaSizeId?.Value,
            recipe.PizzaSizeId.HasValue ? sizes.GetValueOrDefault(recipe.PizzaSizeId.Value) : null,
            recipe.YieldQuantity,
            ingredients.GetValueOrDefault(recipe.Id, []).Select(item => new RecipeItemAdminDto(
                item.InventoryItemId.Value,
                inventoryItems.GetValueOrDefault(item.InventoryItemId, "Item removido"),
                item.Quantity,
                item.UnitOfMeasure)).ToArray())).ToArray();
        return Task.FromResult<IReadOnlyCollection<RecipeAdminDto>>(result);
    }

    public async Task UpdateUnitAsync(UpdateUnitCommand command, Guid identityUserId, CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var unit = GetUnit();
        unit.UpdateIdentification(command.Name, command.LegalName, command.TradeName, command.Cnpj);
        unit.UpdateContactInformation(command.Phone, command.AdministrativeEmail);
        AddAudit(unit.Id, employee.Id, "Core", "Update", nameof(RestaurantUnit), unit.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateOperationSettingsAsync(
        UpdateOperationSettingsCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var settings = context.OperationSettings.Single();
        settings.Update(
            command.AllowTableWithoutWaiter,
            command.AllowOrdersWithoutOpenCashShift,
            command.ClearTabletAfterTableClose,
            new Percentage(command.ServiceFeePercentage),
            new Money(command.DefaultDeliveryFee),
            command.DeliveryOrderSoundEnabled,
            command.TableCallSoundEnabled,
            command.TableCallToleranceMinutes);
        AddAudit(settings.UnitId, employee.Id, "Core", "Update", nameof(OperationSettings), settings.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePizzaRulesAsync(
        UpdatePizzaRulesCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var settings = context.PizzaSettings.Single();
        if (!Enum.TryParse<PizzaPricingPolicy>(command.PricingPolicy, true, out var pricingPolicy))
        {
            throw new BusinessRuleException("pizza_settings.pricing_policy", "Unknown pizza pricing policy.");
        }

        settings.Update(
            command.GlobalMaxFlavors,
            pricingPolicy,
            command.AllowSweetAndSavoryMix,
            command.AllowExtrasPerFlavor,
            command.AllowRepeatedFlavors);
        AddAudit(settings.UnitId, employee.Id, "Core", "Update", nameof(PizzaSettings), settings.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<CommandResultDto> SaveCategoryAsync(
        SaveCategoryCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var unit = GetUnit();
        Category category;
        var action = "Update";
        if (command.Id.HasValue)
        {
            var categoryId = new CategoryId(command.Id.Value);
            category = context.Categories.Single(item => item.Id == categoryId);
        }
        else
        {
            category = new Category(CategoryId.New(), unit.Id, command.Name, command.Slug, context.Categories.Count());
            context.Add(category);
            action = "Create";
        }

        category.Update(command.Name, command.Slug, command.Description, command.IsVisibleOnTablet, command.IsActive);
        AddAudit(unit.Id, employee.Id, "Catalog", action, nameof(Category), category.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(category.Id.Value, category.IsActive ? "Active" : "Inactive");
    }

    public async Task<CommandResultDto> SaveProductAsync(
        SaveProductCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var unit = GetUnit();
        if (!Enum.TryParse<ProductType>(command.Type, true, out var productType))
        {
            throw new BusinessRuleException("product.type", "Unknown product type.");
        }

        var categoryId = new CategoryId(command.CategoryId);
        if (!context.Categories.Any(category => category.Id == categoryId))
        {
            throw new BusinessRuleException("product.category", "Category does not exist.");
        }

        Product product;
        var action = "Update";
        if (command.Id.HasValue)
        {
            var productId = new ProductId(command.Id.Value);
            product = context.Products.Single(item => item.Id == productId);
            product.ChangeCategory(categoryId);
            product.ChangePrice(new Money(command.BasePrice));
        }
        else
        {
            product = new Product(
                ProductId.New(),
                unit.Id,
                categoryId,
                command.Sku,
                command.Name,
                productType,
                new Money(command.BasePrice));
            context.Add(product);
            action = "Create";
        }

        product.UpdateInformation(command.Name, command.Description, command.PreparationTimeMinutes);
        product.SetActive(command.IsActive);
        product.SetAvailable(command.IsAvailable);
        if (command.IsFeatured)
        {
            product.MarkAsFeatured();
        }
        else
        {
            product.RemoveFromFeatured();
        }

        if (command.Complements is not null)
        {
            SynchronizeProductExtras(product, command.Complements, unit.Id);
        }

        AddAudit(unit.Id, employee.Id, "Catalog", action, nameof(Product), product.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(product.Id.Value, product.IsActive ? "Active" : "Inactive");
    }

    public async Task<CommandResultDto> SaveProductImageAsync(
        Guid productId,
        Stream content,
        string contentType,
        string fileName,
        string altText,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var storage = menuImageStorage
            ?? throw new InvalidOperationException("Menu image storage is not configured.");
        var employee = GetEmployee(identityUserId);
        var unit = GetUnit();
        var id = new ProductId(productId);
        var product = context.Products.Single(candidate => candidate.Id == id && candidate.UnitId == unit.Id);
        var previous = context.ProductImages
            .Where(image => image.ProductId == id)
            .OrderByDescending(image => image.IsPrimary)
            .ThenBy(image => image.DisplayOrder)
            .FirstOrDefault();
        var previousUrl = previous?.Url;
        var publicUrl = await storage.StoreAsync(content, contentType, fileName, cancellationToken);
        try
        {
            if (previous is null)
            {
                previous = new ProductImage(ProductImageId.New(), id, publicUrl, altText);
                context.Add(previous);
            }
            previous.Update(publicUrl, altText, isPrimary: true);
            AddAudit(unit.Id, employee.Id, "Catalog", "UpdateImage", nameof(Product), product.Id.Value);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await storage.DeleteAsync(publicUrl, CancellationToken.None);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(previousUrl) && previousUrl != publicUrl)
        {
            await storage.DeleteAsync(previousUrl, cancellationToken);
        }
        return new CommandResultDto(product.Id.Value, publicUrl);
    }

    private void SynchronizeProductExtras(
        Product product,
        IReadOnlyCollection<SaveProductExtraCommand> requestedComplements,
        RestaurantUnitId unitId)
    {
        if (product.ProductType != ProductType.Pizza)
        {
            if (requestedComplements.Count > 0)
            {
                throw new BusinessRuleException(
                    "product.extras_type",
                    "Only pizza products can configure complements.");
            }

            return;
        }

        var existingIngredients = context.Ingredients
            .Where(ingredient => ingredient.UnitId == unitId)
            .ToArray();
        var currentLinks = context.ProductExtras
            .Where(link => link.ProductId == product.Id)
            .ToArray();
        var selectedIngredientIds = new HashSet<IngredientId>();

        foreach (var requested in requestedComplements)
        {
            var normalizedName = requested.Name?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                throw new BusinessRuleException(
                    "product.extra_name",
                    "Complement name is required.");
            }

            Ingredient? ingredient;
            if (requested.IngredientId.HasValue)
            {
                var ingredientId = new IngredientId(requested.IngredientId.Value);
                ingredient = existingIngredients.SingleOrDefault(candidate => candidate.Id == ingredientId)
                    ?? throw new BusinessRuleException(
                        "product.extra_ingredient",
                        "Complement ingredient does not exist in this restaurant unit.");
            }
            else
            {
                ingredient = existingIngredients.SingleOrDefault(candidate =>
                    string.Equals(candidate.Name, normalizedName, StringComparison.OrdinalIgnoreCase));
                if (ingredient is null)
                {
                    ingredient = new Ingredient(IngredientId.New(), unitId, normalizedName);
                    context.Add(ingredient);
                    existingIngredients = [.. existingIngredients, ingredient];
                }
            }

            if (!selectedIngredientIds.Add(ingredient.Id))
            {
                throw new BusinessRuleException(
                    "product.extra_duplicate",
                    "The same complement cannot be selected more than once.");
            }

            if (!ingredient.IsActive || !ingredient.IsAvailableAsExtra)
            {
                ingredient.Update(
                    ingredient.Name,
                    ingredient.Description,
                    isActive: true,
                    isAllergen: ingredient.IsAllergen,
                    allergenDescription: ingredient.AllergenDescription,
                    isAvailableAsExtra: true,
                    extraPrice: new Money(requested.Price),
                    maxExtraQuantity: requested.MaxQuantity);
            }

            var link = currentLinks.SingleOrDefault(candidate => candidate.IngredientId == ingredient.Id);
            if (link is null)
            {
                context.Add(new ProductExtra(
                    product.Id,
                    ingredient.Id,
                    new Money(requested.Price),
                    requested.MaxQuantity));
            }
            else
            {
                link.Update(new Money(requested.Price), requested.MaxQuantity, isActive: true);
            }
        }

        foreach (var removed in currentLinks.Where(link => !selectedIngredientIds.Contains(link.IngredientId)))
        {
            removed.Update(removed.Price, removed.MaxQuantity, isActive: false);
        }

        product.ConfigureCustomExtras(true);
    }

    public async Task<CommandResultDto> SavePizzaSizeAsync(
        SavePizzaSizeCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var unit = GetUnit();
        PizzaSize size;
        var action = "Update";
        if (command.Id.HasValue)
        {
            var sizeId = new PizzaSizeId(command.Id.Value);
            size = context.PizzaSizes.Single(item => item.Id == sizeId);
        }
        else
        {
            size = new PizzaSize(
                PizzaSizeId.New(),
                unit.Id,
                command.Name,
                command.ShortName,
                command.Slices,
                command.DiameterCm,
                new Money(command.BasePrice),
                command.MaxFlavors,
                context.PizzaSizes.Count());
            context.Add(size);
            action = "Create";
        }

        size.Update(
            command.Name,
            command.ShortName,
            command.Slices,
            command.DiameterCm,
            new Money(command.BasePrice),
            command.MaxFlavors,
            command.IsActive);
        AddAudit(unit.Id, employee.Id, "Catalog", action, nameof(PizzaSize), size.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(size.Id.Value, size.IsActive ? "Active" : "Inactive");
    }

    public async Task<CommandResultDto> SavePizzaCrustAsync(
        SavePizzaCrustCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var unit = GetUnit();
        PizzaCrust crust;
        var action = "Update";
        if (command.Id.HasValue)
        {
            var crustId = new PizzaCrustId(command.Id.Value);
            crust = context.PizzaCrusts.Single(item => item.Id == crustId && item.UnitId == unit.Id);
        }
        else
        {
            crust = new PizzaCrust(PizzaCrustId.New(), unit.Id, command.Name, command.Description);
            context.Add(crust);
            action = "Create";
        }

        crust.Update(command.Name, command.Description, command.IsActive, command.IsAvailable);
        if (command.Prices is not null)
        {
            var duplicateSize = command.Prices
                .GroupBy(price => price.PizzaSizeId)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateSize is not null)
            {
                throw new BusinessRuleException("pizza_crust.duplicate_size", "A crust price can be configured only once per pizza size.");
            }

            var unitSizes = context.PizzaSizes
                .Where(size => size.UnitId == unit.Id)
                .ToArray()
                .ToDictionary(size => size.Id);
            var currentPrices = context.PizzaCrustPrices
                .Where(price => price.PizzaCrustId == crust.Id)
                .ToArray()
                .ToDictionary(price => price.PizzaSizeId);
            foreach (var requestedPrice in command.Prices)
            {
                var sizeId = new PizzaSizeId(requestedPrice.PizzaSizeId);
                if (!unitSizes.ContainsKey(sizeId))
                {
                    throw new BusinessRuleException("pizza_crust.invalid_size", "The selected pizza size does not belong to this restaurant unit.");
                }

                var fullPrice = new Money(requestedPrice.FullPrice);
                var halfPrice = new Money(requestedPrice.HalfPrice);
                if (currentPrices.TryGetValue(sizeId, out var currentPrice))
                {
                    currentPrice.Update(fullPrice, halfPrice);
                }
                else
                {
                    context.Add(new PizzaCrustPrice(
                        PizzaCrustPriceId.New(),
                        crust.Id,
                        sizeId,
                        fullPrice,
                        halfPrice));
                }
            }
        }

        AddAudit(unit.Id, employee.Id, "Catalog", action, nameof(PizzaCrust), crust.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(crust.Id.Value, crust.IsAvailable ? "Available" : "Unavailable");
    }

    public async Task<CommandResultDto> SaveIngredientAsync(
        SaveIngredientCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var unit = GetUnit();
        Ingredient ingredient;
        var action = "Update";
        if (command.Id.HasValue)
        {
            var ingredientId = new IngredientId(command.Id.Value);
            ingredient = context.Ingredients.Single(item =>
                item.Id == ingredientId && item.UnitId == unit.Id);
        }
        else
        {
            ingredient = new Ingredient(IngredientId.New(), unit.Id, command.Name);
            context.Add(ingredient);
            action = "Create";
        }

        ingredient.Update(
            command.Name,
            command.Description,
            command.IsActive,
            command.IsAllergen,
            command.AllergenDescription,
            command.IsAvailableAsExtra,
            new Money(command.ExtraPrice),
            command.MaxExtraQuantity);
        AddAudit(unit.Id, employee.Id, "Catalog", action, nameof(Ingredient), ingredient.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(
            ingredient.Id.Value,
            ingredient.IsAvailableAsExtra ? "AvailableAsExtra" : "UnavailableAsExtra");
    }

    public async Task<CommandResultDto> SavePizzaFlavorAsync(
        SavePizzaFlavorCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var unit = GetUnit();
        var categoryId = new CategoryId(command.CategoryId);
        if (!context.Categories.Any(category => category.Id == categoryId))
        {
            throw new BusinessRuleException("pizza_flavor.category", "Category does not exist.");
        }

        if (!Enum.TryParse<PizzaFlavorType>(command.Type, true, out var flavorType))
        {
            throw new BusinessRuleException("pizza_flavor.type", "Unknown pizza flavor type.");
        }

        PizzaFlavor flavor;
        var action = "Update";
        if (command.Id.HasValue)
        {
            var flavorId = new PizzaFlavorId(command.Id.Value);
            flavor = context.PizzaFlavors.Single(item => item.Id == flavorId);
        }
        else
        {
            flavor = new PizzaFlavor(PizzaFlavorId.New(), unit.Id, categoryId, command.Name, flavorType);
            context.Add(flavor);
            action = "Create";
        }

        flavor.Update(
            command.Name,
            command.Description,
            flavorType,
            command.IsPremium,
            command.IsVegetarian,
            command.IsActive,
            command.IsAvailable,
            command.SoldOutReason);
        SynchronizeFlavorExtras(flavor, command.Extras, unit.Id);
        AddAudit(unit.Id, employee.Id, "Catalog", action, nameof(PizzaFlavor), flavor.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(flavor.Id.Value, flavor.IsAvailable ? "Available" : "Unavailable");
    }

    public async Task<CommandResultDto> SavePizzaFlavorImageAsync(
        Guid flavorId,
        Stream content,
        string contentType,
        string fileName,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var storage = menuImageStorage
            ?? throw new InvalidOperationException("Menu image storage is not configured.");
        var employee = GetEmployee(identityUserId);
        var unit = GetUnit();
        var flavor = context.PizzaFlavors.Single(candidate =>
            candidate.Id == new PizzaFlavorId(flavorId) && candidate.UnitId == unit.Id);
        var previousUrl = flavor.ImageUrl;
        var publicUrl = await storage.StoreAsync(content, contentType, fileName, cancellationToken);
        try
        {
            flavor.SetImage(publicUrl);
            AddAudit(unit.Id, employee.Id, "Catalog", "UpdateImage", nameof(PizzaFlavor), flavor.Id.Value);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await storage.DeleteAsync(publicUrl, CancellationToken.None);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(previousUrl))
        {
            await storage.DeleteAsync(previousUrl, cancellationToken);
        }
        return new CommandResultDto(flavor.Id.Value, publicUrl);
    }

    private void SynchronizeFlavorExtras(
        PizzaFlavor flavor,
        IReadOnlyCollection<SavePizzaFlavorExtraCommand>? requestedExtras,
        RestaurantUnitId unitId)
    {
        if (requestedExtras is null)
        {
            return;
        }

        if (requestedExtras
            .GroupBy(extra => extra.IngredientId)
            .Any(group => group.Count() > 1))
        {
            throw new BusinessRuleException(
                "pizza_flavor.extra_duplicate",
                "The same extra ingredient cannot be linked more than once.");
        }

        var existingExtras = context.PizzaFlavorExtras
            .Where(extra => extra.PizzaFlavorId == flavor.Id)
            .ToArray()
            .ToDictionary(extra => extra.IngredientId);
        var requestedIngredientIds = requestedExtras
            .Select(extra => new IngredientId(extra.IngredientId))
            .ToHashSet();

        foreach (var existing in existingExtras.Values
                     .Where(extra => !requestedIngredientIds.Contains(extra.IngredientId)))
        {
            existing.Update(existing.Price, existing.MaxQuantity, isActive: false);
        }

        foreach (var requested in requestedExtras)
        {
            var ingredientId = new IngredientId(requested.IngredientId);
            var ingredientExists = context.Ingredients.Any(ingredient =>
                ingredient.Id == ingredientId &&
                ingredient.UnitId == unitId &&
                ingredient.IsActive &&
                ingredient.IsAvailableAsExtra);
            if (!ingredientExists)
            {
                throw new BusinessRuleException(
                    "pizza_flavor.extra_unavailable",
                    "An extra ingredient is inactive or unavailable.");
            }

            var price = new Money(requested.Price);
            if (existingExtras.TryGetValue(ingredientId, out var existing))
            {
                existing.Update(price, requested.MaxQuantity, isActive: true);
            }
            else
            {
                context.Add(new PizzaFlavorExtra(
                    flavor.Id,
                    ingredientId,
                    price,
                    requested.MaxQuantity));
            }
        }
    }

    public async Task<CommandResultDto> SaveDiningAreaAsync(
        SaveDiningAreaCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var unit = GetUnit();
        if (context.DiningAreas.Any(area =>
                (!command.Id.HasValue || area.Id.Value != command.Id.Value) &&
                area.Name.ToLower() == command.Name.Trim().ToLower()))
        {
            throw new BusinessRuleException("dining_area.name_duplicate", "A dining area with this name already exists.");
        }

        DiningArea area;
        var action = command.Id.HasValue ? "Update" : "Create";
        if (command.Id.HasValue)
        {
            area = context.DiningAreas.Single(candidate => candidate.Id.Value == command.Id.Value);
            if (!command.IsActive && context.RestaurantTables.Any(table => table.DiningAreaId == area.Id && table.IsActive))
            {
                throw new BusinessRuleException("dining_area.active_tables", "A dining area with active tables cannot be deactivated.");
            }

            area.Update(command.Name, command.DisplayOrder, command.IsActive);
        }
        else
        {
            area = new DiningArea(DiningAreaId.New(), unit.Id, command.Name, command.DisplayOrder);
            area.Update(command.Name, command.DisplayOrder, command.IsActive);
            context.Add(area);
        }

        AddAudit(unit.Id, employee.Id, "Dining", action, nameof(DiningArea), area.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(area.Id.Value, action == "Create" ? "Created" : "Updated");
    }

    public async Task<CommandResultDto> SaveRestaurantTableAsync(
        SaveRestaurantTableCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var unit = GetUnit();
        var area = context.DiningAreas.Single(candidate => candidate.Id.Value == command.DiningAreaId);
        if (area.UnitId != unit.Id || !area.IsActive)
        {
            throw new BusinessRuleException("restaurant_table.area", "The selected dining area is not active in this unit.");
        }

        if (context.RestaurantTables.Any(table =>
                (!command.Id.HasValue || table.Id.Value != command.Id.Value) &&
                table.UnitId == unit.Id &&
                table.Number == command.Number))
        {
            throw new BusinessRuleException("restaurant_table.number_duplicate", "A table with this number already exists.");
        }

        RestaurantTable table;
        var action = command.Id.HasValue ? "Update" : "Create";
        if (command.Id.HasValue)
        {
            table = context.RestaurantTables.Single(candidate => candidate.Id.Value == command.Id.Value);
            if (!command.IsActive && HasOpenTableSession(table.Id))
            {
                throw new BusinessRuleException("restaurant_table.open_session", "A table with an open session cannot be deactivated.");
            }
        }
        else
        {
            table = new RestaurantTable(RestaurantTableId.New(), unit.Id, area.Id, command.Number, command.Capacity, command.Name);
            context.Add(table);
        }

        table.Update(area.Id, command.Number, command.Name, command.Capacity, command.DisplayOrder, command.IsActive);
        AddAudit(unit.Id, employee.Id, "Dining", action, nameof(RestaurantTable), table.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(table.Id.Value, action == "Create" ? "Created" : "Updated");
    }

    public async Task<CommandResultDto> SaveCashRegisterAsync(
        SaveCashRegisterCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var unit = GetUnit();
        var code = command.Code.Trim().ToUpperInvariant();
        if (context.CashRegisters.Any(register =>
                (!command.Id.HasValue || register.Id.Value != command.Id.Value) && register.Code == code))
        {
            throw new BusinessRuleException("cash_register.code_duplicate", "A cash register with this code already exists.");
        }

        CashRegister register;
        var action = command.Id.HasValue ? "Update" : "Create";
        if (command.Id.HasValue)
        {
            register = context.CashRegisters.Single(candidate => candidate.Id.Value == command.Id.Value);
            if (!command.IsActive && context.CashShifts.Any(shift =>
                    shift.CashRegisterId == register.Id &&
                    (shift.Status == CashShiftStatus.Open || shift.Status == CashShiftStatus.Closing)))
            {
                throw new BusinessRuleException("cash_register.open_shift", "A cash register with an open shift cannot be deactivated.");
            }
        }
        else
        {
            register = new CashRegister(CashRegisterId.New(), unit.Id, command.Name, code);
            context.Add(register);
        }

        register.Update(command.Name, code, command.IsActive);
        AddAudit(unit.Id, employee.Id, "Cashier", action, nameof(CashRegister), register.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(register.Id.Value, action == "Create" ? "Created" : "Updated");
    }

    public async Task<CommandResultDto> SavePaymentMethodAsync(
        SavePaymentMethodCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var unit = GetUnit();
        var code = command.Code.Trim().ToUpperInvariant();
        if (context.PaymentMethods.Any(method =>
                (!command.Id.HasValue || method.Id.Value != command.Id.Value) && method.Code == code))
        {
            throw new BusinessRuleException("payment_method.code_duplicate", "A payment method with this code already exists.");
        }

        PaymentMethod method;
        var action = command.Id.HasValue ? "Update" : "Create";
        if (command.Id.HasValue)
        {
            method = context.PaymentMethods.Single(candidate => candidate.Id.Value == command.Id.Value);
        }
        else
        {
            method = new PaymentMethod(
                PaymentMethodId.New(), unit.Id, code, command.Name,
                command.RequiresExternalReference, command.AllowsChange, command.DisplayOrder);
            context.Add(method);
        }

        method.Update(
            code, command.Name, command.RequiresExternalReference,
            command.AllowsChange, command.DisplayOrder, command.IsActive);
        AddAudit(unit.Id, employee.Id, "Billing", action, nameof(PaymentMethod), method.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(method.Id.Value, action == "Create" ? "Created" : "Updated");
    }

    public async Task<CommandResultDto> SaveProductionStationAsync(
        SaveProductionStationCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var unit = GetUnit();
        var code = command.Code.Trim().ToUpperInvariant();
        if (context.ProductionStations.Any(station =>
                (!command.Id.HasValue || station.Id.Value != command.Id.Value) && station.Code == code))
        {
            throw new BusinessRuleException("production_station.code_duplicate", "A production station with this code already exists.");
        }

        ProductionStation station;
        var action = command.Id.HasValue ? "Update" : "Create";
        if (command.Id.HasValue)
        {
            station = context.ProductionStations.Single(candidate => candidate.Id.Value == command.Id.Value);
            if (!command.IsActive && context.KitchenTickets.Any(ticket =>
                    ticket.ProductionStationId == station.Id &&
                    ticket.Status != KitchenTicketStatus.Dispatched &&
                    ticket.Status != KitchenTicketStatus.Cancelled))
            {
                throw new BusinessRuleException("production_station.active_tickets", "A station with active tickets cannot be deactivated.");
            }
        }
        else
        {
            station = new ProductionStation(
                ProductionStationId.New(), unit.Id, command.Name, code,
                command.TargetPreparationMinutes, command.DisplayOrder);
            context.Add(station);
        }

        station.Update(command.Name, code, command.TargetPreparationMinutes, command.DisplayOrder, command.IsActive);
        AddAudit(unit.Id, employee.Id, "Production", action, nameof(ProductionStation), station.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(station.Id.Value, action == "Create" ? "Created" : "Updated");
    }

    public async Task<CommandResultDto> SaveServiceCallTypeAsync(
        SaveServiceCallTypeCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var unit = GetUnit();
        var code = command.Code.Trim().ToUpperInvariant();
        if (context.ServiceCallTypes.Any(type =>
                (!command.Id.HasValue || type.Id.Value != command.Id.Value) && type.Code == code))
        {
            throw new BusinessRuleException("service_call_type.code_duplicate", "A service call type with this code already exists.");
        }

        ServiceCallType type;
        var action = command.Id.HasValue ? "Update" : "Create";
        if (command.Id.HasValue)
        {
            type = context.ServiceCallTypes.Single(candidate => candidate.Id.Value == command.Id.Value);
            if (!command.IsActive && context.ServiceCalls.Any(call =>
                    call.ServiceCallTypeId == type.Id &&
                    call.Status != ServiceCallStatus.Completed &&
                    call.Status != ServiceCallStatus.Cancelled))
            {
                throw new BusinessRuleException("service_call_type.active_calls", "A call type with active requests cannot be deactivated.");
            }
        }
        else
        {
            type = new ServiceCallType(ServiceCallTypeId.New(), code, command.Name);
            context.Add(type);
        }

        type.Update(code, command.Name, command.IsActive);
        AddAudit(unit.Id, employee.Id, "Dining", action, nameof(ServiceCallType), type.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(type.Id.Value, action == "Create" ? "Created" : "Updated");
    }

    public async Task<CommandResultDto> SaveInventoryItemAsync(
        SaveInventoryItemCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var unit = GetUnit();
        var sku = command.Sku.Trim().ToUpperInvariant();
        if (context.InventoryItems.Any(item =>
                (!command.Id.HasValue || item.Id.Value != command.Id.Value) && item.Sku == sku))
        {
            throw new BusinessRuleException("inventory_item.sku_duplicate", "An inventory item with this SKU already exists.");
        }

        InventoryItem item;
        var action = command.Id.HasValue ? "Update" : "Create";
        if (command.Id.HasValue)
        {
            item = context.InventoryItems.Single(candidate => candidate.Id.Value == command.Id.Value);
        }
        else
        {
            item = new InventoryItem(
                InventoryItemId.New(), unit.Id, command.Name, sku,
                command.UnitOfMeasure, command.MinimumStock);
            context.Add(item);
            context.Add(new StockBalance(StockBalanceId.New(), item.Id));
        }

        item.Update(command.Name, sku, command.UnitOfMeasure, command.MinimumStock, new Money(command.UnitCost), command.IsActive);
        AddAudit(unit.Id, employee.Id, "Inventory", action, nameof(InventoryItem), item.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(item.Id.Value, action == "Create" ? "Created" : "Updated");
    }

    public async Task<CommandResultDto> AdjustInventoryAsync(
        Guid id,
        AdjustInventoryCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        if (command.QuantityDelta == 0)
        {
            throw new BusinessRuleException("inventory_adjustment.quantity", "Inventory adjustment cannot be zero.");
        }

        var employee = GetEmployee(identityUserId);
        var item = context.InventoryItems.Single(candidate => candidate.Id.Value == id);
        var balance = context.StockBalances.SingleOrDefault(candidate => candidate.InventoryItemId == item.Id);
        if (balance is null)
        {
            balance = new StockBalance(StockBalanceId.New(), item.Id);
            context.Add(balance);
        }

        balance.ApplyAdjustment(command.QuantityDelta);
        context.Add(new StockMovement(
            StockMovementId.New(),
            item.Id,
            command.QuantityDelta > 0 ? StockMovementType.Entry : StockMovementType.Loss,
            Math.Abs(command.QuantityDelta),
            item.UnitCost,
            command.Reason,
            employee.Id));
        AddAudit(item.UnitId, employee.Id, "Inventory", "Adjust", nameof(InventoryItem), item.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(item.Id.Value, "Adjusted");
    }

    public async Task<CommandResultDto> SaveRecipeAsync(
        SaveRecipeCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        if (command.ProductId.HasValue == command.PizzaFlavorId.HasValue)
        {
            throw new BusinessRuleException("recipe.target", "Select exactly one product or pizza flavor for the recipe.");
        }
        var items = command.Items?.ToArray() ?? [];
        if (items.Length == 0 || items.GroupBy(item => item.InventoryItemId).Any(group => group.Count() > 1))
        {
            throw new BusinessRuleException("recipe.items", "A recipe needs unique inventory ingredients.");
        }
        foreach (var item in items)
        {
            _ = context.InventoryItems.Single(candidate => candidate.Id == new InventoryItemId(item.InventoryItemId) && candidate.IsActive);
        }

        var productId = command.ProductId.HasValue ? new ProductId(command.ProductId.Value) : (ProductId?)null;
        var flavorId = command.PizzaFlavorId.HasValue ? new PizzaFlavorId(command.PizzaFlavorId.Value) : (PizzaFlavorId?)null;
        var sizeId = command.PizzaSizeId.HasValue ? new PizzaSizeId(command.PizzaSizeId.Value) : (PizzaSizeId?)null;
        var duplicate = context.Recipes.Any(recipe =>
            (!command.Id.HasValue || recipe.Id.Value != command.Id.Value) &&
            recipe.ProductId == productId && recipe.PizzaFlavorId == flavorId && recipe.PizzaSizeId == sizeId);
        if (duplicate)
        {
            throw new BusinessRuleException("recipe.duplicate", "A recipe already exists for this target and size.");
        }

        Recipe recipe;
        var action = command.Id.HasValue ? "Update" : "Create";
        if (command.Id.HasValue)
        {
            recipe = context.Recipes.Single(candidate => candidate.Id == new RecipeId(command.Id.Value));
            foreach (var currentItem in context.RecipeItems.Where(item => item.RecipeId == recipe.Id).ToArray()) context.Remove(currentItem);
            recipe.Update(command.YieldQuantity, productId: productId, pizzaFlavorId: flavorId, pizzaSizeId: sizeId);
        }
        else
        {
            recipe = new Recipe(RecipeId.New(), command.YieldQuantity, productId: productId, pizzaFlavorId: flavorId, pizzaSizeId: sizeId);
            context.Add(recipe);
        }
        foreach (var item in items)
        {
            context.Add(new RecipeItem(
                RecipeItemId.New(), recipe.Id, new InventoryItemId(item.InventoryItemId),
                item.Quantity, item.UnitOfMeasure));
        }
        AddAudit(GetUnit().Id, employee.Id, "Inventory", action, nameof(Recipe), recipe.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(recipe.Id.Value, action == "Create" ? "Created" : "Updated");
    }

    public async Task<CommandResultDto> OpenTableAsync(
        OpenTableCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var unit = GetUnit();
        var tableId = new RestaurantTableId(command.TableId);
        var table = context.RestaurantTables.Single(item => item.Id == tableId);
        table.EnsureCanOpenSession();
        var openSessionIds = context.TableSessions
            .Where(session => session.Status != TableSessionStatus.Closed && session.Status != TableSessionStatus.Cancelled)
            .Select(session => session.Id)
            .ToHashSet();
        if (context.TableSessionTables.Any(link =>
                link.RestaurantTableId == tableId &&
                link.UnlinkedAt == null &&
                openSessionIds.Contains(link.TableSessionId)))
        {
            throw new BusinessRuleException("table.already_in_open_session", "Table already belongs to an open session.");
        }

        var sessionNumber = numberGenerator is null
            ? context.TableSessions.Any() ? context.TableSessions.Max(session => session.SessionNumber) + 1 : 1
            : await numberGenerator.NextTableSessionNumberAsync(cancellationToken);
        var settings = context.OperationSettings.Single();
        var session = TableSession.Open(
            TableSessionId.New(),
            unit.Id,
            sessionNumber,
            command.GuestCount,
            employee.Id,
            settings.ServiceFeePercentage,
            [table]);
        session.AssignWaiter(employee.Id);
        context.Add(session);
        AddAudit(unit.Id, employee.Id, "Dining", "Open", nameof(TableSession), session.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(session.Id.Value, session.Status.ToString());
    }

    public async Task<CommandResultDto> AssignTableWaiterAsync(
        Guid tableSessionId,
        AssignTableWaiterCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var session = context.TableSessions.Single(item => item.Id == new TableSessionId(tableSessionId));
        session.EnsureCanReceiveOrders();
        var waiter = context.Employees.Single(item => item.Id == new EmployeeId(command.EmployeeId));
        if (!waiter.IsActive || waiter.UnitId != session.UnitId)
        {
            throw new BusinessRuleException("table_session.waiter_unavailable", "The selected waiter is not available for this unit.");
        }
        session.AssignWaiter(waiter.Id);
        AddAudit(session.UnitId, employee.Id, "Dining", "AssignWaiter", nameof(TableSession), session.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(session.Id.Value, "WaiterAssigned");
    }

    public async Task<CommandResultDto> LinkTableAsync(
        Guid tableSessionId,
        LinkTableCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var session = context.TableSessions.Single(item => item.Id == new TableSessionId(tableSessionId));
        _ = context.TableSessionTables.Where(link => link.TableSessionId == session.Id).ToArray();
        var table = context.RestaurantTables.Single(item => item.Id == new RestaurantTableId(command.TableId));
        if (HasOpenTableSession(table.Id))
        {
            throw new BusinessRuleException("table.already_in_open_session", "Table already belongs to an open session.");
        }
        session.LinkTable(table, employee.Id);
        AddAudit(session.UnitId, employee.Id, "Dining", "LinkTable", nameof(TableSession), session.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(session.Id.Value, "TableLinked");
    }

    public async Task<CommandResultDto> TransferTableAsync(
        Guid tableSessionId,
        TransferTableCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var session = context.TableSessions.Single(item => item.Id == new TableSessionId(tableSessionId));
        _ = context.TableSessionTables.Where(link => link.TableSessionId == session.Id).ToArray();
        var target = context.RestaurantTables.Single(item => item.Id == new RestaurantTableId(command.TargetTableId));
        if (HasOpenTableSession(target.Id))
        {
            throw new BusinessRuleException("table.already_in_open_session", "Target table already belongs to an open session.");
        }
        session.TransferTable(new RestaurantTableId(command.CurrentTableId), target, employee.Id);
        AddAudit(session.UnitId, employee.Id, "Dining", "TransferTable", nameof(TableSession), session.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(session.Id.Value, "TableTransferred");
    }

    public async Task<CommandResultDto> RequestBillAsync(
        Guid tableSessionId,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var sessionId = new TableSessionId(tableSessionId);
        var session = context.TableSessions.Single(item => item.Id == sessionId);
        var bill = context.Bills
            .Where(item => item.TableSessionId == sessionId && item.Status != BillStatus.Cancelled)
            .ToArray()
            .OrderByDescending(item => item.RequestedAt)
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

            bill = new Bill(BillId.New(), session.UnitId, session.Id, new Money(subtotal), session.ServiceFeePercentageSnapshot);
            context.Add(bill);
        }

        if (bill.Status == BillStatus.Open)
        {
            bill.Request();
        }

        if (session.Status == TableSessionStatus.Open)
        {
            session.RequestBill();
        }

        AddAudit(session.UnitId, employee.Id, "Billing", "Request", nameof(Bill), bill.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(bill.Id.Value, bill.Status.ToString());
    }

    public async Task<CommandResultDto> TransitionOrderAsync(
        Guid id,
        string transition,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var orderId = new OrderId(id);
        var order = context.Orders.Single(item => item.Id == orderId);
        switch (transition.ToLowerInvariant())
        {
            case "accept": order.Accept(); break;
            case "start-production": order.StartProduction(); break;
            case "ready": order.MarkReady(); break;
            case "complete" when order.FulfillmentType == FulfillmentType.Delivery:
                throw new BusinessRuleException("order.delivery_dispatch", "Delivery orders must be dispatched before completion.");
            case "complete": order.Complete(); break;
            default: throw new BusinessRuleException("order.transition", "Unknown order transition.");
        }

        AddAudit(order.UnitId, employee.Id, "Ordering", transition, nameof(Order), order.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(order.Id.Value, order.Status.ToString());
    }

    public async Task<CommandResultDto> CancelOrderAsync(
        Guid id,
        CancelOrderCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var order = context.Orders.Single(item => item.Id == new OrderId(id));
        var bills = context.Bills.Where(bill =>
            bill.OrderId == order.Id || (order.TableSessionId.HasValue && bill.TableSessionId == order.TableSessionId)).ToArray();
        if (bills.Any(bill => bill.PaidAmount.Amount > 0))
        {
            throw new BusinessRuleException("order.cancel_paid", "Refund every payment before cancelling this order.");
        }

        order.Cancel(command.Reason);
        if (order.CustomerId.HasValue)
        {
            context.Customers.SingleOrDefault(customer => customer.Id == order.CustomerId.Value)?.ReversePurchase(order.Total);
        }
        foreach (var ticket in context.KitchenTickets.Where(ticket => ticket.OrderId == order.Id).ToArray())
        {
            if (ticket.Status != KitchenTicketStatus.Dispatched && ticket.Status != KitchenTicketStatus.Cancelled) ticket.Cancel();
        }
        foreach (var bill in bills)
        {
            if (bill.OrderId.HasValue)
            {
                bill.Cancel();
                continue;
            }
            var activeOrders = context.Orders
                .Where(candidate => candidate.TableSessionId == order.TableSessionId && candidate.Id != order.Id && candidate.Status != OrderStatus.Cancelled)
                .ToArray();
            if (activeOrders.Length == 0) bill.Cancel();
            else bill.Recalculate(
                new Money(activeOrders.Sum(candidate => candidate.Subtotal.Amount)),
                new Money(activeOrders.Sum(candidate => candidate.Discount.Amount)));
        }

        AddAudit(order.UnitId, employee.Id, "Ordering", "CancelApproved", nameof(Order), order.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(order.Id.Value, order.Status.ToString());
    }

    public async Task<CommandResultDto> ApplyOrderDiscountAsync(
        Guid id,
        ApplyOrderDiscountCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var order = context.Orders.Single(item => item.Id == new OrderId(id));
        _ = Guard.Required(command.Reason, nameof(command.Reason), 500);
        if (command.Amount < 0)
        {
            throw new BusinessRuleException("order.discount", "Discount cannot be negative.");
        }
        order.RecalculateTotals(discount: new Money(command.Amount));

        var bill = context.Bills.SingleOrDefault(candidate =>
            candidate.OrderId == order.Id || (order.TableSessionId.HasValue && candidate.TableSessionId == order.TableSessionId));
        if (bill is not null)
        {
            if (context.BillSplits.Any(split => split.BillId == bill.Id))
            {
                throw new BusinessRuleException("bill.discount_split", "A split bill cannot be discounted after its parts are created.");
            }
            if (bill.OrderId.HasValue) bill.ApplyDiscount(order.Discount);
            else
            {
                var sessionOrders = context.Orders
                    .Where(candidate => candidate.TableSessionId == order.TableSessionId && candidate.Status != OrderStatus.Cancelled)
                    .ToArray();
                bill.Recalculate(
                    new Money(sessionOrders.Sum(candidate => candidate.Subtotal.Amount)),
                    new Money(sessionOrders.Sum(candidate => candidate.Discount.Amount)));
            }
        }

        AddAudit(order.UnitId, employee.Id, "Ordering", "DiscountApproved", nameof(Order), order.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(order.Id.Value, order.Status.ToString());
    }

    public async Task<CommandResultDto> DispatchDeliveryAsync(
        Guid id,
        string driverName,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var order = context.Orders.Single(item => item.Id == new OrderId(id));
        order.DispatchDelivery(driverName);
        AddAudit(order.UnitId, employee.Id, "Ordering", "DispatchDelivery", nameof(Order), order.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(order.Id.Value, order.DeliveryStatus!.Value.ToString());
    }

    public async Task<CommandResultDto> CompleteDeliveryAsync(
        Guid id,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var order = context.Orders.Single(item => item.Id == new OrderId(id));
        order.CompleteDelivery();
        AddAudit(order.UnitId, employee.Id, "Ordering", "CompleteDelivery", nameof(Order), order.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(order.Id.Value, order.DeliveryStatus!.Value.ToString());
    }

    public async Task<CommandResultDto> FailDeliveryAsync(
        Guid id,
        string reason,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var order = context.Orders.Single(item => item.Id == new OrderId(id));
        order.FailDelivery(reason);
        AddAudit(order.UnitId, employee.Id, "Ordering", "FailDelivery", nameof(Order), order.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(order.Id.Value, order.DeliveryStatus!.Value.ToString());
    }

    public async Task<CommandResultDto> TransitionKitchenTicketAsync(
        Guid id,
        string transition,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var ticketId = new KitchenTicketId(id);
        var ticket = context.KitchenTickets.Single(item => item.Id == ticketId);
        var order = context.Orders.Single(item => item.Id == ticket.OrderId);
        switch (transition.ToLowerInvariant())
        {
            case "confirm":
                ticket.Confirm();
                if (order.Status == OrderStatus.Submitted) order.Accept();
                QueueKitchenTicketIfConfigured(ticket, order);
                break;
            case "start":
                ticket.StartPreparation();
                if (order.Status == OrderStatus.Accepted) order.StartProduction();
                break;
            case "ready":
                ticket.MarkReady();
                var otherTickets = context.KitchenTickets
                    .Where(item => item.OrderId == ticket.OrderId && item.Id != ticket.Id)
                    .ToArray();
                if (order.Status == OrderStatus.InProduction &&
                    otherTickets.All(item => item.Status is KitchenTicketStatus.Ready or KitchenTicketStatus.Dispatched))
                {
                    order.MarkReady();
                }
                break;
            case "dispatch":
                ticket.Dispatch();
                break;
            default:
                throw new BusinessRuleException("kitchen_ticket.transition", "Unknown kitchen ticket transition.");
        }

        AddAudit(ticket.UnitId, employee.Id, "Production", transition, nameof(KitchenTicket), ticket.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(ticket.Id.Value, ticket.Status.ToString());
    }

    private void QueueKitchenTicketIfConfigured(KitchenTicket ticket, Order order)
    {
        var printer = context.Devices.FirstOrDefault(device =>
            device.UnitId == ticket.UnitId && device.DeviceType == DeviceType.Printer &&
            device.Status == DeviceStatus.Online && device.PrinterPort.HasValue &&
            device.AutoPrintKitchenTickets);
        if (printer is null) return;
        context.Add(new PrintJob(
            PrintJobId.New(), ticket.UnitId, printer.Id,
            PrintDocumentType.KitchenTicket, FormatKitchenTicket(ticket, order)));
    }

    public async Task<CommandResultDto> AcknowledgeServiceCallAsync(
        Guid id,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var callId = new ServiceCallId(id);
        var call = context.ServiceCalls.Single(item => item.Id == callId);
        if (call.Status != ServiceCallStatus.Pending)
        {
            throw new BusinessRuleException(
                "service_call.not_pending",
                "Only a pending service call can be acknowledged.");
        }

        call.Acknowledge(employee.Id);
        AddAudit(call.UnitId, employee.Id, "Dining", "Acknowledge", nameof(ServiceCall), call.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(call.Id.Value, call.Status.ToString());
    }

    public async Task<CommandResultDto> CompleteServiceCallAsync(
        Guid id,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var callId = new ServiceCallId(id);
        var call = context.ServiceCalls.Single(item => item.Id == callId);
        call.Complete(employee.Id);
        AddAudit(call.UnitId, employee.Id, "Dining", "Complete", nameof(ServiceCall), call.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(call.Id.Value, call.Status.ToString());
    }

    public async Task<CommandResultDto> RecordPaymentAsync(
        RecordPaymentCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var billId = new BillId(command.BillId);
        var bill = context.Bills.Single(item => item.Id == billId);
        var methodId = new PaymentMethodId(command.PaymentMethodId);
        var method = context.PaymentMethods.Single(item => item.Id == methodId && item.IsActive);
        var cashShift = context.CashShifts
            .Where(shift => shift.Status == CashShiftStatus.Open)
            .OrderByDescending(shift => shift.OpenedAt)
            .ToArray()
            .FirstOrDefault();
        if (method.Code == "CASH" && cashShift is null)
        {
            throw new BusinessRuleException("payment.cash_shift", "Cash payments require an open cash shift.");
        }

        var amount = new Money(command.Amount);
        var payment = new Payment(
            PaymentId.New(),
            bill.UnitId,
            bill.Id,
            method,
            amount,
            new Money(command.ReceivedAmount),
            employee.Id,
            cashShiftId: cashShift?.Id,
            externalReference: command.ExternalReference);
        bill.RegisterPayment(amount);
        context.Add(payment);

        if (method.Code == "CASH" && cashShift is not null)
        {
            _ = context.CashMovements.Where(movement => movement.CashShiftId == cashShift.Id).ToArray();
            cashShift.RegisterMovement(
                CashMovementId.New(),
                CashMovementType.Sale,
                amount,
                $"Pagamento da conta {bill.Id.Value}",
                "Venda",
                employee.Id,
                paymentId: payment.Id);
        }

        if (bill.TableSessionId.HasValue)
        {
            var session = context.TableSessions.Single(item => item.Id == bill.TableSessionId.Value);
            if (bill.Status == BillStatus.Paid)
            {
                session.Close(employee.Id);
            }
            else if (session.Status == TableSessionStatus.BillRequested)
            {
                session.MarkPaymentPending();
            }
        }

        AddAudit(bill.UnitId, employee.Id, "Billing", "Pay", nameof(Bill), bill.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(payment.Id.Value, payment.Status.ToString());
    }

    public async Task<CommandResultDto> RecordSplitPaymentAsync(
        RecordSplitPaymentCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var parts = command.Payments?.ToArray() ?? [];
        if (parts.Length is < 2 or > 50)
        {
            throw new BusinessRuleException("bill_split.people_count", "A split payment must contain between 2 and 50 people.");
        }

        var employee = GetEmployee(identityUserId);
        var billId = new BillId(command.BillId);
        var bill = context.Bills.Single(item => item.Id == billId);
        var methods = context.PaymentMethods
            .Where(method => method.IsActive)
            .ToDictionary(method => method.Id);
        var splitAmounts = parts.Select(part => new Money(part.Amount)).ToArray();
        if (splitAmounts.Sum(amount => amount.Amount) != bill.RemainingAmount.Amount)
        {
            throw new BusinessRuleException("bill_split.total", "The split total must match the bill remaining amount.");
        }

        var selectedMethods = parts
            .Select(part => methods.GetValueOrDefault(new PaymentMethodId(part.PaymentMethodId))
                ?? throw new BusinessRuleException("payment.method", "The selected payment method is unavailable."))
            .ToArray();
        var cashShift = context.CashShifts
            .Where(shift => shift.Status == CashShiftStatus.Open)
            .OrderByDescending(shift => shift.OpenedAt)
            .ToArray()
            .FirstOrDefault();
        if (selectedMethods.Any(method => method.Code == "CASH") && cashShift is null)
        {
            throw new BusinessRuleException("payment.cash_shift", "Cash payments require an open cash shift.");
        }

        var nextSplitNumber = context.BillSplits
            .Where(split => split.BillId == bill.Id)
            .ToArray()
            .Select(split => split.SplitNumber)
            .DefaultIfEmpty()
            .Max() + 1;
        if (cashShift is not null)
        {
            _ = context.CashMovements.Where(movement => movement.CashShiftId == cashShift.Id).ToArray();
        }

        for (var index = 0; index < parts.Length; index++)
        {
            var part = parts[index];
            var amount = splitAmounts[index];
            var method = selectedMethods[index];
            var split = new BillSplit(
                BillSplitId.New(),
                bill.Id,
                part.Payer,
                nextSplitNumber + index,
                amount);
            var payment = new Payment(
                PaymentId.New(),
                bill.UnitId,
                bill.Id,
                method,
                amount,
                new Money(part.ReceivedAmount),
                employee.Id,
                split.Id,
                cashShift?.Id,
                part.ExternalReference);

            split.RegisterPayment(amount);
            bill.RegisterPayment(amount);
            context.Add(split);
            context.Add(payment);

            if (method.Code == "CASH" && cashShift is not null)
            {
                cashShift.RegisterMovement(
                    CashMovementId.New(),
                    CashMovementType.Sale,
                    amount,
                    $"Pagamento de {split.Name} na conta {bill.Id.Value}",
                    "Venda",
                    employee.Id,
                    paymentId: payment.Id);
            }
        }

        if (bill.TableSessionId.HasValue)
        {
            var session = context.TableSessions.Single(item => item.Id == bill.TableSessionId.Value);
            if (bill.Status == BillStatus.Paid)
            {
                session.Close(employee.Id);
            }
            else if (session.Status == TableSessionStatus.BillRequested)
            {
                session.MarkPaymentPending();
            }
        }

        AddAudit(bill.UnitId, employee.Id, "Billing", "SplitPayment", nameof(Bill), bill.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(bill.Id.Value, bill.Status.ToString());
    }

    public async Task<CommandResultDto> RefundPaymentAsync(
        Guid id,
        RefundPaymentCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var payment = context.Payments.Single(item => item.Id == new PaymentId(id));
        var bill = context.Bills.Single(item => item.Id == payment.BillId);
        var method = context.PaymentMethods.Single(item => item.Id == payment.PaymentMethodId);
        var amount = new Money(command.Amount);

        CashShift? cashShift = null;
        if (method.Code == "CASH")
        {
            cashShift = context.CashShifts
                .Where(shift => shift.Status == CashShiftStatus.Open)
                .OrderByDescending(shift => shift.OpenedAt)
                .ToArray()
                .FirstOrDefault();
            if (cashShift is null)
            {
                throw new BusinessRuleException("refund.cash_shift", "Cash refunds require an open cash shift.");
            }
        }

        payment.Refund(amount, command.Reason);
        bill.RegisterRefund(amount);
        if (payment.BillSplitId.HasValue)
        {
            var split = context.BillSplits.Single(item => item.Id == payment.BillSplitId.Value);
            split.RegisterRefund(amount);
        }
        if (cashShift is not null)
        {
            _ = context.CashMovements.Where(movement => movement.CashShiftId == cashShift.Id).ToArray();
            cashShift.RegisterMovement(
                CashMovementId.New(),
                CashMovementType.Refund,
                amount,
                $"Estorno do pagamento {payment.Id.Value}",
                command.Reason,
                employee.Id,
                authorizedByEmployeeId: employee.Id,
                paymentId: payment.Id);
        }
        if (bill.TableSessionId.HasValue)
        {
            var session = context.TableSessions.Single(item => item.Id == bill.TableSessionId.Value);
            if (session.Status == TableSessionStatus.Closed) session.ReopenPaymentAfterRefund();
        }

        AddAudit(payment.UnitId, employee.Id, "Billing", "RefundApproved", nameof(Payment), payment.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(payment.Id.Value, payment.Status.ToString());
    }

    public async Task<CashShiftDto> OpenCashShiftAsync(
        OpenCashShiftCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        if (context.CashShifts.Any(shift =>
                shift.Status == CashShiftStatus.Open ||
                shift.Status == CashShiftStatus.Closing))
        {
            throw new BusinessRuleException("cash_shift.already_open", "An open cash shift already exists.");
        }

        var registerId = new CashRegisterId(command.CashRegisterId);
        var register = context.CashRegisters
            .SingleOrDefault(candidate => candidate.Id == registerId && candidate.IsActive);
        if (register is null)
        {
            throw new BusinessRuleException("cash_register.unavailable", "Cash register is unavailable.");
        }

        var shift = new CashShift(CashShiftId.New(), register.Id, employee.Id, new Money(command.OpeningAmount));
        context.Add(shift);
        AddAudit(register.UnitId, employee.Id, "Cashier", "Open", nameof(CashShift), shift.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CashShiftDto(
            shift.Id.Value,
            register.Name,
            employee.DisplayName,
            shift.Status.ToString(),
            shift.OpenedAt,
            shift.OpeningAmount.Amount,
            shift.ExpectedCashAmount.Amount,
            null,
            null,
            []);
    }

    public async Task<CommandResultDto> RegisterCashMovementAsync(
        RegisterCashMovementCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var shift = context.CashShifts.Single(item => item.Status == CashShiftStatus.Open);
        _ = context.CashMovements.Where(movement => movement.CashShiftId == shift.Id).ToArray();
        if (!Enum.TryParse<CashMovementType>(command.Type, true, out var movementType) ||
            movementType is CashMovementType.Opening or CashMovementType.Closing or CashMovementType.Sale)
        {
            throw new BusinessRuleException("cash_movement.type", "Unsupported manual cash movement type.");
        }

        var movement = shift.RegisterMovement(
            CashMovementId.New(),
            movementType,
            new Money(command.Amount),
            command.Description,
            command.Reason,
            employee.Id);
        AddAudit(GetUnit().Id, employee.Id, "Cashier", "Create", nameof(CashMovement), movement.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(movement.Id.Value, movement.MovementType.ToString());
    }

    public async Task<CommandResultDto> CloseCashShiftAsync(
        CloseCashShiftCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var shift = context.CashShifts.Single(item => item.Status == CashShiftStatus.Open);
        _ = context.CashMovements.Where(movement => movement.CashShiftId == shift.Id).ToArray();
        shift.Close(employee.Id, new Money(command.CountedCashAmount), command.Notes);
        AddAudit(GetUnit().Id, employee.Id, "Cashier", "Close", nameof(CashShift), shift.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(shift.Id.Value, shift.Status.ToString());
    }

    public async Task<CommandResultDto> UpdateDeviceAsync(
        Guid id,
        UpdateDeviceCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var deviceId = new DeviceId(id);
        var device = context.Devices.Single(item => item.Id == deviceId);
        var previousLinkedTableId = device.LinkedTableId;
        if (!Enum.TryParse<DeviceStatus>(command.Status, true, out var status))
        {
            throw new BusinessRuleException("device.status", "Unknown device status.");
        }

        device.UpdateStatus(
            status,
            command.BatteryPercentage,
            command.IsCharging,
            command.NetworkStatus,
            command.IpAddress,
            command.AppVersion);
        var linkedTableId = command.LinkedTableId.HasValue
            ? new RestaurantTableId(command.LinkedTableId.Value)
            : (RestaurantTableId?)null;
        if (linkedTableId.HasValue &&
            !context.RestaurantTables.Any(table => table.Id == linkedTableId.Value))
        {
            throw new BusinessRuleException("device.table", "Linked table does not exist.");
        }

        device.LinkToTable(linkedTableId);
        device.SetLocked(command.IsLocked);
        if (previousLinkedTableId != linkedTableId || command.IsLocked)
        {
            RevokeDeviceAccess(device.Id, command.IsLocked
                ? "Tablet blocked by an administrator."
                : "Tablet table link changed by an administrator.");
        }
        AddAudit(device.UnitId, employee.Id, "Devices", "Update", nameof(Device), device.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(device.Id.Value, device.Status.ToString());
    }

    public async Task<CommandResultDto> SaveNetworkPrinterAsync(
        SaveNetworkPrinterCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var unit = GetUnit();
        if (context.Devices.Any(device =>
                device.UnitId == unit.Id && device.DeviceType == DeviceType.Printer &&
                (!command.Id.HasValue || device.Id.Value != command.Id.Value) &&
                device.IpAddress == command.Host && device.PrinterPort == command.Port))
        {
            throw new BusinessRuleException("printer.endpoint_duplicate", "Another printer already uses this host and port.");
        }
        Device printer;
        var action = command.Id.HasValue ? "UpdatePrinter" : "CreatePrinter";
        if (command.Id.HasValue)
        {
            printer = context.Devices.Single(device => device.Id == new DeviceId(command.Id.Value));
            if (printer.DeviceType != DeviceType.Printer)
                throw new BusinessRuleException("device.printer_type", "The selected device is not a printer.");
        }
        else
        {
            printer = new Device(
                DeviceId.New(), unit.Id, command.Name,
                $"PRN-{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
                DeviceType.Printer, "ESC/POS TCP");
            context.Add(printer);
        }

        printer.ConfigureNetworkPrinter(
            command.Name, command.Host, command.Port, command.PaperWidthMm,
            command.AutoPrintKitchenTickets, command.AutoPrintCustomerReceipts,
            command.AutoPrintFiscalDocuments);
        printer.UpdateStatus(
            command.IsActive ? DeviceStatus.Online : DeviceStatus.Offline,
            null, false, "Network", command.Host, null);
        AddAudit(unit.Id, employee.Id, "Devices", action, nameof(Device), printer.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(printer.Id.Value, printer.Status.ToString());
    }

    public async Task<CommandResultDto> QueuePrinterTestAsync(
        Guid printerId,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var printer = RequireConfiguredPrinter(new DeviceId(printerId));
        var job = new PrintJob(
            PrintJobId.New(), printer.UnitId, printer.Id, PrintDocumentType.TestPage,
            $"FORNO 27\nTESTE DE IMPRESSAO\n{printer.Name}\n{DateTimeOffset.Now:dd/MM/yyyy HH:mm}\n\nImpressora configurada com sucesso.\n");
        context.Add(job);
        AddAudit(printer.UnitId, employee.Id, "Devices", "QueuePrinterTest", nameof(PrintJob), job.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(job.Id.Value, job.Status.ToString());
    }

    public async Task<CommandResultDto> QueueOrderReceiptAsync(
        Guid orderId,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var order = context.Orders.Single(candidate => candidate.Id == new OrderId(orderId));
        EnsureCounterOrderIsPaid(order);
        var printer = context.Devices.FirstOrDefault(device =>
                device.UnitId == order.UnitId && device.DeviceType == DeviceType.Printer &&
                device.Status == DeviceStatus.Online && device.PrinterPort.HasValue &&
                device.AutoPrintCustomerReceipts)
            ?? context.Devices.FirstOrDefault(device =>
                device.UnitId == order.UnitId && device.DeviceType == DeviceType.Printer &&
                device.Status == DeviceStatus.Online && device.PrinterPort.HasValue)
            ?? throw new BusinessRuleException("printer.unavailable", "Configure an online network printer before printing receipts.");
        var receipt = CreateOrderReceipt(order);
        var payload = FormatReceipt(receipt);
        var job = new PrintJob(PrintJobId.New(), order.UnitId, printer.Id, PrintDocumentType.CustomerReceipt, payload);
        context.Add(job);
        AddAudit(order.UnitId, employee.Id, "Devices", "QueueOrderReceipt", nameof(PrintJob), job.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(job.Id.Value, job.Status.ToString());
    }

    public async Task<PrintBatchResultDto> QueueKitchenCommandAsync(
        Guid orderId,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var order = context.Orders.Single(candidate => candidate.Id == new OrderId(orderId));
        EnsureCounterOrderIsPaid(order);
        var tickets = context.KitchenTickets
            .Where(ticket => ticket.OrderId == order.Id)
            .OrderBy(ticket => ticket.TicketNumber)
            .ToArray();
        if (tickets.Length == 0)
        {
            throw new BusinessRuleException("kitchen_command.empty", "The order has no production tickets to print.");
        }

        var printer = context.Devices.FirstOrDefault(device =>
                device.UnitId == order.UnitId && device.DeviceType == DeviceType.Printer &&
                device.Status == DeviceStatus.Online && device.PrinterPort.HasValue &&
                device.AutoPrintKitchenTickets)
            ?? context.Devices.FirstOrDefault(device =>
                device.UnitId == order.UnitId && device.DeviceType == DeviceType.Printer &&
                device.Status == DeviceStatus.Online && device.PrinterPort.HasValue)
            ?? throw new BusinessRuleException("printer.unavailable", "Configure an online network printer before printing kitchen commands.");

        var jobIds = new List<Guid>(tickets.Length);
        foreach (var ticket in tickets)
        {
            if (ticket.Status == KitchenTicketStatus.New)
            {
                ticket.Confirm();
            }

            var job = new PrintJob(
                PrintJobId.New(),
                order.UnitId,
                printer.Id,
                PrintDocumentType.KitchenTicket,
                FormatKitchenTicket(ticket, order));
            context.Add(job);
            jobIds.Add(job.Id.Value);
            AddAudit(order.UnitId, employee.Id, "Devices", "QueueKitchenCommand", nameof(PrintJob), job.Id.Value);
        }

        if (order.Status == OrderStatus.Submitted)
        {
            order.Accept();
        }

        await context.SaveChangesAsync(cancellationToken);
        return new PrintBatchResultDto(jobIds, PrintJobStatus.Pending.ToString());
    }

    private void EnsureCounterOrderIsPaid(Order order)
    {
        var counterBill = context.Bills.SingleOrDefault(candidate => candidate.OrderId == order.Id);
        if (counterBill is not null && counterBill.Status != BillStatus.Paid)
        {
            throw new BusinessRuleException("counter_checkout.payment_required", "Complete the counter payment before printing this document.");
        }
    }

    private string FormatKitchenTicket(KitchenTicket ticket, Order order)
    {
        var receipt = CreateOrderReceipt(order);
        var receiptItems = receipt.Items.ToDictionary(item => item.Id);
        var links = context.KitchenTicketItems
            .Where(item => item.KitchenTicketId == ticket.Id)
            .ToArray();
        var lines = new List<string>
        {
            "FORNO 27 - COMANDA COZINHA",
            $"TICKET #{ticket.TicketNumber}",
            $"PEDIDO #{order.OrderNumber}",
            order.FulfillmentType == FulfillmentType.Pickup ? "RETIRADA NO BALCAO" : order.FulfillmentType.ToString().ToUpperInvariant(),
            order.PlacedAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("pt-BR")) ?? string.Empty,
            new string('=', 42)
        };
        foreach (var link in links)
        {
            if (!receiptItems.TryGetValue(link.OrderItemId.Value, out var item)) continue;
            lines.Add($"{link.Quantity:0.##}x {item.Name.ToUpperInvariant()}");
            foreach (var detail in item.Details) lines.Add($"  {RemoveKitchenPrice(detail)}");
            if (!string.IsNullOrWhiteSpace(item.Notes)) lines.Add($"  *** OBS: {item.Notes} ***");
            lines.Add(new string('-', 42));
        }
        if (!string.IsNullOrWhiteSpace(order.Notes))
        {
            lines.Add($"*** OBS PEDIDO: {order.Notes} ***");
        }
        lines.AddRange([new string('=', 42), "SEM VALORES - USO DA PRODUCAO", ""]);
        return string.Join('\n', lines);
    }

    private static string RemoveKitchenPrice(string detail)
    {
        var priceStart = detail.IndexOf(" (+ ", StringComparison.Ordinal);
        return priceStart < 0 ? detail : detail[..priceStart];
    }

    private Device RequireConfiguredPrinter(DeviceId printerId)
    {
        var printer = context.Devices.Single(candidate => candidate.Id == printerId);
        if (printer.DeviceType != DeviceType.Printer || string.IsNullOrWhiteSpace(printer.IpAddress) || !printer.PrinterPort.HasValue)
            throw new BusinessRuleException("printer.configuration", "The printer needs a host and port before testing.");
        return printer;
    }

    private static string FormatReceipt(OrderReceiptDto receipt)
    {
        var lines = new List<string>
        {
            "FORNO 27", "COMPROVANTE DO CLIENTE", $"PEDIDO #{receipt.Number}",
            receipt.PlacedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("pt-BR")),
            new string('-', 42), receipt.CustomerName,
            receipt.DeliveryAddress ?? receipt.Fulfillment, new string('-', 42)
        };
        foreach (var item in receipt.Items)
        {
            lines.Add($"{item.Quantity}x {item.Name}");
            foreach (var detail in item.Details) lines.Add($"  {detail}");
            if (!string.IsNullOrWhiteSpace(item.Notes)) lines.Add($"  OBS: {item.Notes}");
            lines.Add($"  {item.TotalPrice:C}");
        }
        if (!string.IsNullOrWhiteSpace(receipt.Notes))
        {
            lines.AddRange([new string('-', 42), $"OBS PEDIDO: {receipt.Notes}"]);
        }
        lines.AddRange([
            new string('-', 42),
            $"SUBTOTAL: {receipt.Subtotal:C}",
            receipt.Discount > 0 ? $"DESCONTO: -{receipt.Discount:C}" : string.Empty,
            $"TOTAL: {receipt.Total:C}",
            new string('-', 42)
        ]);
        foreach (var payment in receipt.Payments)
        {
            lines.Add($"{payment.Method}: {payment.Amount:C}");
            if (payment.ReceivedAmount != payment.Amount) lines.Add($"RECEBIDO: {payment.ReceivedAmount:C}");
        }
        if (receipt.ChangeAmount > 0) lines.Add($"TROCO: {receipt.ChangeAmount:C}");
        lines.AddRange(["", "*** DOCUMENTO SEM VALOR FISCAL ***", "Obrigado pela preferencia!", ""]);
        return string.Join('\n', lines);
    }

    public async Task<DeviceProvisioningDto> CreateCustomerTabletAsync(
        CreateCustomerTabletCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var unit = GetUnit();
        var tableId = new RestaurantTableId(command.LinkedTableId);
        EnsureTableBelongsToUnit(tableId, unit.Id);

        var device = new Device(
            DeviceId.New(),
            unit.Id,
            command.Name,
            CreateTabletSerialNumber(),
            DeviceType.CustomerTablet,
            command.Platform);
        device.LinkToTable(tableId);
        context.Add(device);

        var result = CreateProvisioning(device);
        AddAudit(unit.Id, employee.Id, "Devices", "Create", nameof(Device), device.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task<DeviceProvisioningDto> ProvisionCustomerTabletAsync(
        Guid id,
        ProvisionCustomerTabletCommand command,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var deviceId = new DeviceId(id);
        var device = context.Devices.SingleOrDefault(candidate =>
            candidate.Id == deviceId &&
            candidate.DeviceType == DeviceType.CustomerTablet)
            ?? throw new BusinessRuleException("device.not_found", "Customer tablet does not exist.");
        if (device.IsLocked)
        {
            throw new BusinessRuleException("device.locked", "A locked tablet cannot be linked.");
        }

        var tableId = new RestaurantTableId(command.LinkedTableId);
        EnsureTableBelongsToUnit(tableId, device.UnitId);
        RevokeDeviceAccess(device.Id, "Tablet reprovisioned by an administrator.");
        device.LinkToTable(tableId);

        var result = CreateProvisioning(device);
        AddAudit(device.UnitId, employee.Id, "Devices", "Provision", nameof(Device), device.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return result;
    }

    private DeviceProvisioningDto CreateProvisioning(Device device)
    {
        foreach (var previous in context.DeviceProvisionings
                     .Where(candidate => candidate.DeviceId == device.Id)
                     .ToArray()
                     .Where(candidate => candidate.IsAvailableAt(DateTimeOffset.UtcNow)))
        {
            previous.Revoke();
        }

        var token = DeviceProvisioningTokens.Create();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(30);
        context.Add(new DeviceProvisioning(
            DeviceProvisioningId.New(),
            device.Id,
            DeviceProvisioningTokens.Hash(token),
            expiresAt));
        return new DeviceProvisioningDto(ToDeviceDto(device), token, expiresAt);
    }

    private void RevokeDeviceAccess(DeviceId deviceId, string reason)
    {
        foreach (var session in context.DeviceSessions
                     .Where(candidate => candidate.DeviceId == deviceId && candidate.EndedAt == null)
                     .ToArray())
        {
            session.End(reason);
        }

        foreach (var provisioning in context.DeviceProvisionings
                     .Where(candidate => candidate.DeviceId == deviceId)
                     .ToArray()
                     .Where(candidate => candidate.IsAvailableAt(DateTimeOffset.UtcNow)))
        {
            provisioning.Revoke();
        }
    }

    private void EnsureTableBelongsToUnit(RestaurantTableId tableId, RestaurantUnitId unitId)
    {
        if (!context.RestaurantTables.Any(table => table.Id == tableId && table.UnitId == unitId))
        {
            throw new BusinessRuleException("device.table", "Linked table does not exist in this restaurant unit.");
        }
    }

    private string CreateTabletSerialNumber()
    {
        string serialNumber;
        do
        {
            serialNumber = $"TAB-{Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(6))}";
        }
        while (context.Devices.Any(device => device.SerialNumber == serialNumber));

        return serialNumber;
    }

    private RestaurantUnit GetUnit() => context.RestaurantUnits.Single();

    private static CustomerDto ToCustomerDto(Customer customer) => new(
        customer.Id.Value,
        customer.Name,
        customer.Phone,
        customer.BirthDate,
        customer.IsActive,
        customer.LoyaltyPoints,
        customer.LifetimeSpend.Amount,
        customer.OrderCount,
        customer.LastOrderAt,
        customer.CreatedAt);

    private static ReservationDto ToReservationDto(Reservation reservation) => new(
        reservation.Id.Value, reservation.CustomerId?.Value, reservation.CustomerName, reservation.Phone,
        reservation.PartySize, reservation.ScheduledAt, reservation.DurationMinutes, reservation.Notes,
        reservation.Status.ToString(), reservation.CreatedAt);

    private static WaitlistEntryDto ToWaitlistEntryDto(WaitlistEntry entry) => new(
        entry.Id.Value, entry.CustomerId?.Value, entry.CustomerName, entry.Phone, entry.PartySize,
        entry.EstimatedWaitMinutes, entry.Notes, entry.Status.ToString(), entry.EnteredAt, entry.NotifiedAt);

    private OrderReceiptDto CreateOrderReceipt(Order order)
    {
        var items = context.OrderItems.Where(item => item.OrderId == order.Id).ToArray();
        if (items.Length == 0 && order.Items.Count > 0)
        {
            items = order.Items.ToArray();
        }

        var itemIds = items.Select(item => item.Id).ToHashSet();
        var pizzas = context.OrderItemPizzas
            .Where(pizza => itemIds.Contains(pizza.Id))
            .ToArray()
            .ToDictionary(pizza => pizza.Id);
        var flavors = context.OrderItemPizzaFlavors
            .Where(flavor => itemIds.Contains(flavor.OrderItemId))
            .ToArray()
            .GroupBy(flavor => flavor.OrderItemId)
            .ToDictionary(group => group.Key, group => group.OrderBy(flavor => flavor.PartNumber).ToArray());
        var modifiers = context.OrderItemModifiers
            .Where(modifier => itemIds.Contains(modifier.OrderItemId))
            .ToArray()
            .GroupBy(modifier => modifier.OrderItemId)
            .ToDictionary(group => group.Key, group => group.ToArray());
        var customer = order.CustomerId.HasValue
            ? context.Customers.SingleOrDefault(candidate => candidate.Id == order.CustomerId.Value)
            : null;
        var bill = context.Bills.SingleOrDefault(candidate => candidate.OrderId == order.Id);
        Payment[] payments = bill is null
            ? []
            : context.Payments
                .Where(payment => payment.BillId == bill.Id && payment.Status == PaymentStatus.Paid)
                .OrderBy(payment => payment.PaidAt)
                .ToArray();
        var methodNames = payments.Length == 0
            ? new Dictionary<PaymentMethodId, string>()
            : context.PaymentMethods
                .Where(method => payments.Select(payment => payment.PaymentMethodId).Contains(method.Id))
                .ToDictionary(method => method.Id, method => method.Name);
        var ptBr = CultureInfo.GetCultureInfo("pt-BR");

        var receiptItems = items.Select(item =>
        {
            var details = new List<string>();
            if (pizzas.TryGetValue(item.Id, out var pizza))
            {
                details.Add($"Tamanho: {pizza.SizeNameSnapshot}");
                if (flavors.TryGetValue(item.Id, out var selectedFlavors))
                {
                    details.Add($"Sabores: {string.Join(" / ", selectedFlavors.Select(flavor => flavor.FlavorNameSnapshot))}");
                }
                if (pizza.CrustSelectionMode == CrustSelectionMode.Split)
                {
                    details.Add($"Borda: 1/2 {pizza.CrustNameSnapshot} + 1/2 {pizza.SecondCrustNameSnapshot}");
                }
                else if (pizza.CrustSelectionMode == CrustSelectionMode.Whole)
                {
                    details.Add($"Borda: {pizza.CrustNameSnapshot}");
                }
            }

            if (modifiers.TryGetValue(item.Id, out var itemModifiers))
            {
                details.AddRange(itemModifiers.Select(modifier => modifier.ModifierType switch
                {
                    ModifierType.Remove => $"Sem {modifier.NameSnapshot}",
                    ModifierType.Extra => $"Adicional: {modifier.Quantity:0.##}x {modifier.NameSnapshot} (+ {modifier.TotalPrice.Amount.ToString("C", ptBr)})",
                    _ => $"{modifier.Quantity:0.##}x {modifier.NameSnapshot}"
                }));
            }

            return new OrderReceiptItemDto(
                item.Id.Value,
                item.ProductNameSnapshot,
                item.Quantity,
                item.UnitPrice.Amount,
                item.TotalPrice.Amount,
                item.Notes,
                details);
        }).ToArray();

        var receiptPayments = payments.Select(payment => new OrderReceiptPaymentDto(
            methodNames.GetValueOrDefault(payment.PaymentMethodId, "Pagamento"),
            payment.Amount.Amount,
            payment.ReceivedAmount.Amount,
            payment.ChangeAmount.Amount,
            payment.PaidAt ?? order.PlacedAt ?? order.CreatedAt)).ToArray();

        return new OrderReceiptDto(
            order.Id.Value,
            order.OrderNumber,
            order.CustomerNameSnapshot ?? customer?.Name ?? "Consumidor",
            customer?.Phone ?? string.Empty,
            order.FulfillmentType.ToString(),
            order.DeliveryAddressSnapshot,
            order.PlacedAt ?? order.CreatedAt,
            order.Subtotal.Amount,
            order.DeliveryFee.Amount,
            order.Discount.Amount,
            order.Total.Amount,
            receiptPayments.Sum(payment => payment.Amount),
            receiptPayments.Sum(payment => payment.ChangeAmount),
            order.Notes,
            receiptItems,
            receiptPayments);
    }

    private Employee GetEmployee(Guid identityUserId) =>
        context.Employees.Single(employee => employee.IdentityUserId == identityUserId && employee.IsActive);

    private bool HasOpenTableSession(RestaurantTableId tableId)
    {
        var openSessionIds = context.TableSessions
            .Where(session => session.Status != TableSessionStatus.Closed && session.Status != TableSessionStatus.Cancelled)
            .Select(session => session.Id)
            .ToHashSet();
        return context.TableSessionTables.Any(link =>
            link.RestaurantTableId == tableId &&
            link.UnlinkedAt == null &&
            openSessionIds.Contains(link.TableSessionId));
    }

    private static CashMovementDto ToCashMovementDto(CashMovement movement) => new(
        movement.Id.Value,
        movement.MovementType.ToString(),
        movement.Amount.Amount,
        movement.Description,
        movement.Reason,
        movement.CreatedAt);

    private void AddAudit(
        RestaurantUnitId unitId,
        EmployeeId employeeId,
        string module,
        string action,
        string entityType,
        Guid entityId) =>
        context.Add(new AuditLog(AuditLogId.New(), unitId, module, action, entityType, entityId.ToString(), employeeId));

    private static string DescribeAuditEntity(
        string entityType,
        string entityId,
        IReadOnlyDictionary<Guid, string> kitchenTickets)
    {
        if (entityType == nameof(KitchenTicket) &&
            Guid.TryParse(entityId, out var kitchenTicketId) &&
            kitchenTickets.TryGetValue(kitchenTicketId, out var description))
        {
            return description;
        }

        return Guid.TryParse(entityId, out _)
            ? $"{entityType} {entityId[..8]}"
            : entityId;
    }

    private static UnitSettingsDto ToDto(RestaurantUnit unit) => new(
        unit.Id.Value,
        unit.Name,
        unit.LegalName,
        unit.TradeName,
        unit.Cnpj,
        unit.Phone,
        unit.AdministrativeEmail,
        unit.Timezone,
        unit.CurrencyCode);

    private static DeviceDto ToDeviceDto(Device device) => new(
        device.Id.Value,
        device.Name,
        device.SerialNumber,
        device.DeviceType.ToString(),
        device.Platform,
        device.Status.ToString(),
        device.BatteryPercentage,
        device.IsCharging,
        device.NetworkStatus,
        device.IpAddress,
        device.AppVersion,
        device.LastSeenAt,
        device.LinkedTableId?.Value,
        device.IsLocked,
        device.PrinterPort,
        device.PaperWidthMm,
        device.AutoPrintKitchenTickets,
        device.AutoPrintCustomerReceipts,
        device.AutoPrintFiscalDocuments);
}
