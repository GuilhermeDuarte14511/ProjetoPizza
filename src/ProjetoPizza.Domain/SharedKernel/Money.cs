namespace ProjetoPizza.Domain.SharedKernel;

public readonly record struct Money
{
    public const string Brl = "BRL";

    public Money(decimal amount, string currency = Brl)
    {
        if (amount < 0)
        {
            throw new BusinessRuleException("money.non_negative", "Money cannot be negative.");
        }

        Currency = Guard.Required(currency, nameof(currency), 3).ToUpperInvariant();
        Amount = decimal.Round(amount, 2, MidpointRounding.ToEven);
    }

    public decimal Amount { get; }
    public string Currency { get; }

    public static Money Zero(string currency = Brl) => new(0m, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal quantity)
    {
        Guard.NonNegative(quantity, nameof(quantity));
        return new Money(Amount * quantity, Currency);
    }

    public static Money operator +(Money left, Money right) => left.Add(right);
    public static Money operator -(Money left, Money right) => left.Subtract(right);
    public static Money operator *(Money money, decimal quantity) => money.Multiply(quantity);

    private void EnsureSameCurrency(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
        {
            throw new BusinessRuleException("money.currency_mismatch", "Money operations require the same currency.");
        }
    }
}
