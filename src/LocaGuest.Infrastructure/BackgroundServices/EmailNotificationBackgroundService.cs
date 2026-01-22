using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Emailing.Abstractions;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Infrastructure.BackgroundServices;

public class EmailNotificationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailNotificationBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24); // Check once per day

    public EmailNotificationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<EmailNotificationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email Notification Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndSendNotificationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Email Notification Background Service");
            }

            // Wait for next check
            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Email Notification Background Service stopped");
    }

    private async Task CheckAndSendNotificationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var emailing = scope.ServiceProvider.GetRequiredService<IEmailingService>();

        _logger.LogInformation("Checking for notifications to send...");

        // Check payment reminders (3 days before due date)
        await CheckPaymentRemindersAsync(unitOfWork, emailing, cancellationToken);

        // Check overdue payments
        await CheckOverduePaymentsAsync(unitOfWork, emailing, cancellationToken);

        // Check expiring contracts (30 days before)
        await CheckExpiringContractsAsync(unitOfWork, emailing, cancellationToken);

        _logger.LogInformation("Notification check completed");
    }

    private async Task CheckPaymentRemindersAsync(IUnitOfWork unitOfWork, IEmailingService emailing, CancellationToken cancellationToken)
    {
        try
        {
            var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            var reminderDate = today.AddDays(3);

            // Get payments due in 3 days that are pending
            var upcomingPayments = await unitOfWork.Payments.Query()
                .Where(p => p.Status == Domain.Aggregates.PaymentAggregate.PaymentStatus.Pending &&
                           p.ExpectedDate.Date == reminderDate)
                .ToListAsync(cancellationToken);

            foreach (var payment in upcomingPayments)
            {
                // Load tenant separately
                var tenant = await unitOfWork.Occupants.GetByIdAsync(payment.RenterOccupantId, cancellationToken);
                if (tenant?.Email == null)
                    continue;

                // Check if tenant has PaymentReminder notification enabled
                var notificationSettings = await unitOfWork.NotificationSettings
                    .GetByUserIdAsync(tenant.Id.ToString(), cancellationToken);

                if (notificationSettings?.PaymentReminder == true)
                {
                    var subject = "🔔 Rappel de paiement - LocaGuest";
                    var htmlBody = $@"<h2>Rappel de paiement 🔔</h2>
<p>Bonjour {tenant.FullName},</p>
<p>Un paiement arrive à échéance dans <strong>3 jour(s)</strong>.</p>
<p><strong>Montant :</strong> {payment.AmountDue:C}</p>
<p><strong>Date d'échéance :</strong> {payment.ExpectedDate:dd/MM/yyyy}</p>
<p>Cordialement,<br/>L'équipe LocaGuest</p>";

                    await emailing.QueueHtmlAsync(
                        toEmail: tenant.Email,
                        subject: subject,
                        htmlContent: htmlBody,
                        textContent: null,
                        attachments: null,
                        tags: EmailUseCaseTags.BillingPaymentReminder,
                        cancellationToken: cancellationToken);

                    _logger.LogInformation("Payment reminder sent to {Email} for payment {PaymentId}",
                        tenant.Email, payment.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking payment reminders");
        }
    }

    private async Task CheckOverduePaymentsAsync(IUnitOfWork unitOfWork, IEmailingService emailing, CancellationToken cancellationToken)
    {
        try
        {
            var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

            // Get overdue payments (due date < today and status = Pending)
            var overduePayments = await unitOfWork.Payments.Query()
                .Where(p => p.Status == Domain.Aggregates.PaymentAggregate.PaymentStatus.Pending &&
                           p.ExpectedDate.Date < today)
                .ToListAsync(cancellationToken);

            foreach (var payment in overduePayments)
            {
                // Load tenant separately
                var tenant = await unitOfWork.Occupants.GetByIdAsync(payment.RenterOccupantId, cancellationToken);
                if (tenant?.Email == null)
                    continue;

                var daysLate = (today - payment.ExpectedDate.Date).Days;

                // Check if tenant has PaymentOverdue notification enabled
                var notificationSettings = await unitOfWork.NotificationSettings
                    .GetByUserIdAsync(tenant.Id.ToString(), cancellationToken);

                if (notificationSettings?.PaymentOverdue == true)
                {
                    // Only send if it's 1, 7, 14, or 30 days overdue (avoid spamming)
                    if (daysLate == 1 || daysLate == 7 || daysLate == 14 || daysLate == 30)
                    {
                        var subject = "⚠️ Paiement en retard - LocaGuest";
                        var htmlBody = $@"<h2>Paiement en retard ⚠️</h2>
<p>Bonjour {tenant.FullName},</p>
<p>Un paiement est en retard de <strong>{daysLate} jour(s)</strong>.</p>
<p><strong>Montant dû :</strong> {payment.AmountDue:C}</p>
<p><strong>Date d'échéance :</strong> {payment.ExpectedDate:dd/MM/yyyy}</p>
<p>Cordialement,<br/>L'équipe LocaGuest</p>";

                        await emailing.QueueHtmlAsync(
                            toEmail: tenant.Email,
                            subject: subject,
                            htmlContent: htmlBody,
                            textContent: null,
                            attachments: null,
                            tags: EmailUseCaseTags.BillingPaymentFailed,
                            cancellationToken: cancellationToken);

                        _logger.LogInformation("Overdue payment notification sent to {Email} for payment {PaymentId} ({Days} days late)",
                            tenant.Email, payment.Id, daysLate);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking overdue payments");
        }
    }

    private async Task CheckExpiringContractsAsync(IUnitOfWork unitOfWork, IEmailingService emailing, CancellationToken cancellationToken)
    {
        try
        {
            var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            var expiryCheckDate = today.AddDays(30);

            // Get contracts expiring in 30 days
            var expiringContracts = await unitOfWork.Contracts.Query()
                .Where(c => c.Status == ContractStatus.Active &&
                           c.EndDate.Date == expiryCheckDate)
                .ToListAsync(cancellationToken);

            foreach (var contract in expiringContracts)
            {
                // Load tenant and property separately
                var tenant = await unitOfWork.Occupants.GetByIdAsync(contract.RenterOccupantId, cancellationToken);
                var property = await unitOfWork.Properties.GetByIdAsync(contract.PropertyId, cancellationToken);
                
                if (tenant?.Email == null || property == null)
                    continue;

                // Check if tenant has ContractExpiring notification enabled
                var notificationSettings = await unitOfWork.NotificationSettings
                    .GetByUserIdAsync(tenant.Id.ToString(), cancellationToken);

                if (notificationSettings?.ContractExpiring == true)
                {
                    var subject = "📅 Votre bail arrive à échéance - LocaGuest";
                    var address = property.Address ?? string.Empty;
                    var htmlBody = $@"<h2>Votre bail arrive à échéance 📅</h2>
<p>Bonjour {tenant.FullName},</p>
<p>Votre bail pour le logement situé au <strong>{address}</strong> arrive à échéance dans <strong>30 jour(s)</strong>.</p>
<p><strong>Date de fin :</strong> {contract.EndDate:dd/MM/yyyy}</p>
<p>Cordialement,<br/>L'équipe LocaGuest</p>";

                    await emailing.QueueHtmlAsync(
                        toEmail: tenant.Email,
                        subject: subject,
                        htmlContent: htmlBody,
                        textContent: null,
                        attachments: null,
                        tags: EmailUseCaseTags.RentalContractUpdated,
                        cancellationToken: cancellationToken);

                    _logger.LogInformation("Contract expiring notification sent to {Email} for contract {ContractId}",
                        tenant.Email, contract.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking expiring contracts");
        }
    }
}
