using LocaGuest.Application.Common;
using LocaGuest.Application.Features.Organizations.Queries.GetOrganizationById;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Organizations.Queries.GetActiveOrganizations;

public class GetActiveOrganizationsQueryHandler : IRequestHandler<GetActiveOrganizationsQuery, Result<List<OrganizationDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetActiveOrganizationsQueryHandler> _logger;

    public GetActiveOrganizationsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetActiveOrganizationsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<List<OrganizationDto>>> Handle(
        GetActiveOrganizationsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var organizations = await _unitOfWork.Organizations.GetAllAsync(cancellationToken);

            // ✅ Filter: only active organizations
            var dtos = organizations
                .Where(org => org.Status == Domain.Aggregates.OrganizationAggregate.OrganizationStatus.Active)
                .Select(org => new OrganizationDto
                {
                    Id = org.Id,
                    Number = org.Number,
                    Code = org.Code,
                    Name = org.Name,
                    Email = org.Email,
                    Phone = org.Phone,
                    Status = org.Status.ToString(),
                    SubscriptionPlan = org.SubscriptionPlan,
                    SubscriptionExpiryDate = org.SubscriptionExpiryDate
                }).ToList();

            _logger.LogInformation("Retrieved {Count} active organizations", dtos.Count);
            return Result<List<OrganizationDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active organizations");
            return Result.Failure<List<OrganizationDto>>($"Failed to get active organizations: {ex.Message}");
        }
    }
}
