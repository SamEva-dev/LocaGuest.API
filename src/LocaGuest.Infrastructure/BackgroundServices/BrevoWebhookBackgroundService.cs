using LocaGuest.Application.Features.EmailDelivery.Commands.HandleBrevoWebhook;
using LocaGuest.Infrastructure.Webhooks.Brevo;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Infrastructure.BackgroundServices;

public sealed class BrevoWebhookBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IBrevoWebhookQueue _queue;
    private readonly ILogger<BrevoWebhookBackgroundService> _logger;

    public BrevoWebhookBackgroundService(
        IServiceProvider serviceProvider,
        IBrevoWebhookQueue queue,
        ILogger<BrevoWebhookBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Brevo Webhook Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var payload = await _queue.DequeueAsync(stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new HandleBrevoWebhookCommand { RawPayload = payload }, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing Brevo webhook payload");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }

        _logger.LogInformation("Brevo Webhook Background Service stopped");
    }
}
