using ProjetoPizza.Application.Abstractions.Persistence;
using ProjetoPizza.Domain.Audit;
using ProjetoPizza.Domain.Billing;
using ProjetoPizza.Domain.Cashier;
using ProjetoPizza.Domain.Catalog;
using ProjetoPizza.Domain.Core;
using ProjetoPizza.Domain.Devices;
using ProjetoPizza.Domain.Dining;
using ProjetoPizza.Domain.Identity;
using ProjetoPizza.Domain.Ordering;
using ProjetoPizza.Domain.Production;
using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Application.Admin;

public sealed class AdminManagementService(IProjetoPizzaDbContext context) : IAdminManagementService
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
                order.Total.Amount,
                order.CreatedAt,
                order.PlacedAt,
                lines.GetValueOrDefault(order.Id, [])))
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<OrderManagementDto>>(result);
    }

    public Task<IReadOnlyCollection<PizzaCrustDto>> ListPizzaCrustsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = context.PizzaCrusts
            .OrderBy(crust => crust.DisplayOrder)
            .ToArray()
            .Select(crust => new PizzaCrustDto(crust.Id.Value, crust.Name, crust.Description, crust.IsActive, crust.IsAvailable))
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<PizzaCrustDto>>(result);
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
                method.IsActive))
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<PaymentMethodDto>>(result);
    }

    public Task<IReadOnlyCollection<PaymentDto>> ListPaymentsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var methods = context.PaymentMethods.ToDictionary(method => method.Id, method => method.Name);
        var result = context.Payments
            .OrderByDescending(payment => payment.PaidAt)
            .ToArray()
            .Select(payment => new PaymentDto(
                payment.Id.Value,
                payment.BillId.Value,
                methods.GetValueOrDefault(payment.PaymentMethodId, "Desconhecido"),
                payment.Status.ToString(),
                payment.Amount.Amount,
                payment.ReceivedAmount.Amount,
                payment.ChangeAmount.Amount,
                payment.ExternalReference,
                payment.PaidAt))
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
            .Where(payment => payment.PaidAt >= from && payment.PaidAt <= to && payment.Status == PaymentStatus.Paid)
            .ToArray();
        var methods = context.PaymentMethods.ToDictionary(method => method.Id, method => method.Name);
        var grossSales = orders.Sum(order => order.Total.Amount);
        var result = new FinancialReportDto(
            from,
            to,
            grossSales,
            payments.Sum(payment => payment.Amount.Amount),
            orders.Length == 0 ? 0 : decimal.Round(grossSales / orders.Length, 2),
            orders.Length,
            orders.GroupBy(order => order.SalesChannel)
                .Select(group => new FinancialChannelDto(group.Key.ToString(), group.Count(), group.Sum(order => order.Total.Amount)))
                .OrderByDescending(channel => channel.Total)
                .ToArray(),
            payments.GroupBy(payment => payment.PaymentMethodId)
                .Select(group => new FinancialMethodDto(
                    methods.GetValueOrDefault(group.Key, "Desconhecido"),
                    group.Count(),
                    group.Sum(payment => payment.Amount.Amount)))
                .OrderByDescending(method => method.Total)
                .ToArray());
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
                device.IsLocked))
            .ToArray();
        return Task.FromResult<IReadOnlyCollection<DeviceDto>>(result);
    }

    public Task<IReadOnlyCollection<AuditLogDto>> ListAuditLogsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var employees = context.Employees.ToDictionary(employee => employee.Id, employee => employee.DisplayName);
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

        AddAudit(unit.Id, employee.Id, "Catalog", action, nameof(Product), product.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(product.Id.Value, product.IsActive ? "Active" : "Inactive");
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
            crust = context.PizzaCrusts.Single(item => item.Id == crustId);
        }
        else
        {
            crust = new PizzaCrust(PizzaCrustId.New(), unit.Id, command.Name, command.Description);
            context.Add(crust);
            action = "Create";
        }

        crust.Update(command.Name, command.Description, command.IsActive, command.IsAvailable);
        AddAudit(unit.Id, employee.Id, "Catalog", action, nameof(PizzaCrust), crust.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(crust.Id.Value, crust.IsAvailable ? "Available" : "Unavailable");
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
        AddAudit(unit.Id, employee.Id, "Catalog", action, nameof(PizzaFlavor), flavor.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(flavor.Id.Value, flavor.IsAvailable ? "Available" : "Unavailable");
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

        var sessionNumber = context.TableSessions.Any() ? context.TableSessions.Max(session => session.SessionNumber) + 1 : 1;
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
            case "complete": order.Complete(); break;
            case "cancel": order.Cancel("Cancelado pelo painel administrativo."); break;
            default: throw new BusinessRuleException("order.transition", "Unknown order transition.");
        }

        AddAudit(order.UnitId, employee.Id, "Ordering", transition, nameof(Order), order.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(order.Id.Value, order.Status.ToString());
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

    public async Task<CommandResultDto> ResolveServiceCallAsync(
        Guid id,
        Guid identityUserId,
        CancellationToken cancellationToken)
    {
        var employee = GetEmployee(identityUserId);
        var callId = new ServiceCallId(id);
        var call = context.ServiceCalls.Single(item => item.Id == callId);
        if (call.Status == ServiceCallStatus.Pending)
        {
            call.Acknowledge(employee.Id);
        }

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

        var session = context.TableSessions.Single(item => item.Id == bill.TableSessionId);
        if (bill.Status == BillStatus.Paid)
        {
            session.Close(employee.Id);
        }
        else if (session.Status == TableSessionStatus.BillRequested)
        {
            session.MarkPaymentPending();
        }

        AddAudit(bill.UnitId, employee.Id, "Billing", "Pay", nameof(Bill), bill.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(payment.Id.Value, payment.Status.ToString());
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
        AddAudit(device.UnitId, employee.Id, "Devices", "Update", nameof(Device), device.Id.Value);
        await context.SaveChangesAsync(cancellationToken);
        return new CommandResultDto(device.Id.Value, device.Status.ToString());
    }

    private RestaurantUnit GetUnit() => context.RestaurantUnits.Single();

    private Employee GetEmployee(Guid identityUserId) =>
        context.Employees.Single(employee => employee.IdentityUserId == identityUserId && employee.IsActive);

    private void AddAudit(
        RestaurantUnitId unitId,
        EmployeeId employeeId,
        string module,
        string action,
        string entityType,
        Guid entityId) =>
        context.Add(new AuditLog(AuditLogId.New(), unitId, module, action, entityType, entityId.ToString(), employeeId));

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
}
