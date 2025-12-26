using LocaGuest.Domain.Aggregates.PropertyAggregate;

namespace LocaGuest.Application.Interfaces;

public interface IPropertySheetGeneratorService
{
    Task<byte[]> GeneratePropertySheetPdfAsync(
        Property property,
        string currentUserFullName,
        string currentUserEmail,
        string? currentUserPhone,
        CancellationToken cancellationToken = default);
}
