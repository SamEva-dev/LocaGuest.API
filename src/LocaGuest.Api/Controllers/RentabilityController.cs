using LocaGuest.Application.DTOs.Rentability;
using LocaGuest.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocaGuest.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/rentability")]
public sealed class RentabilityController : ControllerBase
{
    private readonly IRentabilityEngine _engine;

    public RentabilityController(IRentabilityEngine engine)
    {
        _engine = engine;
    }

    [HttpPost("compute")]
    public ActionResult<ComputeRentabilityResponse> Compute([FromBody] ComputeRentabilityRequest req)
    {
        var output = _engine.Compute(req.Inputs, req.ClientCalcVersion);

        return Ok(new ComputeRentabilityResponse(
            Results: output.Result,
            Warnings: output.Warnings,
            CalculationVersion: output.CalculationVersion,
            InputsHash: output.InputsHash,
            IsCertified: true
        ));
    }
}
