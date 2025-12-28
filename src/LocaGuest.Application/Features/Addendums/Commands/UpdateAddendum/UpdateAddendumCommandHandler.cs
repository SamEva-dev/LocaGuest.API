using System.Text.Json;
using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Addendums;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Addendums.Commands.UpdateAddendum;

public class UpdateAddendumCommandHandler : IRequestHandler<UpdateAddendumCommand, Result<AddendumDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateAddendumCommandHandler> _logger;

    public UpdateAddendumCommandHandler(IUnitOfWork unitOfWork, ILogger<UpdateAddendumCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<AddendumDto>> Handle(UpdateAddendumCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var entity = await _unitOfWork.Addendums.GetByIdAsync(request.Id, cancellationToken);
            if (entity == null)
                return Result.Failure<AddendumDto>("Addendum not found");

            var effectiveDate = request.EffectiveDate ?? entity.EffectiveDate;
            var reason = request.Reason ?? entity.Reason;
            var description = request.Description ?? entity.Description;

            entity.UpdateDetails(effectiveDate, reason, description);

            if (request.AttachedDocumentIds != null)
                entity.UpdateDocuments(request.AttachedDocumentIds);

            if (request.Notes != null)
                entity.UpdateNotes(request.Notes);

            await _unitOfWork.CommitAsync(cancellationToken);

            return Result.Success(Map(entity));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating addendum {AddendumId}", request.Id);
            return Result.Failure<AddendumDto>($"Error updating addendum: {ex.Message}");
        }
    }

    private static AddendumDto Map(Addendum entity)
    {
        var docs = new List<Guid>();
        if (!string.IsNullOrWhiteSpace(entity.AttachedDocumentIds))
        {
            try
            {
                docs = JsonSerializer.Deserialize<List<Guid>>(entity.AttachedDocumentIds) ?? new List<Guid>();
            }
            catch
            {
                docs = new List<Guid>();
            }
        }

        return new AddendumDto(
            entity.Id,
            entity.ContractId,
            entity.Type.ToString(),
            entity.EffectiveDate,
            entity.Reason,
            entity.Description,
            entity.OldRent,
            entity.NewRent,
            entity.OldCharges,
            entity.NewCharges,
            entity.OldEndDate,
            entity.NewEndDate,
            entity.OccupantChanges,
            entity.OldRoomId,
            entity.NewRoomId,
            entity.OldClauses,
            entity.NewClauses,
            docs,
            entity.SignatureStatus.ToString(),
            entity.SignedDate,
            entity.Notes,
            entity.CreatedAt,
            entity.LastModifiedAt);
    }
}
