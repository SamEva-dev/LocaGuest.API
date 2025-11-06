using LocaGuest.Domain.Common;
using LocaGuest.Domain.Aggregates.ContractAggregate.Events;
using LocaGuest.Domain.Exceptions;

namespace LocaGuest.Domain.Aggregates.ContractAggregate;

public class Contract : AuditableEntity
{
    public Guid PropertyId { get; private set; }
    public Guid TenantId { get; private set; }
    public ContractType Type { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public decimal Rent { get; private set; }
    public decimal? Deposit { get; private set; }
    public ContractStatus Status { get; private set; }
    public string? Notes { get; private set; }

    private readonly List<Payment> _payments = new();
    public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();

    private Contract() { } // EF

    public static Contract Create(
        Guid propertyId,
        Guid tenantId,
        ContractType type,
        DateTime startDate,
        DateTime endDate,
        decimal rent,
        decimal? deposit = null)
    {
        if (startDate >= endDate)
            throw new ValidationException("CONTRACT_INVALID_DATES", "Start date must be before end date");

        if (rent <= 0)
            throw new ValidationException("CONTRACT_INVALID_RENT", "Rent must be positive");

        var contract = new Contract
        {
            Id = Guid.NewGuid(),
            PropertyId = propertyId,
            TenantId = tenantId,
            Type = type,
            StartDate = startDate,
            EndDate = endDate,
            Rent = rent,
            Deposit = deposit,
            Status = ContractStatus.Active
        };

        contract.AddDomainEvent(new ContractCreated(contract.Id, propertyId, tenantId, startDate, endDate, rent));
        return contract;
    }

    public void Renew(DateTime newEndDate)
    {
        if (newEndDate <= EndDate)
            throw new ValidationException("CONTRACT_INVALID_RENEWAL", "New end date must be after current end date");

        EndDate = newEndDate;
        AddDomainEvent(new ContractRenewed(Id, PropertyId, TenantId, newEndDate));
    }

    public void Terminate(DateTime terminationDate)
    {
        if (Status == ContractStatus.Terminated) return;

        Status = ContractStatus.Terminated;
        AddDomainEvent(new ContractTerminated(Id, PropertyId, TenantId, terminationDate));
    }

    public void MarkAsExpiring()
    {
        if (Status == ContractStatus.Active)
        {
            Status = ContractStatus.Expiring;
        }
    }

    public Payment RecordPayment(decimal amount, DateTime paymentDate, PaymentMethod method)
    {
        if (amount <= 0)
            throw new ValidationException("PAYMENT_INVALID_AMOUNT", "Payment amount must be positive");

        var payment = Payment.Create(Id, amount, paymentDate, method);
        _payments.Add(payment);

        AddDomainEvent(new PaymentRecorded(payment.Id, Id, PropertyId, TenantId, amount, paymentDate));
        return payment;
    }

    public void MarkPaymentAsLate(Guid paymentId)
    {
        var payment = _payments.FirstOrDefault(p => p.Id == paymentId);
        if (payment == null)
            throw new NotFoundException("PAYMENT_NOT_FOUND", "Payment not found");

        payment.MarkAsLate();
        AddDomainEvent(new PaymentLateDetected(paymentId, Id, PropertyId, TenantId));
    }
}

public enum ContractType
{
    Furnished,
    Unfurnished
}

public enum ContractStatus
{
    Active,
    Expiring,
    Terminated
}

public class Payment : Entity
{
    public Guid ContractId { get; private set; }
    public decimal Amount { get; private set; }
    public DateTime PaymentDate { get; private set; }
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; }

    private Payment() { } // EF

    internal static Payment Create(Guid contractId, decimal amount, DateTime paymentDate, PaymentMethod method)
    {
        return new Payment
        {
            Id = Guid.NewGuid(),
            ContractId = contractId,
            Amount = amount,
            PaymentDate = paymentDate,
            Method = method,
            Status = PaymentStatus.Completed
        };
    }

    internal void MarkAsLate()
    {
        Status = PaymentStatus.Late;
    }
}

public enum PaymentMethod
{
    BankTransfer,
    Check,
    Cash,
    CreditCard
}

public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Late
}
