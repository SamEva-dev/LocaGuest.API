using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Users;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.UserAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Users.Queries.GetNotifications;

public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, Result<NotificationSettingsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetNotificationsQueryHandler> _logger;

    public GetNotificationsQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<GetNotificationsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<NotificationSettingsDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
                return Result.Failure<NotificationSettingsDto>("User not authenticated");

            var userId = _currentUserService.UserId.Value.ToString();
            var settings = await _unitOfWork.NotificationSettings.GetByUserIdAsync(userId, cancellationToken);

            // Si pas de settings, créer des valeurs par défaut
            if (settings == null)
            {
                settings = NotificationSettings.CreateDefault(userId);
                await _unitOfWork.NotificationSettings.AddAsync(settings, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);
            }

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
            _logger.LogError(ex, "Error retrieving notification settings");
            return Result.Failure<NotificationSettingsDto>("Error retrieving notifications");
        }
    }
}
