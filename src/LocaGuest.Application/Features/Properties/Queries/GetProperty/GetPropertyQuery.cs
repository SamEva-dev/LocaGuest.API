using LocaGuest.Application.Common;
using LocaGuest.Application.DTOs.Properties;
using MediatR;

namespace LocaGuest.Application.Features.Properties.Queries.GetProperty;

public record GetPropertyQuery : IRequest<Result<PropertyDetailReadDto>>
{
    public required string Id { get; init; }
}
