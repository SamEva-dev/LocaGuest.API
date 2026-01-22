namespace LocaGuest.Infrastructure.Webhooks.Brevo;

public interface IBrevoWebhookQueue
{
    bool TryEnqueue(string payload);
    ValueTask<string> DequeueAsync(CancellationToken cancellationToken);
}
