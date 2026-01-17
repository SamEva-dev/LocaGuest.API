using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.OccupantAggregate;

namespace LocaGuest.Application.Interfaces;

public interface IOccupantSheetGeneratorService
{
    Task<byte[]> GenerateOccupantSheetPdfAsync(
        Occupant occupant,
        Property? associatedProperty,
        string currentUserFullName,
        string currentUserEmail,
        string? currentUserPhone,
        CancellationToken cancellationToken = default);
}
