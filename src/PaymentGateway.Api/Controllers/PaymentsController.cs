using MediatR;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Application.Commands.CreatePayment;
using PaymentGateway.Domain;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly ISender _sender;
    public PaymentsController(ISender sender)
    {
        _sender = sender;   
    }

    [HttpPost]
    public async Task<IActionResult> CreatePayment(CreatePaymentCommand createPaymentCommand)
    {
        var response = await _sender.Send(createPaymentCommand);

        return Ok(response);
    }
}