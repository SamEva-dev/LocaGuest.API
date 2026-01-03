using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Users;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Users.Commands.UpdateUserProfile;

public class UpdateUserProfileCommandHandler : IRequestHandler<UpdateUserProfileCommand, Result<UserProfileDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateUserProfileCommandHandler> _logger;

    public UpdateUserProfileCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<UpdateUserProfileCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<UserProfileDto>> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
                return Result.Failure<UserProfileDto>("User not authenticated");

            var userId = _currentUserService.UserId.Value.ToString();
            var profile = await _unitOfWork.UserProfiles.GetByUserIdAsync(userId, cancellationToken);
            if (profile == null)
                return Result.Failure<UserProfileDto>("Profile not found");

            profile.Update(
                request.FirstName,
                request.LastName,
                request.Phone,
                request.Company,
                request.Role,
                request.Bio);

            await _unitOfWork.CommitAsync(cancellationToken);

            var dto = new UserProfileDto
            {
                UserId = profile.UserId,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                Email = profile.Email,
                Phone = profile.Phone,
                Company = profile.Company,
                Role = profile.Role,
                Bio = profile.Bio,
                PhotoUrl = profile.PhotoUrl
            };

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile");
            return Result.Failure<UserProfileDto>("Error updating profile");
        }
    }
}
