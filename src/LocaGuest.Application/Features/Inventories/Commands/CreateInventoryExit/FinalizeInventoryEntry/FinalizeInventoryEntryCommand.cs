using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Inventories.Commands.FinalizeInventoryEntry;

/// <summary>
/// Finaliser un EDL d'entrée = signer et verrouiller
/// Devient un document légal opposable
/// </summary>
public record FinalizeInventoryEntryCommand(Guid InventoryEntryId) : IRequest<Result>;
