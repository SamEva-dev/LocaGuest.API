using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.PaymentAggregate;

namespace LocaGuest.Application.Services;

public interface IEffectiveContractStateResolver
{
    Task<Result<EffectiveContractState>> ResolveAsync(Guid contractId, DateTime dateUtc, CancellationToken cancellationToken = default);
    Task<Result<EffectiveContractState>> ResolveForPeriodAsync(Guid contractId, DateTime periodStartUtc, DateTime periodEndUtc, CancellationToken cancellationToken = default);
}

public record EffectiveContractState(
    Guid ContractId,
    DateTime DateUtc,
    decimal Rent,
    decimal Charges,
    DateTime StartDate,
    DateTime EndDate,
    Guid? RoomId,
    string? CustomClauses,
    IReadOnlyList<EffectiveContractParticipant> Participants,
    IReadOnlyList<Guid> AppliedAddendumIds);

public record EffectiveContractParticipant(
    Guid RenterTenantId,
    BillingShareType ShareType,
    decimal ShareValue,
    DateTime StartDate,
    DateTime? EndDate);
