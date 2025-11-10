using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Rentability;
using MediatR;

namespace LocaGuest.Application.Features.Rentability.Queries.GetUserScenarios;

public record GetUserScenariosQuery : IRequest<Result<List<RentabilityScenarioDto>>>;
