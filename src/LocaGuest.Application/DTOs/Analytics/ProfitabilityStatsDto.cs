namespace LocaGuest.Application.DTOs.Analytics;

public class ProfitabilityStatsDto
{
    public decimal MonthlyRevenue { get; set; }
    public decimal MonthlyExpenses { get; set; }
    public decimal NetProfit { get; set; }
    public decimal ProfitabilityRate { get; set; }
    public decimal RevenueChangePercent { get; set; }
    public decimal ExpensesChangePercent { get; set; }
    public decimal ProfitChangePercent { get; set; }
    public decimal TargetRate { get; set; } = 8.5m;
}

public class RevenueEvolutionDto
{
    public string Month { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Expenses { get; set; }
}

public class PropertyPerformanceDto
{
    public Guid PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public decimal Expenses { get; set; }
    public decimal NetProfit { get; set; }
    public decimal ROI { get; set; }
}

public class ExpenseBreakdownDto
{
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
}

public class UpcomingPaymentDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty; // "Rent" or "Expense"
    public string Description { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class OccupancyDataPointDto
{
    public DateTime Date { get; set; }
    public decimal OccupancyRate { get; set; }
}
