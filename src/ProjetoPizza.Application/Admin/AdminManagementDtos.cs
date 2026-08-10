using ProjetoPizza.Application.Client;

namespace ProjetoPizza.Application.Admin;

public sealed record OrderManagementDto(
    Guid Id,
    long Number,
    string Channel,
    string Fulfillment,
    string Status,
    Guid? CustomerId,
    string? CustomerName,
    string? DeliveryAddress,
    string? DeliveryStatus,
    string? DeliveryDriverName,
    DateTimeOffset? DispatchedAt,
    DateTimeOffset? DeliveredAt,
    string? Notes,
    decimal Total,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PlacedAt,
    IReadOnlyCollection<OrderLineDto> Items);

public sealed record OrderLineDto(Guid Id, string Name, int Quantity, decimal UnitPrice, decimal TotalPrice, string Status);
public sealed record AdministrativeOrderCatalogDto(ClientCatalogDto Catalog, decimal DefaultDeliveryFee);
public sealed record CustomerDto(
    Guid Id,
    string Name,
    string Phone,
    DateOnly BirthDate,
    bool IsActive,
    DateTimeOffset CreatedAt);
public sealed record CreatedOrderDto(Guid Id, long Number, string Status, decimal Total, OrderReceiptDto Receipt);
public sealed record CounterCheckoutResultDto(Guid Id, long Number, string Status, decimal Total, OrderReceiptDto Receipt);
public sealed record OrderReceiptDto(
    Guid Id,
    long Number,
    string CustomerName,
    string CustomerPhone,
    string Fulfillment,
    string? DeliveryAddress,
    DateTimeOffset PlacedAt,
    decimal Subtotal,
    decimal DeliveryFee,
    decimal Discount,
    decimal Total,
    decimal PaidAmount,
    decimal ChangeAmount,
    string? Notes,
    IReadOnlyCollection<OrderReceiptItemDto> Items,
    IReadOnlyCollection<OrderReceiptPaymentDto> Payments);
public sealed record OrderReceiptItemDto(
    Guid Id,
    string Name,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice,
    string? Notes,
    IReadOnlyCollection<string> Details);
public sealed record OrderReceiptPaymentDto(
    string Method,
    decimal Amount,
    decimal ReceivedAmount,
    decimal ChangeAmount,
    DateTimeOffset PaidAt);
public sealed record PrintBatchResultDto(IReadOnlyCollection<Guid> JobIds, string Status);
public sealed record PizzaCrustDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    bool IsAvailable,
    IReadOnlyList<PizzaCrustPriceDto> Prices);

public sealed record PizzaCrustPriceDto(
    Guid PizzaSizeId,
    string PizzaSizeName,
    int SliceCount,
    decimal FullPrice,
    decimal HalfPrice);
public sealed record IngredientDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive,
    bool IsAllergen,
    string? AllergenDescription,
    bool IsAvailableAsExtra,
    decimal ExtraPrice,
    int MaxExtraQuantity);

public sealed record UnitSettingsDto(
    Guid Id,
    string Name,
    string LegalName,
    string TradeName,
    string Cnpj,
    string? Phone,
    string? AdministrativeEmail,
    string Timezone,
    string CurrencyCode);

public sealed record OperationSettingsDto(
    bool AllowTableWithoutWaiter,
    bool AllowOrdersWithoutOpenCashShift,
    bool ClearTabletAfterTableClose,
    decimal ServiceFeePercentage,
    decimal DefaultDeliveryFee,
    bool DeliveryOrderSoundEnabled,
    bool TableCallSoundEnabled,
    int TableCallToleranceMinutes);

public sealed record PizzaRulesDto(
    int GlobalMaxFlavors,
    string PricingPolicy,
    bool AllowSweetAndSavoryMix,
    bool AllowExtrasPerFlavor,
    bool AllowRepeatedFlavors);

public sealed record CashShiftDto(
    Guid Id,
    string Register,
    string Operator,
    string Status,
    DateTimeOffset OpenedAt,
    decimal OpeningAmount,
    decimal ExpectedCashAmount,
    decimal? CountedCashAmount,
    decimal? DifferenceAmount,
    IReadOnlyCollection<CashMovementDto> Movements);

public sealed record CashRegisterDto(
    Guid Id,
    string Name,
    string Code,
    bool IsActive);

public sealed record CashShiftHistoryDto(
    Guid Id,
    string Register,
    string Operator,
    string? ClosedBy,
    string Status,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt,
    decimal OpeningAmount,
    decimal ExpectedCashAmount,
    decimal? CountedCashAmount,
    decimal? DifferenceAmount,
    string? ClosingNotes,
    IReadOnlyCollection<CashMovementDto> Movements);

public sealed record DiningAreaAdminDto(Guid Id, string Name, int DisplayOrder, bool IsActive);
public sealed record RestaurantTableAdminDto(
    Guid Id,
    Guid DiningAreaId,
    string AreaName,
    int Number,
    string Name,
    int Capacity,
    int DisplayOrder,
    bool IsActive);
