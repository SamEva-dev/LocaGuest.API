using LocaGuest.Application.Common;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Organizations.Commands.UpdateOrganizationSettings;

public class UpdateOrganizationSettingsCommandHandler : IRequestHandler<UpdateOrganizationSettingsCommand, Result<OrganizationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateOrganizationSettingsCommandHandler> _logger;

    public UpdateOrganizationSettingsCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<UpdateOrganizationSettingsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<OrganizationDto>> Handle(
        UpdateOrganizationSettingsCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating organization settings for {OrganizationId}", request.OrganizationId);

            // Get organization
            var organization = await _unitOfWork.Organizations.GetByIdAsync(request.OrganizationId);
            if (organization == null)
            {
                _logger.LogWarning("Organization not found: {OrganizationId}", request.OrganizationId);
                return Result.Failure<OrganizationDto>("Organization not found");
            }

            // Update basic info if provided
            if (!string.IsNullOrWhiteSpace(request.Name) || 
                !string.IsNullOrWhiteSpace(request.Email) || 
                request.Phone != null)
            {
                organization.UpdateInfo(request.Name, request.Email, request.Phone);
            }

            // Update branding settings if any provided
            if (request.LogoUrl != null || 
                request.PrimaryColor != null || 
                request.SecondaryColor != null ||
                request.AccentColor != null ||
                request.Website != null)
            {
                organization.UpdateBrandingSettings(
                    request.LogoUrl,
                    request.PrimaryColor,
                    request.SecondaryColor,
                    request.AccentColor,
                    request.Website
                );
            }

            // Save changes
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Organization settings updated successfully for {OrganizationId}", request.OrganizationId);

            // Map to DTO
            var dto = new OrganizationDto
            {
                Id = organization.Id,
                Code = organization.Code,
                Name = organization.Name,
                Email = organization.Email,
                Phone = organization.Phone,
                Status = organization.Status.ToString(),
                SubscriptionPlan = organization.SubscriptionPlan,
                SubscriptionExpiryDate = organization.SubscriptionExpiryDate,
                LogoUrl = organization.LogoUrl,
                PrimaryColor = organization.PrimaryColor,
                SecondaryColor = organization.SecondaryColor,
                AccentColor = organization.AccentColor,
                Website = organization.Website
            };

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating organization settings for {OrganizationId}", request.OrganizationId);
            return Result.Failure<OrganizationDto>($"Error updating organization settings: {ex.Message}");
        }
    }
}
