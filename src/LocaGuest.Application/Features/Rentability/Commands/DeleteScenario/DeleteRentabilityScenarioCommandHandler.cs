using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Rentability;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.RentabilityAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Rentability.Commands.DeleteScenario;

public class DeleteRentabilityScenarioCommandHandler : IRequestHandler<DeleteRentabilityScenarioCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILocaGuestDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteRentabilityScenarioCommandHandler> _logger;

    public DeleteRentabilityScenarioCommandHandler(
        IUnitOfWork unitOfWork,
        ILocaGuestDbContext context,
        ICurrentUserService currentUserService,
        ILogger<DeleteRentabilityScenarioCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeleteRentabilityScenarioCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException("User not authenticated");

            var scenario = await _context.RentabilityScenarios
                .FirstOrDefaultAsync(s => s.Id == request.Id && s.UserId == userId, cancellationToken);

            if (scenario == null)
            {
                return Result.Failure<bool>("Scenario not found");
            }

            _context.RentabilityScenarios.Remove(scenario);
            await _unitOfWork.CommitAsync(cancellationToken);

            return Result.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting rentability scenario {ScenarioId}", request.Id);
            return Result.Failure<bool>("Error deleting scenario");
        }
    }
}
