using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Provisioning.Organizations.Commands.ProvisionOrganization;

public sealed class ProvisionOrganizationCommandHandler : IRequestHandler<ProvisionOrganizationCommand, Result<ProvisionOrganizationResponseDto>>
{
    private readonly IProvisioningService _provisioning;
    private readonly ILogger<ProvisionOrganizationCommandHandler> _logger;

    public ProvisionOrganizationCommandHandler(
        IProvisioningService provisioning,
        ILogger<ProvisionOrganizationCommandHandler> logger)
    {
        _provisioning = provisioning;
        _logger = logger;
    }

    public async Task<Result<ProvisionOrganizationResponseDto>> Handle(
        ProvisionOrganizationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _provisioning.ProvisionOrganizationAsync(
                new ProvisionOrganizationRequest(
                    OrganizationName: request.OrganizationName,
                    OrganizationEmail: request.OrganizationEmail,
                    OrganizationPhone: request.OrganizationPhone,
                    OwnerUserId: request.OwnerUserId,
                    OwnerEmail: request.OwnerEmail),
                request.IdempotencyKey,
                cancellationToken);

            return Result.Success(new ProvisionOrganizationResponseDto(
                OrganizationId: result.OrganizationId,
                Number: result.Number,
                Code: result.Code,
                Name: result.Name,
                Email: result.Email));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Provisioning organization failed");
            return Result.Failure<ProvisionOrganizationResponseDto>($"Failed to provision organization: {ex.Message}");
        }
    }
}
