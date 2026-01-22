using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using LocaGuest.Infrastructure.Webhooks.Brevo;

namespace LocaGuest.Api.Controllers;

[AllowAnonymous]
[ApiController]
[Route("api/webhooks/brevo")]
public class BrevoWebhookController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IBrevoWebhookQueue _queue;
    private readonly ILogger<BrevoWebhookController> _logger;

    public BrevoWebhookController(IConfiguration configuration, IBrevoWebhookQueue queue, ILogger<BrevoWebhookController> logger)
    {
        _configuration = configuration;
        _queue = queue;
        _logger = logger;
    }

    [HttpPost("transactional")]
    public async Task<IActionResult> Transactional(CancellationToken cancellationToken)
    {
        var expected = _configuration["Brevo:WebhookToken"]
                       ?? _configuration["BREVO_WEBHOOK_TOKEN"];

        if (string.IsNullOrWhiteSpace(expected))
            return Problem("BREVO_WEBHOOK_TOKEN is missing");

        var auth = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(auth) || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Unauthorized();

        var received = auth["Bearer ".Length..].Trim();
        if (!FixedTimeEquals(received, expected))
            return Unauthorized();

        // Read payload as fast as possible and enqueue for async processing.
        string body;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
            body = await reader.ReadToEndAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(body))
            return Ok("ok");

        if (!_queue.TryEnqueue(body))
        {
            // Still return 200 (Brevo retries on non-200 / timeout). We log so we can monitor.
            _logger.LogWarning("Brevo webhook queue is full; payload dropped");
        }

        return Ok("ok");
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        return ba.Length == bb.Length && CryptographicOperations.FixedTimeEquals(ba, bb);
    }
}
