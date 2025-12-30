namespace LocaGuest.Application.DTOs.Payments;

public class PaymentDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid ContractId { get; set; }

    public string PaymentType { get; set; } = "Rent";
    
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RemainingAmount { get; set; }
    
    public DateTime? PaymentDate { get; set; }
    public DateTime ExpectedDate { get; set; }
    
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? Note { get; set; }
    
    public int Month { get; set; }
    public int Year { get; set; }
    
    public Guid? ReceiptId { get; set; }
    public Guid? InvoiceDocumentId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties (optional enrichment)
    public string? TenantName { get; set; }
    public string? PropertyName { get; set; }
    
    /// <summary>
    /// Jour limite de paiement du contrat (1-31)
    /// </summary>
    public int PaymentDueDay { get; set; }
    
    /// <summary>
    /// Nombre de jours de retard (si applicable)
    /// Positif = en retard, Négatif = en avance/à venir
    /// </summary>
    public int? DaysLate { get; set; }
    
    /// <summary>
    /// Date limite réelle de paiement calculée (ExpectedDate + PaymentDueDay)
    /// </summary>
    public DateTime? DueDate { get; set; }
}

public class CreatePaymentDto
{
    public Guid TenantId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid ContractId { get; set; }

    public string PaymentType { get; set; } = "Rent";
    
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    
    public DateTime? PaymentDate { get; set; }
    public DateTime ExpectedDate { get; set; }
    
    public string PaymentMethod { get; set; } = "BankTransfer";
    public string? Note { get; set; }
}

public class UpdatePaymentDto
{
    public Guid Id { get; set; }
    public decimal AmountPaid { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Note { get; set; }
}

public class PaymentStatsDto
{
    public decimal TotalExpected { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalRemaining { get; set; }
    public int CountPaid { get; set; }
    public int CountLate { get; set; }
    public int CountPending { get; set; }
    public int CountPartial { get; set; }
}
