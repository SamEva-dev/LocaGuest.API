using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Inventories.Commands.DeleteInventoryExit;

public class DeleteInventoryExitCommandHandler : IRequestHandler<DeleteInventoryExitCommand, Result>
{
    private readonly ILocaGuestDbContext _context;
    private readonly ILogger<DeleteInventoryExitCommandHandler> _logger;

    public DeleteInventoryExitCommandHandler(
        ILocaGuestDbContext context,
        ILogger<DeleteInventoryExitCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteInventoryExitCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var inventoryExit = await _context.InventoryExits
                .FirstOrDefaultAsync(ie => ie.Id == request.Id, cancellationToken);

            if (inventoryExit == null)
                return Result.Failure("Inventory exit not found");

            _context.InventoryExits.Remove(inventoryExit);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Inventory exit deleted: {InventoryId}", request.Id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting inventory exit {InventoryId}", request.Id);
            return Result.Failure($"Error deleting inventory exit: {ex.Message}");
        }
    }
}
