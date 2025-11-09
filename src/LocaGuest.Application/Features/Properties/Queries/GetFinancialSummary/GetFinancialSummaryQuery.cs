using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Properties;
using MediatR;

namespace LocaGuest.Application.Features.Properties.Queries.GetFinancialSummary;

public record GetFinancialSummaryQuery : IRequest<Result<FinancialSummaryDto>>
{
    public required string PropertyId { get; init; }
}
