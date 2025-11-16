using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.OrganizationAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Organizations.Commands.CreateOrganization;

public class CreateOrganizationCommandHandler : IRequestHandler<CreateOrganizationCommand, Result<CreateOrganizationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateOrganizationCommandHandler> _logger;

    public CreateOrganizationCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<CreateOrganizationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<CreateOrganizationDto>> Handle(
        CreateOrganizationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate email uniqueness
            var emailExists = await _unitOfWork.Organizations.EmailExistsAsync(request.Email, cancellationToken);

            if (emailExists)
            {
                return Result.Failure<CreateOrganizationDto>($"An organization with email '{request.Email}' already exists");
            }

            // Get the last organization number (thread-safe with repository)
            var lastNumber = await _unitOfWork.Organizations.GetLastNumberAsync(cancellationToken);
            var nextNumber = lastNumber + 1;

            // Create organization with auto-generated code
            var organization = Organization.Create(
                number: nextNumber,
                name: request.Name,
                email: request.Email,
                phone: request.Phone
            );

            _unitOfWork.Organizations.Add(organization);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Organization created: {Code} - {Name}",
                organization.Code, organization.Name);

            var dto = new CreateOrganizationDto
            {
                OrganizationId = organization.Id,
                Code = organization.Code,
                Name = organization.Name,
                Email = organization.Email,
                Number = organization.Number
            };

            return Result<CreateOrganizationDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating organization: {Name}", request.Name);
            return Result.Failure<CreateOrganizationDto>($"Failed to create organization: {ex.Message}");
        }
    }
}
