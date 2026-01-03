using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.PropertyAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Properties.Commands.UpdatePropertyStatus;

public class UpdatePropertyStatusCommandHandler : IRequestHandler<UpdatePropertyStatusCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdatePropertyStatusCommandHandler> _logger;

    public UpdatePropertyStatusCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<UpdatePropertyStatusCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(UpdatePropertyStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate tenant authentication
            if (!_currentUserService.IsAuthenticated)
            {
                _logger.LogWarning("Unauthorized property status update attempt");
                return Result.Failure<bool>("User not authenticated");
            }

            // Get existing property
            var property = await _unitOfWork.Properties.GetByIdAsync(request.PropertyId, cancellationToken);
            
            if (property == null)
            {
                return Result.Failure<bool>($"Property with ID {request.PropertyId} not found");
            }

            // Parse and validate status
            if (!Enum.TryParse<PropertyStatus>(request.Status, ignoreCase: true, out var status))
            {
                return Result.Failure<bool>($"Invalid property status: {request.Status}");
            }

            // Update status
            property.SetStatus(status);

            // Save changes
            _unitOfWork.Properties.Update(property);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Property status updated successfully: {PropertyId} - {Status}", 
                property.Id, status);

            return Result.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating property status {PropertyId}", request.PropertyId);
            return Result.Failure<bool>($"Error updating property status: {ex.Message}");
        }
    }
}
