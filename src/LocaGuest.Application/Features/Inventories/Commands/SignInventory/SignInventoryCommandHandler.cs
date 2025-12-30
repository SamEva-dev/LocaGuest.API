using LocaGuest.Application.Common;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Inventories.Commands.SignInventory;

public class SignInventoryCommandHandler : IRequestHandler<SignInventoryCommand, Result>
{
    private readonly LocaGuest.Application.Common.Interfaces.ILocaGuestDbContext _context;
    private readonly ILogger<SignInventoryCommandHandler> _logger;

    public SignInventoryCommandHandler(
        LocaGuest.Application.Common.Interfaces.ILocaGuestDbContext context,
        ILogger<SignInventoryCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result> Handle(SignInventoryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.InventoryType == "Entry")
            {
                var inventory = await _context.InventoryEntries.FindAsync(new object[] { request.InventoryId }, cancellationToken);
                if (inventory == null)
                    return Result.Failure("Inventory entry not found");

                inventory.MarkAsFinalized();
                _logger.LogInformation("Inventory entry {InventoryId} signed by {SignerRole}: {SignerName}",
                    request.InventoryId, request.SignerRole, request.SignerName);
            }
            else
            {
                var inventory = await _context.InventoryExits.FindAsync(new object[] { request.InventoryId }, cancellationToken);
                if (inventory == null)
                    return Result.Failure("Inventory exit not found");

                inventory.MarkAsFinalized();
                _logger.LogInformation("Inventory exit {InventoryId} signed by {SignerRole}: {SignerName}",
                    request.InventoryId, request.SignerRole, request.SignerName);
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing inventory {InventoryId}", request.InventoryId);
            return Result.Failure($"Error signing inventory: {ex.Message}");
        }
    }
}
