using LocaGuest.Application.DTOs.Documents;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Aggregates.OccupantAggregate;

namespace LocaGuest.Application.Interfaces;

public interface IContractGeneratorService
{
    Task<byte[]> GenerateContractPdfAsync(
        Occupant tenant,
        Property property,
        string currentUserFullName,
        string currentUserEmail,
        string? currentUserPhone,
        GenerateContractDto dto,
        CancellationToken cancellationToken = default);
}
