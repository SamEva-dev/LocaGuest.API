using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Dashboard.Queries.GetAvailableYears;

public record GetAvailableYearsQuery : IRequest<Result<AvailableYearsDto>>
{
}

public record AvailableYearsDto
{
    public List<int> Years { get; init; } = new();
}
