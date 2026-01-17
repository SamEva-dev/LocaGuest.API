using LocaGuest.Domain.Common;

namespace LocaGuest.Domain.Aggregates.PaymentAggregate;

/// <summary>
/// Représente une facture de loyer mensuelle pour un contrat
/// Générée automatiquement chaque mois pour les contrats actifs
/// </summary>
public class RentInvoice : AuditableEntity
{
    public Guid ContractId { get; private set; }
    public Guid RenterOccupantId { get; private set; }
    public Guid PropertyId { get; private set; }

    public Guid? InvoiceDocumentId { get; private set; }
    
    /// <summary>
    /// Mois de la facture (1-12)
    /// </summary>
    public int Month { get; private set; }
    
    /// <summary>
    /// Année de la facture
    /// </summary>
    public int Year { get; private set; }
    
    /// <summary>
    /// Montant total dû (loyer + charges)
    /// </summary>
    public decimal Amount { get; private set; }
    
    /// <summary>
    /// Statut de la facture
    /// </summary>
    public InvoiceStatus Status { get; private set; }
    
    /// <summary>
    /// ID du paiement associé (si payé)
    /// </summary>
    public Guid? PaymentId { get; private set; }
    
    /// <summary>
    /// Date de génération de la facture
    /// </summary>
    public DateTime GeneratedAt { get; private set; }
    
    /// <summary>
    /// Date d'échéance du paiement
    /// </summary>
    public DateTime DueDate { get; private set; }
    
    /// <summary>
    /// Date de paiement effective
    /// </summary>
    public DateTime? PaidDate { get; private set; }
    
    /// <summary>
    /// Notes sur la facture
    /// </summary>
    public string? Notes { get; private set; }

    private RentInvoice() { } // EF Core

    public static RentInvoice Create(
        Guid contractId,
        Guid tenantId,
        Guid propertyId,
        int month,
        int year,
        decimal amount,
        DateTime dueDate)
    {
        return new RentInvoice
        {
            Id = Guid.NewGuid(),
            ContractId = contractId,
            RenterOccupantId = tenantId,
            PropertyId = propertyId,
            Month = month,
            Year = year,
            Amount = amount,
            Status = InvoiceStatus.Pending,
            GeneratedAt = DateTime.UtcNow,
            DueDate = dueDate
        };
    }

    public void MarkAsPaid(Guid paymentId)
    {
        Status = InvoiceStatus.Paid;
        PaymentId = paymentId;
        PaidDate = DateTime.UtcNow;
    }
    
    public void MarkAsPaid(DateTime paidDate, string? notes = null)
    {
        Status = InvoiceStatus.Paid;
        PaidDate = paidDate;
        Notes = notes;
    }

    public void MarkAsPartial(Guid paymentId)
    {
        Status = InvoiceStatus.Partial;
        PaymentId = paymentId;
    }

    public void MarkAsLate()
    {
        if (Status == InvoiceStatus.Pending)
            Status = InvoiceStatus.Late;
    }

    public void AttachInvoiceDocument(Guid documentId)
    {
        if (documentId == Guid.Empty)
            throw new ArgumentException("DocumentId cannot be empty", nameof(documentId));

        InvoiceDocumentId = documentId;
        LastModifiedAt = DateTime.UtcNow;
    }

    public void UpdateStatusFromLines(IReadOnlyCollection<RentInvoiceLine> lines)
    {
        if (lines == null || lines.Count == 0)
        {
            Status = InvoiceStatus.Pending;
            return;
        }

        if (lines.All(l => l.Status == InvoiceLineStatus.Paid))
        {
            Status = InvoiceStatus.Paid;
            PaidDate = DateTime.UtcNow;
            return;
        }

        if (lines.Any(l => l.Status == InvoiceLineStatus.Partial || l.Status == InvoiceLineStatus.Paid))
        {
            Status = InvoiceStatus.Partial;
            return;
        }

        Status = InvoiceStatus.Pending;
    }

    public void UpdateStatus(PaymentStatus paymentStatus)
    {
        Status = paymentStatus switch
        {
            PaymentStatus.Paid or PaymentStatus.PaidLate => InvoiceStatus.Paid,
            PaymentStatus.Partial => InvoiceStatus.Partial,
            PaymentStatus.Late => InvoiceStatus.Late,
            PaymentStatus.Pending => InvoiceStatus.Pending,
            _ => Status
        };
    }

    public bool IsOverdue()
    {
        return DateTime.UtcNow.Date > DueDate.Date && Status != InvoiceStatus.Paid;
    }
}

public enum InvoiceStatus
{
    Pending,  // En attente de paiement
    Paid,     // Payé
    Partial,  // Paiement partiel
    Late,     // En retard
    Cancelled // Annulé
}
