using LocaGuest.Application.Features.Contact.Commands.SendContactMessage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaGuest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class ContactController : ControllerBase
{
    private readonly ILogger<ContactController> _logger;
    private readonly IMediator _mediator;

    public ContactController(ILogger<ContactController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    /// <summary>
    /// Send a contact message (public endpoint - no authentication required)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] SendContactMessageRequest request)
    {
        var command = new SendContactMessageCommand(
            request.Name,
            request.Email,
            request.Subject,
            request.Message
        );

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(new
        {
            success = true,
            messageId = result.Data.MessageId,
            message = "Your message has been received. We will respond shortly."
        });
    }
}

public record SendContactMessageRequest(
    string Name,
    string Email,
    string? Subject,
    string Message
);
