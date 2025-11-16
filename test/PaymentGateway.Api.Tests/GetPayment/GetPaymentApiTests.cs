using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using PaymentGateway.Application.Commands.CreatePayment;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Api.Tests.GetPayment;

public class GetPaymentApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public GetPaymentApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetPayment_Should_Return_Ok_When_Payment_Exists()
    {
        // Arrange
        var existingPaymentId = Guid.Parse("11111111-1111-1111-1111-111111111111"); // From seeded data

        // Act
        var response = await _client.GetAsync($"/api/payments/{existingPaymentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PaymentResponseDto<Payment>>();
        result.Should().NotBeNull();
        result!.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(existingPaymentId);
        result.Value.Status.Should().Be(AcquiringStatus.Authorized);
        result.Value.CardLast4.Should().Be("4242");
        result.Value.ExpiryMonth.Should().Be(12);
        result.Value.ExpiryYear.Should().Be(2026);
        result.Value.Currency.Should().Be("GBP");
        result.Value.Amount.Should().Be(1050);
        result.AcquiringStatus.Should().Be(AcquiringStatus.Authorized);
    }

    [Fact]
    public async Task GetPayment_Should_Return_Ok_With_Declined_Payment()
    {
        // Arrange
        var declinedPaymentId = Guid.Parse("22222222-2222-2222-2222-222222222222"); // From seeded data

        // Act
        var response = await _client.GetAsync($"/api/payments/{declinedPaymentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PaymentResponseDto<Payment>>();
        result.Should().NotBeNull();
        result!.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(declinedPaymentId);
        result.Value.Status.Should().Be(AcquiringStatus.Declined);
        result.Value.CardLast4.Should().Be("1234");
        result.Value.ExpiryMonth.Should().Be(6);
        result.Value.ExpiryYear.Should().Be(2025);
        result.Value.Currency.Should().Be("USD");
        result.Value.Amount.Should().Be(2500);
    }

    [Fact]
    public async Task GetPayment_Should_Return_Ok_With_EUR_Payment()
    {
        // Arrange
        var eurPaymentId = Guid.Parse("33333333-3333-3333-3333-333333333333"); // From seeded data

        // Act
        var response = await _client.GetAsync($"/api/payments/{eurPaymentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PaymentResponseDto<Payment>>();
        result.Should().NotBeNull();
        result!.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(eurPaymentId);
        result.Value.Status.Should().Be(AcquiringStatus.Authorized);
        result.Value.CardLast4.Should().Be("9876");
        result.Value.ExpiryMonth.Should().Be(3);
        result.Value.ExpiryYear.Should().Be(2027);
        result.Value.Currency.Should().Be("EUR");
        result.Value.Amount.Should().Be(199);
    }

    [Fact]
    public async Task GetPayment_Should_Return_Ok_With_Empty_Response_When_Payment_Not_Found()
    {
        // Arrange
        var nonExistentPaymentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/payments/{nonExistentPaymentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PaymentResponseDto<Payment>>();
        result.Should().NotBeNull();
        result!.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetPayment_Should_Return_BadRequest_When_Id_Is_Invalid_Format()
    {
        // Arrange
        var invalidId = "not-a-guid";

        // Act
        var response = await _client.GetAsync($"/api/payments/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPayment_Should_Return_Ok_When_Id_Is_Empty_Guid()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        // Act
        var response = await _client.GetAsync($"/api/payments/{emptyGuid}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PaymentResponseDto<Payment>>();
        result.Should().NotBeNull();
        result!.Value.Should().BeNull();
    }

    [Fact]
    public async Task GetPayment_Should_Handle_Multiple_Concurrent_Requests()
    {
        // Arrange
        var paymentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _client.GetAsync($"/api/payments/{paymentId}"))
            .ToArray();

        // Act
        var responses = await Task.WhenAll(tasks);

        // Assert
        responses.Should().AllSatisfy(response =>
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        });

        var results = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<PaymentResponseDto<Payment>>()));
        results.Should().AllSatisfy(result =>
        {
            result.Should().NotBeNull();
            result!.Value.Should().NotBeNull();
            result.Value!.Id.Should().Be(paymentId);
        });
    }

    [Theory]
    [InlineData("11111111-1111-1111-1111-111111111111")]
    [InlineData("22222222-2222-2222-2222-222222222222")]
    [InlineData("33333333-3333-3333-3333-333333333333")]
    public async Task GetPayment_Should_Return_Correct_Payment_For_Each_Seeded_Id(string paymentIdString)
    {
        // Arrange
        var paymentId = Guid.Parse(paymentIdString);

        // Act
        var response = await _client.GetAsync($"/api/payments/{paymentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PaymentResponseDto<Payment>>();
        result.Should().NotBeNull();
        result!.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(paymentId);
        result.Value.CardLast4.Should().NotBeNullOrEmpty();
        result.Value.Currency.Should().NotBeNullOrEmpty();
        result.Value.Amount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetPayment_Should_Return_Same_Result_For_Multiple_Calls()
    {
        // Arrange
        var paymentId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Act
        var response1 = await _client.GetAsync($"/api/payments/{paymentId}");
        var response2 = await _client.GetAsync($"/api/payments/{paymentId}");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result1 = await response1.Content.ReadFromJsonAsync<PaymentResponseDto<Payment>>();
        var result2 = await response2.Content.ReadFromJsonAsync<PaymentResponseDto<Payment>>();
        
        result1.Should().BeEquivalentTo(result2);
    }

    [Fact]
    public async Task GetPayment_Should_Preserve_Payment_Properties()
    {
        // Arrange
        var paymentId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        // Act
        var response = await _client.GetAsync($"/api/payments/{paymentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<PaymentResponseDto<Payment>>();
        result.Should().NotBeNull();
        result!.Value.Should().NotBeNull();
        
        // Verify all payment properties are preserved
        var payment = result.Value!;
        payment.Id.Should().NotBe(Guid.Empty);
        payment.Status.Should().BeDefined();
        payment.CardLast4.Should().HaveLength(4);
        payment.ExpiryMonth.Should().BeInRange(1, 12);
        payment.ExpiryYear.Should().BeGreaterThan(2020);
        payment.Currency.Should().HaveLength(3);
        payment.Amount.Should().BePositive();
    }
}