using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PaymentGateway.Application.Commands.CreatePayment;
using PaymentGateway.Application.Interfaces;
using PaymentGateway.Infrastructure;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreatePaymentCommand).Assembly));
        services.AddValidatorsFromAssembly(typeof(CreatePaymentCommandValidator).Assembly);
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IAcquiringBankHttpClient, AcquiringBankHttpClient>();
        services.AddSingleton<ICurrencyService, CurrencyService>();
        services.AddSingleton<PaymentsRepository>();
        services.AddHttpClient<IAcquiringBankHttpClient, AcquiringBankHttpClient>(client =>
        {
            client.BaseAddress = new Uri("http://localhost:8080/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        return services;
    }
}