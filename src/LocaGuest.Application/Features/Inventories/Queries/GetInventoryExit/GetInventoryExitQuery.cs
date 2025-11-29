using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Inventories;
using MediatR;

namespace LocaGuest.Application.Features.Inventories.Queries.GetInventoryExit;

public record GetInventoryExitQuery(Guid Id) : IRequest<Result<InventoryExitDto>>;
