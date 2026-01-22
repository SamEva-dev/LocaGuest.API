using System.Threading.Channels;

namespace LocaGuest.Infrastructure.Webhooks.Brevo;

public sealed class BrevoWebhookQueue : IBrevoWebhookQueue
{
    private readonly Channel<string> _channel;

    public BrevoWebhookQueue(int capacity = 1000)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropWrite
        };

        _channel = Channel.CreateBounded<string>(options);
    }

    public bool TryEnqueue(string payload)
    {
        return _channel.Writer.TryWrite(payload);
    }

    public ValueTask<string> DequeueAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}
