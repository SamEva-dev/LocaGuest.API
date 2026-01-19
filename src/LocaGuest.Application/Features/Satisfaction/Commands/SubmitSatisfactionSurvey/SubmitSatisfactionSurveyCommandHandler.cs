using LocaGuest.Application.Common;
using LocaGuest.Application.Common.Interfaces;
using LocaGuest.Application.Services;
using LocaGuest.Domain.Aggregates.AnalyticsAggregate;
using LocaGuest.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LocaGuest.Application.Features.Satisfaction.Commands.SubmitSatisfactionSurvey;

public class SubmitSatisfactionSurveyCommandHandler : IRequestHandler<SubmitSatisfactionSurveyCommand, Result<SubmitSatisfactionSurveyResult>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrganizationContext _orgContext;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<SubmitSatisfactionSurveyCommandHandler> _logger;

    public SubmitSatisfactionSurveyCommandHandler(
        IUnitOfWork unitOfWork,
        IOrganizationContext orgContext,
        ICurrentUserService currentUser,
        ILogger<SubmitSatisfactionSurveyCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _orgContext = orgContext;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Result<SubmitSatisfactionSurveyResult>> Handle(SubmitSatisfactionSurveyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.IsAuthenticated)
            {
                return Result.Failure<SubmitSatisfactionSurveyResult>("User not authenticated");
            }

            var orgId = _orgContext.OrganizationId;
            var userId = _currentUser.UserId;

            var entity = SatisfactionFeedback.Create(
                request.Rating,
                request.Comment,
                organizationId: orgId,
                userId: userId);

            await _unitOfWork.SatisfactionFeedbacks.AddAsync(entity, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return Result.Success(new SubmitSatisfactionSurveyResult(true, entity.Id.ToString()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting satisfaction survey");
            return Result.Failure<SubmitSatisfactionSurveyResult>("Error submitting satisfaction survey");
        }
    }
}
