using FluentAssertions;
using Moq;
using PaymentGateway.Application;
using PaymentGateway.Application.Commands.CreatePayment;
using PaymentGateway.Application.Commands.Rules;
using PaymentGateway.Application.Interfaces;
using PaymentGateway.Domain.Enums;

namespace PaymentGateway.Api.Tests.CreatePayment;

public class CreatePaymentHandlerTests
{
    private readonly Mock<ICurrencyService> _mockCurrencyService;
    private readonly Mock<IPaymentRepository> _mockPaymentRepository;
    private readonly Mock<IAcquiringBankHttpClient> _mockAcquiringBankHttpClient;
    private readonly CreatePaymentHandler _handler;
    private readonly IsSupported _isSupported;

    public CreatePaymentHandlerTests()
    {
        _mockCurrencyService = new Mock<ICurrencyService>();
        _mockPaymentRepository = new Mock<IPaymentRepository>();
        _mockAcquiringBankHttpClient = new Mock<IAcquiringBankHttpClient>();
        
        _isSupported = new IsSupported(_mockCurrencyService.Object);

        _handler = new CreatePaymentHandler(
            _isSupported,
            _mockPaymentRepository.Object,
            _mockAcquiringBankHttpClient.Object);
    }

    [Fact]
    public async Task Handle_Should_Throw_InvalidOperationException_When_Currency_Not_Supported()
    {
        // Arrange
        var command = CreateValidCommand();
        _mockCurrencyService.Setup(x => x.IsSupported(It.IsAny<string>())).ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
        
        exception.Message.Should().Be($"Unsupported currency: {command.Currency}");
    }

    [Fact]
    public async Task Handle_Should_Return_Declined_When_Acquiring_Bank_Declines()
    {
        // Arrange
        var command = CreateValidCommand();
        _mockCurrencyService.Setup(x => x.IsSupported(command.Currency))
            .ReturnsAsync(true);
        _mockAcquiringBankHttpClient.Setup(x => x.AuthorizeAsync(It.IsAny<AcquiringRequest>()))
            .ReturnsAsync(AcquiringStatus.Declined);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be(Guid.Empty);
        result.AcquiringStatus.Should().Be(AcquiringStatus.Declined);

        _mockPaymentRepository.Verify(x => x.SaveAsync(It.IsAny<CreatePaymentCommand>(), It.IsAny<AcquiringStatus>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Return_Authorized_Payment_When_Acquiring_Bank_Authorizes()
    {
        // Arrange
        var command = CreateValidCommand();
        var expectedPaymentId = Guid.NewGuid();
        
        _mockCurrencyService.Setup(x => x.IsSupported(command.Currency))
            .ReturnsAsync(true);
        _mockAcquiringBankHttpClient.Setup(x => x.AuthorizeAsync(It.IsAny<AcquiringRequest>()))
            .ReturnsAsync(AcquiringStatus.Authorized);
        _mockPaymentRepository.Setup(x => x.SaveAsync(command, AcquiringStatus.Authorized))
            .ReturnsAsync(expectedPaymentId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().Be(expectedPaymentId);
        result.AcquiringStatus.Should().Be(AcquiringStatus.Authorized);
        
        _mockPaymentRepository.Verify(x => x.SaveAsync(command, AcquiringStatus.Authorized), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Pass_Correct_AcquiringRequest_To_Bank()
    {
        // Arrange
        var command = CreateValidCommand();
        AcquiringRequest? capturedRequest = null;
        
        _mockCurrencyService.Setup(x => x.IsSupported(command.Currency))
            .ReturnsAsync(true);
        _mockAcquiringBankHttpClient.Setup(x => x.AuthorizeAsync(It.IsAny<AcquiringRequest>()))
            .Callback<AcquiringRequest>(req => capturedRequest = req)
            .ReturnsAsync(AcquiringStatus.Authorized);
        _mockPaymentRepository.Setup(x => x.SaveAsync(It.IsAny<CreatePaymentCommand>(), It.IsAny<AcquiringStatus>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.CardNumber.Should().Be(command.NormalizedCardNumber);
        capturedRequest.ExpiryMonth.Should().Be(command.ExpiryMonth);
        capturedRequest.ExpiryYear.Should().Be(command.ExpiryYear);
        capturedRequest.Currency.Should().Be(command.Currency);
        capturedRequest.Amount.Should().Be(command.Amount);
        capturedRequest.Cvv.Should().Be(command.Cvv);
    }

    [Theory]
    [InlineData("1234 5678 9012 3456", "1234567890123456")]
    [InlineData("1234-5678-9012-3456", "1234567890123456")]
    [InlineData("1234567890123456", "1234567890123456")]
    public async Task Handle_Should_Normalize_Card_Number_Correctly(string inputCardNumber, string expectedNormalized)
    {
        // Arrange
        var command = CreateValidCommand();
        command = command with { CardNumber = inputCardNumber };
        AcquiringRequest? capturedRequest = null;
        
        _mockCurrencyService.Setup(x => x.IsSupported(command.Currency))
            .ReturnsAsync(true);
        _mockAcquiringBankHttpClient.Setup(x => x.AuthorizeAsync(It.IsAny<AcquiringRequest>()))
            .Callback<AcquiringRequest>(req => capturedRequest = req)
            .ReturnsAsync(AcquiringStatus.Authorized);
        _mockPaymentRepository.Setup(x => x.SaveAsync(It.IsAny<CreatePaymentCommand>(), It.IsAny<AcquiringStatus>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.CardNumber.Should().Be(expectedNormalized);
    }

    [Fact]
    public async Task Handle_Should_Call_IsSupported_With_Correct_Currency()
    {
        // Arrange
        var command = CreateValidCommand();
        _mockCurrencyService.Setup(x => x.IsSupported(command.Currency))
            .ReturnsAsync(true);
        _mockAcquiringBankHttpClient.Setup(x => x.AuthorizeAsync(It.IsAny<AcquiringRequest>()))
            .ReturnsAsync(AcquiringStatus.Authorized);
        _mockPaymentRepository.Setup(x => x.SaveAsync(It.IsAny<CreatePaymentCommand>(), It.IsAny<AcquiringStatus>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockCurrencyService.Verify(x => x.IsSupported(command.Currency), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Handle_Cancellation_Token()
    {
        // Arrange
        var command = CreateValidCommand();
        var cancellationToken = new CancellationToken(true);
        
        _mockCurrencyService.Setup(x => x.IsSupported(command.Currency))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _handler.Handle(command, cancellationToken));
    }

    private static CreatePaymentCommand CreateValidCommand()
    {
        return new CreatePaymentCommand
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "USD",
            Amount = 1000,
            Cvv = "123"
        };
    }
}