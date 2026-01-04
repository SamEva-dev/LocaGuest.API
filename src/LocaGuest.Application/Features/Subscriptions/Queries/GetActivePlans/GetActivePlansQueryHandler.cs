using LocaGuest.Application.Common;
using LocaGuest.Domain.Aggregates.SubscriptionAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Subscriptions.Queries.GetActivePlans;

public class GetActivePlansQueryHandler : IRequestHandler<GetActivePlansQuery, Result<List<Plan>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetActivePlansQueryHandler> _logger;

    public GetActivePlansQueryHandler(IUnitOfWork unitOfWork, ILogger<GetActivePlansQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<List<Plan>>> Handle(GetActivePlansQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var plans = await _unitOfWork.Plans.GetActiveAsync(cancellationToken);
            return Result.Success(plans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active plans");
            return Result.Failure<List<Plan>>($"Error retrieving plans: {ex.Message}");
        }
    }
}
