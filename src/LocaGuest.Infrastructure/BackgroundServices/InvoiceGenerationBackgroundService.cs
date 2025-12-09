using LocaGuest.Application.Features.Invoices.Commands.GenerateMonthlyInvoices;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that automatically generates monthly invoices
/// Runs on the 1st day of each month at midnight
/// </summary>
public class InvoiceGenerationBackgroundService : BackgroundService
{
    private readonly ILogger<InvoiceGenerationBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public InvoiceGenerationBackgroundService(
        ILogger<InvoiceGenerationBackgroundService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Invoice Generation Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                
                // Check if it's the 1st day of the month at midnight (or shortly after)
                if (now.Day == 1 && now.Hour == 0)
                {
                    await GenerateMonthlyInvoicesAsync(stoppingToken);
                }

                // Wait for 1 hour before checking again
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Invoice Generation Background Service");
                // Wait a bit before retrying
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task GenerateMonthlyInvoicesAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting automatic invoice generation");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var now = DateTime.UtcNow;
            // Generate invoices for the current month
            var command = new GenerateMonthlyInvoicesCommand(now.Month, now.Year);
            var result = await mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "Successfully generated {GeneratedCount} invoices, skipped {SkippedCount} for {Month}/{Year}",
                    result.Data!.GeneratedCount,
                    result.Data.SkippedCount,
                    now.Month,
                    now.Year);

                if (result.Data.Errors.Any())
                {
                    _logger.LogWarning("Errors during invoice generation: {Errors}",
                        string.Join(", ", result.Data.Errors));
                }
            }
            else
            {
                _logger.LogError("Failed to generate invoices: {Error}", result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating monthly invoices");
        }
    }
}
