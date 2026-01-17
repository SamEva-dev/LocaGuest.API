using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Payments;
using MediatR;

namespace LocaGuest.Application.Features.Payments.Queries.GetOverduePayments;

public record GetOverduePaymentsQuery : IRequest<Result<List<PaymentDto>>>
{
    /// <summary>
    /// Filtre optionnel par Property
    /// </summary>
    public Guid? PropertyId { get; init; }
    
    /// <summary>
    /// Filtre optionnel par Tenant
    /// </summary>
    public Guid? OccupantId { get; init; }
    
    /// <summary>
    /// Nombre maximum de jours de retard à inclure (ex: 90)
    /// </summary>
    public int? MaxDaysLate { get; init; }
}
