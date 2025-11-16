using LocaGuest.Application.Common;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Organizations.Commands.HardDeleteOrganization;

public class HardDeleteOrganizationCommandHandler : IRequestHandler<HardDeleteOrganizationCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<HardDeleteOrganizationCommandHandler> _logger;

    public HardDeleteOrganizationCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<HardDeleteOrganizationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(
        HardDeleteOrganizationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var organization = await _unitOfWork.Organizations.GetByIdAsync(request.OrganizationId, cancellationToken);

            if (organization == null)
            {
                return Result.Failure($"Organization with ID '{request.OrganizationId}' not found");
            }

            // ✅ Hard delete - permanently remove from database
            _unitOfWork.Organizations.Delete(organization);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogWarning(
                "Organization PERMANENTLY deleted: {Code} - {Name} (ID: {Id})",
                organization.Code, organization.Name, organization.Id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error permanently deleting organization: {OrganizationId}", request.OrganizationId);
            return Result.Failure($"Failed to permanently delete organization: {ex.Message}");
        }
    }
}
