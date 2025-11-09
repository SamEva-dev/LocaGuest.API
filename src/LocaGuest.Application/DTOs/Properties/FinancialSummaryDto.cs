namespace LocaGuest.Application.DTOs.Properties;

public class FinancialSummaryDto
{
    public Guid PropertyId { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal? LastPaymentAmount { get; set; }
    public DateTime? LastPaymentDate { get; set; }
    public decimal OccupancyRate { get; set; }
    public int TotalPayments { get; set; }
    public int ActiveContracts { get; set; }
}
