using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Inventories.Commands.DeleteInventoryEntry;

/// <summary>
/// Command pour supprimer un état des lieux d'entrée
/// </summary>
public record DeleteInventoryEntryCommand(Guid Id) : IRequest<Result>;
