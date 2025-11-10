using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Rentability;
using MediatR;

namespace LocaGuest.Application.Features.Rentability.Queries.GetScenarioVersions;

public record GetScenarioVersionsQuery(Guid ScenarioId) : IRequest<Result<List<ScenarioVersionDto>>>;
