using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Users;
using LocaGuest.Domain.Aggregates.UserAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Users.Commands.UpdateNotifications;

public class UpdateNotificationsCommandHandler : IRequestHandler<UpdateNotificationsCommand, Result<NotificationSettingsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<UpdateNotificationsCommandHandler> _logger;

    public UpdateNotificationsCommandHandler(
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        ILogger<UpdateNotificationsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<Result<NotificationSettingsDto>> Handle(UpdateNotificationsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_tenantContext.IsAuthenticated || !_tenantContext.UserId.HasValue)
                return Result.Failure<NotificationSettingsDto>("User not authenticated");

            var userId = _tenantContext.UserId.Value.ToString();
            var settings = await _unitOfWork.NotificationSettings.GetByUserIdAsync(userId, cancellationToken);
            
            if (settings == null)
            {
                settings = NotificationSettings.CreateDefault(userId);
                await _unitOfWork.NotificationSettings.AddAsync(settings, cancellationToken);
            }

            settings.UpdateAll(
                request.PaymentReceived,
                request.PaymentOverdue,
                request.PaymentReminder,
                request.ContractSigned,
                request.ContractExpiring,
                request.ContractRenewal,
                request.NewTenantRequest,
                request.TenantCheckout,
                request.MaintenanceRequest,
                request.MaintenanceCompleted,
                request.SystemUpdates,
                request.MarketingEmails
            );
            await _unitOfWork.CommitAsync(cancellationToken);

            var dto = new NotificationSettingsDto
            {
                PaymentReceived = settings.PaymentReceived,
                PaymentOverdue = settings.PaymentOverdue,
                PaymentReminder = settings.PaymentReminder,
                ContractSigned = settings.ContractSigned,
                ContractExpiring = settings.ContractExpiring,
                ContractRenewal = settings.ContractRenewal,
                NewTenantRequest = settings.NewTenantRequest,
                TenantCheckout = settings.TenantCheckout,
                MaintenanceRequest = settings.MaintenanceRequest,
                MaintenanceCompleted = settings.MaintenanceCompleted,
                SystemUpdates = settings.SystemUpdates,
                MarketingEmails = settings.MarketingEmails
            };

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification settings");
            return Result.Failure<NotificationSettingsDto>("Error updating notifications");
        }
    }
}
