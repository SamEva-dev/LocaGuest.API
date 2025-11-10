using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Rentability;
using MediatR;

namespace LocaGuest.Application.Features.Rentability.Commands.SaveScenario;

public record SaveRentabilityScenarioCommand : IRequest<Result<RentabilityScenarioDto>>
{
    public Guid? Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsBase { get; init; }
    public RentabilityInputDto Input { get; init; } = new();
    public string? ResultsJson { get; init; }
}
