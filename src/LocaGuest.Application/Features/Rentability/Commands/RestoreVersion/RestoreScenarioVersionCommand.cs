using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Rentability;
using MediatR;

namespace LocaGuest.Application.Features.Rentability.Commands.RestoreVersion;

public record RestoreScenarioVersionCommand(Guid ScenarioId, Guid VersionId) : IRequest<Result<RentabilityScenarioDto>>;
