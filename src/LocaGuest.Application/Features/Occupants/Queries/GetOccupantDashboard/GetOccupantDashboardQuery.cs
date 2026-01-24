using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Occupants.Queries.GetOccupantDashboard;

public record GetOccupantDashboardQuery(Guid OccupantId) : IRequest<Result<OccupantDashboardDto>>;

public record OccupantDashboardDto
{
    public required OccupantDashboardOccupantDto Occupant { get; init; }

    public List<OccupantDashboardContractDto> Contracts { get; init; } = new();

    public OccupantDashboardPaymentStatsDto PaymentStats { get; init; } = new();

    public OccupantDashboardDocumentsDto Documents { get; init; } = new();
}

public record OccupantDashboardOccupantDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public string Status { get; init; } = string.Empty;

    public Guid? PropertyId { get; init; }
    public string? PropertyCode { get; init; }
}

public record OccupantDashboardContractDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;

    public Guid PropertyId { get; init; }

    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }

    public decimal Rent { get; init; }
    public decimal Deposit { get; init; }

    public decimal? Charges { get; init; }
}

public record OccupantDashboardPaymentStatsDto
{
    public decimal TotalPaid { get; init; }
    public int TotalPayments { get; init; }
    public int LatePayments { get; init; }
    public decimal OnTimeRate { get; init; }
}

public record OccupantDashboardDocumentsDto
{
    public int TotalCount { get; init; }
    public int ActiveCount { get; init; }

    public List<OccupantDashboardDocumentDto> Recent { get; init; } = new();
}

public record OccupantDashboardDocumentDto
{
    public Guid Id { get; init; }
    public string Type { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
