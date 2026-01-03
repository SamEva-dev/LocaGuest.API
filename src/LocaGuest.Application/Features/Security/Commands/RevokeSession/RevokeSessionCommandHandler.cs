using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Security.Commands.RevokeSession;

public class RevokeSessionCommandHandler : IRequestHandler<RevokeSessionCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RevokeSessionCommandHandler> _logger;

    public RevokeSessionCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<RevokeSessionCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(RevokeSessionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
                return Result.Failure<bool>("User not authenticated");

            var session = await _unitOfWork.UserSessions.GetByIdAsync(request.SessionId, cancellationToken);
            
            if (session == null)
                return Result.Failure<bool>("Session not found");

            // Verify session belongs to current user
            var userId = _currentUserService.UserId.Value.ToString();
            if (session.UserId != userId)
                return Result.Failure<bool>("Unauthorized");

            session.Revoke();
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Session {SessionId} revoked by user {UserId}", request.SessionId, userId);
            return Result.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking session");
            return Result.Failure<bool>("Error revoking session");
        }
    }
}
