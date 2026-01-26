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
                .OrderBy(x => EF.Property<string?>(x, "Name") ?? string.Empty)
                .ThenBy(x => x.Id)
                .Select(x => new ProvisioningOrganizationDto
                {
                    Id = x.Id,
                    Number = EF.Property<int?>(x, "Number") ?? 0,
                    Code = EF.Property<string?>(x, "Code") ?? string.Empty,
                    Name = EF.Property<string?>(x, "Name") ?? string.Empty,
                    Email = EF.Property<string?>(x, "Email") ?? string.Empty,
                    Phone = EF.Property<string?>(x, "Phone"),
                    Status = (EF.Property<int?>(x, "Status") ?? 0) == 0
                        ? "Active"
                        : (EF.Property<int?>(x, "Status") ?? 0) == 1
                            ? "Suspended"
                            : "Inactive"
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
