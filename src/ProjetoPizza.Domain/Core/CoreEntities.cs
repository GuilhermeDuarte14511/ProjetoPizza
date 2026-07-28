using ProjetoPizza.Domain.SharedKernel;

namespace ProjetoPizza.Domain.Core;

public sealed class RestaurantUnit : AggregateRoot<RestaurantUnitId>
{
    private RestaurantUnit() : base(default) { }

    public RestaurantUnit(
        RestaurantUnitId id,
        string name,
        string legalName,
        string tradeName,
        string cnpj,
        string timezone = "America/Sao_Paulo",
        string currencyCode = Money.Brl) : base(id)
    {
        Name = Guard.Required(name, nameof(name), 120);
        LegalName = Guard.Required(legalName, nameof(legalName), 160);
        TradeName = Guard.Required(tradeName, nameof(tradeName), 160);
        Cnpj = Guard.Required(cnpj, nameof(cnpj), 18);
        Timezone = Guard.Required(timezone, nameof(timezone), 80);
        CurrencyCode = Guard.Required(currencyCode, nameof(currencyCode), 3).ToUpperInvariant();
        IsActive = true;
        CreatedAt = UpdatedAt = DateTimeOffset.UtcNow;
    }

    public string Name { get; private set; } = string.Empty;
    public string LegalName { get; private set; } = string.Empty;
    public string TradeName { get; private set; } = string.Empty;
    public string Cnpj { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? AdministrativeEmail { get; private set; }
    public string Timezone { get; private set; } = string.Empty;
    public string CurrencyCode { get; private set; } = Money.Brl;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Activate() => ChangeActive(true);
    public void Deactivate() => ChangeActive(false);

    public void UpdateContactInformation(string phone, string administrativeEmail)
    {
        Phone = Guard.Required(phone, nameof(phone), 24);
        AdministrativeEmail = Guard.Required(administrativeEmail, nameof(administrativeEmail), 254);
        Touch();
    }

    public void UpdateIdentification(string name, string legalName, string tradeName, string cnpj)
    {
        Name = Guard.Required(name, nameof(name), 120);
        LegalName = Guard.Required(legalName, nameof(legalName), 160);
        TradeName = Guard.Required(tradeName, nameof(tradeName), 160);
        Cnpj = Guard.Required(cnpj, nameof(cnpj), 18);
        Touch();
    }

    private void ChangeActive(bool value)
    {
        IsActive = value;
        Touch();
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}

public sealed class OperationSettings : Entity<RestaurantUnitId>
{
    private OperationSettings() : base(default) { }

    public OperationSettings(RestaurantUnitId unitId) : base(unitId)
    {
        ServiceFeePercentage = new Percentage(10);
        DefaultDeliveryFee = Money.Zero();
        TableCallToleranceMinutes = 5;
        DeliveryOrderSoundEnabled = true;
        TableCallSoundEnabled = true;
        ClearTabletAfterTableClose = true;
    }

    public RestaurantUnitId UnitId => Id;
    public bool AllowTableWithoutWaiter { get; private set; }
    public bool AllowOrdersWithoutOpenCashShift { get; private set; }
    public bool ClearTabletAfterTableClose { get; private set; }
    public Percentage ServiceFeePercentage { get; private set; }
    public Money DefaultDeliveryFee { get; private set; }
    public bool DeliveryOrderSoundEnabled { get; private set; }
    public bool TableCallSoundEnabled { get; private set; }
    public int TableCallToleranceMinutes { get; private set; }

    public void Update(
        bool allowTableWithoutWaiter,
        bool allowOrdersWithoutOpenCashShift,
        bool clearTabletAfterTableClose,
        Percentage serviceFeePercentage,
        Money defaultDeliveryFee,
        bool deliveryOrderSoundEnabled,
        bool tableCallSoundEnabled,
        int tableCallToleranceMinutes)
    {
        if (tableCallToleranceMinutes is < 1 or > 120)
        {
            throw new BusinessRuleException(
                "operation_settings.table_call_tolerance",
                "Table call tolerance must be between 1 and 120 minutes.");
        }

        AllowTableWithoutWaiter = allowTableWithoutWaiter;
        AllowOrdersWithoutOpenCashShift = allowOrdersWithoutOpenCashShift;
        ClearTabletAfterTableClose = clearTabletAfterTableClose;
        ServiceFeePercentage = serviceFeePercentage;
        DefaultDeliveryFee = defaultDeliveryFee;
        DeliveryOrderSoundEnabled = deliveryOrderSoundEnabled;
        TableCallSoundEnabled = tableCallSoundEnabled;
        TableCallToleranceMinutes = tableCallToleranceMinutes;
    }
}

public enum PizzaPricingPolicy
{
    HighestFlavorPrice,
    AverageFlavorPrice,
    ProportionalFlavorPrice
}

public interface IPizzaPricingPolicy
{
    PizzaPricingPolicy Policy { get; }
    Money Calculate(IReadOnlyCollection<Money> flavorPrices);
}

public sealed class PizzaSettings : Entity<RestaurantUnitId>
{
    private PizzaSettings() : base(default) { }

    public PizzaSettings(RestaurantUnitId unitId) : base(unitId)
    {
        GlobalMaxFlavors = 3;
        PricingPolicy = PizzaPricingPolicy.HighestFlavorPrice;
    }

    public RestaurantUnitId UnitId => Id;
    public int GlobalMaxFlavors { get; private set; }
    public PizzaPricingPolicy PricingPolicy { get; private set; }
    public bool AllowSweetAndSavoryMix { get; private set; }
    public bool AllowExtrasPerFlavor { get; private set; } = true;
    public bool AllowRepeatedFlavors { get; private set; }

    public void Update(
        int globalMaxFlavors,
        PizzaPricingPolicy pricingPolicy,
        bool allowSweetAndSavoryMix,
        bool allowExtrasPerFlavor,
        bool allowRepeatedFlavors)
    {
        if (globalMaxFlavors is < 1 or > 3)
        {
            throw new BusinessRuleException(
                "pizza_settings.global_max_flavors",
                "Global maximum flavors must be between one and three.");
        }

        GlobalMaxFlavors = globalMaxFlavors;
        PricingPolicy = pricingPolicy;
        AllowSweetAndSavoryMix = allowSweetAndSavoryMix;
        AllowExtrasPerFlavor = allowExtrasPerFlavor;
        AllowRepeatedFlavors = allowRepeatedFlavors;
    }
}
