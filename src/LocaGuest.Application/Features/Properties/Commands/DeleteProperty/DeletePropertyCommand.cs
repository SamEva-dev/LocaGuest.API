using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Properties.Commands.DeleteProperty;

public record DeletePropertyCommand : IRequest<Result<DeletePropertyResult>>
{
    public Guid PropertyId { get; init; }
}

public record DeletePropertyResult
{
    public Guid Id { get; init; }
    public int DeletedContracts { get; init; }
    public int DeletedPayments { get; init; }
    public int DeletedDocuments { get; init; }
    public int DissociatedTenants { get; init; }
}
