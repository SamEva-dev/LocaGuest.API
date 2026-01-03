using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.DTOs.Users;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Users.Commands.UpdateUserPhoto;

public class UpdateUserPhotoCommandHandler : IRequestHandler<UpdateUserPhotoCommand, Result<UserProfileDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateUserPhotoCommandHandler> _logger;

    public UpdateUserPhotoCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<UpdateUserPhotoCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<UserProfileDto>> Handle(UpdateUserPhotoCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
                return Result.Failure<UserProfileDto>("User not authenticated");

            var userId = _currentUserService.UserId.Value.ToString();
            var profile = await _unitOfWork.UserProfiles.GetByUserIdAsync(userId, cancellationToken);
            
            if (profile == null)
                return Result.Failure<UserProfileDto>("Profile not found");

            profile.UpdatePhoto(request.PhotoUrl);
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
            _logger.LogError(ex, "Error updating user photo");
            return Result.Failure<UserProfileDto>("Error updating photo");
        }
    }
}
