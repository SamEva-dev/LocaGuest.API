using System.Text.Json;
using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.EmailDelivery.Commands.HandleBrevoWebhook;

public sealed class HandleBrevoWebhookCommandHandler : IRequestHandler<HandleBrevoWebhookCommand, Result<bool>>
{
    private readonly ILocaGuestDbContext _db;
    private readonly ILogger<HandleBrevoWebhookCommandHandler> _logger;

    public HandleBrevoWebhookCommandHandler(ILocaGuestDbContext db, ILogger<HandleBrevoWebhookCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(HandleBrevoWebhookCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RawPayload))
            return Result.Success(true);

        try
        {
            using var doc = JsonDocument.Parse(request.RawPayload);

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    await HandleSingleEventAsync(item, request.RawPayload, cancellationToken);
                }
            }
            else if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                await HandleSingleEventAsync(doc.RootElement, request.RawPayload, cancellationToken);
            }

            return Result.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Brevo webhook processing failed");
            return Result.Failure<bool>("Brevo webhook processing error");
        }
    }

    private async Task HandleSingleEventAsync(JsonElement obj, string rawPayload, CancellationToken cancellationToken)
    {
        var eventType = GetString(obj, "event");
        var email = GetString(obj, "email");
        var messageId = GetString(obj, "message-id");
        var tsEvent = GetLong(obj, "ts_event");

        if (string.IsNullOrWhiteSpace(eventType) || string.IsNullOrWhiteSpace(messageId) || tsEvent == null)
        {
            _logger.LogWarning("Brevo webhook event missing required fields (event/message-id/ts_event)");
            return;
        }

        // Idempotence (best effort without relying only on unique index)
        var exists = await _db.EmailDeliveryEvents
            .AnyAsync(x => x.MessageId == messageId && x.EventType == eventType && x.TsEvent == tsEvent.Value, cancellationToken);
        if (exists) return;

        var entity = new EmailDeliveryEvent
        {
            Id = Guid.NewGuid(),
            MessageId = messageId,
            Email = email ?? string.Empty,
            EventType = eventType,
            TsEvent = tsEvent.Value,
            RawPayload = rawPayload,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.EmailDeliveryEvents.Add(entity);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException dbEx)
        {
            // In case the unique constraint exists in DB and we have a race.
            _logger.LogInformation(dbEx, "Duplicate Brevo webhook event ignored (MessageId={MessageId}, EventType={EventType}, TsEvent={TsEvent})", messageId, eventType, tsEvent);
        }

        ApplySaasRules(eventType, email, messageId);
    }

    private void ApplySaasRules(string eventType, string? email, string messageId)
    {
        // NOTE: The domain currently doesn't store a dedicated 'email valid/invalid' flag.
        // We keep logic here as observability (logs) + storage of events.
        var normalized = eventType.Trim().ToLowerInvariant();

        switch (normalized)
        {
            case "hardbounce":
            case "invalid":
                _logger.LogWarning("Brevo email marked invalid/hardbounce for {Email} (messageId={MessageId})", email, messageId);
                break;
            case "spam":
                _logger.LogError("Brevo email spam complaint for {Email} (messageId={MessageId})", email, messageId);
                break;
            case "blocked":
            case "deferred":
                _logger.LogWarning("Brevo email blocked/deferred for {Email} (messageId={MessageId})", email, messageId);
                break;
            case "delivered":
                _logger.LogInformation("Brevo email delivered for {Email} (messageId={MessageId})", email, messageId);
                break;
            default:
                // opened/click/etc.
                _logger.LogDebug("Brevo email event {EventType} for {Email} (messageId={MessageId})", normalized, email, messageId);
                break;
        }
    }

    private static string? GetString(JsonElement obj, string propertyName)
    {
        if (!obj.TryGetProperty(propertyName, out var prop)) return null;
        return prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.ToString();
    }

    private static long? GetLong(JsonElement obj, string propertyName)
    {
        if (!obj.TryGetProperty(propertyName, out var prop)) return null;
        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out var v)) return v;
        if (prop.ValueKind == JsonValueKind.String && long.TryParse(prop.GetString(), out var vs)) return vs;
        return null;
    }
}
