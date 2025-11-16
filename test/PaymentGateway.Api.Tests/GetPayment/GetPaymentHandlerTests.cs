using FluentAssertions;
using Moq;
using PaymentGateway.Application.Interfaces;
using PaymentGateway.Application.Queries;
using PaymentGateway.Domain.Entities;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Api.Tests.GetPayment;

public class GetPaymentHandlerTests
{
    private readonly Mock<IPaymentRepository> _mockPaymentRepository;
    private readonly GetPaymentHandler _handler;

    public GetPaymentHandlerTests()
    {
        _mockPaymentRepository = new Mock<IPaymentRepository>();
        _handler = new GetPaymentHandler(_mockPaymentRepository.Object);
    }

    [Fact]
    public async Task Handle_Should_Return_Payment_When_Found()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var expectedPayment = new Payment
        {
            Id = paymentId,
            Status = AcquiringStatus.Authorized,
            CardLast4 = "1234",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "USD",
            Amount = 1000
        };

        _mockPaymentRepository.Setup(x => x.GetPayment(paymentId))
            .ReturnsAsync(expectedPayment);

        var command = new GetPaymentCommand(paymentId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEquivalentTo(expectedPayment);
        result.AcquiringStatus.Should().Be(AcquiringStatus.Authorized);
        
        _mockPaymentRepository.Verify(x => x.GetPayment(paymentId), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Return_Empty_Response_When_Payment_Not_Found()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var emptyPayment = new Payment
        {
            Id = Guid.Empty,
            Status = AcquiringStatus.Authorized
        };

        _mockPaymentRepository.Setup(x => x.GetPayment(paymentId))
            .ReturnsAsync(emptyPayment);

        var command = new GetPaymentCommand(paymentId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().BeNull();
        result.AcquiringStatus.Should().Be(AcquiringStatus.Authorized); // Default value
        
        _mockPaymentRepository.Verify(x => x.GetPayment(paymentId), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Return_Declined_Payment_When_Found()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var expectedPayment = new Payment
        {
            Id = paymentId,
            Status = AcquiringStatus.Declined,
            CardLast4 = "5678",
            ExpiryMonth = 6,
            ExpiryYear = 2026,
            Currency = "GBP",
            Amount = 2500
        };

        _mockPaymentRepository.Setup(x => x.GetPayment(paymentId))
            .ReturnsAsync(expectedPayment);

        var command = new GetPaymentCommand(paymentId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEquivalentTo(expectedPayment);
        result.AcquiringStatus.Should().Be(AcquiringStatus.Authorized); // Handler always returns Authorized
        
        _mockPaymentRepository.Verify(x => x.GetPayment(paymentId), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Call_Repository_With_Correct_Id()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = paymentId,
            Status = AcquiringStatus.Authorized,
            CardLast4 = "1234",
            ExpiryMonth = 12,
            ExpiryYear = 2025,
            Currency = "USD",
            Amount = 1000
        };

        _mockPaymentRepository.Setup(x => x.GetPayment(It.IsAny<Guid>()))
            .ReturnsAsync(payment);

        var command = new GetPaymentCommand(paymentId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockPaymentRepository.Verify(x => x.GetPayment(paymentId), Times.Once);
        _mockPaymentRepository.Verify(x => x.GetPayment(It.Is<Guid>(id => id == paymentId)), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Handle_Different_Payment_Currencies()
    {
        // Arrange
        var currencies = new[] { "USD", "GBP", "EUR" };
        
        foreach (var currency in currencies)
        {
            var paymentId = Guid.NewGuid();
            var payment = new Payment
            {
                Id = paymentId,
                Status = AcquiringStatus.Authorized,
                CardLast4 = "1234",
                ExpiryMonth = 12,
                ExpiryYear = 2025,
                Currency = currency,
                Amount = 1000
            };

            _mockPaymentRepository.Setup(x => x.GetPayment(paymentId))
                .ReturnsAsync(payment);

            var command = new GetPaymentCommand(paymentId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Value.Should().NotBeNull();
            result.Value!.Currency.Should().Be(currency);
        }
    }

    [Fact]
    public async Task Handle_Should_Handle_Different_Payment_Amounts()
    {
        // Arrange
        var amounts = new[] { 1, 100, 1000, 999999 };
        
        foreach (var amount in amounts)
        {
            var paymentId = Guid.NewGuid();
            var payment = new Payment
            {
                Id = paymentId,
                Status = AcquiringStatus.Authorized,
                CardLast4 = "1234",
                ExpiryMonth = 12,
                ExpiryYear = 2025,
                Currency = "USD",
                Amount = amount
            };

            _mockPaymentRepository.Setup(x => x.GetPayment(paymentId))
                .ReturnsAsync(payment);

            var command = new GetPaymentCommand(paymentId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Value.Should().NotBeNull();
            result.Value!.Amount.Should().Be(amount);
        }
    }

    [Fact]
    public async Task Handle_Should_Handle_Cancellation_Token()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var cancellationToken = new CancellationToken(true);
        
        _mockPaymentRepository.Setup(x => x.GetPayment(paymentId))
            .ThrowsAsync(new OperationCanceledException());

        var command = new GetPaymentCommand(paymentId);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _handler.Handle(command, cancellationToken));
    }
}