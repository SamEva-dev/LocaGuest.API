namespace LocaGuest.Application.DTOs.Deposits;

public class DepositDto
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }

    public decimal AmountExpected { get; set; }
    public DateTime DueDate { get; set; }
    public bool AllowInstallments { get; set; }

    public string Status { get; set; } = string.Empty;

    public decimal TotalReceived { get; set; }
    public decimal TotalRefunded { get; set; }
    public decimal TotalDeducted { get; set; }
    public decimal BalanceHeld { get; set; }
    public decimal Outstanding { get; set; }

    public List<DepositTransactionDto> Transactions { get; set; } = new();
}

public class DepositTransactionDto
{
    public Guid Id { get; set; }
    public string Kind { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime DateUtc { get; set; }
    public string? Reference { get; set; }
}
