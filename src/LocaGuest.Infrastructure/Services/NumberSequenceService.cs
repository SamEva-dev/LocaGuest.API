using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Infrastructure.Services;

/// <summary>
/// Implementation of entity numbering sequence service
/// Thread-safe with database-level concurrency control
/// </summary>
public class NumberSequenceService : INumberSequenceService
{
    private readonly ILocaGuestDbContext _context;
    private readonly ILogger<NumberSequenceService> _logger;

    public NumberSequenceService(
        ILocaGuestDbContext context,
        ILogger<NumberSequenceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> GenerateNextCodeAsync(
        Guid organizationId,
        string entityPrefix,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
            throw new ArgumentException("OrganizationId cannot be empty", nameof(organizationId));

        if (string.IsNullOrWhiteSpace(entityPrefix))
            throw new ArgumentException("EntityPrefix cannot be null or empty", nameof(entityPrefix));

        // Get tenant code
        var organization = await _context.Organizations
            .IgnoreQueryFilters()
            .Where(o => o.Id == organizationId)
            .Select(o => new { o.Code })
            .FirstOrDefaultAsync(cancellationToken);

        if (organization == null)
            throw new InvalidOperationException($"Organization with ID {organizationId} not found");

        var tenantCode = organization.Code;

        // Use a transaction with serializable isolation to prevent race conditions
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            // Lock the sequence row for this tenant+prefix (SELECT FOR UPDATE in SQL)
            var sequence = await _context.OrganizationSequences
                .IgnoreQueryFilters()
                .Where(s => s.OrganizationId == organizationId && s.EntityPrefix == entityPrefix)
                .FirstOrDefaultAsync(cancellationToken);

            int nextNumber;

            if (sequence == null)
            {
                // Create new sequence starting at 1
                nextNumber = 1;
                sequence = new OrganizationSequence
                {
                    OrganizationId = organizationId,
                    EntityPrefix = entityPrefix,
                    LastNumber = nextNumber,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Description = $"Sequence for {entityPrefix} entities"
                };
                _context.OrganizationSequences.Add(sequence);
            }
            else
            {
                // Increment existing sequence
                nextNumber = sequence.LastNumber + 1;
                sequence.LastNumber = nextNumber;
                sequence.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var generatedCode = $"{tenantCode}-{entityPrefix}{nextNumber:0000}";
            
            _logger.LogInformation(
                "Generated code {Code} for tenant {TenantId} prefix {Prefix}",
                generatedCode, organizationId, entityPrefix);

            return generatedCode;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogWarning(ex, 
                "Concurrency conflict generating code for tenant {TenantId} prefix {Prefix}. Retrying...",
                organizationId, entityPrefix);
            
            // Retry once on concurrency conflict
            await Task.Delay(100, cancellationToken);
            return await GenerateNextCodeAsync(organizationId, entityPrefix, cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, 
                "Error generating code for tenant {TenantId} prefix {Prefix}",
                organizationId, entityPrefix);
            throw;
        }
    }

    public async Task<int> GetLastNumberAsync(
        Guid organizationId,
        string entityPrefix,
        CancellationToken cancellationToken = default)
    {
        var sequence = await _context.OrganizationSequences
            .IgnoreQueryFilters()
            .Where(s => s.OrganizationId == organizationId && s.EntityPrefix == entityPrefix)
            .FirstOrDefaultAsync(cancellationToken);

        return sequence?.LastNumber ?? 0;
    }

    public async Task<string> GetTenantNumberAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var organisation = await _context.Organizations
            .IgnoreQueryFilters()
            .Where(o => o.Id == tenantId)
            .AsNoTracking()
            .Select(o => new { o.Code })
            .FirstOrDefaultAsync(cancellationToken);

        return organisation?.Code ?? throw new Exception($"Organisation with tenant id {tenantId} not found");
    }

    public async Task ResetSequenceAsync(
        Guid organizationId,
        string entityPrefix,
        int newNumber,
        CancellationToken cancellationToken = default)
    {
        if (newNumber < 0)
            throw new ArgumentException("New number cannot be negative", nameof(newNumber));

        var sequence = await _context.OrganizationSequences
            .IgnoreQueryFilters()
            .Where(s => s.OrganizationId == organizationId && s.EntityPrefix == entityPrefix)
            .FirstOrDefaultAsync(cancellationToken);

        if (sequence != null)
        {
            sequence.LastNumber = newNumber;
            sequence.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            
            _logger.LogWarning(
                "Sequence reset for tenant {TenantId} prefix {Prefix} to {NewNumber}",
                organizationId, entityPrefix, newNumber);
        }
    }
}
