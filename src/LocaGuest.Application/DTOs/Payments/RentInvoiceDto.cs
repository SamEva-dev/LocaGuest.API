namespace LocaGuest.Application.DTOs.Payments;

public class RentInvoiceDto
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public Guid TenantId { get; set; }
    public Guid PropertyId { get; set; }
    
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal Amount { get; set; }
    
    public string Status { get; set; } = string.Empty;
    public Guid? PaymentId { get; set; }
    
    public DateTime GeneratedAt { get; set; }
    public DateTime DueDate { get; set; }
    public bool IsOverdue { get; set; }
    
    // Navigation properties
    public string? TenantName { get; set; }
    public string? PropertyName { get; set; }
}
