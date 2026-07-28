namespace ProjetoPizza.Domain.SharedKernel;

public readonly record struct Percentage
{
    public Percentage(decimal value)
    {
        if (value is < 0 or > 100)
        {
            throw new BusinessRuleException("percentage.range", "Percentage must be between zero and one hundred.");
        }

        Value = decimal.Round(value, 2, MidpointRounding.ToEven);
    }

    public decimal Value { get; }
    public decimal AsFactor => Value / 100m;
}
