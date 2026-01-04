using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Aggregates.OrganizationAggregate;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Provisioning.Organizations.Commands.UpdateProvisioningOrganization;

public sealed class UpdateProvisioningOrganizationCommandHandler : IRequestHandler<UpdateProvisioningOrganizationCommand, Result<UpdateProvisioningOrganizationDto>>
{
    private readonly ILocaGuestDbContext _db;
    private readonly ILogger<UpdateProvisioningOrganizationCommandHandler> _logger;

    public UpdateProvisioningOrganizationCommandHandler(
        ILocaGuestDbContext db,
        ILogger<UpdateProvisioningOrganizationCommandHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Result<UpdateProvisioningOrganizationDto>> Handle(
        UpdateProvisioningOrganizationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var org = await _db.Organizations
                .FirstOrDefaultAsync(x => x.Id == request.OrganizationId, cancellationToken);

            if (org is null)
                return Result.Failure<UpdateProvisioningOrganizationDto>($"Organization with ID '{request.OrganizationId}' not found");

            org.UpdateInfo(
                name: request.Name,
                email: request.Email,
                phone: request.Phone);

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (!Enum.TryParse<OrganizationStatus>(request.Status, ignoreCase: true, out var status))
                    return Result.Failure<UpdateProvisioningOrganizationDto>($"Invalid status '{request.Status}'");

                switch (status)
                {
                    case OrganizationStatus.Active:
                        org.Activate();
                        break;
                    case OrganizationStatus.Suspended:
                        org.Suspend();
                        break;
                    case OrganizationStatus.Inactive:
                        org.Deactivate();
                        break;
                    default:
                        return Result.Failure<UpdateProvisioningOrganizationDto>($"Invalid status '{request.Status}'");
                }
            }

            await _db.SaveChangesAsync(cancellationToken);

            return Result.Success(new UpdateProvisioningOrganizationDto
            {
                Id = org.Id,
                Name = org.Name,
                Email = org.Email,
                Phone = org.Phone,
                Status = org.Status.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update provisioning organization {OrganizationId}", request.OrganizationId);
            return Result.Failure<UpdateProvisioningOrganizationDto>($"Failed to update organization: {ex.Message}");
        }
    }
}
