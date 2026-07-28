namespace ProjetoPizza.Domain.SharedKernel;

public class DomainException(string message) : Exception(message);

public sealed class BusinessRuleException(string rule, string message) : DomainException(message)
{
    public string Rule { get; } = rule;
}

public static class Guard
{
    public static string Required(string? value, string name, int maxLength = int.MaxValue)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new BusinessRuleException($"{name}.required", $"{name} is required.");
        }

        if (normalized.Length > maxLength)
        {
            throw new BusinessRuleException($"{name}.max_length", $"{name} must have at most {maxLength} characters.");
        }

        return normalized;
    }

    public static decimal NonNegative(decimal value, string name)
    {
        if (value < 0)
        {
            throw new BusinessRuleException($"{name}.non_negative", $"{name} cannot be negative.");
        }

        return value;
    }

    public static int Positive(int value, string name)
    {
        if (value <= 0)
        {
            throw new BusinessRuleException($"{name}.positive", $"{name} must be greater than zero.");
        }

        return value;
    }
}
