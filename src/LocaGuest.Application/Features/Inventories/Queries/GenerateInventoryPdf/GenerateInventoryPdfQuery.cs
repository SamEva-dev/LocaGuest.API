using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Inventories.Queries.GenerateInventoryPdf;

public record GenerateInventoryPdfQuery : IRequest<Result<byte[]>>
{
    public Guid InventoryId { get; init; }
    public string InventoryType { get; init; } = "Entry"; // Entry or Exit
}
