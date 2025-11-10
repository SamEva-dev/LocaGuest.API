using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Rentability.Commands.DeleteScenario;

public record DeleteRentabilityScenarioCommand(Guid Id) : IRequest<Result<bool>>;
