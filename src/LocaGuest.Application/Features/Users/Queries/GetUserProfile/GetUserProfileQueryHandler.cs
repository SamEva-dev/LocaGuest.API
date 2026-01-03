using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Users;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.UserAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Users.Queries.GetUserProfile;

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, Result<UserProfileDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetUserProfileQueryHandler> _logger;

    public GetUserProfileQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<GetUserProfileQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<UserProfileDto>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
                return Result.Failure<UserProfileDto>("User not authenticated");

            var userId = _currentUserService.UserId.Value.ToString();
            var profile = await _unitOfWork.UserProfiles.GetByUserIdAsync(userId, cancellationToken);

            // Si pas de profil, créer un par défaut
            if (profile == null)
            {
                profile = UserProfile.Create(
                    userId,
                    "User", // Default firstname
                    "",
                    "");

                await _unitOfWork.UserProfiles.AddAsync(profile, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);
            }

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
            _logger.LogError(ex, "Error retrieving user profile");
            return Result.Failure<UserProfileDto>("Error retrieving profile");
        }
    }
}