public sealed record ProductionStationAdminDto(
    Guid Id,
    string Name,
    string Code,
    int TargetPreparationMinutes,
    int DisplayOrder,
    bool IsActive);
public sealed record ServiceCallTypeAdminDto(Guid Id, string Code, string Name, bool IsActive);
public sealed record InventoryItemAdminDto(
    Guid Id,
    string Name,
    string Sku,
    string UnitOfMeasure,
    decimal MinimumStock,
    decimal CurrentQuantity,
    decimal ReservedQuantity,
    decimal AvailableQuantity,
    bool IsLowStock,
    bool IsActive);

public sealed record CashMovementDto(
    Guid Id,
    string Type,
    decimal Amount,
    string Description,
    string Reason,
    DateTimeOffset CreatedAt);

public sealed record PaymentMethodDto(
    Guid Id,
    string Code,
    string Name,
    bool RequiresExternalReference,
    bool AllowsChange,
    int DisplayOrder,
    bool IsActive);

public sealed record PaymentDto(
    Guid Id,
    Guid BillId,
    string? Payer,
    string Method,
    string Status,
    decimal Amount,
    decimal ReceivedAmount,
    decimal ChangeAmount,
    string? ExternalReference,
    DateTimeOffset? PaidAt);

public sealed record FinancialReportDto(
    DateTimeOffset From,
    DateTimeOffset To,
    decimal GrossSales,
    decimal PaidAmount,
    decimal AverageTicket,
    int OrderCount,
    IReadOnlyCollection<FinancialChannelDto> Channels,
    IReadOnlyCollection<FinancialMethodDto> PaymentMethods);

public sealed record FinancialChannelDto(string Channel, int Orders, decimal Total);
public sealed record FinancialMethodDto(string Method, int Payments, decimal Total);

public sealed record DeviceDto(
    Guid Id,
    string Name,
    string SerialNumber,
    string Type,
    string Platform,
    string Status,
    int? BatteryPercentage,
    bool IsCharging,
    string? NetworkStatus,
    string? IpAddress,
    string? AppVersion,
    DateTimeOffset? LastSeenAt,
    Guid? LinkedTableId,
    bool IsLocked,
    int? PrinterPort,
    int? PaperWidthMm,
    bool AutoPrintKitchenTickets,
    bool AutoPrintCustomerReceipts,
    bool AutoPrintFiscalDocuments);

