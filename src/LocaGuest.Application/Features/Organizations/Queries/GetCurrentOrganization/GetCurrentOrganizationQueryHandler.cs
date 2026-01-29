using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Features.Organizations.Commands.UpdateOrganizationSettings;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Organizations.Queries.GetCurrentOrganization;

public class GetCurrentOrganizationQueryHandler : IRequestHandler<GetCurrentOrganizationQuery, Result<OrganizationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrganizationContext _orgContext;
    private readonly ILogger<GetCurrentOrganizationQueryHandler> _logger;

    public GetCurrentOrganizationQueryHandler(
        IUnitOfWork unitOfWork,
        IOrganizationContext orgContext,
        ILogger<GetCurrentOrganizationQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _orgContext = orgContext;
        _logger = logger;
    }

    public async Task<Result<OrganizationDto>> Handle(
        GetCurrentOrganizationQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting current organization for user {UserId}", request.UserId);

            if (!_orgContext.IsAuthenticated || !_orgContext.OrganizationId.HasValue)
            {
                return Result.Failure<OrganizationDto>("User not authenticated");
            }

            // Get user's organization from Users in AuthGate database
            // For now, we'll need to get the OccupantId from the JWT claims
            // and use it to fetch the organization
            
            // TODO: Get OccupantId from HttpContext.User claims
            // For this implementation, we'll query all organizations and return the first one
            // In production, this should be filtered by the user's OccupantId
            
            var organization = await _unitOfWork.Organizations.GetByIdAsync(_orgContext.OrganizationId.Value, cancellationToken, asNoTracking: true);

            if (organization == null)
            {
                _logger.LogWarning("No organization found");
                return Result.Failure<OrganizationDto>("No organization found");
            }

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
            _logger.LogError(ex, "Error getting current organization");
            return Result.Failure<OrganizationDto>($"Error getting organization: {ex.Message}");
        }
    }
}
