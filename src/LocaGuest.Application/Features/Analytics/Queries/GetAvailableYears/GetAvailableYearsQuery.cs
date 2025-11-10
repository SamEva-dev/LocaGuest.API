using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Analytics.Queries.GetAvailableYears;

public class GetAvailableYearsQuery : IRequest<Result<List<int>>>
{
}
