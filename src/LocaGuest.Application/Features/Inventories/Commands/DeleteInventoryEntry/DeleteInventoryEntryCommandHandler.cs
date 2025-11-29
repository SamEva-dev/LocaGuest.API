using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Inventories.Commands.DeleteInventoryEntry;

public class DeleteInventoryEntryCommandHandler : IRequestHandler<DeleteInventoryEntryCommand, Result>
{
    private readonly ILocaGuestDbContext _context;
    private readonly ILogger<DeleteInventoryEntryCommandHandler> _logger;

    public DeleteInventoryEntryCommandHandler(
        ILocaGuestDbContext context,
        ILogger<DeleteInventoryEntryCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteInventoryEntryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var inventoryEntry = await _context.InventoryEntries
                .FirstOrDefaultAsync(ie => ie.Id == request.Id, cancellationToken);

            if (inventoryEntry == null)
                return Result.Failure("Inventory entry not found");

            // ✅ Vérifier qu'il n'y a pas d'EDL sortie lié
            var hasExit = await _context.InventoryExits
                .AnyAsync(ie => ie.InventoryEntryId == request.Id, cancellationToken);

            if (hasExit)
                return Result.Failure("Cannot delete inventory entry: an inventory exit is linked to it");

            _context.InventoryEntries.Remove(inventoryEntry);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Inventory entry deleted: {InventoryId}", request.Id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting inventory entry {InventoryId}", request.Id);
            return Result.Failure($"Error deleting inventory entry: {ex.Message}");
        }
    }
}
