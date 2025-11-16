using PaymentGateway.Application;
using PaymentGateway.Application.Interfaces;

namespace PaymentGateway.Infrastructure;

public class CurrencyService : ICurrencyService
{
    private static readonly IReadOnlyDictionary<string, int> Info =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "USD", 2 },
            { "GBP", 2 },
            { "EUR", 2 }
        };

    public IReadOnlyCollection<string> SupportedCurrencies => Info.Keys.Select(k => k.ToUpperInvariant()).ToList();

    public Task<int> GetMinorUnit(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required", nameof(currency));
        var key = currency.Trim();
        if (Info.TryGetValue(key, out var minor)) return Task.FromResult(minor);
        throw new KeyNotFoundException($"Unsupported currency: {currency}");
    }

    public Task<bool> IsSupported(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency)) throw new ArgumentException("Currency is required", nameof(currency));
        bool supported = Info.ContainsKey(currency.Trim());
        return Task.FromResult(supported);
    }
}
