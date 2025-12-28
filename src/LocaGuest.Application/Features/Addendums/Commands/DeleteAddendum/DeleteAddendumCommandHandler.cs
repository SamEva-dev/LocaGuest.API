using LocaGuest.Application.Common;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Addendums.Commands.DeleteAddendum;

public class DeleteAddendumCommandHandler : IRequestHandler<DeleteAddendumCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteAddendumCommandHandler> _logger;

    public DeleteAddendumCommandHandler(IUnitOfWork unitOfWork, ILogger<DeleteAddendumCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(DeleteAddendumCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var entity = await _unitOfWork.Addendums.GetByIdAsync(request.Id, cancellationToken);
            if (entity == null)
                return Result.Failure<Guid>("Addendum not found");

            _unitOfWork.Addendums.Remove(entity);
            await _unitOfWork.CommitAsync(cancellationToken);

            return Result.Success(request.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting addendum {AddendumId}", request.Id);
            return Result.Failure<Guid>($"Error deleting addendum: {ex.Message}");
        }
    }
}
