using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Properties.Commands.DissociateTenant;

public record DissociateTenantCommand : IRequest<Result<bool>>
{
    public required string PropertyId { get; init; }
    public required string TenantId { get; init; }
}
