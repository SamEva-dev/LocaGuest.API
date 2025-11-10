using LocaGuest.Application.Features.Rentability.Commands.SaveScenario;
using LocaGuest.Application.Features.Rentability.Commands.DeleteScenario;
using LocaGuest.Application.Features.Rentability.Commands.CloneScenario;
using LocaGuest.Application.Features.Rentability.Queries.GetUserScenarios;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaGuest.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class RentabilityScenariosController : ControllerBase
{
    private readonly IMediator _mediator;

    public RentabilityScenariosController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetUserScenarios()
    {
        var result = await _mediator.Send(new GetUserScenariosQuery());
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.ErrorMessage);
    }

    [HttpPost]
    public async Task<IActionResult> SaveScenario([FromBody] SaveRentabilityScenarioCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.ErrorMessage);
    }

    [HttpPost("{id}/clone")]
    public async Task<IActionResult> CloneScenario(Guid id, [FromBody] string newName)
    {
        var result = await _mediator.Send(new CloneRentabilityScenarioCommand(id, newName));
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.ErrorMessage);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteScenario(Guid id)
    {
        var result = await _mediator.Send(new DeleteRentabilityScenarioCommand(id));
        return result.IsSuccess ? NoContent() : BadRequest(result.ErrorMessage);
    }
}
