using LocaGuest.Application.Features.Satisfaction.Commands.SubmitSatisfactionSurvey;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaGuest.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SatisfactionSurveyController : ControllerBase
{
    private readonly IMediator _mediator;

    public SatisfactionSurveyController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitSatisfactionSurveyRequest request)
    {
        var cmd = new SubmitSatisfactionSurveyCommand(request.Rating, request.Comment);
        var result = await _mediator.Send(cmd);

        if (!result.IsSuccess)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(new { success = true, responseId = result.Data.ResponseId });
    }
}

public record SubmitSatisfactionSurveyRequest(
    int Rating,
    string? Comment
);
