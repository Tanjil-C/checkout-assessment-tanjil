namespace PaymentGateway.Application.Interfaces;

public interface ICurrencyService
{
    IReadOnlyCollection<string> SupportedCurrencies { get; }
    Task<bool> IsSupported(string currency);
    Task<int> GetMinorUnit(string currency);
}
