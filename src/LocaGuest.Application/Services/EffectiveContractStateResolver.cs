using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.ContractAggregate;
using LocaGuest.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LocaGuest.Application.Services;

public class EffectiveContractStateResolver : IEffectiveContractStateResolver
{
    private readonly IUnitOfWork _unitOfWork;

    public EffectiveContractStateResolver(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<EffectiveContractState>> ResolveAsync(Guid contractId, DateTime dateUtc, CancellationToken cancellationToken = default)
    {
        var d = EnsureUtc(dateUtc);

        var contract = await _unitOfWork.Contracts.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == contractId, cancellationToken);

        if (contract == null)
            return Result.Failure<EffectiveContractState>("Contract not found");

        decimal rent = contract.Rent;
        decimal charges = contract.Charges;
        DateTime endDate = contract.EndDate;
        Guid? roomId = contract.RoomId;
        string? clauses = contract.CustomClauses;

        var appliedAddendums = new List<Guid>();

        var addendums = await _unitOfWork.Addendums.Query()
            .AsNoTracking()
            .Where(a => a.ContractId == contractId
                        && a.SignatureStatus == AddendumSignatureStatus.Signed
                        && a.EffectiveDate <= d)
            .OrderBy(a => a.EffectiveDate)
            .ThenBy(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

        foreach (var a in addendums)
        {
            appliedAddendums.Add(a.Id);

            if (a.NewRent.HasValue) rent = a.NewRent.Value;
            if (a.NewCharges.HasValue) charges = a.NewCharges.Value;
            if (a.NewEndDate.HasValue) endDate = a.NewEndDate.Value;
            if (a.NewRoomId.HasValue) roomId = a.NewRoomId.Value;
            if (!string.IsNullOrWhiteSpace(a.NewClauses)) clauses = a.NewClauses;
        }

        var participants = await _unitOfWork.ContractParticipants
            .GetEffectiveByContractIdAtDateAsync(contractId, d, cancellationToken);

        var mappedParticipants = participants
            .Select(p => new EffectiveContractParticipant(
                RenterOccupantId: p.RenterOccupantId,
                ShareType: p.ShareType,
                ShareValue: p.ShareValue,
                StartDate: p.StartDate,
                EndDate: p.EndDate))
            .ToList();

        if (mappedParticipants.Count == 0)
        {
            mappedParticipants.Add(new EffectiveContractParticipant(
                RenterOccupantId: contract.RenterOccupantId,
                ShareType: Domain.Aggregates.PaymentAggregate.BillingShareType.Percentage,
                ShareValue: 100m,
                StartDate: contract.StartDate,
                EndDate: null));
        }

        var state = new EffectiveContractState(
            ContractId: contract.Id,
            DateUtc: d,
            Rent: rent,
            Charges: charges,
            StartDate: contract.StartDate,
            EndDate: endDate,
            RoomId: roomId,
            CustomClauses: clauses,
            Participants: mappedParticipants,
            AppliedAddendumIds: appliedAddendums);

        return Result.Success(state);
    }

    public async Task<Result<EffectiveContractState>> ResolveForPeriodAsync(Guid contractId, DateTime periodStartUtc, DateTime periodEndUtc, CancellationToken cancellationToken = default)
    {
        var start = EnsureUtc(periodStartUtc);
        var end = EnsureUtc(periodEndUtc);

        if (end < start)
            return Result.Failure<EffectiveContractState>("Invalid period: end must be greater than or equal to start");

        var baseStateResult = await ResolveAsync(contractId, start, cancellationToken);
        if (!baseStateResult.IsSuccess || baseStateResult.Data == null)
            return baseStateResult;

        var baseState = baseStateResult.Data;

        var contract = await _unitOfWork.Contracts.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == contractId, cancellationToken);

        if (contract == null)
            return Result.Failure<EffectiveContractState>("Contract not found");

        var overlapping = await _unitOfWork.ContractParticipants
            .GetOverlappingByContractIdAsync(contractId, start, end, cancellationToken);

        var mappedParticipants = overlapping
            .Select(p => new EffectiveContractParticipant(
                RenterOccupantId: p.RenterOccupantId,
                ShareType: p.ShareType,
                ShareValue: p.ShareValue,
                StartDate: p.StartDate,
                EndDate: p.EndDate))
            .ToList();

        if (mappedParticipants.Count == 0)
        {
            mappedParticipants.Add(new EffectiveContractParticipant(
                RenterOccupantId: contract.RenterOccupantId,
                ShareType: Domain.Aggregates.PaymentAggregate.BillingShareType.Percentage,
                ShareValue: 100m,
                StartDate: contract.StartDate,
                EndDate: null));
        }

        var state = baseState with
        {
            Participants = mappedParticipants
        };

        return Result.Success(state);
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
