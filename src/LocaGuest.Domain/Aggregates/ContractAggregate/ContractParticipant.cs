using LocaGuest.Domain.Aggregates.PaymentAggregate;
using LocaGuest.Domain.Common;
using LocaGuest.Domain.Exceptions;

namespace LocaGuest.Domain.Aggregates.ContractAggregate;

public class ContractParticipant : AuditableEntity
{
    public Guid ContractId { get; private set; }
    public Guid RenterOccupantId { get; private set; }

    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }

    public BillingShareType ShareType { get; private set; }
    public decimal ShareValue { get; private set; }

    private ContractParticipant() { }

    public static ContractParticipant Create(
        Guid contractId,
        Guid tenantId,
        DateTime startDate,
        DateTime? endDate,
        BillingShareType shareType,
        decimal shareValue)
    {
        if (contractId == Guid.Empty)
            throw new ValidationException("CONTRACT_PARTICIPANT_INVALID_CONTRACT", "ContractId is required");

        if (tenantId == Guid.Empty)
            throw new ValidationException("CONTRACT_PARTICIPANT_INVALID_TENANT", "TenantId is required");

        var sDate = EnsureUtc(startDate);
        var eDate = endDate.HasValue ? (DateTime?)EnsureUtc(endDate.Value) : null;

        if (eDate.HasValue && eDate.Value < sDate)
            throw new ValidationException("CONTRACT_PARTICIPANT_INVALID_DATES", "EndDate must be greater than or equal to StartDate");

        if (shareType == BillingShareType.Percentage)
        {
            if (shareValue <= 0m || shareValue > 100m)
                throw new ValidationException("CONTRACT_PARTICIPANT_INVALID_SHARE", "Percentage share must be between 0 and 100");
        }
        else
        {
            if (shareValue < 0m)
                throw new ValidationException("CONTRACT_PARTICIPANT_INVALID_SHARE", "Fixed amount share cannot be negative");
        }

        return new ContractParticipant
        {
            Id = Guid.NewGuid(),
            ContractId = contractId,
            RenterOccupantId = tenantId,
            StartDate = sDate,
            EndDate = eDate,
            ShareType = shareType,
            ShareValue = shareValue,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void EndAt(DateTime endDate)
    {
        var eDate = EnsureUtc(endDate);

        if (eDate < StartDate)
            throw new ValidationException("CONTRACT_PARTICIPANT_INVALID_DATES", "EndDate must be greater than or equal to StartDate");

        EndDate = eDate;
        LastModifiedAt = DateTime.UtcNow;
    }

    public bool IsEffectiveAt(DateTime dateUtc)
    {
        var d = EnsureUtc(dateUtc);
        return StartDate <= d && (!EndDate.HasValue || EndDate.Value >= d);
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
