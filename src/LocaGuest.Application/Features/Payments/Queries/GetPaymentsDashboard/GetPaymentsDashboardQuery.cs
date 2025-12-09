using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Payments.Queries.GetPaymentsDashboard;

public record GetPaymentsDashboardQuery : IRequest<Result<PaymentsDashboardDto>>
{
    /// <summary>
    /// Mois pour les statistiques (default: mois actuel)
    /// </summary>
    public int? Month { get; init; }
    
    /// <summary>
    /// Année pour les statistiques (default: année actuelle)
    /// </summary>
    public int? Year { get; init; }
}

public record PaymentsDashboardDto
{
    /// <summary>
    /// Total revenus pour la période
    /// </summary>
    public decimal TotalRevenue { get; init; }
    
    /// <summary>
    /// Total attendu pour la période
    /// </summary>
    public decimal TotalExpected { get; init; }
    
    /// <summary>
    /// Montant total en retard
    /// </summary>
    public decimal TotalOverdue { get; init; }
    
    /// <summary>
    /// Nombre de paiements en retard
    /// </summary>
    public int OverdueCount { get; init; }
    
    /// <summary>
    /// Taux de collection (%)
    /// </summary>
    public decimal CollectionRate { get; init; }
    
    /// <summary>
    /// Nombre total de paiements pour la période
    /// </summary>
    public int TotalPayments { get; init; }
    
    /// <summary>
    /// Nombre de paiements payés
    /// </summary>
    public int PaidCount { get; init; }
    
    /// <summary>
    /// Nombre de paiements en attente
    /// </summary>
    public int PendingCount { get; init; }
    
    /// <summary>
    /// Prochains paiements (7 prochains jours)
    /// </summary>
    public List<UpcomingPaymentDto> UpcomingPayments { get; init; } = new();
    
    /// <summary>
    /// Top 5 paiements en retard
    /// </summary>
    public List<OverduePaymentSummaryDto> TopOverduePayments { get; init; } = new();
}

public record UpcomingPaymentDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public Guid PropertyId { get; init; }
    public string PropertyName { get; init; } = string.Empty;
    public decimal AmountDue { get; init; }
    public DateTime ExpectedDate { get; init; }
    public int DaysUntilDue { get; init; }
}

public record OverduePaymentSummaryDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public Guid PropertyId { get; init; }
    public string PropertyName { get; init; } = string.Empty;
    public decimal AmountDue { get; init; }
    public decimal AmountPaid { get; init; }
    public decimal RemainingAmount { get; init; }
    public DateTime ExpectedDate { get; init; }
    public int DaysLate { get; init; }
}
