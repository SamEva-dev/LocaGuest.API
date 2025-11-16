using LocaGuest.Application.Common;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Organizations.Queries.GetOrganizationById;

public class GetOrganizationByIdQueryHandler : IRequestHandler<GetOrganizationByIdQuery, Result<OrganizationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetOrganizationByIdQueryHandler> _logger;

    public GetOrganizationByIdQueryHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetOrganizationByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<OrganizationDto>> Handle(
        GetOrganizationByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var organization = await _unitOfWork.Organizations.GetByIdAsync(request.OrganizationId, cancellationToken);

            if (organization == null)
            {
                return Result.Failure<OrganizationDto>($"Organization with ID '{request.OrganizationId}' not found");
            }

            var dto = new OrganizationDto
            {
                Id = organization.Id,
                Number = organization.Number,
                Code = organization.Code,
                Name = organization.Name,
                Email = organization.Email,
                Phone = organization.Phone,
                Status = organization.Status.ToString(),
                SubscriptionPlan = organization.SubscriptionPlan,
                SubscriptionExpiryDate = organization.SubscriptionExpiryDate
            };

            return Result<OrganizationDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organization by ID: {OrganizationId}", request.OrganizationId);
            return Result.Failure<OrganizationDto>($"Failed to get organization: {ex.Message}");
        }
    }
}
