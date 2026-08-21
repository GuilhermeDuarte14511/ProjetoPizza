using FluentAssertions;
using ProjetoPizza.Application.Abstractions.Persistence;
using ProjetoPizza.Application.Admin;
using ProjetoPizza.Application.Client;
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

namespace ProjetoPizza.Application.Tests.Admin;

public sealed class AdminCashierServiceTests
{
    [Fact]
    public async Task CreateReservation_WithoutExistingCustomer_ShouldCreateAndLinkCustomer()
    {
        var fixture = CreateFixture();
        var service = new AdminManagementService(fixture.Context);

        var result = await service.CreateReservationAsync(
            new CreateReservationCommand(
                null,
                "Marina Costa",
                "11987654321",
                4,
                DateTimeOffset.UtcNow.AddDays(2),
                90,
                "Mesa próxima à janela.",
                new DateOnly(1990, 4, 12)),
            fixture.IdentityUserId,
            CancellationToken.None);

        fixture.Context.CustomerItems.Should().ContainSingle();
        fixture.Context.ReservationEntities.Should().ContainSingle();
        result.CustomerId.Should().Be(fixture.Context.CustomerItems.Single().Id.Value);
        result.Phone.Should().Be("11987654321");
        fixture.Context.AuditLogItems.Should().Contain(log => log.Action == "CreateFromReservation");
        fixture.Context.AuditLogItems.Should().Contain(log => log.Action == "CreateReservation");
    }

