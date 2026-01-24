using System.Text.Json;
using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Aggregates.DocumentAggregate;
using LocaGuest.Domain.Aggregates.PaymentAggregate;
using LocaGuest.Domain.Repositories;
using LocaGuest.Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Addendums.Commands.MarkAddendumAsSigned;

public class MarkAddendumAsSignedCommandHandler : IRequestHandler<MarkAddendumAsSignedCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEffectiveContractStateResolver _effectiveContractStateResolver;
    private readonly ILogger<MarkAddendumAsSignedCommandHandler> _logger;

    public MarkAddendumAsSignedCommandHandler(
        IUnitOfWork unitOfWork,
        IEffectiveContractStateResolver effectiveContractStateResolver,
        ILogger<MarkAddendumAsSignedCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _effectiveContractStateResolver = effectiveContractStateResolver;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(MarkAddendumAsSignedCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var addendum = await _unitOfWork.Addendums.GetByIdAsync(request.AddendumId, cancellationToken);
            if (addendum == null)
                return Result.Failure<Guid>("Addendum not found");

            if (addendum.SignatureStatus == AddendumSignatureStatus.Rejected)
                return Result.Failure<Guid>("Rejected addendum cannot be signed");

            if (addendum.SignatureStatus != AddendumSignatureStatus.Signed)
            {
                addendum.MarkAsSigned(request.SignedDateUtc);
            }

            // Mark associated Avenant document(s) as signed
            var docIds = new List<Guid>();
            if (!string.IsNullOrWhiteSpace(addendum.AttachedDocumentIds))
            {
                try
                {
                    docIds = JsonSerializer.Deserialize<List<Guid>>(addendum.AttachedDocumentIds) ?? new List<Guid>();
                }
                catch
                {
                    docIds = new List<Guid>();
                }
            }

            var signedDate = request.SignedDateUtc ?? DateTime.UtcNow;

            foreach (var docId in docIds)
            {
                var doc = await _unitOfWork.Documents.GetByIdAsync(docId, cancellationToken);
                if (doc == null)
                    continue;

                if (doc.Type != DocumentType.Avenant)
                    continue;

                if (doc.Status == DocumentStatus.Draft)
                {
                    doc.MarkAsSigned(signedDate, request.SignedBy);
                }
            }

            if (addendum.Type == AddendumType.Occupants)
            {
                var applyResult = await ApplyOccupantsChangesIfAnyAsync(addendum, cancellationToken);
                if (!applyResult.IsSuccess)
                    return Result.Failure<Guid>(applyResult.ErrorMessage ?? "Unable to apply occupants changes");
            }

            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Addendum {AddendumId} marked as signed", addendum.Id);

            return Result.Success(addendum.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing addendum {AddendumId}", request.AddendumId);
            return Result.Failure<Guid>($"Error signing addendum: {ex.Message}");
        }
    }

    private async Task<Result> ApplyOccupantsChangesIfAnyAsync(Addendum addendum, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(addendum.OccupantChanges))
            return Result.Failure("Occupants addendum requires occupant changes details");

        OccupantChangesDto? payload;
        try
        {
            payload = JsonSerializer.Deserialize<OccupantChangesDto>(
                addendum.OccupantChanges,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            payload = null;
        }

        if (payload?.Participants == null || payload.Participants.Count == 0)
            return Result.Failure("Occupants addendum requires occupant changes details");

        if (!Enum.TryParse<BillingShareType>(payload.SplitType ?? string.Empty, true, out var shareType))
            return Result.Failure("Invalid splitType for occupants addendum");

        var distinct = payload.Participants
            .Where(p => p != null && p.OccupantId != Guid.Empty)
            .GroupBy(p => p.OccupantId)
            .Select(g => g.First())
            .ToList();

        if (distinct.Count != payload.Participants.Count)
            return Result.Failure("Duplicate OccupantId in occupants addendum");

        if (distinct.Any(p => p.ShareValue < 0m))
            return Result.Failure("ShareValue cannot be negative");

        var shareSum = distinct.Sum(p => p.ShareValue);

        if (shareType == BillingShareType.Percentage)
        {
            if (Math.Abs(shareSum - 100m) > 0.0001m)
                return Result.Failure("Percentage split must total 100");
        }
        else
        {
            var stateResult = await _effectiveContractStateResolver.ResolveAsync(addendum.ContractId, addendum.EffectiveDate, cancellationToken);
            if (!stateResult.IsSuccess || stateResult.Data == null)
                return Result.Failure(stateResult.ErrorMessage ?? "Unable to resolve effective contract state");

            var expected = stateResult.Data.Rent + stateResult.Data.Charges;
            if (Math.Abs(shareSum - expected) > 0.01m)
                return Result.Failure("FixedAmount split must total (Rent + Charges)");
        }

        var effectiveDate = addendum.EffectiveDate;
        var endDate = effectiveDate.AddDays(-1);

        var existing = await _unitOfWork.ContractParticipants
            .GetEffectiveByContractIdAtDateAsync(addendum.ContractId, effectiveDate, cancellationToken);

        foreach (var p in existing)
        {
            p.EndAt(endDate);
            _unitOfWork.ContractParticipants.Update(p);
        }

        foreach (var np in distinct)
        {
            var created = ContractParticipant.Create(
                contractId: addendum.ContractId,
                tenantId: np.OccupantId,
                startDate: effectiveDate,
                endDate: null,
                shareType: shareType,
                shareValue: np.ShareValue);

            await _unitOfWork.ContractParticipants.AddAsync(created, cancellationToken);
        }

        return Result.Success();
    }

    private sealed record OccupantChangesDto(string? SplitType, List<OccupantParticipantDto> Participants);
    private sealed record OccupantParticipantDto(Guid OccupantId, decimal ShareValue);
}
