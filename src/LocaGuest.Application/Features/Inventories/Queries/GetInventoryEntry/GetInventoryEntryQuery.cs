using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Inventories;
using MediatR;

namespace LocaGuest.Application.Features.Inventories.Queries.GetInventoryEntry;

public record GetInventoryEntryQuery(Guid Id) : IRequest<Result<InventoryEntryDto>>;
