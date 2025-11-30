using LocaGuest.Application.Common;
using MediatR;

namespace LocaGuest.Application.Features.Contracts.Commands.UpdateContract;

public record UpdateContractCommand : IRequest<Result>
{
    public required Guid ContractId { get; init; }
    public required Guid TenantId { get; init; }
    public required Guid PropertyId { get; init; }
    public Guid? RoomId { get; init; }
    public required string Type { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
    public required decimal Rent { get; init; }
    public decimal? Charges { get; init; }
    public decimal? Deposit { get; init; }
}
