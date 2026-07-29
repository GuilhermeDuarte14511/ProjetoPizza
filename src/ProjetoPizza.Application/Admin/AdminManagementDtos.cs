namespace ProjetoPizza.Application.Admin;

public sealed record OrderManagementDto(
    Guid Id,
    long Number,
    string Channel,
    string Fulfillment,
    string Status,
    decimal Total,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PlacedAt,
    IReadOnlyCollection<OrderLineDto> Items);

public sealed record OrderLineDto(Guid Id, string Name, int Quantity, decimal UnitPrice, decimal TotalPrice, string Status);
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
    string Code);

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
    bool IsLocked);

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
public sealed record UpdateDeviceCommand(
    string Status,
    int? BatteryPercentage,
    bool IsCharging,
    string? NetworkStatus,
    string? IpAddress,
    string? AppVersion,
    Guid? LinkedTableId,
    bool IsLocked);

public sealed record CreateCustomerTabletCommand(
    string Name,
    string Platform,
    Guid LinkedTableId);

public sealed record ProvisionCustomerTabletCommand(Guid LinkedTableId);

public sealed record CommandResultDto(Guid Id, string Status);
