using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Provisioning.Organizations.Queries.GetProvisioningOrganizationById;

public sealed class GetProvisioningOrganizationByIdQueryHandler : IRequestHandler<GetProvisioningOrganizationByIdQuery, Result<ProvisioningOrganizationDetailsDto>>
{
    private readonly ILocaGuestDbContext _db;
    private readonly ILogger<GetProvisioningOrganizationByIdQueryHandler> _logger;

    public GetProvisioningOrganizationByIdQueryHandler(
        ILocaGuestDbContext db,
        ILogger<GetProvisioningOrganizationByIdQueryHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<ProvisioningOrganizationDetailsDto>> Handle(
        GetProvisioningOrganizationByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var org = await _db.Organizations
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.OrganizationId, cancellationToken);

            if (org is null)
                return Result.Failure<ProvisioningOrganizationDetailsDto>($"Organization with ID '{request.OrganizationId}' not found");

            return Result.Success(new ProvisioningOrganizationDetailsDto
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
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get provisioning organization by id {OrganizationId}", request.OrganizationId);
            return Result.Failure<ProvisioningOrganizationDetailsDto>($"Failed to get organization: {ex.Message}");
        }
    }
}
