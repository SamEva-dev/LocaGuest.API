using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Admin.Commands.CleanDatabase;

public class CleanDatabaseCommandHandler : IRequestHandler<CleanDatabaseCommand, Result<CleanDatabaseDto>>
{
    private readonly IAdminMaintenanceService _adminMaintenanceService;
    private readonly ILogger<CleanDatabaseCommandHandler> _logger;

    public CleanDatabaseCommandHandler(IAdminMaintenanceService adminMaintenanceService, ILogger<CleanDatabaseCommandHandler> logger)
    {
        _adminMaintenanceService = adminMaintenanceService;
        _logger = logger;
    }

    public async Task<Result<CleanDatabaseDto>> Handle(CleanDatabaseCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.PreservedUserId))
                return Result.Failure<CleanDatabaseDto>("Unable to identify connected user");

            var result = await _adminMaintenanceService.CleanDatabaseAsync(request.PreservedUserId, cancellationToken);
            return Result.Success(new CleanDatabaseDto(result.PreservedUser, result.DeletedTables));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning database");
            return Result.Failure<CleanDatabaseDto>("Error cleaning database");
        }
    }
}
