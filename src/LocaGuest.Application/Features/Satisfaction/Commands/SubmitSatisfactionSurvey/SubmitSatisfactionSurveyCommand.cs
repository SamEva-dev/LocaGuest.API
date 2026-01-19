using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Satisfaction.Commands.SubmitSatisfactionSurvey;

public record SubmitSatisfactionSurveyCommand(
    int Rating,
    string? Comment
) : IRequest<Result<SubmitSatisfactionSurveyResult>>;

public record SubmitSatisfactionSurveyResult(
    bool Success,
    string ResponseId
);
