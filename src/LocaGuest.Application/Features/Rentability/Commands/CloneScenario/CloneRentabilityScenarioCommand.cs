using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Rentability;
using MediatR;

namespace LocaGuest.Application.Features.Rentability.Commands.CloneScenario;

public record CloneRentabilityScenarioCommand(Guid SourceId, string NewName) : IRequest<Result<RentabilityScenarioDto>>;
