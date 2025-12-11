using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Billing.Commands.UpdatePlanPriceIds;

public class UpdatePlanPriceIdsCommandHandler : IRequestHandler<UpdatePlanPriceIdsCommand, Result<bool>>
{
    private readonly ILocaGuestDbContext _context;
    private readonly ILogger<UpdatePlanPriceIdsCommandHandler> _logger;

    public UpdatePlanPriceIdsCommandHandler(
        ILocaGuestDbContext context,
        ILogger<UpdatePlanPriceIdsCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(UpdatePlanPriceIdsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var plan = await _context.Plans
                .FirstOrDefaultAsync(p => p.Code == request.PlanCode, cancellationToken);

            if (plan == null)
                return Result.Failure<bool>($"Plan with code '{request.PlanCode}' not found");

            plan.SetStripePriceIds(request.MonthlyPriceId, request.AnnualPriceId);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Updated Stripe Price IDs for plan {PlanCode}: Monthly={MonthlyPriceId}, Annual={AnnualPriceId}",
                request.PlanCode, request.MonthlyPriceId, request.AnnualPriceId);

            return Result.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating plan price IDs for {PlanCode}", request.PlanCode);
            return Result.Failure<bool>("Failed to update plan price IDs");
        }
    }
}
