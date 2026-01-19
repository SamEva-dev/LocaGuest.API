using FluentValidation;

namespace LocaGuest.Application.Features.Satisfaction.Commands.SubmitSatisfactionSurvey;

public class SubmitSatisfactionSurveyCommandValidator : AbstractValidator<SubmitSatisfactionSurveyCommand>
{
    public SubmitSatisfactionSurveyCommandValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5);

        RuleFor(x => x.Comment)
            .MaximumLength(2000);
    }
}