    [Fact]
    public async Task OpenCashShift_ShouldUseAuthenticatedEmployeeAndCreateAudit()
    {
        var fixture = CreateFixture();
        var service = new AdminManagementService(fixture.Context);

        var result = await service.OpenCashShiftAsync(
            new OpenCashShiftCommand(fixture.Register.Id.Value, 250m),
            fixture.IdentityUserId,
            CancellationToken.None);

        result.Status.Should().Be("Open");
        result.Register.Should().Be("Caixa Principal");
        result.Operator.Should().Be("Administrador");
        result.OpeningAmount.Should().Be(250m);
        fixture.Context.CashShiftItems.Should().ContainSingle()
            .Which.OperatorEmployeeId.Should().Be(fixture.Employee.Id);
        fixture.Context.AuditLogItems.Should().ContainSingle(log =>
            log.Module == "Cashier" && log.Action == "Open");
        fixture.Context.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task OpenCashShift_WithActiveShift_ShouldBeRejected()
    {
        var fixture = CreateFixture();
        fixture.Context.CashShiftItems.Add(new CashShift(
            CashShiftId.New(),
            fixture.Register.Id,
            fixture.Employee.Id,
            new Money(100m)));
        var service = new AdminManagementService(fixture.Context);

        var action = () => service.OpenCashShiftAsync(
            new OpenCashShiftCommand(fixture.Register.Id.Value, 200m),
            fixture.IdentityUserId,
            CancellationToken.None);

        (await action.Should().ThrowAsync<BusinessRuleException>())
            .Which.Rule.Should().Be("cash_shift.already_open");
        fixture.Context.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task OpenCashShift_WithUnknownRegister_ShouldBeRejected()
    {
        var fixture = CreateFixture();
        var service = new AdminManagementService(fixture.Context);

        var action = () => service.OpenCashShiftAsync(
            new OpenCashShiftCommand(Guid.NewGuid(), 200m),
            fixture.IdentityUserId,
            CancellationToken.None);

        (await action.Should().ThrowAsync<BusinessRuleException>())
            .Which.Rule.Should().Be("cash_register.unavailable");
    }

    [Fact]
    public async Task AdjustCustomerLoyaltyPoints_ShouldUpdateBalanceLedgerAndAudit()
    {
        var fixture = CreateFixture();
        var customer = new Customer(
            CustomerId.New(),
            fixture.Context.RestaurantUnitItems.Single().Id,
            "Ana Souza",
            "11999998877",
            new DateOnly(1992, 5, 18));
        customer.EarnLoyaltyPoints(150, DateTimeOffset.UtcNow.AddDays(120));
        fixture.Context.CustomerItems.Add(customer);
        var service = new AdminManagementService(fixture.Context);

        var result = await service.AdjustCustomerLoyaltyPointsAsync(
            customer.Id.Value,
            new AdjustCustomerLoyaltyPointsCommand(-40, "Correção do pedido 1047"),
            fixture.IdentityUserId,
            CancellationToken.None);

        result.Customer.LoyaltyPoints.Should().Be(110);
        result.BenefitBalance.Should().Be(5.50m);
        fixture.Context.LoyaltyTransactionItems.Should().Contain(transaction =>
            transaction.Type == LoyaltyTransactionType.ManualAdjustment &&
            transaction.Points == -40 &&
            transaction.BalanceAfter == 110 &&
            transaction.Description == "Ajuste manual: Correção do pedido 1047");
        fixture.Context.AuditLogItems.Should().Contain(log =>
            log.Module == "Customers" && log.Action == "AdjustLoyaltyPoints");
        fixture.Context.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task AdjustCustomerLoyaltyPoints_WithShortReason_ShouldRejectWithoutSaving()
    {
        var fixture = CreateFixture();
        var customer = new Customer(
            CustomerId.New(),
            fixture.Context.RestaurantUnitItems.Single().Id,
            "Ana Souza",
            "11999998877",
            new DateOnly(1992, 5, 18));
        fixture.Context.CustomerItems.Add(customer);
        var service = new AdminManagementService(fixture.Context);

        var action = () => service.AdjustCustomerLoyaltyPointsAsync(
            customer.Id.Value,
            new AdjustCustomerLoyaltyPointsCommand(10, "Erro"),
            fixture.IdentityUserId,
            CancellationToken.None);

        (await action.Should().ThrowAsync<BusinessRuleException>()).Which.Rule.Should().Be("loyalty.adjustment_reason");
        fixture.Context.SaveChangesCalls.Should().Be(0);
    }

    [Fact]
    public async Task CreateOrder_Delivery_ShouldUseServerPriceFeeDiscountAndCreateTicket()
    {
        var fixture = CreateFixture();
        var settings = new OperationSettings(fixture.Context.RestaurantUnitItems.Single().Id);
        settings.Update(false, true, true, new Percentage(10), new Money(8), true, true, 5);
        var customer = new Customer(
            CustomerId.New(),
            fixture.Context.RestaurantUnitItems.Single().Id,
            "Ana Souza",
            "(11) 99999-8877",
            new DateOnly(1992, 5, 18));
        var product = new Product(
            ProductId.New(),
            fixture.Context.RestaurantUnitItems.Single().Id,
            CategoryId.New(),
            "REFRI-01",
            "Refrigerante",
            ProductType.Standard,
            new Money(15));
        var station = new ProductionStation(
            ProductionStationId.New(),
            fixture.Context.RestaurantUnitItems.Single().Id,
            "Cozinha quente",
            "HOT",
            15);
        var inventoryItem = new InventoryItem(
            InventoryItemId.New(), fixture.Context.RestaurantUnitItems.Single().Id,
            "Refrigerante em lata", "EST-REFRI-01", "un", 2, new Money(4));
        var balance = new StockBalance(StockBalanceId.New(), inventoryItem.Id);
        balance.ApplyAdjustment(10);
        var recipe = new Recipe(RecipeId.New(), 1, productId: product.Id);
        fixture.Context.OperationSettingItems = [settings];
        fixture.Context.CustomerItems = [customer];
        fixture.Context.ProductItems = [product];
        fixture.Context.ProductionStationItems = [station];
        fixture.Context.InventoryItemItems = [inventoryItem];
        fixture.Context.StockBalanceItems = [balance];
        fixture.Context.RecipeEntities = [recipe];
        fixture.Context.RecipeItemEntities = [new RecipeItem(RecipeItemId.New(), recipe.Id, inventoryItem.Id, 0.5m, "un")];
        var service = new AdminManagementService(fixture.Context);
        var requestId = Guid.NewGuid();

        var result = await service.CreateOrderAsync(
            new CreateAdministrativeOrderCommand(
                requestId,
                customer.Id.Value,
                "Delivery",
                "Rua das Flores, 27 - Centro",
                3,
                "Entregar na portaria.",
                [new SubmitClientOrderItemCommand(product.Id.Value, 2, "Sem gelo.", null)]),
            fixture.IdentityUserId,
            CancellationToken.None);

        result.Id.Should().Be(requestId);
        result.Total.Should().Be(35);
        result.Receipt.Subtotal.Should().Be(30);
        result.Receipt.DeliveryFee.Should().Be(8);
        result.Receipt.Discount.Should().Be(3);
        result.Receipt.CustomerPhone.Should().Be("11999998877");
        result.Receipt.DeliveryAddress.Should().Be("Rua das Flores, 27 - Centro");
        result.Receipt.Items.Should().ContainSingle().Which.Notes.Should().Be("Sem gelo.");
        fixture.Context.OrderEntities.Should().ContainSingle();
        fixture.Context.KitchenTicketEntities.Should().ContainSingle();
        fixture.Context.KitchenTicketItemEntities.Should().ContainSingle();
        fixture.Context.AuditLogItems.Should().ContainSingle(log => log.Action == "CreateAdministrative");
        fixture.Context.InventoryReservationEntities.Should().ContainSingle().Which.Status.Should().Be(InventoryReservationStatus.Reserved);
        balance.CurrentQuantity.Should().Be(10);
        balance.ReservedQuantity.Should().Be(1);

        await service.TransitionOrderAsync(result.Id, "accept", fixture.IdentityUserId, CancellationToken.None);
        await service.TransitionOrderAsync(result.Id, "start-production", fixture.IdentityUserId, CancellationToken.None);

        balance.CurrentQuantity.Should().Be(9);
        balance.ReservedQuantity.Should().Be(0);
        fixture.Context.InventoryReservationEntities.Single().Status.Should().Be(InventoryReservationStatus.Consumed);
        fixture.Context.StockMovementEntities.Should().ContainSingle(movement =>
            movement.MovementType == StockMovementType.Consumption && movement.Quantity == 1);
    }

    [Fact]
    public async Task SeatReservation_ShouldOpenSessionAndLinkSelectedTablesAtomically()
    {
        var fixture = CreateFixture();
        var unit = fixture.Context.RestaurantUnitItems.Single();
        var area = new DiningArea(DiningAreaId.New(), unit.Id, "Salão");
        var firstTable = new RestaurantTable(RestaurantTableId.New(), unit.Id, area.Id, 1, 2, "Mesa 01");
        var secondTable = new RestaurantTable(RestaurantTableId.New(), unit.Id, area.Id, 2, 4, "Mesa 02");
        var reservation = new Reservation(
            ReservationId.New(), unit.Id, "Família Souza", "11999998877", 5,
            DateTimeOffset.UtcNow.AddMinutes(30), 90, "Cadeira infantil");
        reservation.Transition(ReservationStatus.Confirmed);
        fixture.Context.OperationSettingItems = [new OperationSettings(unit.Id)];
        fixture.Context.DiningAreaItems = [area];
        fixture.Context.RestaurantTableItems = [firstTable, secondTable];
        fixture.Context.ReservationEntities.Add(reservation);
        var service = new AdminManagementService(fixture.Context);

        var result = await service.SeatReservationAsync(
            reservation.Id.Value,
            new SeatDiningEntryCommand([firstTable.Id.Value, secondTable.Id.Value]),
            fixture.IdentityUserId,
            CancellationToken.None);

        reservation.Status.Should().Be(ReservationStatus.Seated);
        reservation.TableSessionId.Should().Be(new TableSessionId(result.Id));
        fixture.Context.TableSessionEntities.Should().ContainSingle();
        fixture.Context.TableSessionTables.Should().HaveCount(2);
        fixture.Context.TableSessionEntities.Single().GuestCount.Should().Be(5);
        fixture.Context.TableSessionEntities.Single().Notes.Should().Be("Cadeira infantil");
        fixture.Context.AuditLogItems.Should().Contain(log => log.Action == "OpenFromSeating");
        fixture.Context.SaveChangesCalls.Should().Be(1);
    }

    [Fact]
    public async Task CancelOrder_BeforeProduction_ShouldReleaseReservedInventory()
    {
        var fixture = CreateFixture();
        var unit = fixture.Context.RestaurantUnitItems.Single();
        var order = new Order(
            OrderId.New(), unit.Id, 42, SalesChannel.Administrative, FulfillmentType.DineIn, fixture.Employee.Id);
        var orderItem = order.AddItem(OrderItemId.New(), ProductId.New(), "Refrigerante", 2, new Money(8));
        order.RecalculateTotals();
        order.Submit();
        var inventoryItem = new InventoryItem(
            InventoryItemId.New(), unit.Id, "Refrigerante em lata", "EST-REFRI-01", "un", 2, new Money(4));
        var balance = new StockBalance(StockBalanceId.New(), inventoryItem.Id);
        balance.ApplyAdjustment(5);
        balance.Reserve(2);
        var reservation = new InventoryReservation(
            InventoryReservationId.New(), inventoryItem.Id, orderItem.Id, 2, inventoryItem.UnitCost);
        fixture.Context.OrderEntities.Add(order);
        fixture.Context.InventoryItemItems = [inventoryItem];
        fixture.Context.StockBalanceItems = [balance];
        fixture.Context.InventoryReservationEntities.Add(reservation);
        var service = new AdminManagementService(fixture.Context);

        await service.CancelOrderAsync(
            order.Id.Value, new CancelOrderCommand("Cliente desistiu."), fixture.IdentityUserId, CancellationToken.None);

        order.Status.Should().Be(OrderStatus.Cancelled);
        reservation.Status.Should().Be(InventoryReservationStatus.Released);
        balance.CurrentQuantity.Should().Be(5);
        balance.ReservedQuantity.Should().Be(0);
        balance.AvailableQuantity.Should().Be(5);
    }

    [Fact]
    public async Task DeleteRestaurantTable_WithoutUsage_ShouldRemoveAndAudit()
    {
        var fixture = CreateFixture();
        var unit = fixture.Context.RestaurantUnitItems.Single();
        var area = new DiningArea(DiningAreaId.New(), unit.Id, "Salão");
        var table = new RestaurantTable(RestaurantTableId.New(), unit.Id, area.Id, 8, 4, "Mesa 08");
        fixture.Context.DiningAreaItems = [area];
        fixture.Context.RestaurantTableItems = [table];
        var service = new AdminManagementService(fixture.Context);

        var result = await service.DeleteRestaurantTableAsync(table.Id.Value, fixture.IdentityUserId, CancellationToken.None);

        result.Status.Should().Be("Deleted");
        fixture.Context.RestaurantTableItems.Should().BeEmpty();
        fixture.Context.AuditLogItems.Should().ContainSingle(log => log.Action == "Delete");
    }

    [Fact]
    public async Task DeleteRestaurantTable_WithServiceHistory_ShouldBeRejected()
    {
        var fixture = CreateFixture();
        var unit = fixture.Context.RestaurantUnitItems.Single();
        var area = new DiningArea(DiningAreaId.New(), unit.Id, "Salão");
        var table = new RestaurantTable(RestaurantTableId.New(), unit.Id, area.Id, 8, 4, "Mesa 08");
        var session = TableSession.Open(
            TableSessionId.New(), unit.Id, 1, 2, fixture.Employee.Id, new Percentage(10), [table]);
        fixture.Context.DiningAreaItems = [area];
        fixture.Context.RestaurantTableItems = [table];
        fixture.Context.TableSessionEntities.Add(session);
        var service = new AdminManagementService(fixture.Context);

        var action = () => service.DeleteRestaurantTableAsync(table.Id.Value, fixture.IdentityUserId, CancellationToken.None);

        (await action.Should().ThrowAsync<BusinessRuleException>())
            .Which.Rule.Should().Be("restaurant_table.history");
        fixture.Context.RestaurantTableItems.Should().ContainSingle();
    }

    [Fact]
    public async Task CheckoutCounterOrder_ShouldPersistPaidBillPaymentAndKitchenTicketAtomically()
    {
        var fixture = CreateFixture();
        var unit = fixture.Context.RestaurantUnitItems.Single();
        var settings = new OperationSettings(unit.Id);
        settings.Update(false, false, true, new Percentage(10), new Money(8), true, true, 5);
        var customer = new Customer(CustomerId.New(), unit.Id, "Ana Souza", "11999998877", new DateOnly(1992, 5, 18));
        var product = new Product(ProductId.New(), unit.Id, CategoryId.New(), "PIZZA-01", "Pizza", ProductType.Standard, new Money(50));
        var station = new ProductionStation(ProductionStationId.New(), unit.Id, "Cozinha", "HOT", 15);
        var paymentMethod = new PaymentMethod(PaymentMethodId.New(), unit.Id, "CASH", "Dinheiro", false, true);
        var shift = new CashShift(CashShiftId.New(), fixture.Register.Id, fixture.Employee.Id, new Money(100));
        fixture.Context.OperationSettingItems = [settings];
        fixture.Context.CustomerItems = [customer];
        fixture.Context.ProductItems = [product];
        fixture.Context.ProductionStationItems = [station];
        fixture.Context.PaymentMethodItems = [paymentMethod];
        fixture.Context.CashShiftItems.Add(shift);
        var service = new AdminManagementService(fixture.Context);
        var requestId = Guid.NewGuid();

        var result = await service.CheckoutCounterOrderAsync(
            new CheckoutCounterOrderCommand(
                new CreateAdministrativeOrderCommand(
                    requestId,
                    customer.Id.Value,
                    "Pickup",
                    null,
                    5,
                    "Retirar cebola.",
                    [new SubmitClientOrderItemCommand(product.Id.Value, 2, "Bem assada.", null)]),
                new CounterPaymentCommand(paymentMethod.Id.Value, 100, null)),
            fixture.IdentityUserId,
            CancellationToken.None);

        result.Id.Should().Be(requestId);
        result.Total.Should().Be(95);
        result.Receipt.PaidAmount.Should().Be(95);
        result.Receipt.ChangeAmount.Should().Be(5);
        result.Receipt.Payments.Should().ContainSingle().Which.Method.Should().Be("Dinheiro");
        fixture.Context.BillEntities.Should().ContainSingle().Which.Status.Should().Be(BillStatus.Paid);
        fixture.Context.PaymentEntities.Should().ContainSingle().Which.ChangeAmount.Amount.Should().Be(5);
        fixture.Context.BillItemEntities.Should().ContainSingle();
        fixture.Context.KitchenTicketEntities.Should().ContainSingle().Which.Status.Should().Be(KitchenTicketStatus.New);
        fixture.Context.PrintJobEntities.Should().BeEmpty();
        fixture.Context.AuditLogItems.Should().Contain(log => log.Action == "CounterCheckout");

        var printer = new Device(DeviceId.New(), unit.Id, "Impressora balcão", "PRN-01", DeviceType.Printer, "ESC/POS TCP");
        printer.ConfigureNetworkPrinter("Impressora balcão", "192.168.1.50", 9100, 80, true, true, false);
        printer.UpdateStatus(DeviceStatus.Online, null, false, "Connected", "192.168.1.50", null);
        fixture.Context.DeviceItems = [printer];

        await service.QueueOrderReceiptAsync(result.Id, fixture.IdentityUserId, CancellationToken.None);
        var kitchenResult = await service.QueueKitchenCommandAsync(result.Id, fixture.IdentityUserId, CancellationToken.None);

        kitchenResult.JobIds.Should().ContainSingle();
        fixture.Context.PrintJobEntities.Should().HaveCount(2);
        var customerJob = fixture.Context.PrintJobEntities.Single(job => job.DocumentType == PrintDocumentType.CustomerReceipt);
        customerJob.Payload.Should().Contain("COMPROVANTE DO CLIENTE").And.Contain("DOCUMENTO SEM VALOR FISCAL").And.Contain("Dinheiro").And.Contain("TROCO");
        var kitchenJob = fixture.Context.PrintJobEntities.Single(job => job.DocumentType == PrintDocumentType.KitchenTicket);
        kitchenJob.Payload.Should().Contain("COMANDA COZINHA").And.Contain("Bem assada.").And.Contain("Retirar cebola.").And.Contain("SEM VALORES").And.NotContain("R$");
        fixture.Context.KitchenTicketEntities.Single().Status.Should().Be(KitchenTicketStatus.Confirmed);
        fixture.Context.OrderEntities.Single().Status.Should().Be(OrderStatus.Accepted);
        fixture.Context.SaveChangesCalls.Should().Be(3);
    }

    private static CashierFixture CreateFixture()
    {
        var identityUserId = Guid.NewGuid();
        var unit = new RestaurantUnit(
            RestaurantUnitId.New(),
            "Unidade Principal",
            "Projeto Pizza LTDA",
            "Forno 27",
            "00.000.000/0001-00");
        var employee = new Employee(
            EmployeeId.New(),
            unit.Id,
            identityUserId,
            "Administrador",
            "admin@local.test",
            "ADMIN");
        var register = new CashRegister(CashRegisterId.New(), unit.Id, "Caixa Principal", "CX-01");
        var context = new FakeContext
        {
            RestaurantUnitItems = [unit],
            EmployeeItems = [employee],
            CashRegisterItems = [register],
        };
        return new CashierFixture(context, identityUserId, employee, register);
    }

    private sealed record CashierFixture(
        FakeContext Context,
        Guid IdentityUserId,
        Employee Employee,
        CashRegister Register);

    private sealed class FakeContext : IProjetoPizzaDbContext
    {
        public RestaurantUnit[] RestaurantUnitItems { get; init; } = [];
        public Employee[] EmployeeItems { get; init; } = [];
        public CashRegister[] CashRegisterItems { get; init; } = [];
        public OperationSettings[] OperationSettingItems { get; set; } = [];
        public List<Customer> CustomerItems { get; set; } = [];
        public Product[] ProductItems { get; set; } = [];
        public ProductionStation[] ProductionStationItems { get; set; } = [];
        public PaymentMethod[] PaymentMethodItems { get; set; } = [];
        public Device[] DeviceItems { get; set; } = [];
        public InventoryItem[] InventoryItemItems { get; set; } = [];
        public StockBalance[] StockBalanceItems { get; set; } = [];
        public Recipe[] RecipeEntities { get; set; } = [];
        public RecipeItem[] RecipeItemEntities { get; set; } = [];
        public DiningArea[] DiningAreaItems { get; set; } = [];
        public List<RestaurantTable> RestaurantTableItems { get; set; } = [];
        public List<CashShift> CashShiftItems { get; } = [];
        public List<Bill> BillEntities { get; } = [];
        public List<BillItem> BillItemEntities { get; } = [];
        public List<Payment> PaymentEntities { get; } = [];
        public List<PrintJob> PrintJobEntities { get; } = [];
        public List<Order> OrderEntities { get; } = [];
        public List<InventoryReservation> InventoryReservationEntities { get; } = [];
        public List<StockMovement> StockMovementEntities { get; } = [];
        public List<Reservation> ReservationEntities { get; } = [];
        public List<TableSession> TableSessionEntities { get; } = [];
        public List<KitchenTicket> KitchenTicketEntities { get; } = [];
        public List<KitchenTicketItem> KitchenTicketItemEntities { get; } = [];
        public List<AuditLog> AuditLogItems { get; } = [];
        public List<LoyaltySettings> LoyaltySettingItems { get; } = [];
        public List<LoyaltyTransaction> LoyaltyTransactionItems { get; } = [];
        public List<PromotionCoupon> PromotionCouponItems { get; } = [];
        public int SaveChangesCalls { get; private set; }

        public IQueryable<RestaurantUnit> RestaurantUnits => RestaurantUnitItems.AsQueryable();
        public IQueryable<OperationSettings> OperationSettings => OperationSettingItems.AsQueryable();
        public IQueryable<PizzaSettings> PizzaSettings => Array.Empty<PizzaSettings>().AsQueryable();
        public IQueryable<Employee> Employees => EmployeeItems.AsQueryable();
        public IQueryable<Customer> Customers => CustomerItems.AsQueryable();
        public IQueryable<LoyaltySettings> LoyaltySettings => LoyaltySettingItems.AsQueryable();
        public IQueryable<LoyaltyTransaction> LoyaltyTransactions => LoyaltyTransactionItems.AsQueryable();
        public IQueryable<PromotionCoupon> PromotionCoupons => PromotionCouponItems.AsQueryable();
        public IQueryable<Category> Categories => Array.Empty<Category>().AsQueryable();
        public IQueryable<Product> Products => ProductItems.AsQueryable();
        public IQueryable<ProductExtra> ProductExtras => Array.Empty<ProductExtra>().AsQueryable();
        public IQueryable<ProductImage> ProductImages => Array.Empty<ProductImage>().AsQueryable();
        public IQueryable<PizzaSize> PizzaSizes => Array.Empty<PizzaSize>().AsQueryable();
        public IQueryable<PizzaFlavor> PizzaFlavors => Array.Empty<PizzaFlavor>().AsQueryable();
        public IQueryable<PizzaFlavorPrice> PizzaFlavorPrices => Array.Empty<PizzaFlavorPrice>().AsQueryable();
        public IQueryable<PizzaCrust> PizzaCrusts => Array.Empty<PizzaCrust>().AsQueryable();
        public IQueryable<PizzaCrustPrice> PizzaCrustPrices => Array.Empty<PizzaCrustPrice>().AsQueryable();
        public IQueryable<Ingredient> Ingredients => Array.Empty<Ingredient>().AsQueryable();
        public IQueryable<PizzaFlavorIngredient> PizzaFlavorIngredients => Array.Empty<PizzaFlavorIngredient>().AsQueryable();
        public IQueryable<PizzaFlavorExtra> PizzaFlavorExtras => Array.Empty<PizzaFlavorExtra>().AsQueryable();
        public IQueryable<InventoryItem> InventoryItems => InventoryItemItems.AsQueryable();
        public IQueryable<StockBalance> StockBalances => StockBalanceItems.AsQueryable();
        public IQueryable<StockMovement> StockMovements => StockMovementEntities.AsQueryable();
        public IQueryable<InventoryReservation> InventoryReservations => InventoryReservationEntities.AsQueryable();
        public IQueryable<Recipe> Recipes => RecipeEntities.AsQueryable();
        public IQueryable<RecipeItem> RecipeItems => RecipeItemEntities.AsQueryable();
        public IQueryable<DiningArea> DiningAreas => DiningAreaItems.AsQueryable();
        public IQueryable<RestaurantTable> RestaurantTables => RestaurantTableItems.AsQueryable();
        public IQueryable<TableSession> TableSessions => TableSessionEntities.AsQueryable();
        public IQueryable<TableSessionTable> TableSessionTables => TableSessionEntities.SelectMany(session => session.Tables).AsQueryable();
        public IQueryable<Reservation> Reservations => ReservationEntities.AsQueryable();
        public IQueryable<WaitlistEntry> WaitlistEntries => Array.Empty<WaitlistEntry>().AsQueryable();
        public IQueryable<ServiceCallType> ServiceCallTypes => Array.Empty<ServiceCallType>().AsQueryable();
        public IQueryable<ServiceCall> ServiceCalls => Array.Empty<ServiceCall>().AsQueryable();
        public IQueryable<Order> Orders => OrderEntities.AsQueryable();
        public IQueryable<OrderItem> OrderItems => OrderEntities.SelectMany(order => order.Items).AsQueryable();
        public IQueryable<OrderItemPizza> OrderItemPizzas => Array.Empty<OrderItemPizza>().AsQueryable();
        public IQueryable<OrderItemPizzaFlavor> OrderItemPizzaFlavors => Array.Empty<OrderItemPizzaFlavor>().AsQueryable();
        public IQueryable<OrderItemModifier> OrderItemModifiers => Array.Empty<OrderItemModifier>().AsQueryable();
        public IQueryable<ProductionStation> ProductionStations => ProductionStationItems.AsQueryable();
        public IQueryable<KitchenTicket> KitchenTickets => KitchenTicketEntities.AsQueryable();
        public IQueryable<KitchenTicketItem> KitchenTicketItems => KitchenTicketItemEntities.AsQueryable();
        public IQueryable<Bill> Bills => BillEntities.AsQueryable();
        public IQueryable<BillSplit> BillSplits => Array.Empty<BillSplit>().AsQueryable();
        public IQueryable<PaymentMethod> PaymentMethods => PaymentMethodItems.AsQueryable();
        public IQueryable<Payment> Payments => PaymentEntities.AsQueryable();
        public IQueryable<CashRegister> CashRegisters => CashRegisterItems.AsQueryable();
        public IQueryable<CashShift> CashShifts => CashShiftItems.AsQueryable();
        public IQueryable<CashMovement> CashMovements => Array.Empty<CashMovement>().AsQueryable();
        public IQueryable<Device> Devices => DeviceItems.AsQueryable();
        public IQueryable<DeviceSession> DeviceSessions => Array.Empty<DeviceSession>().AsQueryable();
        public IQueryable<DeviceProvisioning> DeviceProvisionings => Array.Empty<DeviceProvisioning>().AsQueryable();
        public IQueryable<PrintJob> PrintJobs => PrintJobEntities.AsQueryable();
        public IQueryable<AuditLog> AuditLogs => AuditLogItems.AsQueryable();

        public void Add<TEntity>(TEntity entity) where TEntity : class
        {
            if (entity is CashShift shift) CashShiftItems.Add(shift);
            if (entity is Customer customer) CustomerItems.Add(customer);
            if (entity is Reservation reservation) ReservationEntities.Add(reservation);
            if (entity is Order order) OrderEntities.Add(order);
            if (entity is InventoryReservation inventoryReservation) InventoryReservationEntities.Add(inventoryReservation);
            if (entity is StockMovement stockMovement) StockMovementEntities.Add(stockMovement);
            if (entity is KitchenTicket kitchenTicket) KitchenTicketEntities.Add(kitchenTicket);
            if (entity is TableSession tableSession) TableSessionEntities.Add(tableSession);
            if (entity is KitchenTicketItem kitchenTicketItem) KitchenTicketItemEntities.Add(kitchenTicketItem);
            if (entity is Bill bill) BillEntities.Add(bill);
            if (entity is BillItem billItem) BillItemEntities.Add(billItem);
            if (entity is Payment payment) PaymentEntities.Add(payment);
            if (entity is PrintJob printJob) PrintJobEntities.Add(printJob);
            if (entity is AuditLog auditLog) AuditLogItems.Add(auditLog);
            if (entity is LoyaltySettings loyaltySettings) LoyaltySettingItems.Add(loyaltySettings);
            if (entity is LoyaltyTransaction loyaltyTransaction) LoyaltyTransactionItems.Add(loyaltyTransaction);
            if (entity is PromotionCoupon promotionCoupon) PromotionCouponItems.Add(promotionCoupon);
        }

        public void Remove<TEntity>(TEntity entity) where TEntity : class
        {
            if (entity is RestaurantTable table) RestaurantTableItems.Remove(table);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalls += 1;
            return Task.FromResult(1);
        }
    }
}
