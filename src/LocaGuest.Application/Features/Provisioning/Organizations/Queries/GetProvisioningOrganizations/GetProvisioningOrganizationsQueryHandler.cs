using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Provisioning.Organizations.Queries.GetProvisioningOrganizations;

public sealed class GetProvisioningOrganizationsQueryHandler : IRequestHandler<GetProvisioningOrganizationsQuery, Result<List<ProvisioningOrganizationDto>>>
{
    private readonly ILocaGuestDbContext _db;
    private readonly ILogger<GetProvisioningOrganizationsQueryHandler> _logger;

    public GetProvisioningOrganizationsQueryHandler(
        ILocaGuestDbContext db,
        ILogger<GetProvisioningOrganizationsQueryHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<List<ProvisioningOrganizationDto>>> Handle(
        GetProvisioningOrganizationsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var organizations = await _db.Organizations
                .AsNoTracking()
                .OrderBy(x => x.Number)
                .Select(x => new ProvisioningOrganizationDto
                {
                    Id = x.Id,
                    Number = x.Number,
                    Code = x.Code,
                    Name = x.Name,
                    Email = x.Email,
                    Phone = x.Phone,
                    Status = x.Status.ToString()
                })
                .ToListAsync(cancellationToken);

            return Result.Success(organizations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get provisioning organizations");
            return Result.Failure<List<ProvisioningOrganizationDto>>($"Failed to get organizations: {ex.Message}");
        }
    }
}
