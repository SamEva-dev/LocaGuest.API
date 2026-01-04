using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Admin.Queries.GetDatabaseStats;

public class GetDatabaseStatsQueryHandler : IRequestHandler<GetDatabaseStatsQuery, Result<object>>
{
    private readonly ILocaGuestDbContext _context;
    private readonly ILogger<GetDatabaseStatsQueryHandler> _logger;

    public GetDatabaseStatsQueryHandler(ILocaGuestDbContext context, ILogger<GetDatabaseStatsQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<object>> Handle(GetDatabaseStatsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var stats = new
            {
                Organizations = new
                {
                    Raw = await _context.Organizations.IgnoreQueryFilters().CountAsync(cancellationToken),
                    Filtered = await _context.Organizations.CountAsync(cancellationToken)
                },
                Properties = new
                {
                    Raw = await _context.Properties.IgnoreQueryFilters().CountAsync(cancellationToken),
                    Filtered = await _context.Properties.CountAsync(cancellationToken)
                },
                Tenants = new
                {
                    Raw = await _context.Occupants.IgnoreQueryFilters().CountAsync(cancellationToken),
                    Filtered = await _context.Occupants.CountAsync(cancellationToken)
                },
                Contracts = new
                {
                    Raw = await _context.Contracts.IgnoreQueryFilters().CountAsync(cancellationToken),
                    Filtered = await _context.Contracts.CountAsync(cancellationToken)
                },
                Payments = new
                {
                    Raw = await _context.Payments.IgnoreQueryFilters().CountAsync(cancellationToken),
                    Filtered = await _context.Payments.CountAsync(cancellationToken)
                },
                Documents = new
                {
                    Raw = await _context.Documents.IgnoreQueryFilters().CountAsync(cancellationToken),
                    Filtered = await _context.Documents.CountAsync(cancellationToken)
                },
                InventoryEntries = new
                {
                    Raw = await _context.InventoryEntries.IgnoreQueryFilters().CountAsync(cancellationToken),
                    Filtered = await _context.InventoryEntries.CountAsync(cancellationToken)
                },
                InventoryExits = new
                {
                    Raw = await _context.InventoryExits.IgnoreQueryFilters().CountAsync(cancellationToken),
                    Filtered = await _context.InventoryExits.CountAsync(cancellationToken)
                }
            };

            return Result.Success<object>(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving database stats");
            return Result.Failure<object>("Error retrieving database stats");
        }
    }
}
