using LocaGuest.Application.Common;
using LocaGuest.Application.Features.Organizations.Queries.GetOrganizationById;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Organizations.Queries.GetAllOrganizations;

public class GetAllOrganizationsQueryHandler : IRequestHandler<GetAllOrganizationsQuery, Result<List<OrganizationDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetAllOrganizationsQueryHandler> _logger;

    public GetAllOrganizationsQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetAllOrganizationsQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<List<OrganizationDto>>> Handle(
        GetAllOrganizationsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var organizations = await _unitOfWork.Organizations.GetAllAsync(cancellationToken);

            var dtos = organizations.Select(org => new OrganizationDto
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

            _logger.LogInformation("Retrieved {Count} organizations", dtos.Count);
            return Result<List<OrganizationDto>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all organizations");
            return Result.Failure<List<OrganizationDto>>($"Failed to get organizations: {ex.Message}");
        }
    }
}
