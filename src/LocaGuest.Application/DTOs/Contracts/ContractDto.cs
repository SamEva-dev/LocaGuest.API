namespace LocaGuest.Application.DTOs.Contracts;

public class ContractDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;  // ✅ T0001-CTR0001
    public Guid PropertyId { get; set; }
    public Guid TenantId { get; set; }
    public string? PropertyName { get; set; }
    public string? TenantName { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Rent { get; set; }
    public decimal? Deposit { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public int PaymentsCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateContractDto
{
    public Guid PropertyId { get; set; }
    public Guid TenantId { get; set; }
    public string Type { get; set; } = "Unfurnished";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal Rent { get; set; }
    public decimal? Deposit { get; set; }
    public string? Notes { get; set; }
}

public class PaymentDto
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? PropertyName { get; set; }
    public string? TenantName { get; set; }
}
