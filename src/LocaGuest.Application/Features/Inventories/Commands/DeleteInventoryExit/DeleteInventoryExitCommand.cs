using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Inventories.Commands.DeleteInventoryExit;

/// <summary>
/// Command pour supprimer un état des lieux de sortie
/// </summary>
public record DeleteInventoryExitCommand(Guid Id) : IRequest<Result>;