public sealed record PrintJobDto(
    Guid Id,
    Guid PrinterId,
    string PrinterName,
    string DocumentType,
    string Status,
    int Attempts,
    string? LastError,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

public sealed record DeviceProvisioningDto(
    DeviceDto Device,
    string ActivationToken,
    DateTimeOffset ExpiresAt);

public sealed record AuditLogDto(
    Guid Id,
    string Module,
    string Action,
    string EntityType,
    string EntityId,
    string EntityDescription,
    string? Employee,
    DateTimeOffset OccurredAt);

public sealed record SystemSnapshotDto(
    DateTimeOffset GeneratedAt,
    UnitSettingsDto Unit,
    int Categories,
    int Products,
    int Tables,
    int Orders,
    int Payments,
    int Devices);

public sealed record DatabaseBackupDto(
    string FileName,
    DateTimeOffset CreatedAt,
    long SizeBytes,
    string Type);

public sealed record UpdateUnitCommand(
    string Name,
    string LegalName,
    string TradeName,
    string Cnpj,
    string Phone,
    string AdministrativeEmail);

public sealed record UpdateOperationSettingsCommand(
    bool AllowTableWithoutWaiter,
    bool AllowOrdersWithoutOpenCashShift,
    bool ClearTabletAfterTableClose,
    decimal ServiceFeePercentage,
    decimal DefaultDeliveryFee,
    bool DeliveryOrderSoundEnabled,
    bool TableCallSoundEnabled,
    int TableCallToleranceMinutes);

public sealed record UpdatePizzaRulesCommand(
    int GlobalMaxFlavors,
    string PricingPolicy,
    bool AllowSweetAndSavoryMix,
    bool AllowExtrasPerFlavor,
    bool AllowRepeatedFlavors);

public sealed record SaveCategoryCommand(
    Guid? Id,
    string Name,
    string Slug,
    string? Description,
    bool IsVisibleOnTablet,
    bool IsActive);

public sealed record SaveProductCommand(
    Guid? Id,
    Guid CategoryId,
    string Sku,
    string Name,
    string? Description,
    string Type,
    decimal BasePrice,
    int PreparationTimeMinutes,
    bool IsActive,
    bool IsAvailable,
    bool IsFeatured,
    IReadOnlyCollection<SaveProductExtraCommand>? Complements = null);

public sealed record SaveProductExtraCommand(
    Guid? IngredientId,
    string Name,
    decimal Price,
    int MaxQuantity);

public sealed record SavePizzaSizeCommand(
    Guid? Id,
    string Name,
    string ShortName,
    int Slices,
    decimal DiameterCm,
    decimal BasePrice,
    int MaxFlavors,
    bool IsActive);

public sealed record SavePizzaCrustCommand(
    Guid? Id,
    string Name,
    string? Description,
    bool IsActive,
    bool IsAvailable,
    IReadOnlyList<SavePizzaCrustPriceCommand>? Prices = null);

public sealed record SavePizzaCrustPriceCommand(
    Guid PizzaSizeId,
    decimal FullPrice,
    decimal HalfPrice);

public sealed record SaveIngredientCommand(
    Guid? Id,
    string Name,
    string? Description,
    bool IsActive,
    bool IsAllergen,
    string? AllergenDescription,
    bool IsAvailableAsExtra,
    decimal ExtraPrice,
    int MaxExtraQuantity);

public sealed record SavePizzaFlavorCommand(
    Guid? Id,
    Guid CategoryId,
    string Name,
    string? Description,
    string Type,
    bool IsPremium,
    bool IsVegetarian,
    bool IsActive,
    bool IsAvailable,
    string? SoldOutReason,
    IReadOnlyCollection<SavePizzaFlavorExtraCommand>? Extras = null);

public sealed record SavePizzaFlavorExtraCommand(
    Guid IngredientId,
    decimal Price,
    int MaxQuantity);

public sealed record SaveCustomerCommand(
    Guid? Id,
    string Name,
    string Phone,
    DateOnly BirthDate,
    bool IsActive = true);

public sealed record CreateAdministrativeOrderCommand(
    Guid RequestId,
    Guid CustomerId,
    string Fulfillment,
    string? DeliveryAddress,
    decimal DiscountAmount,
    string? Notes,
    IReadOnlyList<SubmitClientOrderItemCommand> Items);

public sealed record CounterPaymentCommand(
    Guid PaymentMethodId,
    decimal ReceivedAmount,
    string? ExternalReference);

public sealed record CheckoutCounterOrderCommand(
    CreateAdministrativeOrderCommand Order,
    CounterPaymentCommand Payment);

public sealed record DispatchDeliveryCommand(string DriverName);
public sealed record FailDeliveryCommand(string Reason);

public sealed record OpenTableCommand(Guid TableId, int GuestCount);
public sealed record RecordPaymentCommand(Guid BillId, Guid PaymentMethodId, decimal Amount, decimal ReceivedAmount, string? ExternalReference);
public sealed record SplitPaymentPartCommand(
    string Payer,
    Guid PaymentMethodId,
    decimal Amount,
    decimal ReceivedAmount,
    string? ExternalReference);
public sealed record RecordSplitPaymentCommand(Guid BillId, IReadOnlyCollection<SplitPaymentPartCommand> Payments);
public sealed record RegisterCashMovementCommand(string Type, decimal Amount, string Description, string Reason);
public sealed record OpenCashShiftCommand(Guid CashRegisterId, decimal OpeningAmount);
public sealed record CloseCashShiftCommand(decimal CountedCashAmount, string? Notes);
public sealed record SaveDiningAreaCommand(Guid? Id, string Name, int DisplayOrder, bool IsActive);
public sealed record SaveRestaurantTableCommand(
    Guid? Id,
    Guid DiningAreaId,
    int Number,
    string Name,
    int Capacity,
    int DisplayOrder,
    bool IsActive);
public sealed record SaveCashRegisterCommand(Guid? Id, string Name, string Code, bool IsActive);
public sealed record SavePaymentMethodCommand(
    Guid? Id,
    string Code,
    string Name,
    bool RequiresExternalReference,
    bool AllowsChange,
    int DisplayOrder,
    bool IsActive);
public sealed record SaveProductionStationCommand(
    Guid? Id,
    string Name,
    string Code,
    int TargetPreparationMinutes,
    int DisplayOrder,
    bool IsActive);
public sealed record SaveServiceCallTypeCommand(Guid? Id, string Code, string Name, bool IsActive);
public sealed record SaveInventoryItemCommand(
    Guid? Id,
    string Name,
    string Sku,
    string UnitOfMeasure,
    decimal MinimumStock,
    bool IsActive);
public sealed record AdjustInventoryCommand(decimal QuantityDelta, string Reason);
public sealed record UpdateDeviceCommand(
    string Status,
    int? BatteryPercentage,
    bool IsCharging,
    string? NetworkStatus,
    string? IpAddress,
    string? AppVersion,
    Guid? LinkedTableId,
    bool IsLocked);

public sealed record SaveNetworkPrinterCommand(
    Guid? Id,
    string Name,
    string Host,
    int Port,
    int PaperWidthMm,
    bool AutoPrintKitchenTickets,
    bool AutoPrintCustomerReceipts,
    bool AutoPrintFiscalDocuments,
    bool IsActive);

public sealed record CreateCustomerTabletCommand(
    string Name,
    string Platform,
    Guid LinkedTableId);

public sealed record ProvisionCustomerTabletCommand(Guid LinkedTableId);

public sealed record CommandResultDto(Guid Id, string Status);
