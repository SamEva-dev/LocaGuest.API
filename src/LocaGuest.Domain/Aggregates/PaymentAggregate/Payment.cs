using LocaGuest.Domain.Common;
using LocaGuest.Domain.Aggregates.PaymentAggregate.Events;

namespace LocaGuest.Domain.Aggregates.PaymentAggregate;

/// <summary>
/// Représente un paiement de loyer effectué par un locataire
/// </summary>
public class Payment : AuditableEntity
{
    public Guid RenterTenantId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid ContractId { get; private set; }
    
    /// <summary>
    /// Montant total dû pour cette période
    /// </summary>
    public decimal AmountDue { get; private set; }
    
    /// <summary>
    /// Montant réellement payé
    /// </summary>
    public decimal AmountPaid { get; private set; }
    
    /// <summary>
    /// Date à laquelle le paiement a été effectué
    /// </summary>
    public DateTime? PaymentDate { get; private set; }
    
    /// <summary>
    /// Date prévue du loyer (ex: 01/03/2025)
    /// </summary>
    public DateTime ExpectedDate { get; private set; }
    
    /// <summary>
    /// Statut du paiement
    /// </summary>
    public PaymentStatus Status { get; private set; }
    
    /// <summary>
    /// Méthode de paiement utilisée
    /// </summary>
    public PaymentMethod PaymentMethod { get; private set; }
    
    /// <summary>
    /// Note ou commentaire sur le paiement
    /// </summary>
    public string? Note { get; private set; }
    
    /// <summary>
    /// Mois concerné (1-12)
    /// </summary>
    public int Month { get; private set; }
    
    /// <summary>
    /// Année concernée
    /// </summary>
    public int Year { get; private set; }
    
    /// <summary>
    /// ID de la quittance générée (si applicable)
    /// </summary>
    public Guid? ReceiptId { get; private set; }

    private Payment() { } // EF Core

    public static Payment Create(
        Guid tenantId,
        Guid propertyId,
        Guid contractId,
        decimal amountDue,
        decimal amountPaid,
        DateTime expectedDate,
        DateTime? paymentDate,
        PaymentMethod paymentMethod,
        string? note = null)
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            RenterTenantId = tenantId,
            PropertyId = propertyId,
            ContractId = contractId,
            AmountDue = amountDue,
            AmountPaid = amountPaid,
            ExpectedDate = expectedDate,
            PaymentDate = paymentDate,
            PaymentMethod = paymentMethod,
            Note = note,
            Month = expectedDate.Month,
            Year = expectedDate.Year,
            Status = DetermineStatus(amountDue, amountPaid, expectedDate, paymentDate)
        };

        payment.AddDomainEvent(new PaymentCreated(payment.Id, payment.RenterTenantId, payment.AmountPaid));
        return payment;
    }

    public void UpdatePayment(
        decimal amountPaid,
        DateTime? paymentDate,
        PaymentMethod? paymentMethod = null,
        string? note = null)
    {
        AmountPaid = amountPaid;
        PaymentDate = paymentDate;
        
        if (paymentMethod.HasValue)
            PaymentMethod = paymentMethod.Value;
        
        if (note != null)
            Note = note;
        
        Status = DetermineStatus(AmountDue, AmountPaid, ExpectedDate, PaymentDate);
        
        AddDomainEvent(new PaymentUpdated(Id, RenterTenantId, AmountPaid));
    }

    public void AttachReceipt(Guid receiptId)
    {
        ReceiptId = receiptId;
    }

    public void MarkAsVoided()
    {
        Status = PaymentStatus.Voided;
        AddDomainEvent(new PaymentVoided(Id, RenterTenantId));
    }

    private static PaymentStatus DetermineStatus(
        decimal amountDue,
        decimal amountPaid,
        DateTime expectedDate,
        DateTime? paymentDate)
    {
        // Si aucun paiement n'a été effectué
        if (amountPaid == 0 || !paymentDate.HasValue)
        {
            // Si on est après la date prévue
            if (DateTime.UtcNow.Date > expectedDate.Date)
                return PaymentStatus.Late;
            
            return PaymentStatus.Pending;
        }

        // Si paiement complet
        if (amountPaid >= amountDue)
        {
            // Si payé en retard
            if (paymentDate.Value.Date > expectedDate.Date)
                return PaymentStatus.PaidLate;
            
            return PaymentStatus.Paid;
        }

        // Si paiement partiel
        return PaymentStatus.Partial;
    }

    public bool IsLate()
    {
        return Status == PaymentStatus.Late || Status == PaymentStatus.PaidLate;
    }

    public bool IsPaid()
    {
        return Status == PaymentStatus.Paid || Status == PaymentStatus.PaidLate;
    }

    public decimal GetRemainingAmount()
    {
        return Math.Max(0, AmountDue - AmountPaid);
    }
}

public enum PaymentStatus
{
    Pending,      // En attente de paiement
    Paid,         // Payé à temps
    PaidLate,     // Payé en retard
    Partial,      // Paiement partiel
    Late,         // En retard (non payé)
    Voided        // Annulé
}

public enum PaymentMethod
{
    Cash,         // Espèces
    BankTransfer, // Virement bancaire
    Check,        // Chèque
    Other         // Autre
}
