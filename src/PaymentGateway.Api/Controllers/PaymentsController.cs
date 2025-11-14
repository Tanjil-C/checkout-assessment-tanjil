using MediatR;
using Microsoft.AspNetCore.Mvc;
using PaymentGateway.Application.Commands.CreatePayment;
using PaymentGateway.Application.Queries;

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
    public async Task<IActionResult> CreatePayment(CreatePaymentCommand command)
    {
        var response = await _sender.Send(command);
        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPayment(Guid id)
    {
        var command = new GetPaymentCommand(id);
        var response = await _sender.Send(command);
        return Ok(response);
    }
}