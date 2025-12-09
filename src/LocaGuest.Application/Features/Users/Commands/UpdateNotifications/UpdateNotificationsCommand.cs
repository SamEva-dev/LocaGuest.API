using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Users;
using MediatR;

namespace LocaGuest.Application.Features.Users.Commands.UpdateNotifications;

public record UpdateNotificationsCommand : IRequest<Result<NotificationSettingsDto>>
{
    public bool PaymentReceived { get; init; }
    public bool PaymentOverdue { get; init; }
    public bool PaymentReminder { get; init; }
    public bool ContractSigned { get; init; }
    public bool ContractExpiring { get; init; }
    public bool ContractRenewal { get; init; }
    public bool NewTenantRequest { get; init; }
    public bool TenantCheckout { get; init; }
    public bool MaintenanceRequest { get; init; }
    public bool MaintenanceCompleted { get; init; }
    public bool SystemUpdates { get; init; }
    public bool MarketingEmails { get; init; }
}
