using LocaGuest.Application.Common;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Organizations.Commands.DeleteOrganization;

public class DeleteOrganizationCommandHandler : IRequestHandler<DeleteOrganizationCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteOrganizationCommandHandler> _logger;

    public DeleteOrganizationCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteOrganizationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(
        DeleteOrganizationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var organization = await _unitOfWork.Organizations.GetByIdAsync(request.OrganizationId, cancellationToken);

            if (organization == null)
            {
                return Result.Failure($"Organization with ID '{request.OrganizationId}' not found");
            }

            // Soft delete - mark as inactive
            organization.Deactivate();
            _unitOfWork.Organizations.Update(organization);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Organization deleted (deactivated): {Code} - {Name}",
                organization.Code, organization.Name);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting organization: {OrganizationId}", request.OrganizationId);
            return Result.Failure($"Failed to delete organization: {ex.Message}");
        }
    }
}
