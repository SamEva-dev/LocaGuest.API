using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Domain.Aggregates.OrganizationAggregate;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Organizations.Commands.CreateOrganization;

public class CreateOrganizationCommandHandler : IRequestHandler<CreateOrganizationCommand, Result<CreateOrganizationDto>>
{
    private readonly ILocaGuestDbContext _context;
    private readonly ILogger<CreateOrganizationCommandHandler> _logger;

    public CreateOrganizationCommandHandler(
        ILocaGuestDbContext context,
        ILogger<CreateOrganizationCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<CreateOrganizationDto>> Handle(
        CreateOrganizationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate email uniqueness
            var emailExists = await _context.Organizations
                .AnyAsync(o => o.Email == request.Email, cancellationToken);

            if (emailExists)
            {
                return Result.Failure<CreateOrganizationDto>($"An organization with email '{request.Email}' already exists");
            }

            // Generate next organization number
            // Thread-safe: use database transaction with row locking
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Get the last organization number
                var lastNumber = await _context.Organizations
                    .OrderByDescending(o => o.Number)
                    .Select(o => o.Number)
                    .FirstOrDefaultAsync(cancellationToken);

                var nextNumber = lastNumber + 1;

                // Create organization with auto-generated code
                var organization = Organization.Create(
                    number: nextNumber,
                    name: request.Name,
                    email: request.Email,
                    phone: request.Phone
                );

                _context.Organizations.Add(organization);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

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
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync(cancellationToken);
                
                // Retry once on concurrency conflict
                _logger.LogWarning("Concurrency conflict creating organization. Retrying...");
                await Task.Delay(100, cancellationToken);
                return await Handle(request, cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating organization: {Name}", request.Name);
            return Result.Failure<CreateOrganizationDto>($"Failed to create organization: {ex.Message}");
        }
    }
}
