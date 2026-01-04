using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Provisioning.Organizations.Commands.HardDeleteProvisioningOrganization;

public sealed class HardDeleteProvisioningOrganizationCommandHandler : IRequestHandler<HardDeleteProvisioningOrganizationCommand, Result>
{
    private readonly ILocaGuestDbContext _db;
    private readonly ILogger<HardDeleteProvisioningOrganizationCommandHandler> _logger;

    public HardDeleteProvisioningOrganizationCommandHandler(
        ILocaGuestDbContext db,
        ILogger<HardDeleteProvisioningOrganizationCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result> Handle(
        HardDeleteProvisioningOrganizationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var org = await _db.Organizations
                .FirstOrDefaultAsync(x => x.Id == request.OrganizationId, cancellationToken);

            if (org is null)
                return Result.Failure($"Organization with ID '{request.OrganizationId}' not found");

            _db.Organizations.Remove(org);
            await _db.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to hard delete organization {OrganizationId}", request.OrganizationId);
            return Result.Failure($"Failed to hard delete organization: {ex.Message}");
        }
    }
}
