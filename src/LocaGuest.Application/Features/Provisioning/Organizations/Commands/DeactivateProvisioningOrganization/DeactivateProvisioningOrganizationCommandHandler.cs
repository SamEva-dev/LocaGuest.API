using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Provisioning.Organizations.Commands.DeactivateProvisioningOrganization;

public sealed class DeactivateProvisioningOrganizationCommandHandler : IRequestHandler<DeactivateProvisioningOrganizationCommand, Result>
{
    private readonly ILocaGuestDbContext _db;
    private readonly ILogger<DeactivateProvisioningOrganizationCommandHandler> _logger;

    public DeactivateProvisioningOrganizationCommandHandler(
        ILocaGuestDbContext db,
        ILogger<DeactivateProvisioningOrganizationCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result> Handle(
        DeactivateProvisioningOrganizationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var org = await _db.Organizations
                .FirstOrDefaultAsync(x => x.Id == request.OrganizationId, cancellationToken);

            if (org is null)
                return Result.Failure($"Organization with ID '{request.OrganizationId}' not found");

            org.Deactivate();
            await _db.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate organization {OrganizationId}", request.OrganizationId);
            return Result.Failure($"Failed to deactivate organization: {ex.Message}");
        }
    }
}
