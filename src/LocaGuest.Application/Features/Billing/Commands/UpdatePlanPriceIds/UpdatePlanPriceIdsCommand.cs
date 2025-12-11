using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Billing.Commands.UpdatePlanPriceIds;

public record UpdatePlanPriceIdsCommand(
    string PlanCode,
    string? MonthlyPriceId,
    string? AnnualPriceId
) : IRequest<Result<bool>>;
