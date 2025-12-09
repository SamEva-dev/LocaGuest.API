using LocaGuest.Application.Features.Inventories.Commands.SendInventoryEmail;

namespace LocaGuest.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task SendEmailAsync(string to, string subject, string body, List<EmailAttachment>? attachments = null, CancellationToken cancellationToken = default);
    
    // Payment notifications
    Task SendPaymentReceivedEmailAsync(string to, string tenantName, decimal amount, DateTime paymentDate);
    Task SendPaymentOverdueEmailAsync(string to, string tenantName, decimal amount, int daysLate, DateTime dueDate);
    Task SendPaymentReminderEmailAsync(string to, string tenantName, decimal amount, DateTime dueDate, int daysUntilDue);
    
    // Contract notifications
    Task SendContractExpiringEmailAsync(string to, string tenantName, string propertyAddress, DateTime endDate, int daysUntilExpiry);
    Task SendContractRenewalEmailAsync(string to, string tenantName, string propertyAddress, DateTime currentEndDate);
    
    // Maintenance notifications
    Task SendMaintenanceScheduledEmailAsync(string to, string propertyAddress, string description, DateTime scheduledDate);
    Task SendMaintenanceCompletedEmailAsync(string to, string propertyAddress, string description, DateTime completedDate);
    
    // Document notifications
    Task SendDocumentUploadedEmailAsync(string to, string documentName, string uploadedBy, DateTime uploadedDate);
    
    // Team notifications
    Task SendTeamInvitationEmailAsync(string toEmail, string invitationToken, string organizationName, string inviterName, string role, CancellationToken cancellationToken = default);
    Task SendTeamInvitationAcceptedEmailAsync(string toEmail, string memberName, string organizationName, CancellationToken cancellationToken = default);
    
    // User notifications
    Task SendWelcomeEmailAsync(string to, string firstName);
    Task SendPasswordChangedEmailAsync(string to, string firstName);
}
