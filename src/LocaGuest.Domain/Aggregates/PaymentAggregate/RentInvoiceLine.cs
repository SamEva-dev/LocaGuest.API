using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.PaymentAggregate;

public class RentInvoiceLine : AuditableEntity
{
    public Guid RentInvoiceId { get; private set; }
    public Guid RenterTenantId { get; private set; }

    public decimal AmountDue { get; private set; }
    public decimal AmountPaid { get; private set; }

    public InvoiceLineStatus Status { get; private set; }

    public BillingShareType ShareType { get; private set; }
    public decimal ShareValue { get; private set; }

    public Guid? PaymentId { get; private set; }
    public DateTime? PaidDate { get; private set; }

    private RentInvoiceLine() { }

    public static RentInvoiceLine Create(
        Guid rentInvoiceId,
        Guid tenantId,
        decimal amountDue,
        BillingShareType shareType,
        decimal shareValue)
    {
        return new RentInvoiceLine
        {
            Id = Guid.NewGuid(),
            RentInvoiceId = rentInvoiceId,
            RenterTenantId = tenantId,
            AmountDue = amountDue,
            AmountPaid = 0m,
            Status = InvoiceLineStatus.Pending,
            ShareType = shareType,
            ShareValue = shareValue,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void ApplyPayment(Guid paymentId, decimal amountPaid, DateTime? paidDate)
    {
        AmountPaid = amountPaid;
        PaymentId = paymentId;
        PaidDate = paidDate;

        if (AmountPaid <= 0m)
        {
            Status = InvoiceLineStatus.Pending;
            return;
        }

        Status = AmountPaid >= AmountDue
            ? InvoiceLineStatus.Paid
            : InvoiceLineStatus.Partial;

        LastModifiedAt = DateTime.UtcNow;
    }

    public decimal RemainingAmount => Math.Max(0m, AmountDue - AmountPaid);
}

public enum InvoiceLineStatus
{
    Pending,
    Partial,
    Paid
}

public enum BillingShareType
{
    Percentage,
    FixedAmount
}
