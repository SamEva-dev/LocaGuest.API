using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.TenantAggregate;

namespace LocaGuest.Application.Interfaces;

public interface ITenantSheetGeneratorService
{
    Task<byte[]> GenerateTenantSheetPdfAsync(
        Tenant tenant,
        Property? associatedProperty,
        string currentUserFullName,
        string currentUserEmail,
        string? currentUserPhone,
        CancellationToken cancellationToken = default);
}
