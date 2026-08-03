namespace ProjetoPizza.Application.Client;

public sealed record ActivateClientSessionCommand(
    string? DeviceCode = null,
    string? ProvisioningToken = null);

public sealed record ClientActivationDto(
    string Token,
    ClientBootstrapDto Bootstrap);

public sealed record ClientSessionContext(
    Guid DeviceSessionId,
    Guid DeviceId,
    Guid? TableSessionId,
    Guid RestaurantUnitId,
    Guid TableId,
    int TableNumber);

public sealed record ClientSessionDto(
    Guid DeviceId,
    Guid? TableSessionId,
    string RestaurantName,
    int TableNumber,
    string TableName,
    int GuestCount,
    string Status,
    string? WaiterName,
    bool ClearTabletAfterTableClose);

public sealed record StartClientTableSessionCommand(int GuestCount);

public sealed record ClientBootstrapDto(
    ClientSessionDto Session,
    ClientCatalogDto Catalog,
    IReadOnlyList<ClientServiceCallTypeDto> ServiceCallTypes,
    IReadOnlyList<ClientOrderDto> Orders,
    ClientBillDto Bill);

public sealed record ClientStateDto(
    ClientSessionDto Session,
    IReadOnlyList<ClientOrderDto> Orders,
    ClientBillDto Bill);

public sealed record ClientCatalogDto(
    IReadOnlyList<ClientCategoryDto> Categories,
    IReadOnlyList<ClientProductDto> Products,
    ClientPizzaCatalogDto Pizza,
    decimal ServiceFeePercentage);

public sealed record ClientCategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Icon,
    int DisplayOrder);

public sealed record ClientProductDto(
    Guid Id,
    Guid CategoryId,
    string Name,
    string? Description,
    string ProductType,
    decimal Price,
    string? ImageUrl,
    bool IsFeatured,
    bool IsPopular,
    int PreparationTimeMinutes,
    bool UsesCustomExtras,
    IReadOnlyList<ClientPizzaExtraDto> Complements);

public sealed record ClientPizzaCatalogDto(
    int GlobalMaxFlavors,
    string PricingPolicy,
    bool AllowSweetAndSavoryMix,
    bool AllowExtrasPerFlavor,
    bool AllowRepeatedFlavors,
    IReadOnlyList<ClientPizzaSizeDto> Sizes,
    IReadOnlyList<ClientPizzaFlavorDto> Flavors,
    IReadOnlyList<ClientPizzaCrustDto> Crusts,
    IReadOnlyList<ClientPizzaExtraDto> Extras);

public sealed record ClientPizzaSizeDto(
    Guid Id,
    string Name,
    string ShortName,
    int Slices,
    decimal DiameterCm,
    decimal BasePrice,
    int MaxFlavors);

public sealed record ClientPizzaFlavorDto(
    Guid Id,
    Guid CategoryId,
    string Name,
    string? Description,
    string FlavorType,
    bool IsPremium,
    bool IsVegetarian,
    bool IsAvailable,
    string? SoldOutReason,
    string? ImageUrl,
    IReadOnlyList<ClientPizzaFlavorPriceDto> Prices,
    IReadOnlyList<ClientIngredientDto> Ingredients,
    IReadOnlyList<ClientPizzaExtraDto> Extras);

public sealed record ClientPizzaFlavorPriceDto(
    Guid PizzaSizeId,
    decimal Price,
    decimal AdditionalPrice,
    bool IsAvailable);

public sealed record ClientIngredientDto(
    Guid Id,
    string Name,
    bool IsRemovable,
    bool IsAllergen,
    string? AllergenDescription);

public sealed record ClientPizzaExtraDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int MaxQuantity,
    bool IsAllergen,
    string? AllergenDescription);

public sealed record ClientPizzaCrustDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsAvailable,
    IReadOnlyList<ClientPizzaCrustPriceDto> Prices);

public sealed record ClientPizzaCrustPriceDto(
    Guid PizzaSizeId,
    decimal FullPrice,
    decimal HalfPrice);

public sealed record ClientServiceCallTypeDto(Guid Id, string Code, string Name);

public sealed record CreateClientServiceCallCommand(
    Guid ServiceCallTypeId,
    string? Details);

public sealed record RequestClientBillCommand(int? SplitCount);

public sealed record SubmitClientOrderCommand(
    Guid RequestId,
    IReadOnlyList<SubmitClientOrderItemCommand> Items,
    string? Notes);

public sealed record SubmitClientOrderItemCommand(
    Guid ProductId,
    int Quantity,
    string? Notes,
    SubmitClientPizzaCommand? Pizza);

public sealed record SubmitClientPizzaCommand(
    Guid SizeId,
    IReadOnlyList<Guid> FlavorIds,
    Guid? CrustId,
    Guid? SecondCrustId,
    IReadOnlyList<Guid> RemovedIngredientIds,
    IReadOnlyList<SubmitClientPizzaExtraCommand>? ExtraIngredients = null);

public sealed record SubmitClientPizzaExtraCommand(
    Guid IngredientId,
    Guid? PizzaFlavorId,
    int Quantity);

public sealed record ClientOrderDto(
    Guid Id,
    long Number,
    string Status,
    DateTimeOffset? PlacedAt,
    decimal Subtotal,
    decimal Total,
    IReadOnlyList<ClientOrderItemDto> Items);

public sealed record ClientOrderItemDto(
    Guid Id,
    string Name,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice,
    string Status,
    string? Notes,
    ClientOrderPizzaDto? Pizza,
    IReadOnlyList<ClientOrderModifierDto> Modifiers);

public sealed record ClientOrderPizzaDto(
    string Size,
    IReadOnlyList<string> Flavors,
    string? Crust);

public sealed record ClientOrderModifierDto(
    string Type,
    string Name,
    decimal Quantity,
    decimal UnitPrice,
    decimal TotalPrice,
    Guid? PizzaFlavorId);

public sealed record ClientBillDto(
    Guid? Id,
    string Status,
    decimal Subtotal,
    decimal ServiceFeePercentage,
    decimal ServiceFeeAmount,
    decimal Total,
    decimal Paid,
    decimal Remaining,
    DateTimeOffset? RequestedAt,
    int? RequestedSplitCount);

public sealed record ClientCommandResultDto(Guid Id, string Status);
