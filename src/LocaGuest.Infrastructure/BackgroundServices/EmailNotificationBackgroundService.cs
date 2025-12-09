using LocaGuest.Application.Common.Interfaces;
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
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        _logger.LogInformation("Checking for notifications to send...");

        // Check payment reminders (3 days before due date)
        await CheckPaymentRemindersAsync(unitOfWork, emailService, cancellationToken);

        // Check overdue payments
        await CheckOverduePaymentsAsync(unitOfWork, emailService, cancellationToken);

        // Check expiring contracts (30 days before)
        await CheckExpiringContractsAsync(unitOfWork, emailService, cancellationToken);

        _logger.LogInformation("Notification check completed");
    }

    private async Task CheckPaymentRemindersAsync(IUnitOfWork unitOfWork, IEmailService emailService, CancellationToken cancellationToken)
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var reminderDate = today.AddDays(3);

            // Get payments due in 3 days that are pending
            var upcomingPayments = await unitOfWork.Payments.Query()
                .Where(p => p.Status == Domain.Aggregates.PaymentAggregate.PaymentStatus.Pending &&
                           p.ExpectedDate.Date == reminderDate)
                .ToListAsync(cancellationToken);

            foreach (var payment in upcomingPayments)
            {
                // Load tenant separately
                var tenant = await unitOfWork.Tenants.GetByIdAsync(payment.TenantId, cancellationToken);
                if (tenant?.Email == null)
                    continue;

                // Check if tenant has PaymentReminder notification enabled
                var notificationSettings = await unitOfWork.NotificationSettings
                    .GetByUserIdAsync(tenant.Id.ToString(), cancellationToken);

                if (notificationSettings?.PaymentReminder == true)
                {
                    await emailService.SendPaymentReminderEmailAsync(
                        tenant.Email,
                        tenant.FullName,
                        payment.AmountDue,
                        payment.ExpectedDate,
                        3);

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

    private async Task CheckOverduePaymentsAsync(IUnitOfWork unitOfWork, IEmailService emailService, CancellationToken cancellationToken)
    {
        try
        {
            var today = DateTime.UtcNow.Date;

            // Get overdue payments (due date < today and status = Pending)
            var overduePayments = await unitOfWork.Payments.Query()
                .Where(p => p.Status == Domain.Aggregates.PaymentAggregate.PaymentStatus.Pending &&
                           p.ExpectedDate.Date < today)
                .ToListAsync(cancellationToken);

            foreach (var payment in overduePayments)
            {
                // Load tenant separately
                var tenant = await unitOfWork.Tenants.GetByIdAsync(payment.TenantId, cancellationToken);
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
                        await emailService.SendPaymentOverdueEmailAsync(
                            tenant.Email,
                            tenant.FullName,
                            payment.AmountDue,
                            daysLate,
                            payment.ExpectedDate);

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

    private async Task CheckExpiringContractsAsync(IUnitOfWork unitOfWork, IEmailService emailService, CancellationToken cancellationToken)
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var expiryCheckDate = today.AddDays(30);

            // Get contracts expiring in 30 days
            var expiringContracts = await unitOfWork.Contracts.Query()
                .Where(c => c.Status == ContractStatus.Active &&
                           c.EndDate.Date == expiryCheckDate)
                .ToListAsync(cancellationToken);

            foreach (var contract in expiringContracts)
            {
                // Load tenant and property separately
                var tenant = await unitOfWork.Tenants.GetByIdAsync(contract.RenterTenantId, cancellationToken);
                var property = await unitOfWork.Properties.GetByIdAsync(contract.PropertyId, cancellationToken);
                
                if (tenant?.Email == null || property == null)
                    continue;

                // Check if tenant has ContractExpiring notification enabled
                var notificationSettings = await unitOfWork.NotificationSettings
                    .GetByUserIdAsync(tenant.Id.ToString(), cancellationToken);

                if (notificationSettings?.ContractExpiring == true)
                {
                    await emailService.SendContractExpiringEmailAsync(
                        tenant.Email,
                        tenant.FullName,
                        property.Address ?? "",
                        contract.EndDate,
                        30);

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
